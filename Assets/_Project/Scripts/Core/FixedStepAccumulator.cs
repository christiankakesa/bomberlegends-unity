using System;

namespace BomberLegends.Core
{
    /// <summary>
    /// Converts variable frame times into a fixed number of simulation steps.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Kept as plain arithmetic with no engine types so the pacing that drives the whole game can be
    /// tested against exact frame times rather than only observed at runtime.
    /// </para>
    /// <para>
    /// The step cap matters more than it looks. A frame that stalls — a hitch, a breakpoint, the app
    /// resuming from the background — leaves a backlog that would take longer to work through than
    /// the frame it is owed to, producing an ever-growing debt and an apparent freeze. Capping the
    /// burst and discarding the rest trades a moment of slow motion for a game that keeps running.
    /// </para>
    /// </remarks>
    public struct FixedStepAccumulator
    {
        private readonly double _stepDuration;
        private double _accumulated;

        /// <summary>Creates an accumulator for steps of the given length in seconds.</summary>
        /// <exception cref="ArgumentOutOfRangeException">The duration is not positive.</exception>
        public FixedStepAccumulator(double stepDuration)
        {
            if (stepDuration <= 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(stepDuration), stepDuration, "Step duration must be positive.");
            }

            _stepDuration = stepDuration;
            _accumulated = 0d;
        }

        /// <summary>
        /// How far into the next step the accumulator sits, from zero up to but never reaching one.
        /// Used to interpolate the view between the last two steps.
        /// </summary>
        public readonly float Alpha => (float)(_accumulated / _stepDuration);

        /// <summary>
        /// Adds a frame's elapsed time and reports how many whole steps are owed.
        /// </summary>
        /// <param name="deltaSeconds">Seconds since the previous frame. Negative values are ignored.</param>
        /// <param name="maxSteps">Most steps allowed in one frame.</param>
        /// <param name="discardedSteps">Steps dropped because the cap was reached.</param>
        /// <returns>How many steps to run this frame.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxSteps"/> is not positive.</exception>
        public int Advance(double deltaSeconds, int maxSteps, out int discardedSteps)
        {
            if (maxSteps <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxSteps), maxSteps, "At least one step per frame must be allowed.");
            }

            discardedSteps = 0;

            if (deltaSeconds > 0d)
            {
                _accumulated += deltaSeconds;
            }

            var steps = 0;
            while (_accumulated >= _stepDuration && steps < maxSteps)
            {
                _accumulated -= _stepDuration;
                steps++;
            }

            if (_accumulated >= _stepDuration)
            {
                discardedSteps = (int)(_accumulated / _stepDuration);
                _accumulated %= _stepDuration;
            }

            return steps;
        }

        /// <summary>Drops any partial progress, so the next step starts from a clean boundary.</summary>
        public void Reset() => _accumulated = 0d;
    }
}
