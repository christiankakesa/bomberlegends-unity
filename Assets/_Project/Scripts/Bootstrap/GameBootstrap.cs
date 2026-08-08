using BomberLegends.Services;
using BomberLegends.Services.Analytics;
using BomberLegends.Services.Assets;
using BomberLegends.Services.Audio;
using BomberLegends.Services.Save;
using BomberLegends.Services.Scenes;
using BomberLegends.Services.Settings;
using UnityEngine;

namespace BomberLegends.Bootstrap
{
    /// <summary>
    /// The composition root. Configures the application, builds the service graph, and hands control
    /// to the hub.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the only place services are constructed, and the only assembly that references every
    /// other one. Nothing references it back, so the wiring can be rearranged without touching a
    /// single feature.
    /// </para>
    /// <para>
    /// Lives on the persistent bootstrap scene, which is build index zero and is never unloaded.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Header("Scene references")]
        [SerializeField]
        [Tooltip("Full-screen fade used to cover scene swaps. Lives on this scene so it survives them.")]
        private LoadingScreenView? _loadingScreen;

        [SerializeField]
        [Tooltip("Flushes the save when the application is backgrounded or quits.")]
        private SaveLifecycleHandler? _saveLifecycle;

        [Header("Application")]
        [SerializeField, Range(30, 120)]
        [Tooltip("Frame rate cap. Never left to the platform default: rendering above the display " +
                 "target burns battery and triggers thermal throttling for no visible benefit.")]
        private int _targetFrameRate = 60;

        [SerializeField]
        [Tooltip("Whether the screen is allowed to sleep. Off during play.")]
        private bool _allowScreenSleep;

        private GameContext? _context;

        /// <summary>The composed service graph. Null until start-up has finished.</summary>
        public GameContext? Context => _context;

        private async void Start()
        {
            ConfigureApplication();

            if (_loadingScreen == null)
            {
                Debug.LogError(
                    "[Bootstrap] No loading screen is assigned. Scene transitions cannot cover the view.");
                return;
            }

            await _loadingScreen.ShowAsync();

            // Constructed in dependency order. Settings needs both save and audio, so it comes last.
            var repository = CreateSaveRepository();
            var save = new SaveService(repository);
            // Hosted beneath the bootstrap object, which survives every scene change, so voices are
            // pooled once for the whole session rather than rebuilt per match.
            var audio = new AudioService(transform);
            var assets = new UnavailableAssetService();
            var analytics = new NullAnalyticsService();
            var scenes = new SceneService(_loadingScreen);
            var settings = new SettingsService(save, audio);

            _context = new GameContext(settings, save, assets, audio, scenes, analytics);

            // Closes the loop: the scene service is inside the graph it hands to installers.
            scenes.Initialise(_context);

            await save.LoadAsync();
            settings.ApplyLoaded();

            if (_saveLifecycle != null)
            {
                _saveLifecycle.Initialise(save);
            }
            else
            {
                Debug.LogError(
                    "[Bootstrap] No save lifecycle handler is assigned. Progress will not be flushed " +
                    "when the application is backgrounded.");
            }

            analytics.Track("session_start", AnalyticsPayload.Empty);

            await scenes.TransitionToAsync(ResolveFirstScene());
        }

        /// <summary>
        /// The scene to open once start-up finishes. Always the hub in a build; in the Editor it is
        /// whichever scene the developer pressed Play from, so iterating on a scene does not mean
        /// routing through the hub every time.
        /// </summary>
        private static SceneId ResolveFirstScene()
        {
#if UNITY_EDITOR
            var requested = UnityEditor.SessionState.GetString(
                SceneService.EditorStartSceneKey, string.Empty);

            if (requested == SceneService.NameOf(SceneId.Match))
            {
                return SceneId.Match;
            }
#endif
            return SceneId.Hub;
        }

        private void ConfigureApplication()
        {
            Application.targetFrameRate = _targetFrameRate;
            Screen.sleepTimeout = _allowScreenSleep ? SleepTimeout.SystemSetting : SleepTimeout.NeverSleep;
            QualitySettings.vSyncCount = 0;
        }

        private static ISaveRepository CreateSaveRepository()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // The browser has no dependable filesystem: persistentDataPath maps to a store that
            // needs an explicit flush and cannot recover a partial write.
            return new PlayerPrefsSaveRepository();
#else
            return new FileSaveRepository(Application.persistentDataPath);
#endif
        }
    }
}
