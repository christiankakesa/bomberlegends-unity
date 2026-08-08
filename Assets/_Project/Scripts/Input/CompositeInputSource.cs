using System;
using BomberLegends.Simulation;

namespace BomberLegends.Input
{
    /// <summary>
    /// Combines several control surfaces, using whichever one the player is actually touching.
    /// </summary>
    /// <remarks>
    /// Lets a developer pick up a keyboard, a controller or the on-screen stick interchangeably
    /// without a settings change or a restart, which matters a great deal while feel is being tuned.
    /// Sources are polled in order and the first that reports any activity wins.
    /// </remarks>
    public sealed class CompositeInputSource : IInputSource
    {
        private readonly IInputSource[] _sources;
        private readonly ControlSchemeTracker _tracker;

        /// <summary>Creates a composite over the given sources, in priority order.</summary>
        /// <exception cref="ArgumentException">No sources were supplied.</exception>
        public CompositeInputSource(params IInputSource[] sources)
            : this(null, sources)
        {
        }

        /// <summary>Creates a composite using an existing tracker.</summary>
        /// <exception cref="ArgumentException">No sources were supplied.</exception>
        public CompositeInputSource(ControlSchemeTracker? tracker, params IInputSource[] sources)
        {
            if (sources == null || sources.Length == 0)
            {
                throw new ArgumentException("A composite needs at least one source.", nameof(sources));
            }

            _sources = sources;
            _tracker = tracker ?? new ControlSchemeTracker();
        }

        /// <inheritdoc />
        public ControlScheme Scheme => _tracker.Current;

        /// <summary>Which device family is currently in charge.</summary>
        /// <remarks>
        /// Exposed for on-screen button prompts and cursor visibility, which have to agree with
        /// whatever is actually driving the game.
        /// </remarks>
        public ControlSchemeTracker Devices => _tracker;

        /// <inheritdoc />
        /// <remarks>
        /// <para>
        /// One device is in charge at a time, and it is whichever the player last deliberately
        /// used. Merging every source instead — which this used to do — meant a mouse resting
        /// anywhere on screen supplied an aim direction on every single frame, so a gamepad's right
        /// stick could never be heard. Aim followed the pointer even with both hands on a pad.
        /// </para>
        /// <para>
        /// Handing the whole tick to one device also keeps movement and aim consistent with each
        /// other. Reading the stick for one and the pointer for the other produces a character who
        /// runs where the pad says and shoots where the mouse happens to be.
        /// </para>
        /// </remarks>
        public PlayerIntent Sample(int tick)
        {
            _tracker.Poll();

            for (var i = 0; i < _sources.Length; i++)
            {
                if (_sources[i].Scheme == _tracker.Current)
                {
                    return _sources[i].Sample(tick);
                }
            }

            // The active family has no source here — a pad recognised before one was supplied, say.
            // Falling back to whatever is actually being touched beats going unresponsive.
            for (var i = 0; i < _sources.Length; i++)
            {
                var intent = _sources[i].Sample(tick);

                if (intent.MoveX != 0 || intent.MoveY != 0 || intent.Buttons != Core.IntentButtons.None)
                {
                    return intent;
                }
            }

            return PlayerIntent.None;
        }
    }
}
