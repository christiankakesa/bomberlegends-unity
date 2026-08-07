using System;

namespace BomberLegends.Simulation.Skills
{
    /// <summary>
    /// Behaviours an item can graft onto a skill.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Generic across skills, exactly as <see cref="SkillSlot"/>'s numbers are. A trait describes
    /// <i>what happens on contact</i>, not which skill is doing the touching, so the same flag reads
    /// sensibly on a dash and on a projectile without either knowing the other exists.
    /// </para>
    /// <para>
    /// This is where synergy comes from. Two items that each add a trait produce a third behaviour
    /// nobody wrote down, which is the whole reason there is no table of item pairs anywhere in this
    /// codebase — such a table grows as the square of the item count and is the standard way this
    /// kind of system dies.
    /// </para>
    /// </remarks>
    [Flags]
    public enum SkillTraits : byte
    {
        /// <summary>Plain behaviour.</summary>
        None = 0,

        /// <summary>Bombs the skill touches go off immediately.</summary>
        DetonatesBombs = 1 << 0,

        /// <summary>Enemies the skill touches take its <see cref="SkillSlot.Power"/> as damage.</summary>
        DamagesContacts = 1 << 1
    }

    /// <summary>Allocation-free helpers for <see cref="SkillTraits"/>.</summary>
    public static class SkillTraitsExtensions
    {
        /// <summary>Returns <see langword="true"/> when any bit of <paramref name="flag"/> is set.</summary>
        /// <remarks>
        /// Used instead of <see cref="Enum.HasFlag"/>, which boxes both operands and is banned from
        /// per-tick code.
        /// </remarks>
        public static bool Has(this SkillTraits value, SkillTraits flag) => (value & flag) != 0;
    }
}
