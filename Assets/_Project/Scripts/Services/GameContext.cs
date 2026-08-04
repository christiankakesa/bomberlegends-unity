using System;
using BomberLegends.Services.Analytics;
using BomberLegends.Services.Assets;
using BomberLegends.Services.Audio;
using BomberLegends.Services.Save;
using BomberLegends.Services.Scenes;
using BomberLegends.Services.Settings;

namespace BomberLegends.Services
{
    /// <summary>
    /// The root object graph, composed once at start-up and passed down explicitly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Deliberately a set of strongly typed properties rather than a container with a
    /// <c>Resolve&lt;T&gt;()</c> method. A missing dependency is then a compile error instead of a
    /// runtime one, every service is reachable by Go To Definition, nothing relies on reflection,
    /// and the complete dependency surface of the game is this one readable class.
    /// </para>
    /// <para>
    /// MonoBehaviours receive this through their scene installer. Nothing searches the scene for a
    /// service, and there is no static access point.
    /// </para>
    /// <para>
    /// Lives in Services rather than Bootstrap because scene installers in the Gameplay and UI
    /// assemblies need it, and nothing may reference Bootstrap — Bootstrap composes this graph and
    /// is otherwise a leaf.
    /// </para>
    /// <para>
    /// Progression and wallet services join this graph in T-032, when the meta loop exists.
    /// </para>
    /// </remarks>
    public sealed class GameContext
    {
        /// <summary>Creates the graph. Every dependency is required.</summary>
        /// <exception cref="ArgumentNullException">Any service is null.</exception>
        public GameContext(
            ISettingsService settings,
            ISaveService save,
            IAssetService assets,
            IAudioService audio,
            ISceneService scenes,
            IAnalyticsService analytics)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Save = save ?? throw new ArgumentNullException(nameof(save));
            Assets = assets ?? throw new ArgumentNullException(nameof(assets));
            Audio = audio ?? throw new ArgumentNullException(nameof(audio));
            Scenes = scenes ?? throw new ArgumentNullException(nameof(scenes));
            Analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
        }

        /// <summary>Player options.</summary>
        public ISettingsService Settings { get; }

        /// <summary>Persisted player data.</summary>
        public ISaveService Save { get; }

        /// <summary>Content loading.</summary>
        public IAssetService Assets { get; }

        /// <summary>Sound and music.</summary>
        public IAudioService Audio { get; }

        /// <summary>Scene flow.</summary>
        public ISceneService Scenes { get; }

        /// <summary>Telemetry.</summary>
        public IAnalyticsService Analytics { get; }
    }
}
