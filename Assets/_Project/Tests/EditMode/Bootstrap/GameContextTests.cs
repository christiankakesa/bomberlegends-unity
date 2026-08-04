using System;
using BomberLegends.Services;
using BomberLegends.Services.Analytics;
using BomberLegends.Services.Scenes;
using BomberLegends.Tests.EditMode.Fakes;
using NUnit.Framework;

namespace BomberLegends.Tests.EditMode.Bootstrap
{
    /// <summary>
    /// Verifies the composition root can be built entirely from test doubles, with no scene, no
    /// GameObject and no Unity lifecycle. If this ever stops being possible, the graph has grown a
    /// hidden dependency on engine state and everything downstream becomes harder to test.
    /// </summary>
    public sealed class GameContextTests
    {
        private static GameContext CreateContext() =>
            new GameContext(
                new FakeSettingsService(),
                new FakeSaveService(),
                new FakeAssetService(),
                new FakeAudioService(),
                new FakeSceneService(),
                new RecordingAnalyticsService());

        [Test]
        public void Constructor_ExposesEveryServiceItWasGiven()
        {
            var settings = new FakeSettingsService();
            var save = new FakeSaveService();
            var assets = new FakeAssetService();
            var audio = new FakeAudioService();
            var scenes = new FakeSceneService();
            var analytics = new RecordingAnalyticsService();

            var context = new GameContext(settings, save, assets, audio, scenes, analytics);

            Assert.That(context.Settings, Is.SameAs(settings));
            Assert.That(context.Save, Is.SameAs(save));
            Assert.That(context.Assets, Is.SameAs(assets));
            Assert.That(context.Audio, Is.SameAs(audio));
            Assert.That(context.Scenes, Is.SameAs(scenes));
            Assert.That(context.Analytics, Is.SameAs(analytics));
        }

        [Test]
        public void Constructor_WithMissingSettings_Throws() =>
            Assert.Throws<ArgumentNullException>(() => new GameContext(
                null!,
                new FakeSaveService(),
                new FakeAssetService(),
                new FakeAudioService(),
                new FakeSceneService(),
                new RecordingAnalyticsService()));

        [Test]
        public void Constructor_WithMissingSave_Throws() =>
            Assert.Throws<ArgumentNullException>(() => new GameContext(
                new FakeSettingsService(),
                null!,
                new FakeAssetService(),
                new FakeAudioService(),
                new FakeSceneService(),
                new RecordingAnalyticsService()));

        [Test]
        public void Constructor_WithMissingAssets_Throws() =>
            Assert.Throws<ArgumentNullException>(() => new GameContext(
                new FakeSettingsService(),
                new FakeSaveService(),
                null!,
                new FakeAudioService(),
                new FakeSceneService(),
                new RecordingAnalyticsService()));

        [Test]
        public void Constructor_WithMissingAudio_Throws() =>
            Assert.Throws<ArgumentNullException>(() => new GameContext(
                new FakeSettingsService(),
                new FakeSaveService(),
                new FakeAssetService(),
                null!,
                new FakeSceneService(),
                new RecordingAnalyticsService()));

        [Test]
        public void Constructor_WithMissingScenes_Throws() =>
            Assert.Throws<ArgumentNullException>(() => new GameContext(
                new FakeSettingsService(),
                new FakeSaveService(),
                new FakeAssetService(),
                new FakeAudioService(),
                null!,
                new RecordingAnalyticsService()));

        [Test]
        public void Constructor_WithMissingAnalytics_Throws() =>
            Assert.Throws<ArgumentNullException>(() => new GameContext(
                new FakeSettingsService(),
                new FakeSaveService(),
                new FakeAssetService(),
                new FakeAudioService(),
                new FakeSceneService(),
                null!));

        [Test]
        public void Services_AreReachableThroughTheContext_WithoutTypeLookup()
        {
            var context = CreateContext();

            context.Scenes.TransitionToAsync(SceneId.Hub);
            context.Save.MarkDirty();
            context.Analytics.Track("context_smoke", AnalyticsPayload.Empty.With("ok", true));

            Assert.That(context.Scenes.Current, Is.EqualTo(SceneId.Hub));
            Assert.That(context.Save.IsDirty, Is.True);
            Assert.That(((RecordingAnalyticsService)context.Analytics).CountOf("context_smoke"), Is.EqualTo(1));
        }
    }
}
