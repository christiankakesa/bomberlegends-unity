using System;
using System.IO;
using System.Text;
using BomberLegends.Services.Save;
using NUnit.Framework;

namespace BomberLegends.Tests.EditMode.Services
{
    /// <summary>
    /// Covers the atomic write and the states a crash can leave on disk.
    /// </summary>
    /// <remarks>
    /// Rather than actually killing a process, these tests construct each intermediate on-disk state
    /// directly and assert the repository recovers from it. That covers the same failure windows and
    /// is deterministic, which a real kill would not be.
    /// </remarks>
    public sealed class FileSaveRepositoryTests
    {
        private string _directory = string.Empty;
        private FileSaveRepository _repository = null!;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "bomberlegends-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            _repository = new FileSaveRepository(_directory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }

        [Test]
        public void ReadCandidates_OnEmptyStorage_ReturnsNothing()
        {
            Assert.That(_repository.Exists, Is.False);
            Assert.That(_repository.ReadCandidates(), Is.Empty);
        }

        [Test]
        public void Write_ThenRead_RoundTrips()
        {
            _repository.Write("payload one");

            Assert.That(_repository.Exists, Is.True);
            Assert.That(_repository.ReadCandidates(), Is.EqualTo(new[] { "payload one" }));
        }

        [Test]
        public void Write_PreservesThePreviousPayloadAsBackup()
        {
            _repository.Write("first");
            _repository.Write("second");

            Assert.That(_repository.ReadCandidates(), Is.EqualTo(new[] { "second", "first" }),
                "the newest payload comes first, the previous one remains recoverable");
        }

        [Test]
        public void Write_LeavesNoTemporaryFileBehind()
        {
            _repository.Write("payload");

            Assert.That(File.Exists(_repository.PrimaryPath + ".tmp"), Is.False);
        }

        [Test]
        public void Write_HandlesUnicodeAndLargePayloads()
        {
            var payload = new StringBuilder();
            for (var i = 0; i < 5000; i++)
            {
                payload.Append("Ébène-Prime ").Append(i).Append(' ');
            }

            _repository.Write(payload.ToString());

            Assert.That(_repository.ReadCandidates()[0], Is.EqualTo(payload.ToString()));
        }

        [Test]
        public void Write_CreatesTheDirectoryIfItIsMissing()
        {
            var nested = Path.Combine(_directory, "nested", "deeper");
            var repository = new FileSaveRepository(nested);

            repository.Write("payload");

            Assert.That(repository.ReadCandidates(), Is.EqualTo(new[] { "payload" }));
        }

        [Test]
        public void CrashDuringTemporaryWrite_LeavesTheCurrentSaveIntact()
        {
            _repository.Write("good payload");

            // A process killed while writing the temporary file leaves it behind, half-written.
            File.WriteAllText(_repository.PrimaryPath + ".tmp", "half-written gar");

            Assert.That(_repository.ReadCandidates()[0], Is.EqualTo("good payload"),
                "a stray temporary file must never be offered as a candidate");
        }

        [Test]
        public void CrashBetweenMoves_LeavesThePreviousSaveRecoverable()
        {
            _repository.Write("first");
            _repository.Write("second");

            // The window after the current save is moved aside and before the new one is moved in.
            File.Delete(_repository.PrimaryPath);

            Assert.That(_repository.Exists, Is.True);
            Assert.That(_repository.ReadCandidates(), Is.EqualTo(new[] { "first" }),
                "the backup must still be readable when the primary is gone");
        }

        [Test]
        public void Quarantine_MovesThePayloadAsideAndKeepsIt()
        {
            _repository.Write("corrupt payload");

            var quarantinePath = _repository.Quarantine();

            Assert.That(quarantinePath, Is.Not.Null);
            Assert.That(File.Exists(quarantinePath!), Is.True);
            Assert.That(File.ReadAllText(quarantinePath!), Is.EqualTo("corrupt payload"));
            Assert.That(File.Exists(_repository.PrimaryPath), Is.False);
        }

        [Test]
        public void Quarantine_WithNothingStored_ReturnsNull()
        {
            Assert.That(_repository.Quarantine(), Is.Null);
        }

        [Test]
        public void Delete_RemovesEveryGeneration()
        {
            _repository.Write("first");
            _repository.Write("second");

            _repository.Delete();

            Assert.That(_repository.Exists, Is.False);
            Assert.That(_repository.ReadCandidates(), Is.Empty);
        }

        [Test]
        public void SupportsBackgroundIo_IsTrue()
        {
            Assert.That(_repository.SupportsBackgroundIo, Is.True,
                "file access has no thread affinity, so writes belong off the main thread");
        }

        [Test]
        public void Constructor_WithEmptyArguments_Throws()
        {
            Assert.Throws<ArgumentException>(() => _ = new FileSaveRepository("   "));
            Assert.Throws<ArgumentException>(() => _ = new FileSaveRepository(_directory, "  "));
        }

        [Test]
        public void Write_WithNullPayload_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _repository.Write(null!));
        }
    }
}
