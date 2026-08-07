using BomberLegends.Core;
using BomberLegends.Gameplay.Camera;
using BomberLegends.Gameplay.Player;
using BomberLegends.Input;
using BomberLegends.Simulation;
using UnityEngine;

namespace BomberLegends.Gameplay.Match
{
    /// <summary>
    /// Drives the simulation at a fixed rate and renders it at the display rate.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The simulation advances in fixed steps from an explicit accumulator rather than from
    /// <c>FixedUpdate</c>. <c>FixedUpdate</c> belongs to the physics system, its interval can be
    /// changed by anything in the project, and it is not driven at all when the timescale is zero —
    /// none of which are acceptable for the authoritative rules of a match.
    /// </para>
    /// <para>
    /// A frame that takes far longer than a tick — a hitch, a breakpoint, the app resuming from the
    /// background — would otherwise demand a burst of catch-up ticks, which takes even longer, which
    /// demands more. <see cref="MaxCatchUpTicks"/> caps the burst and the remaining backlog is
    /// discarded: the match runs briefly in slow motion rather than freezing outright.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class MatchRunner : MonoBehaviour
    {
        /// <summary>Most simulation ticks allowed in a single frame.</summary>
        public const int MaxCatchUpTicks = 5;

        private const double TickDuration = 1.0 / SimulationConstants.TicksPerSecond;

        private GameSimulation? _simulation;
        private IInputSource? _input;
        private PlayerView? _playerView;
        private MatchViewSynchroniser? _views;
        private MatchCameraRig? _cameraRig;

        private FixedStepAccumulator _accumulator = new FixedStepAccumulator(TickDuration);
        private SubTilePoint _previousPlayerPosition;
        private SubTilePoint _currentPlayerPosition;
        private bool _reportedDroppedEvents;

        /// <summary>The running simulation, or null before a match starts.</summary>
        public GameSimulation? Simulation => _simulation;

        /// <summary>How far through the current tick the last rendered frame was, from zero to one.</summary>
        public float InterpolationAlpha { get; private set; }

        /// <summary>How many ticks the most recent frame advanced.</summary>
        public int TicksLastFrame { get; private set; }

        /// <summary>How many ticks have been discarded because the frame budget was exceeded.</summary>
        public int DiscardedTicks { get; private set; }

        /// <summary>Starts driving a match.</summary>
        public void Begin(
            GameSimulation simulation,
            IInputSource input,
            PlayerView playerView,
            MatchViewSynchroniser? views = null,
            MatchCameraRig? cameraRig = null)
        {
            _cameraRig = cameraRig;
            _simulation = simulation;
            _input = input;
            _playerView = playerView;
            _views = views;

            _accumulator.Reset();
            _previousPlayerPosition = simulation.State.Player.Position;
            _currentPlayerPosition = _previousPlayerPosition;
            InterpolationAlpha = 0f;

            _playerView.Render(_previousPlayerPosition, _currentPlayerPosition, 0f);
        }

        /// <summary>Stops driving the match. The simulation is left untouched.</summary>
        public void Stop()
        {
            _simulation = null;
            _views?.Stop();
        }

        private void Update()
        {
            if (_simulation == null || _input == null || _playerView == null)
            {
                return;
            }

            TicksLastFrame = _accumulator.Advance(Time.deltaTime, MaxCatchUpTicks, out var discarded);
            DiscardedTicks += discarded;

            for (var i = 0; i < TicksLastFrame; i++)
            {
                _previousPlayerPosition = _simulation.State.Player.Position;
                _views?.BeforeTick(_simulation);

                _simulation.Tick(_input.Sample(_simulation.CurrentTick));

                _currentPlayerPosition = _simulation.State.Player.Position;

                // Events are consumed inside the tick loop: they last exactly one tick, and a frame
                // that runs several ticks would otherwise see only the last one's effects.
                _views?.Consume(_simulation);
                DrainEvents();
            }

            InterpolationAlpha = _accumulator.Alpha;
            _playerView.Render(_previousPlayerPosition, _currentPlayerPosition, InterpolationAlpha);
            _views?.Render(_simulation, Time.deltaTime, InterpolationAlpha);
        }

        private void LateUpdate()
        {
            // After the player has been placed for this frame, never before: following in Update
            // leaves the camera a frame behind, which reads as a judder that is hard to trace later.
            if (_simulation != null && _playerView != null)
            {
                _cameraRig?.Follow(_playerView.WorldPosition, Time.deltaTime);
            }
        }

        private void DrainEvents()
        {
            if (_simulation == null)
            {
                return;
            }

            var events = _simulation.Events;

            // Blocks being destroyed, effects and sounds hook in here from Milestone 2. For now the
            // only thing worth acting on is the buffer overflowing, which would silently swallow
            // those reactions later if it went unnoticed.
            if (events.DroppedCount > 0 && !_reportedDroppedEvents)
            {
                _reportedDroppedEvents = true;
                Debug.LogError(
                    $"[Match] The simulation produced more events than the buffer holds " +
                    $"({events.Capacity}). Raise the capacity; effects are being dropped.");
                events.ResetDroppedCount();
            }
        }
    }
}
