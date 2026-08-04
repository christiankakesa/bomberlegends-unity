using System;
using System.Collections.Generic;

namespace BomberLegends.Services.Save
{
    /// <summary>
    /// Keeps the save in memory only. Used by tests and by editor workflows where persistence
    /// between sessions would get in the way.
    /// </summary>
    public sealed class MemorySaveRepository : ISaveRepository
    {
        private string? _primary;
        private string? _backup;
        private string? _quarantined;

        private readonly bool _supportsBackgroundIo;

        /// <summary>Creates an empty repository, or one preloaded with <paramref name="payload"/>.</summary>
        /// <param name="payload">An initial stored payload, or null for empty storage.</param>
        /// <param name="supportsBackgroundIo">
        /// Whether the save service may move reads and writes off the main thread. Tests set this to
        /// false to keep the whole operation synchronous, and to true to exercise the threaded path.
        /// </param>
        public MemorySaveRepository(string? payload = null, bool supportsBackgroundIo = true)
        {
            _primary = payload;
            _supportsBackgroundIo = supportsBackgroundIo;
        }

        /// <summary>How many times <see cref="Write"/> has been called.</summary>
        public int WriteCount { get; private set; }

        /// <summary>The payload most recently moved aside, or null if none was.</summary>
        public string? QuarantinedPayload => _quarantined;

        /// <inheritdoc />
        public bool Exists => _primary != null || _backup != null;

        /// <inheritdoc />
        public bool SupportsBackgroundIo => _supportsBackgroundIo;

        /// <inheritdoc />
        public IReadOnlyList<string> ReadCandidates()
        {
            var candidates = new List<string>(2);

            if (_primary != null)
            {
                candidates.Add(_primary);
            }

            if (_backup != null)
            {
                candidates.Add(_backup);
            }

            return candidates;
        }

        /// <inheritdoc />
        public void Write(string payload)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            _backup = _primary;
            _primary = payload;
            WriteCount++;
        }

        /// <inheritdoc />
        public string? Quarantine()
        {
            if (_primary == null)
            {
                return null;
            }

            _quarantined = _primary;
            _primary = null;
            return "memory://quarantined";
        }

        /// <inheritdoc />
        public void Delete()
        {
            _primary = null;
            _backup = null;
        }
    }
}
