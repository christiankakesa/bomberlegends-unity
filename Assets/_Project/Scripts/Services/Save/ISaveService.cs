using System;
using System.Threading;
using BomberLegends.Services.Settings;
using UnityEngine;

namespace BomberLegends.Services.Save
{
    /// <summary>
    /// Everything that survives between sessions.
    /// </summary>
    /// <remarks>
    /// <see cref="SchemaVersion"/> is present from the very first release. Adding a version field
    /// later means shipping a build whose saves cannot be migrated, which permanently orphans the
    /// progress of everyone who played before it.
    /// </remarks>
    [Serializable]
    public sealed class PlayerSaveData
    {
        /// <summary>The schema version this project currently writes.</summary>
        public const int CurrentSchemaVersion = 1;

        [SerializeField] private int _schemaVersion = CurrentSchemaVersion;
        [SerializeField] private long _dataCoins;
        [SerializeField] private int _bombRangeLevel;
        [SerializeField] private SettingsData _settings = SettingsData.Default;

        [SerializeField] private bool _hasRunInProgress;
        [SerializeField] private int _runSeed;
        [SerializeField] private int _runArenaIndex;
        [SerializeField] private int _runHealth;
        [SerializeField] private int[] _runItems = Array.Empty<int>();
        [SerializeField] private int _runOfferState;

        /// <summary>The schema version this payload was written with.</summary>
        public int SchemaVersion
        {
            get => _schemaVersion;
            set => _schemaVersion = value;
        }

        /// <summary>Soft currency earned in matches.</summary>
        public long DataCoins
        {
            get => _dataCoins;
            set => _dataCoins = value;
        }

        /// <summary>Purchased tier of the bomb range upgrade track.</summary>
        public int BombRangeLevel
        {
            get => _bombRangeLevel;
            set => _bombRangeLevel = value;
        }

        /// <summary>Persisted player options.</summary>
        public SettingsData Settings
        {
            get => _settings;
            set => _settings = value;
        }

        /// <summary>
        /// Whether a run was left unfinished and should be offered back.
        /// </summary>
        /// <remarks>
        /// A run is minutes of a player's attention. Losing one to a closed tab or a backgrounded
        /// phone is the surest way to not get a second one, which is exactly the thing the slice
        /// measures.
        /// </remarks>
        public bool HasRunInProgress
        {
            get => _hasRunInProgress;
            set => _hasRunInProgress = value;
        }

        /// <summary>The seed the unfinished run was rolled from.</summary>
        /// <remarks>
        /// Stored signed because <c>JsonUtility</c> does not serialise unsigned integers. The bits
        /// are preserved either way; only the reading of them changes.
        /// </remarks>
        public int RunSeed
        {
            get => _runSeed;
            set => _runSeed = value;
        }

        /// <summary>How far through the unfinished run the player was.</summary>
        public int RunArenaIndex
        {
            get => _runArenaIndex;
            set => _runArenaIndex = value;
        }

        /// <summary>Health carried into that arena.</summary>
        public int RunHealth
        {
            get => _runHealth;
            set => _runHealth = value;
        }

        /// <summary>Items held, in the order they were taken.</summary>
        public int[] RunItems
        {
            get => _runItems ??= Array.Empty<int>();
            set => _runItems = value ?? Array.Empty<int>();
        }

        /// <summary>
        /// Where the run's offer generator had reached.
        /// </summary>
        /// <remarks>
        /// Signed for the same reason as the seed: <c>JsonUtility</c> has no unsigned integers, and
        /// only the bits matter.
        /// </remarks>
        public int RunOfferState
        {
            get => _runOfferState;
            set => _runOfferState = value;
        }

        /// <summary>Creates the save a brand new player starts with.</summary>
        public static PlayerSaveData CreateNew() => new PlayerSaveData
        {
            SchemaVersion = CurrentSchemaVersion,
            DataCoins = 0,
            BombRangeLevel = 0,
            Settings = SettingsData.Default,
            HasRunInProgress = false,
            RunItems = Array.Empty<int>()
        };
    }

    /// <summary>
    /// Owns the player's persisted data and decides when it reaches storage.
    /// </summary>
    /// <remarks>
    /// Gameplay and meta code read and write <see cref="Data"/> and call <see cref="MarkDirty"/>;
    /// they never learn whether that ends up in a file, in player preferences, or on a server. That
    /// separation is what allows the move to server-authoritative saves without touching a feature.
    /// Writes are batched, and a flush is forced when the application is backgrounded — on Android
    /// that callback is frequently the last one the process receives.
    /// </remarks>
    public interface ISaveService
    {
        /// <summary>The loaded save. Valid only after <see cref="LoadAsync"/> has completed.</summary>
        PlayerSaveData Data { get; }

        /// <summary>Whether changes are waiting to be written.</summary>
        bool IsDirty { get; }

        /// <summary>
        /// Loads the save from storage, migrating older schema versions forward. A missing or
        /// unreadable save yields a fresh one rather than throwing.
        /// </summary>
        Awaitable LoadAsync(CancellationToken cancellationToken = default);

        /// <summary>Writes the save to storage immediately, whether or not it is dirty.</summary>
        Awaitable SaveAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Writes the save synchronously, blocking the calling thread until it is stored.
        /// </summary>
        /// <remarks>
        /// This is the one path where blocking the main thread is correct. It is called when the
        /// application is being backgrounded or shut down, where an awaited write frequently does
        /// not survive: Android routinely kills the process before a background write completes.
        /// Everywhere else, use <see cref="SaveAsync"/>.
        /// </remarks>
        void FlushImmediate();

        /// <summary>
        /// Records that the save has changed. The write itself is batched, so callers can mark
        /// freely without causing a write per change.
        /// </summary>
        void MarkDirty();
    }
}
