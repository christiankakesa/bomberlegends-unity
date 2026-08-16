namespace BomberLegends.Simulation.Skills
{
    /// <summary>
    /// One equipped skill: what it does, how it is tuned, and how ready it is.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The tuning fields are deliberately generic. <see cref="Power"/>, <see cref="Magnitude"/> and
    /// <see cref="DurationTicks"/> mean different things per <see cref="SkillId"/>, and that is the
    /// point: an item that says "+40% magnitude" applies to a dash's speed and a skillshot's
    /// velocity without knowing which skill it landed on. The alternative — a bespoke config type
    /// per skill — forces every item to switch over every skill, which is the combinatorial
    /// explosion the item system exists to avoid.
    /// </para>
    /// <para>
    /// This lives in simulation state rather than configuration precisely because items rewrite it
    /// mid-run. Configuration only supplies the starting values.
    /// </para>
    /// </remarks>
    public struct SkillSlot
    {
        /// <summary>Which behaviour this slot runs.</summary>
        public SkillId Id;

        /// <summary>Ticks to recover one charge.</summary>
        public int CooldownTicks;

        /// <summary>Ticks left before the next charge returns. Zero when fully charged.</summary>
        public int CooldownRemaining;

        /// <summary>How many uses can be banked. One gives a plain cooldown.</summary>
        public int MaxCharges;

        /// <summary>How many uses are banked right now.</summary>
        public int Charges;

        /// <summary>Damage, for skills that deal any.</summary>
        public int Power;

        /// <summary>Speed in sub-tile units per tick, for skills that move something.</summary>
        public int Magnitude;

        /// <summary>How long the effect lasts. Multiplied by <see cref="Magnitude"/> this is range.</summary>
        public int DurationTicks;

        /// <summary>Behaviours items have grafted onto this skill.</summary>
        public SkillTraits Traits;

        /// <summary>Whether this slot's button was down last tick, so use triggers on the press.</summary>
        public bool HeldLastTick;

        /// <summary>Whether the slot holds a skill at all.</summary>
        public readonly bool IsEquipped => Id != SkillId.None;

        /// <summary>Whether it could be used right now.</summary>
        public readonly bool IsReady => IsEquipped && Charges > 0;

        /// <summary>How far the skill reaches, in sub-tile units.</summary>
        public readonly int Reach => Magnitude * DurationTicks;

        /// <summary>
        /// How far through its recharge the skill is, from nought to a hundred.
        /// </summary>
        /// <remarks>
        /// Integer, like everything else here, and defined once because two separate views need it
        /// and two separate views computing it is two chances to disagree. A skill with no cooldown
        /// is always fully recharged rather than dividing by zero.
        /// </remarks>
        public readonly int RechargePercent => CooldownTicks <= 0 || CooldownRemaining <= 0
            ? 100
            : 100 - (CooldownRemaining * 100 / CooldownTicks);

        /// <summary>Creates a fully charged slot.</summary>
        public static SkillSlot Create(
            SkillId id, int cooldownTicks, int magnitude, int durationTicks, int power = 0, int maxCharges = 1) =>
            new SkillSlot
            {
                Id = id,
                CooldownTicks = cooldownTicks,
                CooldownRemaining = 0,
                MaxCharges = maxCharges,
                Charges = maxCharges,
                Power = power,
                Magnitude = magnitude,
                DurationTicks = durationTicks,
                Traits = SkillTraits.None,
                HeldLastTick = false
            };

        /// <summary>Advances the recharge by one tick.</summary>
        /// <remarks>
        /// Recharging is sequential rather than parallel: one charge comes back, and only then does
        /// the timer restart for the next. That keeps a multi-charge dash from refilling its whole
        /// bank at once, which would make extra charges strictly better than a shorter cooldown and
        /// collapse the choice between the two items.
        /// </remarks>
        public void TickCooldown()
        {
            if (Charges >= MaxCharges)
            {
                CooldownRemaining = 0;
                return;
            }

            if (CooldownRemaining > 0)
            {
                CooldownRemaining--;

                if (CooldownRemaining > 0)
                {
                    return;
                }
            }

            // Reached with no time left to serve, which also covers a cooldown of zero — a skill
            // tuned to have none must come straight back rather than never returning at all.
            Charges++;

            if (Charges < MaxCharges)
            {
                CooldownRemaining = CooldownTicks;
            }
        }

        /// <summary>Spends a charge, returning whether one was available.</summary>
        public bool TrySpend()
        {
            if (!IsReady)
            {
                return false;
            }

            Charges--;

            if (CooldownRemaining <= 0)
            {
                CooldownRemaining = CooldownTicks;
            }

            return true;
        }
    }
}
