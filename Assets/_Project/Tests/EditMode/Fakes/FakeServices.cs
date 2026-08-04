using System;
using System.Collections.Generic;
using System.Threading;
using BomberLegends.Core;
using BomberLegends.Data.Audio;
using BomberLegends.Services.Analytics;
using BomberLegends.Services.Assets;
using BomberLegends.Services.Audio;
using BomberLegends.Services.Save;
using BomberLegends.Services.Scenes;
using BomberLegends.Services.Settings;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BomberLegends.Tests.EditMode.Fakes
{
    /// <summary>Helpers for producing already-completed awaitables without a running player loop.</summary>
    internal static class CompletedAwaitable
    {
        internal static Awaitable Create()
        {
            var source = new AwaitableCompletionSource();
            source.SetResult();
            return source.Awaitable;
        }

        internal static Awaitable<T> Create<T>(T value)
        {
            var source = new AwaitableCompletionSource<T>();
            source.SetResult(value);
            return source.Awaitable;
        }
    }

    /// <summary>In-memory settings, applied instantly and never persisted.</summary>
    internal sealed class FakeSettingsService : ISettingsService
    {
        public SettingsData Current { get; private set; } = SettingsData.Default;

        public event Action<SettingsData>? Changed;

        public int ApplyCount { get; private set; }

        public void Apply(in SettingsData settings)
        {
            Current = settings;
            ApplyCount++;
            Changed?.Invoke(Current);
        }

        public void ResetToDefaults() => Apply(SettingsData.Default);
    }

    /// <summary>An in-memory save that records how often it was loaded and written.</summary>
    internal sealed class FakeSaveService : ISaveService
    {
        public PlayerSaveData Data { get; private set; } = PlayerSaveData.CreateNew();

        public bool IsDirty { get; private set; }

        public int LoadCount { get; private set; }

        public int SaveCount { get; private set; }

        public Awaitable LoadAsync(CancellationToken cancellationToken = default)
        {
            LoadCount++;
            Data = PlayerSaveData.CreateNew();
            IsDirty = false;
            return CompletedAwaitable.Create();
        }

        public Awaitable SaveAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            IsDirty = false;
            return CompletedAwaitable.Create();
        }

        public void FlushImmediate()
        {
            SaveCount++;
            IsDirty = false;
        }

        public void MarkDirty() => IsDirty = true;
    }

    /// <summary>
    /// An asset service that loads nothing. Tests that need real content supply their own
    /// implementation rather than extending this one.
    /// </summary>
    internal sealed class FakeAssetService : IAssetService
    {
        public List<string> WarmedLabels { get; } = new List<string>();

        public int ReleaseCount { get; private set; }

        public Awaitable<T> LoadAsync<T>(AssetKey key, CancellationToken cancellationToken = default)
            where T : Object => CompletedAwaitable.Create<T>(null!);

        public Awaitable<GameObject> InstantiateAsync(
            AssetKey key,
            Transform? parent = null,
            CancellationToken cancellationToken = default) => CompletedAwaitable.Create<GameObject>(null!);

        public Awaitable WarmupAsync(string label, CancellationToken cancellationToken = default)
        {
            WarmedLabels.Add(label);
            return CompletedAwaitable.Create();
        }

        public void Release(Object asset) => ReleaseCount++;

        public void ReleaseLabel(string label) => ReleaseCount++;
    }

    /// <summary>Records audio requests instead of playing them.</summary>
    internal sealed class FakeAudioService : IAudioService
    {
        private readonly Dictionary<AudioBus, float> _volumes = new Dictionary<AudioBus, float>();

        public List<SfxDefinition> PlayedSfx { get; } = new List<SfxDefinition>();

        public MusicDefinition? CurrentMusic { get; private set; }

        public int StopAllCount { get; private set; }

        public void PlaySfx(SfxDefinition definition, Vector3? worldPosition = null) =>
            PlayedSfx.Add(definition);

        public void PlayMusic(MusicDefinition definition) => CurrentMusic = definition;

        public void StopMusic(float fadeSeconds = 1f) => CurrentMusic = null;

        public void SetBusVolume(AudioBus bus, float normalized01) => _volumes[bus] = normalized01;

        public float GetBusVolume(AudioBus bus) => _volumes.TryGetValue(bus, out var value) ? value : 1f;

        public void StopAll()
        {
            CurrentMusic = null;
            StopAllCount++;
        }
    }

    /// <summary>Tracks scene transitions without loading anything.</summary>
    internal sealed class FakeSceneService : ISceneService
    {
        public SceneId Current { get; private set; } = SceneId.Bootstrap;

        public bool IsTransitioning => false;

        public List<SceneId> TransitionHistory { get; } = new List<SceneId>();

        public Awaitable TransitionToAsync(
            SceneId target,
            ISceneTransitionPayload? payload = null,
            CancellationToken cancellationToken = default)
        {
            Current = target;
            TransitionHistory.Add(target);
            return CompletedAwaitable.Create();
        }
    }

    /// <summary>
    /// Captures analytics events so tests can assert an event fired exactly once with the expected
    /// payload. This is the implementation the T-035 instrumentation tests are written against.
    /// </summary>
    internal sealed class RecordingAnalyticsService : IAnalyticsService
    {
        private readonly List<(string Name, AnalyticsPayload Payload)> _events =
            new List<(string, AnalyticsPayload)>();

        public IReadOnlyList<(string Name, AnalyticsPayload Payload)> Events => _events;

        public void Track(string eventName, in AnalyticsPayload payload) =>
            _events.Add((eventName, payload));

        public int CountOf(string eventName)
        {
            var count = 0;
            foreach (var recorded in _events)
            {
                if (string.Equals(recorded.Name, eventName, StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
