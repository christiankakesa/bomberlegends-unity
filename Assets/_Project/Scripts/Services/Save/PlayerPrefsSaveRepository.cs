using System;
using System.Collections.Generic;
using UnityEngine;

namespace BomberLegends.Services.Save
{
    /// <summary>
    /// Stores the save in player preferences. Used on WebGL, where there is no dependable
    /// filesystem: <c>Application.persistentDataPath</c> maps to a browser-managed store that needs
    /// an explicit flush, and partial writes cannot be recovered from.
    /// </summary>
    /// <remarks>
    /// Player preferences must be touched from the main thread, so
    /// <see cref="SupportsBackgroundIo"/> is false and the save service keeps this work inline.
    /// One backup generation is kept under a second key, mirroring the file repository, so a failed
    /// parse still has somewhere to fall back to.
    /// </remarks>
    public sealed class PlayerPrefsSaveRepository : ISaveRepository
    {
        private readonly string _primaryKey;
        private readonly string _backupKey;

        /// <summary>Creates a repository storing under keys derived from <paramref name="key"/>.</summary>
        /// <exception cref="ArgumentException">The key is empty.</exception>
        public PlayerPrefsSaveRepository(string key = "bomberlegends.save")
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Preference key must not be empty.", nameof(key));
            }

            _primaryKey = key;
            _backupKey = key + ".bak";
        }

        /// <inheritdoc />
        public bool Exists => PlayerPrefs.HasKey(_primaryKey) || PlayerPrefs.HasKey(_backupKey);

        /// <inheritdoc />
        public bool SupportsBackgroundIo => false;

        /// <inheritdoc />
        public IReadOnlyList<string> ReadCandidates()
        {
            var candidates = new List<string>(2);
            AddIfPresent(_primaryKey, candidates);
            AddIfPresent(_backupKey, candidates);
            return candidates;
        }

        /// <inheritdoc />
        public void Write(string payload)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            if (PlayerPrefs.HasKey(_primaryKey))
            {
                PlayerPrefs.SetString(_backupKey, PlayerPrefs.GetString(_primaryKey));
            }

            PlayerPrefs.SetString(_primaryKey, payload);
            PlayerPrefs.Save();
        }

        /// <inheritdoc />
        public string? Quarantine()
        {
            if (!PlayerPrefs.HasKey(_primaryKey))
            {
                return null;
            }

            var quarantineKey = $"{_primaryKey}.{DateTime.UtcNow:yyyyMMdd-HHmmss}.corrupt";
            PlayerPrefs.SetString(quarantineKey, PlayerPrefs.GetString(_primaryKey));
            PlayerPrefs.DeleteKey(_primaryKey);
            PlayerPrefs.Save();
            return quarantineKey;
        }

        /// <inheritdoc />
        public void Delete()
        {
            PlayerPrefs.DeleteKey(_primaryKey);
            PlayerPrefs.DeleteKey(_backupKey);
            PlayerPrefs.Save();
        }

        private static void AddIfPresent(string key, ICollection<string> candidates)
        {
            if (PlayerPrefs.HasKey(key))
            {
                candidates.Add(PlayerPrefs.GetString(key));
            }
        }
    }
}
