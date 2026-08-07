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
        [Tooltip("Seed for every random decision the match makes. A fixed value makes runs repeatable.")]
        private uint _seed = 1u;

        [Header("Loadout")]
        [SerializeField]
        [Tooltip(
            "Items the player starts with. Two slots by default. Change these between runs to feel " +
            "whether one item visibly changes how the game plays — that is the question the slice asks.")]
        private ItemId[] _startingItems = System.Array.Empty<ItemId>();

        private GameContext? _context;

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

            if (!TryParseLevel(out var layout))
            {
                return;
            }

            var projector = new BoardProjector(_tileSize, _blockHeight);
            var config = SimulationConfig.FromTilesPerSecond(_moveSpeedTilesPerSecond);
            var simulation = new GameSimulation(config, layout, _seed);

            GrantStartingItems(simulation);

            _boardRenderer.Build(simulation.State.Board, projector);
            _playerView.Initialise(projector);

            var spawn = simulation.State.Player.Position;
            _playerView.Render(spawn, spawn, 0f);

            if (_cameraRig != null)
            {
                _cameraRig.Begin(layout.Width, layout.Height, projector, _playerView.WorldPosition);
            }
            else
            {
                Debug.LogWarning("[Match] No camera rig assigned; the arena will not be framed.");
            }

            if (_views != null)
            {
                _views.Begin(_boardRenderer, projector, config);
                _views.SpawnEnemies(simulation);
            }

            _runner.Begin(
                simulation, CreateInputSource(projector), _playerView, _views, _cameraRig);
        }

        private void OnDestroy()
        {
            if (_quitButton != null)
            {
                _quitButton.onClick.RemoveListener(ReturnToHub);
            }
        }

        /// <summary>
        /// Hands the player their starting build.
        /// </summary>
        /// <remarks>
        /// Milestone 6 replaces this with a choice between arenas. Granting from the Inspector until
        /// then is what lets the slice's real question — does swapping one item change how the game
        /// plays — be answered before a run loop exists.
        /// </remarks>
        private void GrantStartingItems(GameSimulation simulation)
        {
            for (var i = 0; i < _startingItems.Length; i++)
            {
                var id = _startingItems[i];

                if (id != ItemId.None && !simulation.TryGrantItem(id))
                {
                    Debug.LogWarning(
                        $"[Match] {ItemCatalog.Name(id)} was not granted; it is either a duplicate " +
                        "or the item slots are full.");
                }
            }
        }

        private bool TryParseLevel(out LevelLayout layout)
        {
            layout = default;

            var rows = _levelLayout
                .Replace("\r", string.Empty)
                .Split('\n', System.StringSplitOptions.RemoveEmptyEntries);

            try
            {
                layout = LevelLayout.Parse(rows);
                return true;
            }
            catch (System.ArgumentException exception)
            {
                Debug.LogError($"[Match] The level layout is invalid: {exception.Message}");
                return false;
            }
        }

        private IInputSource CreateInputSource(IGridProjection projection)
        {
            // A developer can pick up whichever control surface is to hand without changing a
            // setting, which matters a great deal while feel is being tuned.
            var keyboard = new KeyboardInputSource(CreateAimSource());

            if (_joystick != null && _inputFeel != null)
            {
                return new CompositeInputSource(
                    keyboard,
                    new GamepadInputSource(),
                    _bombButton != null
                        ? new TouchInputSource(_joystick, _inputFeel, projection, _bombButton)
                        : new TouchInputSource(_joystick, _inputFeel, projection));
            }

            if (_joystick != null)
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
