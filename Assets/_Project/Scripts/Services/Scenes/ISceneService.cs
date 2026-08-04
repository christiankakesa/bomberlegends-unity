using System.Threading;
using UnityEngine;

namespace BomberLegends.Services.Scenes
{
    /// <summary>
    /// The scenes the game can be in.
    /// </summary>
    /// <remarks>
    /// Scenes are referenced by this enum rather than by name or build index, so a rename or a
    /// reorder is a compile error instead of a runtime failure.
    /// </remarks>
    public enum SceneId : byte
    {
        /// <summary>The persistent scene. Loaded first and never unloaded.</summary>
        Bootstrap = 0,

        /// <summary>The hub: progression, upgrades, and the entry point to a match.</summary>
        Hub = 1,

        /// <summary>A playable match.</summary>
        Match = 2
    }

    /// <summary>
    /// Data handed to a scene as it loads.
    /// </summary>
    /// <remarks>
    /// Implemented by per-scene payload types, for example the level to load into a match. Exists so
    /// scene arguments are passed explicitly rather than parked in a static field between loads.
    /// </remarks>
    public interface ISceneTransitionPayload
    {
        /// <summary>The scene this payload is intended for.</summary>
        SceneId Target { get; }
    }

    /// <summary>
    /// Loads and unloads scenes additively on top of the persistent bootstrap scene.
    /// </summary>
    public interface ISceneService
    {
        /// <summary>The scene currently loaded on top of bootstrap.</summary>
        SceneId Current { get; }

        /// <summary>Whether a transition is in progress. Further requests are rejected while true.</summary>
        bool IsTransitioning { get; }

        /// <summary>
        /// Unloads the current scene and loads <paramref name="target"/>, showing the loading screen
        /// for the duration. Completes once the new scene's installer has run and the scene is
        /// interactive.
        /// </summary>
        /// <param name="target">The scene to load.</param>
        /// <param name="payload">Optional data for the incoming scene.</param>
        /// <param name="cancellationToken">Cancels the transition.</param>
        Awaitable TransitionToAsync(
            SceneId target,
            ISceneTransitionPayload? payload = null,
            CancellationToken cancellationToken = default);
    }
}
