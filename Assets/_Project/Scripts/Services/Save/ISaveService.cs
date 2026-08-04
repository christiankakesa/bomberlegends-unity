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

        /// <summary>Creates the save a brand new player starts with.</summary>
        public static PlayerSaveData CreateNew() => new PlayerSaveData
        {
            SchemaVersion = CurrentSchemaVersion,
            DataCoins = 0,
            BombRangeLevel = 0,
            Settings = SettingsData.Default
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
