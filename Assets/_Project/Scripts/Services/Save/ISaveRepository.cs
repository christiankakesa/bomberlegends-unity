using System.Collections.Generic;

namespace BomberLegends.Services.Save
{
    /// <summary>
    /// Raw persistence for the save payload.
    /// </summary>
    /// <remarks>
    /// Deliberately knows nothing about the payload's structure: it stores and returns strings.
    /// Everything above this interface — schema versions, migrations, batching — belongs to
    /// <see cref="ISaveService"/>. That split is what allows the eventual move to a
    /// server-authoritative store to be a new implementation of this one small interface rather
    /// than a change to any feature.
    /// </remarks>
    public interface ISaveRepository
    {
        /// <summary>Whether any stored payload exists, including a recoverable backup.</summary>
        bool Exists { get; }

        /// <summary>
        /// Whether reads and writes may be performed off the main thread. False for backing stores
        /// with thread affinity, such as player preferences.
        /// </summary>
        bool SupportsBackgroundIo { get; }

        /// <summary>
        /// Returns every stored payload that might be usable, newest first.
        /// </summary>
        /// <remarks>
        /// More than one candidate is returned so the caller can fall back to the previous save if
        /// the newest one fails to parse. Returns an empty list when nothing is stored.
        /// </remarks>
        IReadOnlyList<string> ReadCandidates();

        /// <summary>
        /// Stores a payload, replacing what was there. Implementations must be atomic: an
        /// interrupted write must leave a previously stored payload readable.
        /// </summary>
        void Write(string payload);

        /// <summary>
        /// Moves the stored payload aside under a name that marks it unusable, and returns the
        /// location it was moved to for logging. Used when a payload cannot be parsed or migrated,
        /// so the evidence survives instead of being overwritten.
        /// </summary>
        string? Quarantine();

        /// <summary>Removes all stored payloads, including backups.</summary>
        void Delete();
    }
}
