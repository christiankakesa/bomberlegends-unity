using System;

namespace BomberLegends.Core
{
    /// <summary>
    /// A simulation tick count.
    /// </summary>
    /// <remarks>
    /// The simulation advances in fixed steps, so every gameplay duration — fuses, cooldowns,
    /// blast lifetimes, level timers — is stored in ticks rather than seconds. Wrapping the count in
    /// a type prevents it being confused with tile counts or frame counts, both of which are also
    /// plain integers. This type is deliberately unaware of the tick rate: conversions take the rate
    /// as an explicit argument so the rate lives in one place in the simulation layer.
    /// </remarks>
    public readonly struct Tick : IEquatable<Tick>, IComparable<Tick>
    {
        /// <summary>The number of ticks.</summary>
        public readonly int Value;

        /// <summary>Creates a tick count.</summary>
        public Tick(int value)
        {
            Value = value;
        }

        /// <summary>Tick zero, the first tick of a match.</summary>
        public static Tick Zero => new Tick(0);

        /// <summary>
        /// Converts a duration in seconds to the nearest whole number of ticks.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="seconds"/> is negative, or <paramref name="ticksPerSecond"/> is not positive.
        /// </exception>
        public static Tick FromSeconds(float seconds, int ticksPerSecond)
        {
            if (seconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "Duration must not be negative.");
            }

            if (ticksPerSecond <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(ticksPerSecond), ticksPerSecond, "Tick rate must be positive.");
            }

            return new Tick((int)Math.Round((double)seconds * ticksPerSecond, MidpointRounding.AwayFromZero));
        }

        /// <summary>Converts this tick count to seconds at the given rate.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="ticksPerSecond"/> is not positive.</exception>
        public float ToSeconds(int ticksPerSecond)
        {
            if (ticksPerSecond <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(ticksPerSecond), ticksPerSecond, "Tick rate must be positive.");
            }

            return (float)Value / ticksPerSecond;
        }

        /// <summary>Advances a tick count by a number of ticks.</summary>
        public static Tick operator +(Tick tick, int ticks) => new Tick(tick.Value + ticks);

        /// <summary>Rewinds a tick count by a number of ticks.</summary>
        public static Tick operator -(Tick tick, int ticks) => new Tick(tick.Value - ticks);

        /// <summary>Returns the number of ticks between two tick counts.</summary>
        public static int operator -(Tick left, Tick right) => left.Value - right.Value;

        /// <summary>Returns <see langword="true"/> when both tick counts are equal.</summary>
        public static bool operator ==(Tick left, Tick right) => left.Value == right.Value;

        /// <summary>Returns <see langword="true"/> when the tick counts differ.</summary>
        public static bool operator !=(Tick left, Tick right) => left.Value != right.Value;

        /// <summary>Returns <see langword="true"/> when <paramref name="left"/> is earlier.</summary>
        public static bool operator <(Tick left, Tick right) => left.Value < right.Value;

        /// <summary>Returns <see langword="true"/> when <paramref name="left"/> is later.</summary>
        public static bool operator >(Tick left, Tick right) => left.Value > right.Value;

        /// <summary>Returns <see langword="true"/> when <paramref name="left"/> is earlier or equal.</summary>
        public static bool operator <=(Tick left, Tick right) => left.Value <= right.Value;

        /// <summary>Returns <see langword="true"/> when <paramref name="left"/> is later or equal.</summary>
        public static bool operator >=(Tick left, Tick right) => left.Value >= right.Value;

        /// <inheritdoc />
        public bool Equals(Tick other) => Value == other.Value;

        /// <inheritdoc />
        public int CompareTo(Tick other) => Value.CompareTo(other.Value);

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is Tick other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => Value;

        /// <inheritdoc />
        public override string ToString() => $"t{Value}";
    }
}
