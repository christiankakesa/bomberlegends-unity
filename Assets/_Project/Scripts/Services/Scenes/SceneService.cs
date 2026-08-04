using System;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BomberLegends.Services.Scenes
{
    /// <summary>
    /// Loads gameplay scenes additively on top of the persistent bootstrap scene.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Bootstrap is never unloaded: it owns the only <c>AudioListener</c>, the only
    /// <c>EventSystem</c>, the service graph and the loading screen. Exactly one other scene is
    /// loaded at a time, and swapping is strictly ordered — cover the screen, unload the old scene,
    /// load the new one, install it, uncover. Installing before uncovering is what guarantees a
    /// scene is never visible in a half-wired state.
    /// </para>
    /// <para>
    /// The context is supplied after construction because the graph contains this service: they
    /// cannot both be constructor-injected. <see cref="Initialise"/> closes that loop exactly once,
    /// in the composition root.
    /// </para>
    /// </remarks>
    public sealed class SceneService : ISceneService
    {
        private readonly ILoadingScreen _loadingScreen;
        private GameContext? _context;
        private bool _hasAdditiveScene;

        /// <summary>Creates the service.</summary>
        public SceneService(ILoadingScreen loadingScreen)
        {
            _loadingScreen = loadingScreen ?? throw new ArgumentNullException(nameof(loadingScreen));
            Current = SceneId.Bootstrap;
        }

        /// <inheritdoc />
        public SceneId Current { get; private set; }

        /// <inheritdoc />
        public bool IsTransitioning { get; private set; }

        /// <summary>
        /// Supplies the composed graph that loaded scenes are installed with. Called once, by the
        /// composition root, immediately after the graph is built.
        /// </summary>
        public void Initialise(GameContext context)
        {
            if (_context != null)
            {
                Debug.LogError("[Scenes] The scene service was initialised twice. The second call is ignored.");
                return;
            }

            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc />
        /// <remarks>
        /// Validation happens here, in a non-async method, so a bad argument throws at the call site.
        /// Had this method been <c>async</c>, the exception would have been captured in the returned
        /// awaitable and silently lost for any caller that does not await — which is most callers of
        /// a fire-and-forget transition.
        /// </remarks>
        public Awaitable TransitionToAsync(
            SceneId target,
            ISceneTransitionPayload? payload = null,
            CancellationToken cancellationToken = default)
        {
            if (_context == null)
            {
                throw new InvalidOperationException(
                    "The scene service was used before Initialise supplied the service graph.");
            }

            if (target == SceneId.Bootstrap)
            {
                throw new ArgumentException(
                    "Bootstrap is persistent and is never transitioned to.", nameof(target));
            }

            if (payload != null && payload.Target != target)
            {
                throw new ArgumentException(
                    $"Payload targets {payload.Target} but the transition is to {target}.", nameof(payload));
            }

            if (IsTransitioning)
            {
                Debug.LogWarning($"[Scenes] Ignoring a request to load {target} during a transition.");
                return AlreadyCompleted();
            }

            return RunTransitionAsync(target, payload, cancellationToken);
        }

        private static Awaitable AlreadyCompleted()
        {
            var source = new AwaitableCompletionSource();
            source.SetResult();
            return source.Awaitable;
        }

        private async Awaitable RunTransitionAsync(
            SceneId target,
            ISceneTransitionPayload? payload,
            CancellationToken cancellationToken)
        {
            IsTransitioning = true;
            try
            {
                await _loadingScreen.ShowAsync(cancellationToken);

                if (_hasAdditiveScene)
                {
                    await UnloadAsync(Current, cancellationToken);
                    _hasAdditiveScene = false;
                }

                var scene = await LoadAsync(target, cancellationToken);
                SceneManager.SetActiveScene(scene);
                _hasAdditiveScene = true;
                Current = target;

                InstallScene(scene, target, payload);

                await _loadingScreen.HideAsync(cancellationToken);
            }
            finally
            {
                IsTransitioning = false;
            }
        }

        /// <summary>
        /// Session-state key naming the scene the developer pressed Play from.
        /// </summary>
        /// <remarks>
        /// Declared here, in the runtime layer, because both the Editor tool that writes it and the
        /// bootstrap that reads it need it — and the Editor assembly references Bootstrap, so the
        /// constant cannot live on the Editor side without creating a cycle.
        /// </remarks>
        public const string EditorStartSceneKey = "BomberLegends.StartScene";

        /// <summary>Maps a scene identifier to its asset name.</summary>
        /// <remarks>A switch over constants rather than <c>ToString</c>, which allocates.</remarks>
        public static string NameOf(SceneId scene) => scene switch
        {
            SceneId.Bootstrap => "Bootstrap",
            SceneId.Hub => "Hub",
            SceneId.Match => "Match",
            _ => throw new ArgumentOutOfRangeException(nameof(scene), scene, "Unknown scene.")
        };

        private static async Awaitable<Scene> LoadAsync(SceneId target, CancellationToken cancellationToken)
        {
            var name = NameOf(target);
            var operation = SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive);

            if (operation == null)
            {
                throw new InvalidOperationException(
                    $"Scene '{name}' could not be loaded. Is it in the build settings?");
            }

            await Awaitable.FromAsyncOperation(operation, cancellationToken);
            return SceneManager.GetSceneByName(name);
        }

        private static async Awaitable UnloadAsync(SceneId current, CancellationToken cancellationToken)
        {
            var operation = SceneManager.UnloadSceneAsync(NameOf(current));
            if (operation == null)
            {
                return;
            }

            await Awaitable.FromAsyncOperation(operation, cancellationToken);
        }

        private void InstallScene(Scene scene, SceneId target, ISceneTransitionPayload? payload)
        {
            // Enumerating the loaded scene's roots, not a global search: this touches only the
            // objects that just loaded, and only once per transition.
            var roots = scene.GetRootGameObjects();

            for (var i = 0; i < roots.Length; i++)
            {
                if (!roots[i].TryGetComponent<SceneInstaller>(out var installer))
                {
                    continue;
                }

                if (installer.Scene != target)
                {
                    Debug.LogError(
                        $"[Scenes] Scene '{NameOf(target)}' holds an installer declaring {installer.Scene}.");
                }

                installer.Install(_context!, payload);
                return;
            }

            Debug.LogError(
                $"[Scenes] Scene '{NameOf(target)}' has no {nameof(SceneInstaller)} on a root object. " +
                "Nothing in it received the service graph.");
        }
    }
}
