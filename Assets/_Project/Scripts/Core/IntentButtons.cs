using System;

namespace BomberLegends.Core
{
    /// <summary>
    /// The action buttons held during a single simulation tick.
    /// </summary>
    /// <remarks>
    /// Packed into one byte so a tick of player intent stays small enough to record for replays and,
    /// later, to send over the wire without further encoding.
    /// </remarks>
    [Flags]
    public enum IntentButtons : byte
    {
        /// <summary>No action requested.</summary>
        None = 0,

        /// <summary>Place a bomb.</summary>
        Bomb = 1 << 0,

        /// <summary>Use the equipped special ability.</summary>
        Special = 1 << 1,

        /// <summary>Spend the sprint charge.</summary>
        Sprint = 1 << 2
    }

    /// <summary>
    /// Allocation-free helpers for <see cref="IntentButtons"/>.
    /// </summary>
    public static class IntentButtonsExtensions
    {
        /// <summary>
        /// Returns <see langword="true"/> when any bit of <paramref name="flag"/> is set.
        /// </summary>
        /// <remarks>
        /// Used in place of <see cref="Enum.HasFlag"/>, which boxes both operands and is therefore
        /// banned from per-tick code.
        /// </remarks>
        public static bool Has(this IntentButtons value, IntentButtons flag) => (value & flag) != 0;

        /// <summary>Returns <paramref name="value"/> with <paramref name="flag"/> set.</summary>
        public static IntentButtons With(this IntentButtons value, IntentButtons flag) => value | flag;

        /// <summary>Returns <paramref name="value"/> with <paramref name="flag"/> cleared.</summary>
        public static IntentButtons Without(this IntentButtons value, IntentButtons flag) => value & ~flag;
    }
}
