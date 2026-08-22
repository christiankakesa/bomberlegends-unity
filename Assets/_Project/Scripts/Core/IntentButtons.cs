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

        /// <summary>Use the skill in the first loadout slot.</summary>
        Skill1 = 1 << 1,

        /// <summary>Use the skill in the second loadout slot.</summary>
        Skill2 = 1 << 2,

        /// <summary>Use the skill in the third loadout slot.</summary>
        Skill3 = 1 << 3,

        /// <summary>
        /// The aim carried by this intent was drawn for the skill being cast this tick.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Not a button. It is the one thing the aim bytes cannot say on their own: <b>who the aim
        /// belongs to.</b> On touch each skill button is its own stick, so a drag on the dash button
        /// is a direction meant for the dash. On a pad the right stick is a standing aim that
        /// belongs to nothing in particular — it is where you are pointing, not where you asked to
        /// go — and a dash that followed it would leap into the enemy you were lining up.
        /// </para>
        /// <para>
        /// Set only by a device whose aim gesture is attached to a specific button, which today
        /// means touch alone. Never by the pad, and never by the mouse.
        /// </para>
        /// </remarks>
        AimedCast = 1 << 4
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
