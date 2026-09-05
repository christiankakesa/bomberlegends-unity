using BomberLegends.Gameplay.Board;
using BomberLegends.Gameplay.Camera;
using BomberLegends.Gameplay.Match;
using BomberLegends.Gameplay.Player;
using BomberLegends.Input;
using BomberLegends.Services.Save;
using BomberLegends.Simulation;
using BomberLegends.Simulation.Run;
using UnityEngine;

namespace BomberLegends.Gameplay.Run
{
    /// <summary>
    /// Drives a run: shows the arena the run says is current, and rebuilds when it changes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every decision — cleared, choose, carry forward, died, restart — belongs to
    /// <see cref="GameRun"/>, which is engine-free and tested without a scene. This class only
    /// notices that <see cref="GameRun.Current"/> is a different object and rebuilds the view to
    /// match. That split is why the loop could be written and proven before any of this existed.
    /// </para>
    /// <para>
    /// A rebuild is deliberately in-place: no scene load, no asset load, no transition. The pools,
    /// materials and camera rig all survive, so restarting after a death costs about as much as
    /// walking through a door. Players who just died want to be playing again, not watching a
    /// loading bar.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class RunController : MonoBehaviour
    {
        private GameRun _run = null!;
        private MatchRunner _runner = null!;
        private BoardRenderer _board = null!;
        private PlayerView _playerView = null!;
        private BoardProjector _projector = null!;
        private MatchViewSynchroniser? _views;
        private MatchCameraRig? _camera;
        private RunOverlayView? _overlay;
        private ISaveService? _save;
        private RunStart? _start;
        private IInputSource _input = null!;

        private GameSimulation? _shown;
        private RunPhase _shownPhase = RunPhase.Fighting;
        private bool _started;

        /// <summary>Hands the controller everything it needs and shows the first arena.</summary>
        public void Begin(
            GameRun run,
            MatchRunner runner,
            BoardRenderer board,
            PlayerView playerView,
            BoardProjector projector,
            IInputSource input,
            MatchViewSynchroniser? views = null,
            MatchCameraRig? camera = null,
            RunOverlayView? overlay = null,
            ISaveService? save = null,
            RunStart? start = null)
        {
            _run = run;
            _runner = runner;
            _board = board;
            _playerView = playerView;
            _projector = projector;
            _input = input;
            _views = views;
            _camera = camera;
            _overlay = overlay;
            _save = save;
            _start = start;

            if (_overlay != null)
            {
                _overlay.Chosen += OnChosen;
                _overlay.Discarded += OnDiscarded;
                _overlay.Skipped += OnSkipped;
                _overlay.Restarted += OnRestart;
            }

            _started = true;
            Rebuild();
            ApplyPhase(force: true);
        }

        private void OnDestroy()
        {
            if (_overlay != null)
            {
                _overlay.Chosen -= OnChosen;
                _overlay.Discarded -= OnDiscarded;
                _overlay.Skipped -= OnSkipped;
                _overlay.Restarted -= OnRestart;
            }
        }

        private void LateUpdate()
        {
            if (!_started)
            {
                return;
            }

            _run.Observe();

            // A changed simulation is the one signal that matters. Every transition that starts a
            // new arena produces one, so nothing has to enumerate which transitions those are.
            if (!ReferenceEquals(_run.Current, _shown))
            {
                Rebuild();
            }

            ApplyPhase(force: false);
        }

        private void OnChosen(Simulation.Items.ItemId id)
        {
            if (_run.TryChoose(id))
            {
                // Choosing with a full inventory opens the discard step rather than advancing, so
                // the overlay has to be refreshed now rather than waiting for an arena change.
                ApplyPhase(force: true);
            }
        }

        private void OnDiscarded(Simulation.Items.ItemId id) => _run.TryDiscard(id);

        private void OnSkipped() => _run.Skip();

        /// <summary>
        /// Begins another attempt, on whatever seed and arena the start policy says.
        /// </summary>
        /// <remarks>
        /// Without a policy a restart replays the same seed from the first arena, which is what a
        /// player build does today and what a tuning session on one board wants.
        /// </remarks>
        private void OnRestart()
        {
            if (_start == null)
            {
                _run.Restart();
                return;
            }

            _run.Restart(_start.NextSeed(), _start.StartingArenaIndex);
        }

        private void ApplyPhase(bool force)
        {
            if (!force && _run.Phase == _shownPhase)
            {
                return;
            }

            _shownPhase = _run.Phase;

            if (_overlay == null)
            {
                return;
            }

            switch (_run.Phase)
            {
                case RunPhase.Choosing:
                    _overlay.ShowChoices(_run.Offers, _run.ArenaNumber);
                    break;

                case RunPhase.Discarding:
                    _overlay.ShowDiscard(_run.Held, _run.Pending);
                    break;

                case RunPhase.Ended:
                    RunPersistence.Clear(_save);
                    Flush();
                    _overlay.ShowEnded(_run.ArenaNumber);
                    break;

                default:
                    _overlay.Hide();
                    break;
            }
        }

        /// <summary>Points the whole view layer at whatever arena the run is on now.</summary>
        private void Rebuild()
        {
            var simulation = _run.Current;
            _shown = simulation;

            // Retire everything belonging to the arena being left, or its bombs and blasts would
            // hang in the air over the new one.
            _views?.Stop();

            _board.Build(simulation.State.Board, _projector);

            var spawn = simulation.State.Player.Position;
            _playerView.Render(spawn, spawn, 0f);

            _views?.SpawnEnemies(simulation);

            if (_views?.Hud != null)
            {
                _views.Hud.ArenaNumber = _run.ArenaNumber;
            }

            _camera?.Begin(
                simulation.State.Board.Width,
                simulation.State.Board.Height,
                _projector,
                _playerView.WorldPosition);

            _runner.Begin(simulation, _input, _playerView, _views, _camera);

            PersistRun();
        }

        /// <summary>
        /// Writes the run to storage, or forgets it once it has ended.
        /// </summary>
        /// <remarks>
        /// Written the moment an arena begins rather than left to the application quitting. A
        /// browser tab closing does not reliably deliver a quit callback, and a run lost to a stray
        /// refresh is a player who does not start another one.
        /// </remarks>
        private void PersistRun()
        {
            if (_save == null)
            {
                return;
            }

            if (_run.Phase == RunPhase.Ended)
            {
                RunPersistence.Clear(_save);
            }
            else
            {
                RunPersistence.Write(_save, _run.CreateSnapshot());
            }

            Flush();
        }

        /// <summary>Pushes the save to storage without blocking the frame it happened on.</summary>
        private async void Flush()
        {
            if (_save == null || !_save.IsDirty)
            {
                return;
            }

            try
            {
                await _save.SaveAsync();
            }
            catch (System.Exception exception)
            {
                // A failed save must never take the match down with it. The run continues; the
                // player simply cannot resume it later.
                Debug.LogError($"[Run] The run could not be saved: {exception.Message}");
            }
        }
    }
}
