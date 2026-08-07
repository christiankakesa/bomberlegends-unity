using BomberLegends.Core;
using BomberLegends.Simulation;
using BomberLegends.Simulation.Board;
using BomberLegends.Simulation.Events;
using BomberLegends.Simulation.Items;
using BomberLegends.Simulation.Skills;
using NUnit.Framework;

namespace BomberLegends.Tests.EditMode.Simulation
{
    /// <summary>
    /// Covers the item framework, the three starting items, and the behaviours they compose into.
    /// </summary>
    public sealed class ItemTests
    {
        private const int DashSpeed = 500;
        private const int DashTicks = 6;
        private const int ShotSpeed = 400;
        private const int ShotTicks = 30;
        private const int PlayerRadius = 340;
        private const int EnemyRadius = 320;

        private static SimulationConfig Config(
            int fuse = 90,
            int bombCapacity = 2,
            int itemSlots = 2,
            int enemySpeed = 80) =>
            new SimulationConfig(
                moveSpeedPerTick: 133,
                laneSnapPerTick: 200,
                turnTolerance: 300,
                directionDeadzone: PlayerIntent.DefaultDeadzone,
                cornerAssistEnabled: true,
                fuseTicks: fuse,
                blastLingerTicks: 12,
                bombCooldownTicks: 0,
                startingBombCapacity: bombCapacity,
                startingBlastRange: 2,
                maxBombs: 16,
                playerRadius: PlayerRadius,
                cornerSlipPerTick: 90,
                cornerSlipTolerance: 320,
                playerMaxHealth: 100,
                blastDamageToPlayer: 34,
                enemyContactDamage: 10,
                invulnerabilityTicks: 30,
                enemyMaxHealth: 100,
                blastDamageToEnemy: 100,
                enemySpeedPerTick: enemySpeed,
                enemyRadius: EnemyRadius,
                maxEnemies: 32,
                dashSpeedPerTick: DashSpeed,
                dashDurationTicks: DashTicks,
                dashCooldownTicks: 60,
                dashCharges: 1,
                skillshotSpeedPerTick: ShotSpeed,
                skillshotDurationTicks: ShotTicks,
                skillshotCooldownTicks: 45,
                skillshotDamage: 50,
                maxProjectiles: 16,
                itemSlots: itemSlots);

        private static PlayerIntent Idle => PlayerIntent.None;

        private static PlayerIntent Bomb => new PlayerIntent(0, 0, IntentButtons.Bomb);

        private static PlayerIntent Dash(Direction direction) =>
            PlayerIntent.FromDirection(direction, IntentButtons.Skill1);

        private static PlayerIntent Shoot(sbyte aimX, sbyte aimY) =>
            new PlayerIntent(0, 0, IntentButtons.Skill2, aimX, aimY);

        private static void Advance(GameSimulation simulation, int ticks, PlayerIntent? intent = null)
        {
            var value = intent ?? Idle;
            for (var i = 0; i < ticks; i++)
            {
                simulation.Tick(value);
            }
        }

        /// <summary>Runs until the given event appears, returning whether it did.</summary>
        private static bool AdvanceUntilEvent(
            GameSimulation simulation, SimEventType type, int maxTicks, PlayerIntent? intent = null)
        {
            var value = intent ?? Idle;

            for (var i = 0; i < maxTicks; i++)
            {
                simulation.Tick(value);

                for (var e = 0; e < simulation.Events.Count; e++)
                {
                    if (simulation.Events[e].Type == type)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static GameSimulation Room(SimulationConfig? config = null) =>
            new GameSimulation(
                config ?? Config(),
                LevelLayout.Parse(
                    "###############",
                    "#.............#",
                    "#..P..........#",
                    "#.............#",
                    "###############"),
                seed: 1u);

        // ---------- the framework ----------

        [Test]
        public void AnItemTargetingNoSkillInParticular_ReachesEveryEquippedSkill()
        {
            // The generic-number payoff: one item touching two skills without naming either.
            var simulation = Room();

            var dashBefore = simulation.State.Player.Skills[0].Magnitude;
            var shotBefore = simulation.State.Player.Skills[1].Magnitude;

            Assert.That(simulation.TryGrantItem(ItemId.KineticCore), Is.True);

            Assert.That(simulation.State.Player.Skills[0].Magnitude, Is.EqualTo(dashBefore * 3 / 2));
            Assert.That(simulation.State.Player.Skills[1].Magnitude, Is.EqualTo(shotBefore * 3 / 2));
        }

        [Test]
        public void AnItemTargetingOneSkill_LeavesTheOthersAlone()
        {
            var simulation = Room();

            var dashPowerBefore = simulation.State.Player.Skills[0].Power;

            Assert.That(simulation.TryGrantItem(ItemId.Overcharge), Is.True);

            Assert.That(simulation.State.Player.Skills[1].Traits.Has(SkillTraits.DetonatesBombs), Is.True);
            Assert.That(simulation.State.Player.Skills[0].Traits.Has(SkillTraits.DetonatesBombs), Is.False);
            Assert.That(simulation.State.Player.Skills[0].Power, Is.EqualTo(dashPowerBefore));
        }

        [Test]
        public void FlatPowerCanArmASkillThatDealtNoDamage()
        {
            // A percentage of zero is zero, which would make "the dash now hurts" inexpressible.
            var simulation = Room();

            Assert.That(simulation.State.Player.Skills[0].Power, Is.Zero);

            simulation.TryGrantItem(ItemId.Momentum);

            Assert.That(simulation.State.Player.Skills[0].Power, Is.EqualTo(40));
        }

        [Test]
        public void ItemsApplyInEitherOrderIdentically()
        {
            // Two builds holding the same pair must *play* the same, whatever order they arrived in.
            //
            // Deliberately compares the skills rather than the state hash: the hash also covers
            // inventory order, and two runs that made the same choices at different times really are
            // different runs. What must not differ is the resulting numbers.
            //
            // This holds because no field takes both a flat addition and a percentage — flat power,
            // percentage magnitude. Adding a percentage to a field that also takes a flat bonus
            // would make order matter, and this test is what would catch it.
            var first = Room();
            first.TryGrantItem(ItemId.KineticCore);
            first.TryGrantItem(ItemId.Momentum);

            var second = Room();
            second.TryGrantItem(ItemId.Momentum);
            second.TryGrantItem(ItemId.KineticCore);

            for (var index = 0; index < SkillLoadout.SlotCount; index++)
            {
                var a = first.State.Player.Skills[index];
                var b = second.State.Player.Skills[index];

                Assert.That(b.Id, Is.EqualTo(a.Id), $"slot {index} skill");
                Assert.That(b.Magnitude, Is.EqualTo(a.Magnitude), $"slot {index} magnitude");
                Assert.That(b.Power, Is.EqualTo(a.Power), $"slot {index} power");
                Assert.That(b.DurationTicks, Is.EqualTo(a.DurationTicks), $"slot {index} duration");
                Assert.That(b.CooldownTicks, Is.EqualTo(a.CooldownTicks), $"slot {index} cooldown");
                Assert.That(b.MaxCharges, Is.EqualTo(a.MaxCharges), $"slot {index} charges");
                Assert.That(b.Traits, Is.EqualTo(a.Traits), $"slot {index} traits");
            }
        }

        [Test]
        public void TheSameItemCannotBeTakenTwice()
        {
            // Stacking a percentage adds nothing to a build and turns a choice into arithmetic.
            var simulation = Room();

            Assert.That(simulation.TryGrantItem(ItemId.KineticCore), Is.True);
            var magnitude = simulation.State.Player.Skills[0].Magnitude;

            Assert.That(simulation.TryGrantItem(ItemId.KineticCore), Is.False);
            Assert.That(simulation.State.Player.Skills[0].Magnitude, Is.EqualTo(magnitude));
        }

        [Test]
        public void ItemSlotsAreScarce()
        {
            var simulation = Room(Config(itemSlots: 2));

            Assert.That(simulation.TryGrantItem(ItemId.Overcharge), Is.True);
            Assert.That(simulation.TryGrantItem(ItemId.Momentum), Is.True);
            Assert.That(simulation.TryGrantItem(ItemId.KineticCore), Is.False,
                "a third item must not fit within one arena; scarcity is what makes a build a choice");

            Assert.That(simulation.State.Player.Items.Count, Is.EqualTo(2));
            Assert.That(simulation.State.Player.Items.IsFull, Is.True);
        }

        [Test]
        public void TakingAnItemAnnouncesIt()
        {
            var simulation = Room();
            simulation.TryGrantItem(ItemId.Momentum);

            var found = false;
            for (var i = 0; i < simulation.Events.Count; i++)
            {
                if (simulation.Events[i].Type == SimEventType.ItemAcquired &&
                    simulation.Events[i].Value == (int)ItemId.Momentum)
                {
                    found = true;
                }
            }

            Assert.That(found, Is.True);
        }

        [Test]
        public void EveryCatalogItemDoesSomething()
        {
            // Guards against an item being added to the enum and forgotten in the table.
            foreach (var id in ItemCatalog.All)
            {
                var effect = ItemCatalog.Effect(id);

                var changesSomething =
                    effect.AddTraits != SkillTraits.None ||
                    effect.FlatPower != 0 ||
                    effect.MagnitudePercent != 0 ||
                    effect.CooldownPercent != 0 ||
                    effect.DurationPercent != 0 ||
                    effect.BonusCharges != 0;

                Assert.That(changesSomething, Is.True, $"{ItemCatalog.Name(id)} has no effect");
                Assert.That(ItemCatalog.Name(id), Is.Not.EqualTo("—"), $"{id} has no name");
            }
        }

        [Test]
        public void EveryItemExplainsItselfToThePlayer()
        {
            // The slice measures whether players choose deliberately. An item with no description
            // gets picked at random, which reads in the data as the synergy pillar failing when the
            // truth is only that the screen said nothing.
            foreach (var id in ItemCatalog.All)
            {
                var description = ItemCatalog.Description(id);

                Assert.That(description, Is.Not.Empty, $"{ItemCatalog.Name(id)} has no description");
                Assert.That(description.Length, Is.GreaterThan(25),
                    $"{ItemCatalog.Name(id)}'s description is too terse to decide from");
                Assert.That(description, Is.Not.EqualTo(ItemCatalog.Name(id)));
            }
        }

        [Test]
        public void NoStartingItemGrantsADashCharge()
        {
            // Recorded design decision: a second dash charge converts "dash in *or* out" into
            // "in *and* out" and deletes the choice that makes the dash interesting.
            foreach (var id in ItemCatalog.All)
            {
                var effect = ItemCatalog.Effect(id);

                if (effect.Targets(SkillId.Dash))
                {
                    Assert.That(effect.BonusCharges, Is.Zero,
                        $"{ItemCatalog.Name(id)} grants a dash charge; see the M4 play verdict");
                }
            }
        }

        // ---------- Overcharge ----------

        [Test]
        public void Overcharge_MakesTheShotSetOffABombItFliesOver()
        {
            var simulation = Room(Config(fuse: 600));
            simulation.TryGrantItem(ItemId.Overcharge);

            // Drop a bomb, walk well clear of it, then shoot back at it.
            simulation.Tick(Bomb);
            Advance(simulation, 40, PlayerIntent.FromDirection(Direction.East));

            Assert.That(
                AdvanceUntilEvent(simulation, SimEventType.BombDetonated, 30, Shoot(-100, 0)),
                Is.True,
                "the fuse has hundreds of ticks left, so only the shot can have set it off");
        }

        [Test]
        public void WithoutOvercharge_TheShotLeavesBombsAlone()
        {
            var simulation = Room(Config(fuse: 600));

            simulation.Tick(Bomb);
            Advance(simulation, 40, PlayerIntent.FromDirection(Direction.East));

            Assert.That(
                AdvanceUntilEvent(simulation, SimEventType.BombDetonated, 30, Shoot(-100, 0)),
                Is.False);
        }

        [Test]
        public void Overcharge_DoesNotSetOffTheBombUnderYourFeet()
        {
            // Otherwise equipping this item would turn every shot fired while standing over your own
            // bomb into a suicide, contradicting the grace the game already grants for walking off it.
            var simulation = Room(Config(fuse: 600));
            simulation.TryGrantItem(ItemId.Overcharge);

            simulation.Tick(Bomb);

            Assert.That(
                AdvanceUntilEvent(simulation, SimEventType.BombDetonated, 20, Shoot(100, 0)),
                Is.False);
            Assert.That(simulation.State.Player.Health.Current,
                Is.EqualTo(simulation.State.Player.Health.Max));
        }

        [Test]
        public void Overcharge_DoesNotConsumeTheShotOnTheBombItTriggers()
        {
            // Carrying on is what lets one trigger walk a line of bombs — the point of the item.
            var simulation = Room(Config(fuse: 600));
            simulation.TryGrantItem(ItemId.Overcharge);

            simulation.Tick(Bomb);
            Advance(simulation, 40, PlayerIntent.FromDirection(Direction.East));

            simulation.Tick(Shoot(-100, 0));

            var stillFlying = false;
            for (var i = 0; i < 30; i++)
            {
                simulation.Tick(Idle);

                for (var e = 0; e < simulation.Events.Count; e++)
                {
                    if (simulation.Events[e].Type == SimEventType.BombDetonated)
                    {
                        stillFlying = simulation.State.Projectiles.ActiveCount > 0;
                    }
                }
            }

            Assert.That(stillFlying, Is.True, "the shot must survive the bomb it sets off");
        }

        // ---------- Momentum ----------

        [Test]
        public void Momentum_MakesTheDashInjureWhatItPassesThrough()
        {
            var simulation = new GameSimulation(
                Config(enemySpeed: 1),
                LevelLayout.Parse(
                    "#######",
                    "#P.E..#",
                    "#######"),
                seed: 1u);

            simulation.TryGrantItem(ItemId.Momentum);

            var max = simulation.State.Enemies[0].Health.Max;

            simulation.Tick(Dash(Direction.East));
            Advance(simulation, DashTicks);

            Assert.That(simulation.State.Enemies[0].Health.Current, Is.EqualTo(max - 40));
        }

        [Test]
        public void WithoutMomentum_TheDashIsHarmless()
        {
            var simulation = new GameSimulation(
                Config(enemySpeed: 1),
                LevelLayout.Parse(
                    "#######",
                    "#P.E..#",
                    "#######"),
                seed: 1u);

            var max = simulation.State.Enemies[0].Health.Max;

            simulation.Tick(Dash(Direction.East));
            Advance(simulation, DashTicks);

            Assert.That(simulation.State.Enemies[0].Health.Current, Is.EqualTo(max));
        }

        [Test]
        public void Momentum_DoesNotMakeDashingSafe()
        {
            // The dash grants no immunity. Passing through a mob is a trade, not a free kill —
            // which is what keeps the skill honest now that it is a weapon as well as an escape.
            var simulation = new GameSimulation(
                Config(enemySpeed: 1),
                LevelLayout.Parse(
                    "#######",
                    "#P.E..#",
                    "#######"),
                seed: 1u);

            simulation.TryGrantItem(ItemId.Momentum);

            simulation.Tick(Dash(Direction.East));
            Advance(simulation, DashTicks + 4);

            Assert.That(simulation.State.Player.Health.Current,
                Is.LessThan(simulation.State.Player.Health.Max),
                "the enemy must still land its hit");
        }

        [Test]
        public void ADashCannotOutrunItsOwnContactCheck()
        {
            // Contact is tested once per tick, so a dash must never cover more ground in a tick than
            // the two boxes are wide together — including after the strongest magnitude item.
            var config = Config();
            var combinedWidth = 2 * (config.PlayerRadius + config.EnemyRadius);

            var fastest = config.DashSpeedPerTick;
            foreach (var id in ItemCatalog.All)
            {
                var effect = ItemCatalog.Effect(id);

                if (effect.Targets(SkillId.Dash) && effect.MagnitudePercent > 0)
                {
                    fastest += fastest * effect.MagnitudePercent / 100;
                }
            }

            Assert.That(fastest, Is.LessThan(combinedWidth),
                "a dash this fast could pass through an enemy without registering");
        }

        // ---------- composition ----------

        [Test]
        public void TwoItemsCompose_WithoutEitherKnowingAboutTheOther()
        {
            // Synergy with no table of item pairs anywhere: Overcharge changes what the shot does,
            // Kinetic Core changes how far it reaches, and together they are a long-range detonator.
            var simulation = Room();

            var baseSpeed = simulation.State.Player.Skills[1].Magnitude;

            simulation.TryGrantItem(ItemId.Overcharge);
            simulation.TryGrantItem(ItemId.KineticCore);

            var shot = simulation.State.Player.Skills[1];

            Assert.That(shot.Traits.Has(SkillTraits.DetonatesBombs), Is.True);
            Assert.That(shot.Magnitude, Is.GreaterThan(baseSpeed));
            Assert.That(shot.Reach, Is.GreaterThan(baseSpeed * ShotTicks));
        }

        [Test]
        public void ABuildIsVisibleInTheState()
        {
            // The slice measures whether players can describe their build. One they cannot see is
            // one they cannot describe.
            var simulation = Room();

            simulation.TryGrantItem(ItemId.Overcharge);
            simulation.TryGrantItem(ItemId.Momentum);

            var items = simulation.State.Player.Items;

            Assert.That(items.Contains(ItemId.Overcharge), Is.True);
            Assert.That(items.Contains(ItemId.Momentum), Is.True);
            Assert.That(items.Contains(ItemId.KineticCore), Is.False);
        }

        // ---------- the wider pool ----------

        [Test]
        public void ThePoolIsWideEnoughToKeepOfferingSomethingNew()
        {
            // Three items into two slots gave a run two decisions and then nothing. The pool has to
            // stay comfortably ahead of the slots or a run goes flat, which is exactly what the
            // M6 notes flagged as the binding constraint on run length.
            Assert.That(ItemCatalog.All.Length, Is.GreaterThanOrEqualTo(8));
        }

        [Test]
        public void EveryItemIsDistinctFromEveryOther()
        {
            // Two items with identical effects are one item and a wasted offer slot.
            for (var i = 0; i < ItemCatalog.All.Length; i++)
            {
                for (var j = i + 1; j < ItemCatalog.All.Length; j++)
                {
                    var a = ItemCatalog.Effect(ItemCatalog.All[i]);
                    var b = ItemCatalog.Effect(ItemCatalog.All[j]);

                    var identical =
                        a.Target == b.Target &&
                        a.AddTraits == b.AddTraits &&
                        a.FlatPower == b.FlatPower &&
                        a.MagnitudePercent == b.MagnitudePercent &&
                        a.CooldownPercent == b.CooldownPercent &&
                        a.DurationPercent == b.DurationPercent &&
                        a.BonusCharges == b.BonusCharges;

                    Assert.That(identical, Is.False,
                        $"{ItemCatalog.Name(ItemCatalog.All[i])} and " +
                        $"{ItemCatalog.Name(ItemCatalog.All[j])} do the same thing");
                }
            }
        }

        [Test]
        public void PiercingRounds_LetsAShotHitMoreThanOneEnemy()
        {
            var simulation = new GameSimulation(
                Config(enemySpeed: 1),
                LevelLayout.Parse(
                    "###########",
                    "#P..E.E...#",
                    "###########"),
                seed: 1u);

            simulation.TryGrantItem(ItemId.PiercingRounds);

            simulation.Tick(Shoot(100, 0));
            Advance(simulation, 30);

            var hurt = 0;
            for (var i = 0; i < simulation.State.Enemies.Capacity; i++)
            {
                var enemy = simulation.State.Enemies[i];
                if (enemy.IsActive && enemy.Health.Current < enemy.Health.Max)
                {
                    hurt++;
                }
            }

            Assert.That(hurt, Is.EqualTo(2), "a piercing shot must reach the enemy behind the first");
        }

        [Test]
        public void WithoutPiercing_AShotStopsAtTheFirstEnemy()
        {
            var simulation = new GameSimulation(
                Config(enemySpeed: 1),
                LevelLayout.Parse(
                    "###########",
                    "#P..E.E...#",
                    "###########"),
                seed: 1u);

            simulation.Tick(Shoot(100, 0));
            Advance(simulation, 30);

            var hurt = 0;
            for (var i = 0; i < simulation.State.Enemies.Capacity; i++)
            {
                var enemy = simulation.State.Enemies[i];
                if (enemy.IsActive && enemy.Health.Current < enemy.Health.Max)
                {
                    hurt++;
                }
            }

            Assert.That(hurt, Is.EqualTo(1));
        }

        [Test]
        public void BombTrail_LaysABombWhereTheDashBegan()
        {
            var simulation = Room();
            simulation.TryGrantItem(ItemId.BombTrail);

            var from = simulation.State.Player.Tile;

            simulation.Tick(Dash(Direction.East));

            Assert.That(simulation.State.BombGrid.HasBomb(from), Is.True,
                "the bomb must mark where the player left, not where they arrived");
        }

        [Test]
        public void BombTrail_IsStillBoundByBombCapacity()
        {
            // An item may add a way to place bombs. It must never add a way to place more of them,
            // or it quietly breaks the economy the whole Bomberman layer rests on.
            var simulation = Room(Config(bombCapacity: 1));
            simulation.TryGrantItem(ItemId.BombTrail);

            simulation.Tick(Bomb);
            Assume.That(simulation.State.Bombs.ActiveCount, Is.EqualTo(1));

            Advance(simulation, 4, PlayerIntent.FromDirection(Direction.East));
            simulation.Tick(Dash(Direction.East));

            Assert.That(simulation.State.Bombs.ActiveCount, Is.EqualTo(1));
        }

        [Test]
        public void BombTrailAndOvercharge_ComposeIntoPlaceAndTrigger()
        {
            // The strongest pairing in the pool, and written down nowhere: the dash lays the bomb,
            // the shot sets it off.
            var simulation = Room(Config(fuse: 600));
            simulation.TryGrantItem(ItemId.BombTrail);
            simulation.TryGrantItem(ItemId.Overcharge);

            simulation.Tick(Dash(Direction.East));
            Assume.That(simulation.State.Bombs.ActiveCount, Is.EqualTo(1));

            // Dash clear, turn round, and trigger it. The fuse has hundreds of ticks left.
            Advance(simulation, 30, PlayerIntent.FromDirection(Direction.East));

            Assert.That(
                AdvanceUntilEvent(simulation, SimEventType.BombDetonated, 40, Shoot(-100, 0)),
                Is.True);
        }

        [Test]
        public void Quickstep_ShortensTheDashCooldownWithoutBankingACharge()
        {
            var simulation = Room();
            var before = simulation.State.Player.Skills[0].CooldownTicks;
            var charges = simulation.State.Player.Skills[0].MaxCharges;

            simulation.TryGrantItem(ItemId.Quickstep);

            Assert.That(simulation.State.Player.Skills[0].CooldownTicks, Is.LessThan(before));
            Assert.That(simulation.State.Player.Skills[0].MaxCharges, Is.EqualTo(charges),
                "the safe dash upgrade shortens commitment; it does not remove the choice");
        }

        [Test]
        public void FocusingLens_TradesSpeedForDamage()
        {
            var simulation = Room();
            var power = simulation.State.Player.Skills[1].Power;
            var speed = simulation.State.Player.Skills[1].Magnitude;

            simulation.TryGrantItem(ItemId.FocusingLens);

            var shot = simulation.State.Player.Skills[1];

            Assert.That(shot.Power, Is.GreaterThan(power));
            Assert.That(shot.Magnitude, Is.LessThan(speed), "a trade, not an upgrade");
        }

        [Test]
        public void TwinShot_BanksASecondShotAtACost()
        {
            var simulation = Room();
            var cooldown = simulation.State.Player.Skills[1].CooldownTicks;

            simulation.TryGrantItem(ItemId.TwinShot);

            var shot = simulation.State.Player.Skills[1];

            Assert.That(shot.MaxCharges, Is.EqualTo(2));
            Assert.That(shot.Charges, Is.EqualTo(2), "and the extra charge is available immediately");
            Assert.That(shot.CooldownTicks, Is.GreaterThan(cooldown), "burst is paid for in sustain");
        }

        [Test]
        public void Overclock_ShortensEverySkillsCooldown()
        {
            var simulation = Room();
            var dash = simulation.State.Player.Skills[0].CooldownTicks;
            var shot = simulation.State.Player.Skills[1].CooldownTicks;

            simulation.TryGrantItem(ItemId.Overclock);

            Assert.That(simulation.State.Player.Skills[0].CooldownTicks, Is.LessThan(dash));
            Assert.That(simulation.State.Player.Skills[1].CooldownTicks, Is.LessThan(shot));
        }

        // ---------- guarantees ----------

        [Test]
        public void ItemsDoNotBreakDeterminism()
        {
            GameSimulation Create()
            {
                var simulation = new GameSimulation(
                    Config(),
                    LevelLayout.Parse(
                        "#########",
                        "#P..E...#",
                        "#.#.#.#.#",
                        "#...E...#",
                        "#########"),
                    seed: 31u);

                simulation.TryGrantItem(ItemId.Overcharge);
                simulation.TryGrantItem(ItemId.Momentum);
                return simulation;
            }

            var first = Create();
            var second = Create();

            for (var i = 0; i < 400; i++)
            {
                var buttons = IntentButtons.None;

                if (i % 37 == 0)
                {
                    buttons = buttons.With(IntentButtons.Skill1);
                }

                if (i % 19 == 0)
                {
                    buttons = buttons.With(IntentButtons.Skill2);
                }

                if (i % 53 == 0)
                {
                    buttons = buttons.With(IntentButtons.Bomb);
                }

                var offset = Directions.Cardinals[(i / 17) % 4].ToOffset();
                var intent = new PlayerIntent(
                    (sbyte)(offset.X * PlayerIntent.AxisRange),
                    (sbyte)(offset.Y * PlayerIntent.AxisRange),
                    buttons,
                    (sbyte)((i % 200) - 100),
                    (sbyte)(97 - (i % 200)));

                first.Tick(intent);
                second.Tick(intent);

                Assert.That(second.ComputeStateHash(), Is.EqualTo(first.ComputeStateHash()),
                    $"simulations diverged on tick {i + 1}");
            }
        }

        [Test]
        public void ItemEffectsAllocateNothingMidMatch()
        {
            var simulation = new GameSimulation(
                Config(),
                LevelLayout.Parse(
                    "###########",
                    "#P..E.....#",
                    "#.#.#.#.#.#",
                    "#...E.....#",
                    "###########"),
                seed: 7u);

            simulation.TryGrantItem(ItemId.Overcharge);
            simulation.TryGrantItem(ItemId.Momentum);

            PlayerIntent Intent(int i) => new PlayerIntent(
                (sbyte)(i % 3 == 0 ? PlayerIntent.AxisRange : 0),
                0,
                (i % 5 == 0 ? IntentButtons.Skill1 : IntentButtons.None)
                    .With(i % 7 == 0 ? IntentButtons.Skill2 : IntentButtons.None)
                    .With(i % 41 == 0 ? IntentButtons.Bomb : IntentButtons.None),
                (sbyte)(i % 2 == 0 ? 100 : -100),
                0);

            for (var i = 0; i < 200; i++)
            {
                simulation.Tick(Intent(i));
            }

            var before = System.GC.GetAllocatedBytesForCurrentThread();

            for (var i = 0; i < 5000; i++)
            {
                simulation.Tick(Intent(i));
            }

            Assert.That(System.GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero,
                "carrying items must not cost anything per tick");
        }
    }
}
