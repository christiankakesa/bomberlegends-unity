using BomberLegends.Data.Balance;
using BomberLegends.Gameplay.Board;
using BomberLegends.Gameplay.Camera;
using BomberLegends.Gameplay.Player;
using BomberLegends.Input;
using BomberLegends.Data.Audio;
using BomberLegends.Services;
using BomberLegends.Services.Scenes;
using BomberLegends.Simulation;
using BomberLegends.Simulation.Board;
using BomberLegends.Simulation.Items;
using BomberLegends.Simulation.Run;
using BomberLegends.Gameplay.Run;
using BomberLegends.Gameplay.Ui;
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
        [Tooltip("Opens the pause menu. Relabelled at run time; the menu offers resume and quit.")]
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

        [SerializeField, Range(0f, 1f)]
        [Tooltip(
            "How strongly the player is drawn to the middle of a corridor while running along it. " +
            "Applied only when the stick is near-aligned to an axis, so diagonals are untouched. " +
            "Zero disables it; raise it if a gamepad still catches on pillars.")]
        private float _laneAssist = 1f;

        [Header("Feedback")]
        [SerializeField]
        [Tooltip(
            "Binds simulation events to sound and camera shake. Leave empty to use generated " +
            "placeholder sounds, so the game is never silent by default.")]
        private FeedbackTable? _feedback;

        [Header("View")]
        [SerializeField, Range(0.5f, 2f)]
        [Tooltip("World units across one tile.")]
        private float _tileSize = 1f;

        [SerializeField, Range(0.3f, 2.5f)]
        [Tooltip("How tall a standing block is, in world units.")]
        private float _blockHeight = 1f;

        [Header("Level")]
        [SerializeField]
        [Tooltip(
            "Roll a fresh arena for every stage instead of cycling the authored layouts below. " +
            "The authored ones stay useful for tuning a specific board.")]
        private bool _generateArenas = true;

        [SerializeField, Range(15, 31)]
        [Tooltip("Width of the first generated arena. Grows as the run goes on.")]
        private int _arenaWidth = 21;

        [SerializeField, Range(11, 21)]
        [Tooltip("Height of the first generated arena. Grows as the run goes on.")]
        private int _arenaHeight = 15;

        [SerializeField, Range(0, 90)]
        [Tooltip("Chance a free tile becomes a destructible block.")]
        private int _destructiblePercent = 55;

        [SerializeField, Range(1, 12)]
        [Tooltip("Enemies in the first generated arena. One more is added per arena cleared.")]
        private int _startingEnemies = 4;

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
        private PauseController? _pause;

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

            // Nothing may stay selected during a match. On a pad, Submit and Bomb are the same
            // button, so a selected control would be clicked every time the player throws a bomb.
            UiFocus.Clear();

            var projector = new BoardProjector(_tileSize, _blockHeight);
            var config = SimulationConfig.FromTilesPerSecond(
                _moveSpeedTilesPerSecond, laneAssistStrength: _laneAssist);
            var run = new GameRun(config, CreateArenaSource(arenas), _seed, _startingItems);

            _playerView.Initialise(projector);

            if (_cameraRig == null)
            {
                Debug.LogWarning("[Match] No camera rig assigned; the arena will not be framed.");
            }

            // Pools and materials are built once and survive every arena change, which is most of
            // why moving between arenas costs nothing.
            _views?.Begin(_boardRenderer, projector, config);

            var overlay = CreateOverlay();
            var controller = _runner.gameObject.AddComponent<RunController>();

            controller.Begin(
                run,
                _runner,
                _boardRenderer,
                _playerView,
                projector,
                CreateInputSource(projector, run.Current.State.Player.Skills),
                _views,
                _cameraRig,
                overlay);

            InstallPauseMenu(overlay);
            InstallFeedback(context, projector);
        }

        /// <summary>
        /// Wires sound and camera shake to the simulation's events.
        /// </summary>
        /// <remarks>
        /// Falls back to generated placeholder sounds when no table is assigned. Silence is not a
        /// neutral default: a player who cannot hear that they were hurt reports that the controls
        /// killed them, which corrupts the measurement the slice exists to take.
        /// </remarks>
        private void InstallFeedback(GameContext context, BoardProjector projector)
        {
            if (_runner == null)
            {
                return;
            }

            var table = _feedback != null ? _feedback : PlaceholderFeedback.CreateTable();
            var feedback = _runner.gameObject.AddComponent<MatchFeedback>();

            feedback.Begin(context.Audio, table, projector, _cameraRig);

            _runner.Feedback = feedback;
        }

        /// <summary>
        /// Builds the pause menu and gives the on-screen button its real job.
        /// </summary>
        /// <remarks>
        /// The button used to abandon the match outright. It now opens a menu instead, which is what
        /// lets a pad leave a match at all: selection has to stay clear while playing, because
        /// Submit and Bomb are the same button on a pad, so no in-world control can be reachable.
        /// </remarks>
        private void InstallPauseMenu(RunOverlayView? overlay)
        {
            if (_runner == null)
            {
                return;
            }

            var canvas = ResolveCanvas();
            if (canvas == null)
            {
                Debug.LogWarning("[Match] No canvas found, so the match cannot be paused.");
                return;
            }

            var host = new GameObject("Pause Menu");
            host.transform.SetParent(canvas.transform, false);

            var menu = host.AddComponent<PauseMenuView>();
            menu.Build(canvas);

            var pause = _runner.gameObject.AddComponent<PauseController>();
            pause.Begin(_runner, menu, ReturnToHub, () => overlay != null && overlay.IsShowing);

            if (_quitButton == null)
            {
                return;
            }

            _quitButton.onClick.AddListener(OpenPause);

            ShrinkToPauseControl(_quitButton);

            _pause = pause;
        }

        /// <summary>
        /// Turns the shared menu-sized button into a compact in-match pause control.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The scene builds every button at the size the hub's PLAY needs, which is far too large
        /// sitting over the arena — it crowds the readout and eats play area on a phone.
        /// </para>
        /// <para>
        /// It keeps the word rather than a pause glyph. The greybox draws with Unity's built-in
        /// legacy font, which has no coverage for symbols like U+23F8, and a missing glyph renders
        /// as an empty box — worse than the word it replaced. An icon belongs with the UI pass,
        /// alongside a real font or sprite atlas.
        /// </para>
        /// </remarks>
        private static void ShrinkToPauseControl(Button button)
        {
            var rect = button.GetComponent<RectTransform>();

            if (rect != null)
            {
                rect.sizeDelta = new Vector2(170f, 84f);
            }

            var label = button.GetComponentInChildren<Text>();

            if (label == null)
            {
                return;
            }

            label.text = "PAUSE";
            label.fontSize = 26;

            var labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }

        private void OpenPause()
        {
            // Routed through the same key handling, so the button and the Start button cannot drift
            // into behaving differently.
            _pause?.TogglePause();
        }

        /// <summary>Builds the between-arena screen, or none when there is no canvas to host it.</summary>
        private Canvas? ResolveCanvas() => _quitButton != null
            ? _quitButton.GetComponentInParent<Canvas>()
            : FindFirstObjectByType<Canvas>();

        private RunOverlayView? CreateOverlay()
        {
            var canvas = ResolveCanvas();

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
                _quitButton.onClick.RemoveListener(OpenPause);
            }
        }

        /// <summary>
        /// Chooses between rolled arenas and the authored list.
        /// </summary>
        /// <remarks>
        /// The authored layouts are kept rather than deleted. Generated variety is what a run wants,
        /// but tuning anything — a blast radius, an enemy count, a corridor width — needs the same
        /// board twice, and a seed is a clumsier way to ask for that than a list.
        /// </remarks>
        private IArenaSource CreateArenaSource(LevelLayout[] authored)
        {
            if (!_generateArenas)
            {
                return new AuthoredArenaSource(authored);
            }

            return new GeneratedArenaSource(new ArenaSettings(
                baseWidth: _arenaWidth,
                baseHeight: _arenaHeight,
                destructiblePercent: _destructiblePercent,
                baseEnemies: _startingEnemies));
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

        private IInputSource CreateInputSource(
            IGridProjection projection, in Simulation.Skills.SkillLoadout loadout)
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
                    new TouchInputSource(
                        _joystick,
                        _inputFeel,
                        projection,
                        _bombButton != null ? new[] { _bombButton } : null,
                        BuildSkillButtons(loadout)));
            }

            if (_joystick != null && _inputFeel == null && ShowTouchControls)
            {
                Debug.LogWarning(
                    "[Match] No input feel config is assigned, so the on-screen stick is disabled.");
            }

            return new CompositeInputSource(keyboard, new GamepadInputSource());
        }

        /// <summary>
        /// Builds the on-screen skill cluster, when there is a bomb button to anchor it to.
        /// </summary>
        /// <remarks>
        /// Anchored to the bomb button so the whole right-hand cluster stays together whatever the
        /// screen size, rather than being positioned against a corner and drifting apart on a
        /// different aspect ratio.
        /// </remarks>
        private SkillTouchButton[]? BuildSkillButtons(in Simulation.Skills.SkillLoadout loadout)
        {
            if (_bombButton == null)
            {
                Debug.LogWarning(
                    "[Match] No bomb button to anchor the skill cluster to, so touch play has no " +
                    "way to use skills.");
                return null;
            }

            var anchor = _bombButton.GetComponent<RectTransform>();

            return anchor != null ? TouchControlsBuilder.Build(anchor, loadout) : null;
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
