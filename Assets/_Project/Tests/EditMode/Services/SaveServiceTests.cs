using System.Collections.Generic;
using BomberLegends.Services.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BomberLegends.Tests.EditMode.Services
{
    /// <summary>
    /// Covers parsing, the migration chain, and recovery from unusable payloads.
    /// </summary>
    /// <remarks>
    /// Every repository here declares <c>supportsBackgroundIo: false</c>, which keeps
    /// <see cref="SaveService.LoadAsync"/> and <see cref="SaveService.SaveAsync"/> free of any
    /// await. An async method runs synchronously until its first await, so with that path taken the
    /// work is complete by the time the call returns and these tests need no player loop. The
    /// threaded path is covered by the PlayMode suite, where a player loop exists.
    /// </remarks>
    public sealed class SaveServiceTests
    {
        private static string Serialise(int schemaVersion, long coins, int bombRangeLevel)
        {
            var data = PlayerSaveData.CreateNew();
            data.SchemaVersion = schemaVersion;
            data.DataCoins = coins;
            data.BombRangeLevel = bombRangeLevel;
            return JsonUtility.ToJson(data);
        }

        private sealed class RecordingMigration : ISaveMigration
        {
            private readonly List<int> _log;

            public RecordingMigration(int fromVersion, List<int> log)
            {
                FromVersion = fromVersion;
                _log = log;
            }

            public int FromVersion { get; }

            public void Apply(PlayerSaveData data)
            {
                _log.Add(FromVersion);
                data.DataCoins += 10;
            }
        }

        [Test]
        public void LoadAsync_WithNoStoredPayload_StartsFresh()
        {
            var service = new SaveService(new MemorySaveRepository(supportsBackgroundIo: false));

            _ = service.LoadAsync();

            Assert.That(service.Data.SchemaVersion, Is.EqualTo(PlayerSaveData.CurrentSchemaVersion));
            Assert.That(service.Data.DataCoins, Is.EqualTo(0));
            Assert.That(service.LastLoadRecovered, Is.False, "an empty store is a first run, not a recovery");
        }

        [Test]
        public void SaveThenLoad_PreservesEveryField()
        {
            var repository = new MemorySaveRepository(supportsBackgroundIo: false);
            var writer = new SaveService(repository);

            writer.Data.DataCoins = 1234;
            writer.Data.BombRangeLevel = 2;
            _ = writer.SaveAsync();

            var reader = new SaveService(repository);
            _ = reader.LoadAsync();

            Assert.That(reader.Data.DataCoins, Is.EqualTo(1234));
            Assert.That(reader.Data.BombRangeLevel, Is.EqualTo(2));
            Assert.That(reader.Data.SchemaVersion, Is.EqualTo(PlayerSaveData.CurrentSchemaVersion));
        }

        [Test]
        public void SaveAsync_ClearsTheDirtyFlag()
        {
            var service = new SaveService(new MemorySaveRepository(supportsBackgroundIo: false));
            service.MarkDirty();
            Assert.That(service.IsDirty, Is.True);

            _ = service.SaveAsync();

            Assert.That(service.IsDirty, Is.False);
        }

        [Test]
        public void FlushImmediate_WritesAndClearsTheDirtyFlag()
        {
            var repository = new MemorySaveRepository(supportsBackgroundIo: false);
            var service = new SaveService(repository);
            service.Data.DataCoins = 77;
            service.MarkDirty();

            service.FlushImmediate();

            Assert.That(service.IsDirty, Is.False);
            Assert.That(repository.WriteCount, Is.EqualTo(1));
        }

        [Test]
        public void MarkDirty_DoesNotWriteByItself()
        {
            var repository = new MemorySaveRepository(supportsBackgroundIo: false);
            var service = new SaveService(repository);

            service.MarkDirty();
            service.MarkDirty();
            service.MarkDirty();

            Assert.That(repository.WriteCount, Is.EqualTo(0), "writes are batched, not one per change");
        }

        [Test]
        public void LoadAsync_MigratesAnOlderPayloadForward()
        {
            var log = new List<int>();
            var repository = new MemorySaveRepository(Serialise(0, coins: 5, bombRangeLevel: 0),
                supportsBackgroundIo: false);
            var service = new SaveService(
                repository,
                new ISaveMigration[] { new RecordingMigration(0, log) },
                targetSchemaVersion: 1);

            _ = service.LoadAsync();

            Assert.That(log, Is.EqualTo(new[] { 0 }));
            Assert.That(service.Data.SchemaVersion, Is.EqualTo(1));
            Assert.That(service.Data.DataCoins, Is.EqualTo(15), "the migration ran exactly once");
        }

        [Test]
        public void LoadAsync_RunsAChainOfMigrationsInOrder()
        {
            var log = new List<int>();
            var repository = new MemorySaveRepository(Serialise(1, coins: 0, bombRangeLevel: 0),
                supportsBackgroundIo: false);

            // Supplied out of order on purpose: the service must sequence them by version.
            var migrations = new ISaveMigration[]
            {
                new RecordingMigration(3, log),
                new RecordingMigration(1, log),
                new RecordingMigration(2, log)
            };

            var service = new SaveService(repository, migrations, targetSchemaVersion: 4);

            _ = service.LoadAsync();

            Assert.That(log, Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(service.Data.SchemaVersion, Is.EqualTo(4));
            Assert.That(service.Data.DataCoins, Is.EqualTo(30));
        }

        [Test]
        public void LoadAsync_WithNoMigrationPath_QuarantinesAndStartsFresh()
        {
            var repository = new MemorySaveRepository(Serialise(0, coins: 999, bombRangeLevel: 3),
                supportsBackgroundIo: false);
            var service = new SaveService(repository, migrations: null, targetSchemaVersion: 5);

            LogAssert.ignoreFailingMessages = true;
            _ = service.LoadAsync();
            LogAssert.ignoreFailingMessages = false;

            Assert.That(service.Data.DataCoins, Is.EqualTo(0), "an unmigratable payload must not be used");
            Assert.That(repository.QuarantinedPayload, Is.Not.Null, "the original payload must be kept");
            Assert.That(service.LastLoadRecovered, Is.True);
        }

        [Test]
        public void LoadAsync_WithCorruptPrimary_FallsBackToTheBackup()
        {
            var repository = new MemorySaveRepository(supportsBackgroundIo: false);
            repository.Write(Serialise(PlayerSaveData.CurrentSchemaVersion, coins: 500, bombRangeLevel: 1));
            repository.Write("{ this is not valid json");

            var service = new SaveService(repository);

            LogAssert.ignoreFailingMessages = true;
            _ = service.LoadAsync();
            LogAssert.ignoreFailingMessages = false;

            Assert.That(service.Data.DataCoins, Is.EqualTo(500), "the previous save should have been used");
            Assert.That(service.LastLoadRecovered, Is.True);
        }

        [Test]
        public void LoadAsync_WithEverythingUnreadable_QuarantinesAndStartsFresh()
        {
            var repository = new MemorySaveRepository(supportsBackgroundIo: false);
            repository.Write("garbage one");
            repository.Write("garbage two");

            var service = new SaveService(repository);

            LogAssert.ignoreFailingMessages = true;
            _ = service.LoadAsync();
            LogAssert.ignoreFailingMessages = false;

            Assert.That(service.Data.DataCoins, Is.EqualTo(0));
            Assert.That(repository.QuarantinedPayload, Is.Not.Null);
        }

        [Test]
        public void LoadAsync_WithAPayloadFromANewerBuild_KeepsIt()
        {
            var repository = new MemorySaveRepository(Serialise(99, coins: 4200, bombRangeLevel: 3),
                supportsBackgroundIo: false);
            var service = new SaveService(repository);

            LogAssert.ignoreFailingMessages = true;
            _ = service.LoadAsync();
            LogAssert.ignoreFailingMessages = false;

            Assert.That(service.Data.DataCoins, Is.EqualTo(4200),
                "discarding a newer save would delete real progress");
            Assert.That(service.Data.BombRangeLevel, Is.EqualTo(3));
        }

        [Test]
        public void Constructor_WithoutARepository_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() => _ = new SaveService(null!));
        }
    }
}
