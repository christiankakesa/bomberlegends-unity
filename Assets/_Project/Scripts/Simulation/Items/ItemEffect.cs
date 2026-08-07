using BomberLegends.Simulation.Skills;

namespace BomberLegends.Simulation.Items
{
    /// <summary>
    /// What holding an item does to a skill.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Pure data, and deliberately generic: an effect names a target skill, some traits to graft on,
    /// and some numbers to shift. Nothing here knows what a dash or a skillshot is. That is what
    /// makes a new item a row in <see cref="ItemCatalog"/> rather than a new branch in a system, and
    /// what lets one item touch two skills without mentioning either.
    /// </para>
    /// <para>
    /// Percentages are applied to the slot's current value with integer arithmetic, so two items in
    /// either order give the same result on every platform.
    /// </para>
    /// </remarks>
    public readonly struct ItemEffect
    {
        /// <summary>Creates an effect.</summary>
        public ItemEffect(
            SkillId target = SkillId.None,
            SkillTraits addTraits = SkillTraits.None,
            int flatPower = 0,
            int magnitudePercent = 0,
            int cooldownPercent = 0,
            int durationPercent = 0,
            int bonusCharges = 0)
        {
            Target = target;
            AddTraits = addTraits;
            FlatPower = flatPower;
            MagnitudePercent = magnitudePercent;
            CooldownPercent = cooldownPercent;
            DurationPercent = durationPercent;
            BonusCharges = bonusCharges;
        }

        /// <summary>
        /// Which skill this changes, or <see cref="SkillId.None"/> for every equipped skill.
        /// </summary>
        public SkillId Target { get; }

        /// <summary>Behaviours to graft onto the target.</summary>
        public SkillTraits AddTraits { get; }

        /// <summary>
        /// Damage added outright.
        /// </summary>
        /// <remarks>
        /// Flat rather than proportional so an item can arm a skill that deals no damage at all. A
        /// percentage of zero is zero, which would make "the dash now hurts" impossible to express.
        /// </remarks>
        public int FlatPower { get; }

        /// <summary>Percent added to speed or distance.</summary>
        public int MagnitudePercent { get; }

        /// <summary>Percent added to recharge time. Negative shortens it.</summary>
        public int CooldownPercent { get; }

        /// <summary>Percent added to how long the effect lasts.</summary>
        public int DurationPercent { get; }

        /// <summary>Extra uses banked.</summary>
        public int BonusCharges { get; }

        /// <summary>Whether this effect applies to the given skill.</summary>
        public bool Targets(SkillId id) => Target == SkillId.None || Target == id;

        /// <summary>Returns the slot with this effect folded in.</summary>
        public SkillSlot ApplyTo(SkillSlot slot)
        {
            if (!slot.IsEquipped || !Targets(slot.Id))
            {
                return slot;
            }

            slot.Traits |= AddTraits;
            slot.Power += FlatPower;
            slot.Magnitude = Scale(slot.Magnitude, MagnitudePercent);
            slot.DurationTicks = Scale(slot.DurationTicks, DurationPercent);
            slot.CooldownTicks = Scale(slot.CooldownTicks, CooldownPercent);

            if (slot.CooldownTicks < 0)
            {
                slot.CooldownTicks = 0;
            }

            slot.MaxCharges += BonusCharges;
            slot.Charges += BonusCharges;

            return slot;
        }

        private static int Scale(int value, int percent) =>
            percent == 0 ? value : value + (value * percent / 100);
    }
}
