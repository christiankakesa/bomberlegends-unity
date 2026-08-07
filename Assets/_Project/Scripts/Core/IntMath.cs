using System;

namespace BomberLegends.Core
{
    /// <summary>
    /// Integer maths the simulation needs, with results identical on every platform.
    /// </summary>
    /// <remarks>
    /// Continuous movement needs to normalise a stick vector, which needs a square root. The
    /// floating-point one is fast but its low bits are not guaranteed identical across CPUs, and a
    /// single differing bit in a velocity compounds into a divergent match. These are exact.
    /// </remarks>
    public static class IntMath
    {
        /// <summary>
        /// Returns the integer square root of <paramref name="value"/>, rounded towards zero.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The value is negative.</exception>
        public static int Sqrt(int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Cannot root a negative.");
            }

            if (value < 2)
            {
                return value;
            }

            // Newton's method from a power-of-two seed. Converges in a handful of iterations and
            // uses nothing but integer operations.
            var estimate = value;
            var next = (estimate + 1) / 2;

            while (next < estimate)
            {
                estimate = next;
                next = (estimate + (value / estimate)) / 2;
            }

            return estimate;
        }

        /// <summary>Returns the absolute value without branching into <see cref="Math"/>.</summary>
        public static int Abs(int value) => value < 0 ? -value : value;

        /// <summary>Clamps a value into an inclusive range.</summary>
        public static int Clamp(int value, int min, int max) =>
            value < min ? min : value > max ? max : value;
    }
}
