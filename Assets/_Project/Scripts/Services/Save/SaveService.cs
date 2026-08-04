using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace BomberLegends.Services.Save
{
    /// <summary>
    /// Owns the player's save: parses it, migrates it forward, and decides when it reaches storage.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Serialisation happens on the main thread because <see cref="JsonUtility"/> requires it; only
    /// the storage call is moved off-thread, and only when the repository allows it. On WebGL
    /// everything stays inline because the platform is single-threaded.
    /// </para>
    /// <para>
    /// A payload that cannot be parsed or migrated is moved aside rather than overwritten. Silently
    /// replacing a player's progress with a fresh save destroys the only copy and leaves nothing to
    /// diagnose.
    /// </para>
    /// </remarks>
    public sealed class SaveService : ISaveService
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        private const bool PlatformSupportsBackgroundIo = false;
#else
        private const bool PlatformSupportsBackgroundIo = true;
#endif

        private static readonly ISaveMigration[] NoMigrations = Array.Empty<ISaveMigration>();

        private readonly ISaveRepository _repository;
        private readonly IReadOnlyList<ISaveMigration> _migrations;
        private readonly int _targetSchemaVersion;

        /// <summary>Creates the service.</summary>
        /// <param name="repository">Where payloads are stored.</param>
        /// <param name="migrations">Upgrade steps, in any order. One per version transition.</param>
        /// <param name="targetSchemaVersion">
        /// The version this build writes. Defaults to <see cref="PlayerSaveData.CurrentSchemaVersion"/>.
        /// </param>
        public SaveService(
            ISaveRepository repository,
            IReadOnlyList<ISaveMigration>? migrations = null,
            int targetSchemaVersion = PlayerSaveData.CurrentSchemaVersion)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _migrations = migrations ?? NoMigrations;
            _targetSchemaVersion = targetSchemaVersion;
            Data = PlayerSaveData.CreateNew();
        }

        /// <inheritdoc />
        public PlayerSaveData Data { get; private set; }

        /// <inheritdoc />
        public bool IsDirty { get; private set; }

        /// <summary>Whether the last load recovered from a corrupt or unusable payload.</summary>
        public bool LastLoadRecovered { get; private set; }

        private bool UseBackgroundIo => PlatformSupportsBackgroundIo && _repository.SupportsBackgroundIo;

        /// <inheritdoc />
        public async Awaitable LoadAsync(CancellationToken cancellationToken = default)
        {
            LastLoadRecovered = false;

            var hadStoredPayload = SafeExists();
            IReadOnlyList<string> candidates;

            if (UseBackgroundIo)
            {
                await Awaitable.BackgroundThreadAsync();
                candidates = SafeReadCandidates();
                await Awaitable.MainThreadAsync();
            }
            else
            {
                candidates = SafeReadCandidates();
            }

            cancellationToken.ThrowIfCancellationRequested();

            for (var i = 0; i < candidates.Count; i++)
            {
                if (!TryPrepare(candidates[i], out var loaded))
                {
                    continue;
                }

                Data = loaded;
                IsDirty = false;
                LastLoadRecovered = i > 0;

                if (LastLoadRecovered)
                {
                    Debug.LogError(
                        "[Save] The current save could not be used; recovered from the backup instead.");
                }

                return;
            }

            if (hadStoredPayload)
            {
                var quarantinedAt = SafeQuarantine();
                Debug.LogError(
                    "[Save] No stored payload could be parsed or migrated. Starting a new save. " +
                    $"The unusable payload was kept at: {quarantinedAt ?? "(nothing to keep)"}");
                LastLoadRecovered = true;
            }

            Data = PlayerSaveData.CreateNew();
            IsDirty = false;
        }

        /// <inheritdoc />
        public async Awaitable SaveAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Serialised here because JsonUtility is main-thread only. Only the store call moves off.
            var payload = JsonUtility.ToJson(Data);

            if (UseBackgroundIo)
            {
                await Awaitable.BackgroundThreadAsync();
                var written = SafeWrite(payload);
                await Awaitable.MainThreadAsync();

                if (written)
                {
                    IsDirty = false;
                }

                return;
            }

            if (SafeWrite(payload))
            {
                IsDirty = false;
            }
        }

        /// <inheritdoc />
        public void FlushImmediate()
        {
            if (SafeWrite(JsonUtility.ToJson(Data)))
            {
                IsDirty = false;
            }
        }

        /// <inheritdoc />
        public void MarkDirty() => IsDirty = true;

        private bool TryPrepare(string payload, out PlayerSaveData data)
        {
            data = null!;

            if (string.IsNullOrWhiteSpace(payload))
            {
                return false;
            }

            PlayerSaveData? parsed;
            try
            {
                parsed = JsonUtility.FromJson<PlayerSaveData>(payload);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Save] Payload could not be parsed: {exception.Message}");
                return false;
            }

            if (parsed == null)
            {
                return false;
            }

            if (parsed.SchemaVersion > _targetSchemaVersion)
            {
                // Written by a newer build. Keeping it is the lesser harm: the fields this build
                // understands still load, whereas discarding it would delete real progress.
                Debug.LogWarning(
                    $"[Save] Payload is schema v{parsed.SchemaVersion}, newer than this build's " +
                    $"v{_targetSchemaVersion}. Loading it as-is; unknown fields are ignored.");
                data = parsed;
                return true;
            }

            while (parsed.SchemaVersion < _targetSchemaVersion)
            {
                var migration = FindMigration(parsed.SchemaVersion);
                if (migration == null)
                {
                    Debug.LogError(
                        $"[Save] No migration from schema v{parsed.SchemaVersion}. " +
                        "The payload cannot be brought forward.");
                    return false;
                }

                migration.Apply(parsed);

                // Stamped by the service rather than the migration, so a migration that forgets to
                // update the version cannot spin this loop forever.
                parsed.SchemaVersion = migration.FromVersion + 1;
            }

            data = parsed;
            return true;
        }

        private ISaveMigration? FindMigration(int fromVersion)
        {
            for (var i = 0; i < _migrations.Count; i++)
            {
                if (_migrations[i].FromVersion == fromVersion)
                {
                    return _migrations[i];
                }
            }

            return null;
        }

        private bool SafeExists()
        {
            try
            {
                return _repository.Exists;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Save] Could not query storage: {exception.Message}");
                return false;
            }
        }

        private IReadOnlyList<string> SafeReadCandidates()
        {
            try
            {
                return _repository.ReadCandidates();
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Save] Read failed: {exception.Message}");
                return Array.Empty<string>();
            }
        }

        private bool SafeWrite(string payload)
        {
            try
            {
                _repository.Write(payload);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Save] Write failed, progress kept in memory: {exception.Message}");
                return false;
            }
        }

        private string? SafeQuarantine()
        {
            try
            {
                return _repository.Quarantine();
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Save] Could not set the unusable payload aside: {exception.Message}");
                return null;
            }
        }
    }
}
