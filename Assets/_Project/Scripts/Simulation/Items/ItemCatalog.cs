using BomberLegends.Simulation.Skills;

namespace BomberLegends.Simulation.Items
{
    /// <summary>
    /// What every item does.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A table, not a hierarchy. Authoring assets in the Data layer will bake into these values
    /// later; keeping the canonical definitions here means the whole item system is testable in
    /// milliseconds with no engine and no assets.
    /// </para>
    /// <para>
    /// The three starting items are chosen to <b>compose rather than stack</b>. Two of them change
    /// what a skill does and one changes every skill's numbers, so any pair produces a build that
    /// plays differently — which is the question the vertical slice exists to answer.
    /// </para>
    /// </remarks>
    public static class ItemCatalog
    {
        /// <summary>Every item that currently exists, in display order.</summary>
        public static readonly ItemId[] All =
        {
            ItemId.Overcharge,
            ItemId.Momentum,
            ItemId.KineticCore
        };

        /// <summary>What holding <paramref name="id"/> does.</summary>
        public static ItemEffect Effect(ItemId id) => id switch
        {
            // Turns the skillshot from a weapon into a remote detonator. The single largest
            // behavioural change available, and the one that most changes how bombs get placed:
            // a bomb is no longer on a timer you have to plan around, it is a trigger you hold.
            ItemId.Overcharge => new ItemEffect(
                target: SkillId.Skillshot,
                addTraits: SkillTraits.DetonatesBombs),

            // Rewards using the dash offensively, which is how it is already being played. It does
            // not grant immunity — dashing through a mob remains a trade, not a free kill.
            ItemId.Momentum => new ItemEffect(
                target: SkillId.Dash,
                addTraits: SkillTraits.DamagesContacts,
                flatPower: 40),

            // The generic-number payoff: one item reaching every skill without naming any of them.
            // Deliberately grants no charges — a second dash charge would convert "dash in *or*
            // out" into "in *and* out" and delete the decision that makes the dash interesting.
            ItemId.KineticCore => new ItemEffect(
                magnitudePercent: 50),

            _ => default
        };

        /// <summary>A short name, for readouts.</summary>
        public static string Name(ItemId id) => id switch
        {
            ItemId.Overcharge => "OVERCHARGE",
            ItemId.Momentum => "MOMENTUM",
            ItemId.KineticCore => "KINETIC CORE",
            _ => "—"
        };
    }
}
