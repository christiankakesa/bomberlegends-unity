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

        /// <summary>Creates a composite over the given sources, in priority order.</summary>
        /// <exception cref="ArgumentException">No sources were supplied.</exception>
        public CompositeInputSource(params IInputSource[] sources)
        {
            if (sources == null || sources.Length == 0)
            {
                throw new ArgumentException("A composite needs at least one source.", nameof(sources));
            }

            _sources = sources;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Aim is merged separately from movement and buttons. A player standing perfectly still
        /// while lining up a skillshot is the single most common thing they will do with it, and
        /// treating aim as activity would let a resting mouse pointer outrank a held gamepad.
        /// </remarks>
        public PlayerIntent Sample(int tick)
        {
            var active = PlayerIntent.None;
            var hasActive = false;
            sbyte aimX = 0;
            sbyte aimY = 0;

            for (var i = 0; i < _sources.Length; i++)
            {
                var intent = _sources[i].Sample(tick);

                if (!hasActive &&
                    (intent.MoveX != 0 || intent.MoveY != 0 || intent.Buttons != Core.IntentButtons.None))
                {
                    active = intent;
                    hasActive = true;
                }

                if (aimX == 0 && aimY == 0 && intent.HasAim)
                {
                    aimX = intent.AimX;
                    aimY = intent.AimY;
                }
            }

            return new PlayerIntent(active.MoveX, active.MoveY, active.Buttons, aimX, aimY);
        }
    }
}
