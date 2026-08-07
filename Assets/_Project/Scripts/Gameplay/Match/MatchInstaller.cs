using BomberLegends.Data.Balance;
using BomberLegends.Gameplay.Board;
using BomberLegends.Gameplay.Camera;
using BomberLegends.Gameplay.Player;
using BomberLegends.Input;
using BomberLegends.Services;
using BomberLegends.Services.Scenes;
using BomberLegends.Simulation;
using BomberLegends.Simulation.Board;
using BomberLegends.Simulation.Items;
using BomberLegends.Simulation.Run;
using BomberLegends.Gameplay.Run;
using UnityEngine;
using UnityEngine.UI;

namespace BomberLegends.Gameplay.Match
{
    /// <summary>
    /// Builds a match: reads the level, starts the simulation, draws the board and connects input.
    /// </summary>
    /// <remarks>
    /// The level is authored here as text for Milestone 1 so movement can be tried against many
    /// shapes without any tooling existing yet. T-025 replaces this with a level asset, at which
    /// point this reads the asset and nothing else changes.
    /// </remarks>
    public sealed class MatchInstaller : SceneInstaller
    {
        [Header("Scene references")]
        [SerializeField]
        [Tooltip("Drives the simulation and the view.")]
        private MatchRunner? _runner;

        [SerializeField]
        [Tooltip("Draws the tile grid.")]
        private BoardRenderer? _boardRenderer;

        [SerializeField]
        [Tooltip("Draws the player.")]
        private PlayerView? _playerView;

        [SerializeField]
        [Tooltip("Frames the board so it fits whatever screen the game is running on.")]
        private MatchCameraRig? _cameraRig;

        [SerializeField]
        [Tooltip("Turns simulation events into bombs, blasts and debris.")]
        private MatchViewSynchroniser? _views;

        [SerializeField]
        [Tooltip("On-screen thumbstick. Optional: keyboard and gamepad work without it.")]
        private VirtualJoystick? _joystick;

        [SerializeField]
        [Tooltip("On-screen bomb button. Optional; space bar and gamepad also place bombs.")]
        private ActionButton? _bombButton;

        [SerializeField]
        [Tooltip("Abandons the match and returns to the hub.")]
        private Button? _quitButton;

        [Header("Configuration")]
        [SerializeField]
        [Tooltip(
            "When the on-screen stick and bomb button appear. Auto shows them only where there is " +
            "a touchscreen, so a desktop build is not cluttered with controls it cannot use.")]
        private TouchControlMode _touchControls = TouchControlMode.Auto;

        [SerializeField]
        [Tooltip("Stick handling. Tuned on device; see T-015.")]
        private InputFeelConfig? _inputFeel;

        [SerializeField, Range(1f, 12f)]
        [Tooltip("Player speed in tiles per second.")]
        private float _moveSpeedTilesPerSecond = 4f;

        [Header("View")]
        [SerializeField, Range(0.5f, 2f)]
        [Tooltip("World units across one tile.")]
        private float _tileSize = 1f;

        [SerializeField, Range(0.3f, 2.5f)]
        [Tooltip("How tall a standing block is, in world units.")]
        private float _blockHeight = 1f;

        [Header("Level")]
        [SerializeField]
        [Tooltip("'#' solid, 'X' destructible, '.' floor, 'P' spawn. The first row is the top.")]
        [TextArea(6, 20)]
        private string _levelLayout =
            "#########################\n" +
            "#P...X...X...X...X...X..#\n" +
            "#.#.#.#.#.#.#.#.#.#.#.#.#\n" +
            "#..X...XE..X...X...X...X#\n" +
            "#.#.#.#.#.#.#.#.#.#.#.#.#\n" +
            "#X...X...X...X..EX...X..#\n" +
            "#.#.#.#.#.#.#.#.#.#.#.#.#\n" +
            "#..X...X...X...X...X...X#\n" +
            "#.#.#.#.#.#.#.#.#.#.#.#.#\n" +
            "#X...XE..X...X...X...X..#\n" +
            "#.#.#.#.#.#.#.#.#.#.#.#.#\n" +
            "#..X...X...X...X..EX...X#\n" +
            "#.#.#.#.#.#.#.#.#.#.#.#.#\n" +
            "#....X...X...X...X...X..#\n" +
            "#.#.#.#.#.#.#.#.#.#.#.#.#\n" +
            "#......X...XE..X...X...X#\n" +
            "#########################";

        [SerializeField]
        [Tooltip(
            "Further arenas, in order. A run cycles through these after the layout above. Same " +
            "glyphs; leave empty to repeat the first arena forever.")]
        [TextArea(6, 20)]
        private string[] _additionalArenas =
        {
            "#####################\n" +
            "#P...X...X...X...X..#\n" +
            "#.###.###.###.###.#.#\n" +
            "#..X...XE..X...X..X.#\n" +
            "#.#.#####.#####.#.#.#\n" +
            "#X...X...E...X...X..#\n" +
            "#.###.#.#####.#.###.#\n" +
            "#..X..#..EX..#..X...#\n" +
            "#.###.#.#####.#.###.#\n" +
            "#X...X...E...X...X..#\n" +
            "#.#.#####.#####.#.#.#\n" +
            "#..X...XE..X...X..X.#\n" +
            "#####################",

            "###########################\n" +
            "#P..X....X....X....X....E.#\n" +
            "#.#.#.##.#.##.#.##.#.##.#.#\n" +
            "#..X..X....X....X....X....#\n" +
            "#.##.###.####.####.###.##.#\n" +
            "#X....E.....X....E.....X..#\n" +
            "#.##.###.####.####.###.##.#\n" +
            "#..X..X....X....X....X....#\n" +
            "#.#.#.##.#.##.#.##.#.##.#.#\n" +
            "#E....X....X....X....X...E#\n" +
            "###########################"
        };

        [SerializeField]
        [Tooltip("Seed for every random decision the run makes. A fixed value makes runs repeatable.")]
        private uint _seed = 1u;

        [Header("Loadout")]
        [SerializeField]
        [Tooltip(
            "A build every attempt begins with. Leave empty to earn items by clearing arenas; fill " +
            "it to try a specific pairing without playing up to it. Survives a restart and occupies " +
            "real slots, so the run offers correspondingly fewer.")]
        private ItemId[] _startingItems = System.Array.Empty<ItemId>();

        private GameContext? _context;

        /// <summary>When the on-screen controls are shown.</summary>
        private enum TouchControlMode
        {
            /// <summary>Only where the device actually has a touchscreen.</summary>
            Auto = 0,

            /// <summary>Always, which is how they get tested from the Editor.</summary>
            AlwaysShow = 1,

            /// <summary>Never.</summary>
            AlwaysHide = 2
        }

        /// <inheritdoc />
        public override SceneId Scene => SceneId.Match;

        /// <inheritdoc />
        protected override void OnInstall(GameContext context, ISceneTransitionPayload? payload)
        {
            _context = context;

            if (_quitButton != null)
            {
                _quitButton.onClick.AddListener(ReturnToHub);
            }

            if (_runner == null || _boardRenderer == null || _playerView == null)
            {
                Debug.LogError("[Match] The scene is missing its runner, board renderer or player view.");
                return;
            }

            if (!TryParseArenas(out var arenas))
            {
                return;
            }

            ApplyTouchControlVisibility();

            var projector = new BoardProjector(_tileSize, _blockHeight);
            var config = SimulationConfig.FromTilesPerSecond(_moveSpeedTilesPerSecond);
            var run = new GameRun(config, arenas, _seed, _startingItems);

            _playerView.Initialise(projector);

            if (_cameraRig == null)
            {
                Debug.LogWarning("[Match] No camera rig assigned; the arena will not be framed.");
            }

            // Pools and materials are built once and survive every arena change, which is most of
            // why moving between arenas costs nothing.
            _views?.Begin(_boardRenderer, projector, config);

            var controller = _runner.gameObject.AddComponent<RunController>();

            controller.Begin(
                run,
                _runner,
                _boardRenderer,
                _playerView,
                projector,
                CreateInputSource(projector),
                _views,
                _cameraRig,
                CreateOverlay());
        }

        /// <summary>Builds the between-arena screen, or none when there is no canvas to host it.</summary>
        private RunOverlayView? CreateOverlay()
        {
            var canvas = _quitButton != null
                ? _quitButton.GetComponentInParent<Canvas>()
                : FindFirstObjectByType<Canvas>();

            if (canvas == null)
            {
                Debug.LogWarning(
                    "[Match] No canvas found, so the run overlay is disabled: arenas will chain " +
                    "without offering an item and death will not offer a restart.");
                return null;
            }

            var host = new GameObject("Run Overlay");
            host.transform.SetParent(canvas.transform, false);

            var overlay = host.AddComponent<RunOverlayView>();
            overlay.Build(canvas);

            return overlay;
        }

        private void OnDestroy()
        {
            if (_quitButton != null)
            {
                _quitButton.onClick.RemoveListener(ReturnToHub);
            }
        }

        /// <summary>Parses every authored arena, in the order a run visits them.</summary>
        private bool TryParseArenas(out LevelLayout[] arenas)
        {
            arenas = System.Array.Empty<LevelLayout>();

            var parsed = new System.Collections.Generic.List<LevelLayout>(_additionalArenas.Length + 1);

            if (!TryParseLayout(_levelLayout, 0, out var first))
            {
                return false;
            }

            parsed.Add(first);

            for (var i = 0; i < _additionalArenas.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(_additionalArenas[i]))
                {
                    continue;
                }

                // One bad arena must not cost the whole run. The rest still make a playable
                // sequence, and the error names which one to fix.
                if (TryParseLayout(_additionalArenas[i], i + 1, out var arena))
                {
                    parsed.Add(arena);
                }
            }

            arenas = parsed.ToArray();
            return true;
        }

        private static bool TryParseLayout(string text, int index, out LevelLayout layout)
        {
            layout = default;

            var rows = text
                .Replace("\r", string.Empty)
                .Split('\n', System.StringSplitOptions.RemoveEmptyEntries);

            try
            {
                layout = LevelLayout.Parse(rows);
                return true;
            }
            catch (System.ArgumentException exception)
            {
                Debug.LogError($"[Match] Arena {index + 1} is invalid: {exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// Shows the on-screen stick and bomb button only where they can actually be used.
        /// </summary>
        /// <remarks>
        /// A desktop build was drawing a thumbstick and a BOMB button over the arena, neither of
        /// which does anything with a mouse and keyboard. Touch presence is the honest test rather
        /// than the platform name, so a Windows tablet or a device plugged into the Editor still
        /// gets them.
        /// </remarks>
        private void ApplyTouchControlVisibility()
        {
            if (_joystick != null)
            {
                _joystick.gameObject.SetActive(ShowTouchControls);
            }

            if (_bombButton != null)
            {
                _bombButton.gameObject.SetActive(ShowTouchControls);
            }
        }

        private bool ShowTouchControls => _touchControls switch
        {
            TouchControlMode.AlwaysShow => true,
            TouchControlMode.AlwaysHide => false,
            _ => UnityEngine.InputSystem.Touchscreen.current != null || Application.isMobilePlatform
        };

        private IInputSource CreateInputSource(IGridProjection projection)
        {
            // A developer can pick up whichever control surface is to hand without changing a
            // setting, which matters a great deal while feel is being tuned.
            var keyboard = new KeyboardInputSource(CreateAimSource());

            // A hidden stick must not still be sampled, or an invisible control would keep feeding
            // the simulation whatever it was last left holding.
            if (_joystick != null && _inputFeel != null && ShowTouchControls)
            {
                return new CompositeInputSource(
                    keyboard,
                    new GamepadInputSource(),
                    _bombButton != null
                        ? new TouchInputSource(_joystick, _inputFeel, projection, _bombButton)
                        : new TouchInputSource(_joystick, _inputFeel, projection));
            }

            if (_joystick != null && _inputFeel == null && ShowTouchControls)
            {
                Debug.LogWarning(
                    "[Match] No input feel config is assigned, so the on-screen stick is disabled.");
            }

            return new CompositeInputSource(keyboard, new GamepadInputSource());
        }

        /// <summary>
        /// Builds the mouse aim source, or none when there is no camera to unproject through.
        /// </summary>
        /// <remarks>
        /// Absence is not an error. Without it the skillshot follows the direction of travel, which
        /// is exactly what a pad or a touch screen does anyway.
        /// </remarks>
        private IAimSource? CreateAimSource()
        {
            var camera = _cameraRig != null ? _cameraRig.Camera : UnityEngine.Camera.main;

            return camera != null && _playerView != null
                ? new PointerAimSource(camera, _playerView)
                : null;
        }

        private async void ReturnToHub()
        {
            if (_context == null)
            {
                return;
            }

            _runner?.Stop();
            await _context.Scenes.TransitionToAsync(SceneId.Hub);
        }
    }
}
