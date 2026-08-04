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
        public PlayerIntent Sample(int tick)
        {
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
