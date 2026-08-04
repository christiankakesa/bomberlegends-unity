using System;
using BomberLegends.Core;
using BomberLegends.Services.Analytics;
using BomberLegends.Services.Assets;
using BomberLegends.Services.Save;
using BomberLegends.Services.Settings;
using NUnit.Framework;

namespace BomberLegends.Tests.EditMode.Services
{
    /// <summary>Covers the allocation-free analytics payload and its bounds.</summary>
    public sealed class AnalyticsPayloadTests
    {
        [Test]
        public void Empty_HasNoFields()
        {
            Assert.That(AnalyticsPayload.Empty.Count, Is.EqualTo(0));
        }

        [Test]
        public void With_AppendsFieldsInOrder()
        {
            var payload = AnalyticsPayload.Empty
                .With("nodes", 5)
                .With("coins", 120);

            Assert.That(payload.Count, Is.EqualTo(2));
            Assert.That(payload[0].Name, Is.EqualTo("nodes"));
            Assert.That(payload[0].Value, Is.EqualTo(5));
            Assert.That(payload[1].Name, Is.EqualTo("coins"));
            Assert.That(payload[1].Value, Is.EqualTo(120));
        }

        [Test]
        public void With_DoesNotMutateTheOriginal()
        {
            var original = AnalyticsPayload.Empty.With("a", 1);

            original.With("b", 2);

            Assert.That(original.Count, Is.EqualTo(1));
        }

        [Test]
        public void With_RecordsBooleansAsZeroOrOne()
        {
            var payload = AnalyticsPayload.Empty.With("won", true).With("perfect", false);

            Assert.That(payload[0].Value, Is.EqualTo(1));
            Assert.That(payload[1].Value, Is.EqualTo(0));
        }

        [Test]
        public void With_FillsEverySlot()
        {
            var payload = AnalyticsPayload.Empty;
            for (var i = 0; i < AnalyticsPayload.MaxFields; i++)
            {
                payload = payload.With($"field{i}", i);
            }

            Assert.That(payload.Count, Is.EqualTo(AnalyticsPayload.MaxFields));
            for (var i = 0; i < AnalyticsPayload.MaxFields; i++)
            {
                Assert.That(payload[i].Value, Is.EqualTo(i));
            }
        }

        [Test]
        public void With_BeyondCapacity_Throws()
        {
            var payload = AnalyticsPayload.Empty;
            for (var i = 0; i < AnalyticsPayload.MaxFields; i++)
            {
                payload = payload.With($"field{i}", i);
            }

            var full = payload;
            Assert.Throws<InvalidOperationException>(() => full.With("overflow", 1));
        }

        [Test]
        public void With_EmptyName_Throws()
        {
            Assert.Throws<ArgumentException>(() => AnalyticsPayload.Empty.With(" ", 1));
        }

        [Test]
        public void Indexer_BeyondCount_Throws()
        {
            var payload = AnalyticsPayload.Empty.With("only", 1);

            Assert.Throws<ArgumentOutOfRangeException>(() => _ = payload[1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = payload[-1]);
        }
    }

    /// <summary>Covers asset key validation and equality.</summary>
    public sealed class AssetKeyTests
    {
        [Test]
        public void Constructor_StoresAddress()
        {
            Assert.That(new AssetKey("Prefabs/Bomb").Address, Is.EqualTo("Prefabs/Bomb"));
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase(null)]
        public void Constructor_WithEmptyAddress_Throws(string? address)
        {
            Assert.Throws<ArgumentException>(() => _ = new AssetKey(address!));
        }

        [Test]
        public void Equality_ComparesAddressOrdinally()
        {
            var a = new AssetKey("Prefabs/Bomb");
            var b = new AssetKey("Prefabs/Bomb");
            var c = new AssetKey("prefabs/bomb");

            Assert.That(a.Equals(b), Is.True);
            Assert.That(a.Equals((object)b), Is.True);
            Assert.That(a.Equals(c), Is.False, "addresses are case sensitive");
            Assert.That(a.Equals("not a key"), Is.False);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void ToString_ReturnsAddress()
        {
            Assert.That(new AssetKey("Audio/Blast").ToString(), Is.EqualTo("Audio/Blast"));
        }
    }

    /// <summary>Covers default settings and bus lookup.</summary>
    public sealed class SettingsDataTests
    {
        [Test]
        public void Default_EnablesAccessibilityEffectsAndAudibleBuses()
        {
            var settings = SettingsData.Default;

            Assert.That(settings.MasterVolume, Is.GreaterThan(0f));
            Assert.That(settings.SfxVolume, Is.GreaterThan(0f));
            Assert.That(settings.ScreenShakeEnabled, Is.True);
            Assert.That(settings.ScreenFlashEnabled, Is.True);
            Assert.That(settings.HapticsEnabled, Is.True);
        }

        [Test]
        public void GetBusVolume_ReturnsThePerBusSlider()
        {
            var settings = new SettingsData(1f, 0.5f, 0.25f, 0.75f, 2, false, false, false);

            Assert.That(settings.GetBusVolume(AudioBus.Master), Is.EqualTo(1f));
            Assert.That(settings.GetBusVolume(AudioBus.Music), Is.EqualTo(0.5f));
            Assert.That(settings.GetBusVolume(AudioBus.Sfx), Is.EqualTo(0.25f));
            Assert.That(settings.GetBusVolume(AudioBus.Ui), Is.EqualTo(0.75f));
        }

        [Test]
        public void GetBusVolume_ForBusesWithoutASlider_IsUnattenuated()
        {
            Assert.That(SettingsData.Default.GetBusVolume(AudioBus.Voice), Is.EqualTo(1f));
            Assert.That(SettingsData.Default.GetBusVolume(AudioBus.Ambience), Is.EqualTo(1f));
        }

        [Test]
        public void Constructor_StoresEveryField()
        {
            var settings = new SettingsData(0.1f, 0.2f, 0.3f, 0.4f, 3, true, false, true);

            Assert.That(settings.QualityTier, Is.EqualTo(3));
            Assert.That(settings.ScreenShakeEnabled, Is.True);
            Assert.That(settings.ScreenFlashEnabled, Is.False);
            Assert.That(settings.HapticsEnabled, Is.True);
        }
    }

    /// <summary>Covers the shape of a brand new save.</summary>
    public sealed class PlayerSaveDataTests
    {
        [Test]
        public void CreateNew_StartsAtTheCurrentSchemaVersion()
        {
            Assert.That(PlayerSaveData.CreateNew().SchemaVersion,
                Is.EqualTo(PlayerSaveData.CurrentSchemaVersion));
        }

        [Test]
        public void CreateNew_StartsWithNoProgress()
        {
            var save = PlayerSaveData.CreateNew();

            Assert.That(save.DataCoins, Is.EqualTo(0));
            Assert.That(save.BombRangeLevel, Is.EqualTo(0));
        }

        [Test]
        public void CreateNew_StartsWithDefaultSettings()
        {
            Assert.That(PlayerSaveData.CreateNew().Settings.MasterVolume,
                Is.EqualTo(SettingsData.Default.MasterVolume));
        }

        [Test]
        public void Fields_AreMutable()
        {
            var save = PlayerSaveData.CreateNew();

            save.DataCoins = 250;
            save.BombRangeLevel = 2;

            Assert.That(save.DataCoins, Is.EqualTo(250));
            Assert.That(save.BombRangeLevel, Is.EqualTo(2));
        }

        [Test]
        public void SchemaVersion_IsPresentFromTheFirstRelease()
        {
            Assert.That(PlayerSaveData.CurrentSchemaVersion, Is.GreaterThanOrEqualTo(1),
                "a save without a version cannot be migrated and orphans early players");
        }
    }

    /// <summary>Covers the no-op analytics implementation used until Milestone 9.</summary>
    public sealed class NullAnalyticsServiceTests
    {
        [Test]
        public void Track_WithLoggingDisabled_DoesNothingAndDoesNotThrow()
        {
            var service = new NullAnalyticsService(logInEditor: false);

            Assert.DoesNotThrow(() =>
                service.Track("match_ended", AnalyticsPayload.Empty.With("won", true).With("score", 4200)));
        }

        [Test]
        public void Track_AcceptsAnEmptyPayload()
        {
            var service = new NullAnalyticsService(logInEditor: false);

            Assert.DoesNotThrow(() => service.Track("session_start", AnalyticsPayload.Empty));
        }
    }
}
