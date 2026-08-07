using System;
using BomberLegends.Core;

namespace BomberLegends.Simulation.Skills
{
    /// <summary>
    /// The three active skills a player carries.
    /// </summary>
    /// <remarks>
    /// Three, hard, because the concept rests on it: a small enough loadout that every choice is
    /// felt and every item has to compete for a slot. Making this growable would quietly turn
    /// buildcrafting into accumulation.
    /// </remarks>
    public struct SkillLoadout
    {
        /// <summary>How many active skills a player may carry.</summary>
        public const int SlotCount = 3;

        private readonly SkillSlot[] _slots;

        private SkillLoadout(SkillSlot[] slots) => _slots = slots;

        /// <summary>Reads or writes a slot.</summary>
        /// <exception cref="IndexOutOfRangeException">The index is not a valid slot.</exception>
        public SkillSlot this[int index]
        {
            readonly get => _slots[index];
            set => _slots[index] = value;
        }

        /// <summary>Whether this loadout has been created.</summary>
        public readonly bool IsCreated => _slots != null;

        /// <summary>Creates an empty loadout.</summary>
        public static SkillLoadout Empty() => new SkillLoadout(new SkillSlot[SlotCount]);

        /// <summary>Creates a loadout holding the given skills, in order.</summary>
        /// <exception cref="ArgumentException">More skills were given than there are slots.</exception>
        public static SkillLoadout Of(params SkillSlot[] skills)
        {
            if (skills == null)
            {
                throw new ArgumentNullException(nameof(skills));
            }

            if (skills.Length > SlotCount)
            {
                throw new ArgumentException(
                    $"A loadout holds at most {SlotCount} active skills but {skills.Length} were given.",
                    nameof(skills));
            }

            var slots = new SkillSlot[SlotCount];
            Array.Copy(skills, slots, skills.Length);
            return new SkillLoadout(slots);
        }

        /// <summary>The button that triggers a slot.</summary>
        /// <exception cref="ArgumentOutOfRangeException">The index is not a valid slot.</exception>
        public static IntentButtons ButtonFor(int index) => index switch
        {
            0 => IntentButtons.Skill1,
            1 => IntentButtons.Skill2,
            2 => IntentButtons.Skill3,
            _ => throw new ArgumentOutOfRangeException(
                nameof(index), index, $"A loadout has {SlotCount} slots.")
        };

        /// <summary>The first slot holding the given skill, or <c>-1</c>.</summary>
        public readonly int IndexOf(SkillId id)
        {
            for (var i = 0; i < SlotCount; i++)
            {
                if (_slots[i].Id == id)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
