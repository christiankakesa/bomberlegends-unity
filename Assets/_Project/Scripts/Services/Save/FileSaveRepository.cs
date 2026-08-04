using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BomberLegends.Services.Save
{
    /// <summary>
    /// Stores the save as a file, with an atomic write and one generation of backup.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A write proceeds in three steps, each of which leaves a readable payload on disk if the
    /// process dies partway:
    /// </para>
    /// <list type="number">
    /// <item>Write the new payload to a temporary file and flush it to the physical disk.</item>
    /// <item>Move the current save aside to the backup name.</item>
    /// <item>Move the temporary file into place as the current save.</item>
    /// </list>
    /// <para>
    /// Interrupted during step 1 the current save is untouched; interrupted between steps 2 and 3
    /// the backup holds the previous payload and <see cref="ReadCandidates"/> returns it. Only
    /// <c>Move</c> and <c>Delete</c> are used, so this does not depend on
    /// <see cref="File.Replace(string,string,string)"/> behaving identically across platforms.
    /// </para>
    /// </remarks>
    public sealed class FileSaveRepository : ISaveRepository
    {
        private readonly string _directory;
        private readonly string _primaryPath;
        private readonly string _temporaryPath;
        private readonly string _backupPath;

        /// <summary>Creates a repository writing into <paramref name="directory"/>.</summary>
        /// <exception cref="ArgumentException">The directory or file name is empty.</exception>
        public FileSaveRepository(string directory, string fileName = "player.save")
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentException("Save directory must not be empty.", nameof(directory));
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("Save file name must not be empty.", nameof(fileName));
            }

            _directory = directory;
            _primaryPath = Path.Combine(directory, fileName);
            _temporaryPath = _primaryPath + ".tmp";
            _backupPath = _primaryPath + ".bak";
        }

        /// <summary>The file the current save is written to.</summary>
        public string PrimaryPath => _primaryPath;

        /// <summary>The file holding the previous save.</summary>
        public string BackupPath => _backupPath;

        /// <inheritdoc />
        public bool Exists => File.Exists(_primaryPath) || File.Exists(_backupPath);

        /// <inheritdoc />
        public bool SupportsBackgroundIo => true;

        /// <inheritdoc />
        public IReadOnlyList<string> ReadCandidates()
        {
            var candidates = new List<string>(2);
            AddIfReadable(_primaryPath, candidates);
            AddIfReadable(_backupPath, candidates);
            return candidates;
        }

        /// <inheritdoc />
        public void Write(string payload)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            Directory.CreateDirectory(_directory);

            WriteThrough(_temporaryPath, payload);

            if (File.Exists(_primaryPath))
            {
                if (File.Exists(_backupPath))
                {
                    File.Delete(_backupPath);
                }

                File.Move(_primaryPath, _backupPath);
            }

            File.Move(_temporaryPath, _primaryPath);
        }

        /// <inheritdoc />
        public string? Quarantine()
        {
            if (!File.Exists(_primaryPath))
            {
                return null;
            }

            var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var quarantinePath = $"{_primaryPath}.{stamp}.corrupt";

            if (File.Exists(quarantinePath))
            {
                File.Delete(quarantinePath);
            }

            File.Move(_primaryPath, quarantinePath);
            return quarantinePath;
        }

        /// <inheritdoc />
        public void Delete()
        {
            DeleteIfPresent(_primaryPath);
            DeleteIfPresent(_backupPath);
            DeleteIfPresent(_temporaryPath);
        }

        private static void AddIfReadable(string path, ICollection<string> candidates)
        {
            if (!File.Exists(path))
            {
                return;
            }

            try
            {
                candidates.Add(File.ReadAllText(path, Encoding.UTF8));
            }
            catch (IOException)
            {
                // An unreadable file is treated as absent so the next candidate gets its turn.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static void WriteThrough(string path, string payload)
        {
            var bytes = Encoding.UTF8.GetBytes(payload);

            using var stream = new FileStream(
                path,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.WriteThrough);

            stream.Write(bytes, 0, bytes.Length);

            // Force the bytes past the OS cache. Without this the move below can complete while the
            // new payload is still only in memory, which a power loss would discard.
            stream.Flush(flushToDisk: true);
        }

        private static void DeleteIfPresent(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
