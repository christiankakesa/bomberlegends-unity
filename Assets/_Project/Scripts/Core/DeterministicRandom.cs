using System;

namespace BomberLegends.Core
{
    /// <summary>
    /// A deterministic pseudo-random number generator (xorshift32).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every random decision the simulation makes — coin drops, spawn selection, tie-breaks — must
    /// come from an instance of this type seeded from the match seed. Using a shared or
    /// platform-provided generator would break replays, determinism tests and any future
    /// server-side replay validation.
    /// </para>
    /// <para>
    /// This is a <b>mutable</b> struct: drawing a number advances <see cref="State"/>. Store it by
    /// value inside the simulation state and pass it by <see langword="ref"/>. Passing it by value
    /// produces an independent copy, which is almost never what the caller intends.
    /// </para>
    /// </remarks>
    public struct DeterministicRandom : IEquatable<DeterministicRandom>
    {
        /// <summary>
        /// Substituted when a zero seed is supplied. Xorshift is degenerate at zero: it would emit
        /// nothing but zeroes forever.
        /// </summary>
        private const uint FallbackSeed = 0x6D2B79F5u;

        private uint _state;

        /// <summary>
        /// Creates a generator from <paramref name="seed"/>. A zero seed is replaced by a fixed
        /// non-zero constant, so a zero seed is reproducible rather than degenerate.
        /// </summary>
        public DeterministicRandom(uint seed)
        {
            _state = seed == 0u ? FallbackSeed : seed;
        }

        /// <summary>
        /// The current internal state. Exposed so it can be included in a simulation state hash and
        /// written to a save or replay.
        /// </summary>
        public uint State => _state;

        /// <summary>Returns the next value in the sequence, uniformly distributed over the full range.</summary>
        public uint NextUInt()
        {
            unchecked
            {
                var x = _state;
                x ^= x << 13;
                x ^= x >> 17;
                x ^= x << 5;
                _state = x;
                return x;
            }
        }

        /// <summary>
        /// Returns a value in <c>[0, exclusiveMax)</c>, free of modulo bias.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="exclusiveMax"/> is not positive.</exception>
        public int NextInt(int exclusiveMax)
        {
            if (exclusiveMax <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(exclusiveMax), exclusiveMax, "Upper bound must be positive.");
            }

            var bound = (uint)exclusiveMax;

            // Reject the unevenly covered tail of the 32-bit range so every outcome is equally
            // likely. Biased draws would quietly skew drop rates and spawn selection.
            var threshold = unchecked(((uint.MaxValue % bound) + 1u) % bound);

            uint draw;
            do
            {
                draw = NextUInt();
            }
            while (draw < threshold);

            return (int)(draw % bound);
        }

        /// <summary>
        /// Returns a value in <c>[inclusiveMin, exclusiveMax)</c>, free of modulo bias.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="exclusiveMax"/> is not greater than <paramref name="inclusiveMin"/>.
        /// </exception>
        public int NextInt(int inclusiveMin, int exclusiveMax)
        {
            if (exclusiveMax <= inclusiveMin)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(exclusiveMax), exclusiveMax, "Upper bound must exceed the lower bound.");
            }

            return inclusiveMin + NextInt(exclusiveMax - inclusiveMin);
        }

        /// <summary>Returns <see langword="true"/> or <see langword="false"/> with equal probability.</summary>
        public bool NextBool() => (NextUInt() & 1u) == 1u;

        /// <summary>
        /// Returns <see langword="true"/> with the given percentage chance.
        /// A chance of zero or less never succeeds; one hundred or more always succeeds.
        /// </summary>
        public bool Chance(int percent)
        {
            if (percent <= 0)
            {
                return false;
            }

            return percent >= 100 || NextInt(100) < percent;
        }

        /// <inheritdoc />
        public bool Equals(DeterministicRandom other) => _state == other._state;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is DeterministicRandom other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => (int)_state;

        /// <inheritdoc />
        public override string ToString() => $"Random(0x{_state:X8})";
    }
}
