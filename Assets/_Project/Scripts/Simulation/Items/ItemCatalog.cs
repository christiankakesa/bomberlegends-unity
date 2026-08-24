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
            ItemId.KineticCore,
            ItemId.PiercingRounds,
            ItemId.BombTrail,
            ItemId.Quickstep,
            ItemId.FocusingLens,
            ItemId.Overclock,
            ItemId.TwinShot
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

            // Turns the shot from a single answer into a line of them. Composes hard with
            // Overcharge: one trigger that walks a row of enemies *and* the bombs among them.
            ItemId.PiercingRounds => new ItemEffect(
                target: SkillId.Skillshot,
                addTraits: SkillTraits.Pierces),

            // The strongest composition in the pool, and deliberately so. On its own it is a way to
            // lay a trap while escaping; with Overcharge it becomes place-and-trigger at will, which
            // is a genuinely different game. Bound by the same bomb capacity as the button, so it
            // adds a way to place bombs and never a way to place more of them.
            ItemId.BombTrail => new ItemEffect(
                target: SkillId.Dash,
                addTraits: SkillTraits.LeavesBombs),

            // The safe dash upgrade. Shortens the window you are committed for without giving you a
            // second charge, which is the distinction the M4 play verdict turned on.
            ItemId.Quickstep => new ItemEffect(
                target: SkillId.Dash,
                cooldownPercent: -40),

            // A real trade rather than an upgrade: far more damage per shot, but slow enough that
            // leading a moving target becomes a skill. Pairs naturally with Kinetic Core, which
            // buys the lost speed back.
            ItemId.FocusingLens => new ItemEffect(
                target: SkillId.Skillshot,
                flatPower: 30,
                magnitudePercent: -30),

            ItemId.Overclock => new ItemEffect(
                cooldownPercent: -25),

            // Banking a second shot costs recharge speed, so it is a choice between burst and
            // sustain rather than a strict gain.
            ItemId.TwinShot => new ItemEffect(
                target: SkillId.Skillshot,
                cooldownPercent: 25,
                bonusCharges: 1),

            _ => default
        };

        /// <summary>
        /// Whether the item is worth only as much as the build underneath it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// True for an item that names no skill and grafts no behaviour — a percentage spread over
        /// whatever happens to be equipped. Its worth is a function of how much build there is, so
        /// it is strongest last and weakest first, and over an empty build it is very nearly
        /// nothing.
        /// </para>
        /// <para>
        /// Read out of the effect rather than kept as a list of names, so an item added later is
        /// classified by what it does instead of by whoever remembers to update the list.
        /// </para>
        /// <para>
        /// Round 3 has Overclock taken by 8 of 12 testers and never on the first pick, with testers
        /// calling it useless there. That is a good item at the wrong moment, which is an offer
        /// problem and not a balance one — see <c>docs/14-INSIGHTS.md</c> §5.
        /// </para>
        /// </remarks>
        public static bool ScalesWithTheBuild(ItemId id)
        {
            var effect = Effect(id);

            // Naming a skill makes the item concrete: it does a stated thing to a thing you have.
            if (effect.Target != SkillId.None)
            {
                return false;
            }

            // Anything given outright — a behaviour, damage, a banked use — is noticeable on a build
            // of one. What is left is a multiplier, and a multiplier needs something to multiply.
            return effect.AddTraits == SkillTraits.None &&
                   effect.FlatPower == 0 &&
                   effect.BonusCharges == 0 &&
                   (effect.MagnitudePercent != 0 ||
                    effect.CooldownPercent != 0 ||
                    effect.DurationPercent != 0);
        }

        /// <summary>
        /// What the item does, in the player's terms.
        /// </summary>
        /// <remarks>
        /// Written as <i>what changes about how you play</i> rather than as a stat line, and it
        /// carries real weight: the vertical slice measures whether players deliberately pick a
        /// different item on their second run. A player who cannot tell what an item does picks at
        /// random, which reads in the data as the synergy pillar failing when the truth is only that
        /// the screen said nothing. Any cost is stated in the same breath as the benefit.
        /// </remarks>
        public static string Description(ItemId id) => id switch
        {
            ItemId.Overcharge =>
                "Your skillshot sets off any bomb it flies over. Bombs become triggers you hold, " +
                "not timers you plan around.",

            ItemId.Momentum =>
                "Your dash injures enemies it passes through. It still gives no protection — " +
                "charging through a mob is a trade.",

            ItemId.KineticCore =>
                "Every skill travels half again as far and as fast. Your dash and your shot both " +
                "reach further.",

            ItemId.PiercingRounds =>
                "Your skillshot passes through enemies instead of stopping at the first one.",

            ItemId.BombTrail =>
                "Dashing lays a bomb where you left. Uses one of your bombs, so you can carry no " +
                "more than before.",

            ItemId.Quickstep =>
                "Your dash comes back 40% sooner. You are committed for less of the fight.",

            ItemId.FocusingLens =>
                "Your skillshot hits far harder but travels noticeably slower. You will have to " +
                "lead moving targets.",

            ItemId.Overclock =>
                "Every skill comes back 25% sooner.",

            ItemId.TwinShot =>
                "Bank a second skillshot for back-to-back firing. Each one recharges more slowly.",

            _ => string.Empty
        };

        /// <summary>A short name, for readouts.</summary>
        public static string Name(ItemId id) => id switch
        {
            ItemId.Overcharge => "OVERCHARGE",
            ItemId.Momentum => "MOMENTUM",
            ItemId.KineticCore => "KINETIC CORE",
            ItemId.PiercingRounds => "PIERCING ROUNDS",
            ItemId.BombTrail => "BOMB TRAIL",
            ItemId.Quickstep => "QUICKSTEP",
            ItemId.FocusingLens => "FOCUSING LENS",
            ItemId.Overclock => "OVERCLOCK",
            ItemId.TwinShot => "TWIN SHOT",
            _ => "—"
        };
    }
}
