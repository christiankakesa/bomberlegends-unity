using BomberLegends.Core;
using BomberLegends.Simulation;
using BomberLegends.Simulation.Board;
using BomberLegends.Simulation.Events;
using BomberLegends.Simulation.Skills;
using NUnit.Framework;

namespace BomberLegends.Tests.EditMode.Simulation
{
    /// <summary>
    /// Covers the skill loadout, the dash, and aimed skillshots.
    /// </summary>
    public sealed class SkillTests
    {
        private const int DashSpeed = 500;
        private const int DashTicks = 6;
        private const int DashCooldown = 60;
        private const int ShotSpeed = 400;
        private const int ShotTicks = 30;
        private const int ShotDamage = 50;

        private static SimulationConfig Config(
            int dashSpeed = DashSpeed,
            int dashTicks = DashTicks,
            int dashCooldown = DashCooldown,
            int dashCharges = 1,
            int shotDamage = ShotDamage,
            int shotCooldown = 45,
            int shotTicks = ShotTicks,
            int maxProjectiles = 16) =>
            new SimulationConfig(
                moveSpeedPerTick: 133,
                laneSnapPerTick: 200,
                turnTolerance: 300,
                directionDeadzone: PlayerIntent.DefaultDeadzone,
                cornerAssistEnabled: true,
                fuseTicks: 90,
                blastLingerTicks: 12,
                bombCooldownTicks: 0,
                startingBombCapacity: 1,
                startingBlastRange: 2,
                maxBombs: 16,
                playerRadius: 340,
                cornerSlipPerTick: 90,
                cornerSlipTolerance: 320,
                playerMaxHealth: 100,
                blastDamageToPlayer: 34,
                enemyContactDamage: 10,
                invulnerabilityTicks: 30,
                enemyMaxHealth: 100,
                blastDamageToEnemy: 100,
                enemySpeedPerTick: 80,
                enemyRadius: 320,
                maxEnemies: 32,
                dashSpeedPerTick: dashSpeed,
                dashDurationTicks: dashTicks,
                dashCooldownTicks: dashCooldown,
                dashCharges: dashCharges,
                skillshotSpeedPerTick: ShotSpeed,
                skillshotDurationTicks: shotTicks,
                skillshotCooldownTicks: shotCooldown,
                skillshotDamage: shotDamage,
                maxProjectiles: maxProjectiles);

        private static PlayerIntent Dash(Direction direction) =>
            PlayerIntent.FromDirection(direction, IntentButtons.Skill1);

        private static PlayerIntent Shoot(sbyte aimX, sbyte aimY) =>
            new PlayerIntent(0, 0, IntentButtons.Skill2, aimX, aimY);

        private static PlayerIntent Idle => PlayerIntent.None;

        private static void Advance(GameSimulation simulation, int ticks, PlayerIntent? intent = null)
        {
            var value = intent ?? Idle;
            for (var i = 0; i < ticks; i++)
            {
                simulation.Tick(value);
            }
        }

        private static int CountEvents(GameSimulation simulation, SimEventType type)
        {
            var count = 0;
            for (var i = 0; i < simulation.Events.Count; i++)
            {
                if (simulation.Events[i].Type == type)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>A wide empty corridor with the player near the west end.</summary>
        private static GameSimulation Corridor(SimulationConfig? config = null) =>
            new GameSimulation(
                config ?? Config(),
                LevelLayout.Parse(
                    "###############",
                    "#.............#",
                    "#..P..........#",
                    "#.............#",
                    "###############"),
                seed: 1u);

        // ---------- loadout ----------

        [Test]
        public void ThePlayerStartsWithADashAndASkillshot()
        {
            var simulation = Corridor();
            var skills = simulation.State.Player.Skills;

            Assert.That(skills.IndexOf(SkillId.Dash), Is.EqualTo(0));
            Assert.That(skills.IndexOf(SkillId.Skillshot), Is.EqualTo(1));
            Assert.That(skills[2].IsEquipped, Is.False, "the third slot is left for an item to fill");
        }

        [Test]
        public void ALoadoutHoldsNoMoreThanThreeSkills()
        {
            // Three is a design constraint, not a storage detail. If this ever silently grows,
            // every choice in the game gets cheaper.
            Assert.That(SkillLoadout.SlotCount, Is.EqualTo(3));

            Assert.Throws<System.ArgumentException>(() => SkillLoadout.Of(
                SkillSlot.Create(SkillId.Dash, 1, 1, 1),
                SkillSlot.Create(SkillId.Dash, 1, 1, 1),
                SkillSlot.Create(SkillId.Dash, 1, 1, 1),
                SkillSlot.Create(SkillId.Dash, 1, 1, 1)));
        }

        [Test]
        public void EachSlotHasItsOwnButton()
        {
            Assert.That(SkillLoadout.ButtonFor(0), Is.EqualTo(IntentButtons.Skill1));
            Assert.That(SkillLoadout.ButtonFor(1), Is.EqualTo(IntentButtons.Skill2));
            Assert.That(SkillLoadout.ButtonFor(2), Is.EqualTo(IntentButtons.Skill3));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => SkillLoadout.ButtonFor(3));
        }

        // ---------- charges ----------

        [Test]
        public void ASkillRechargesOneChargeAtATime()
        {
            // Sequential recharge is what keeps "more charges" and "shorter cooldown" from being
            // the same item.
            var slot = SkillSlot.Create(SkillId.Dash, cooldownTicks: 10, magnitude: 1,
                durationTicks: 1, maxCharges: 3);

            Assert.That(slot.TrySpend(), Is.True);
            Assert.That(slot.TrySpend(), Is.True);
            Assert.That(slot.TrySpend(), Is.True);
            Assert.That(slot.TrySpend(), Is.False, "an empty skill must not fire");

            for (var i = 0; i < 10; i++)
            {
                slot.TickCooldown();
            }

            Assert.That(slot.Charges, Is.EqualTo(1), "one charge back, not the whole bank");

            for (var i = 0; i < 10; i++)
            {
                slot.TickCooldown();
            }

            Assert.That(slot.Charges, Is.EqualTo(2));
        }

        [Test]
        public void AFullyChargedSkillStopsItsTimer()
        {
            var slot = SkillSlot.Create(SkillId.Dash, cooldownTicks: 4, magnitude: 1, durationTicks: 1);

            Assert.That(slot.TrySpend(), Is.True);

            for (var i = 0; i < 40; i++)
            {
                slot.TickCooldown();
            }

            Assert.That(slot.Charges, Is.EqualTo(1));
            Assert.That(slot.CooldownRemaining, Is.Zero, "a full skill must not keep counting");
        }

        [Test]
        public void ASpentSkillReportsHowFarThroughItsRechargeItIs()
        {
            // The number both readouts draw. A playtester hoarded both skills for an entire run
            // because nothing on screen said they come back, so this is the fix's foundation.
            var slot = SkillSlot.Create(SkillId.Dash, cooldownTicks: 100, magnitude: 1, durationTicks: 1);

            Assert.That(slot.RechargePercent, Is.EqualTo(100), "a ready skill is fully charged");

            slot.TrySpend();
            Assert.That(slot.RechargePercent, Is.Zero, "and a just-spent one is at nothing");

            for (var i = 0; i < 50; i++)
            {
                slot.TickCooldown();
            }

            Assert.That(slot.RechargePercent, Is.EqualTo(50).Within(2));

            for (var i = 0; i < 50; i++)
            {
                slot.TickCooldown();
            }

            Assert.That(slot.RechargePercent, Is.EqualTo(100));
            Assert.That(slot.IsReady, Is.True);
        }

        [Test]
        public void ASkillWithNoCooldownIsAlwaysFullyCharged()
        {
            // Never a division by zero, and never a readout stuck at empty.
            var slot = SkillSlot.Create(SkillId.Dash, cooldownTicks: 0, magnitude: 1, durationTicks: 1);

            Assert.That(slot.RechargePercent, Is.EqualTo(100));

            slot.TrySpend();
            Assert.That(slot.RechargePercent, Is.EqualTo(100));
        }

        // ---------- dash ----------

        [Test]
        public void DashingCarriesThePlayerFurtherThanWalking()
        {
            var walking = Corridor();
            var dashing = Corridor();

            Advance(walking, DashTicks, PlayerIntent.FromDirection(Direction.East));

            dashing.Tick(Dash(Direction.East));
            Advance(dashing, DashTicks - 1, PlayerIntent.FromDirection(Direction.East));

            var walked = walking.State.Player.Position.X;
            var dashed = dashing.State.Player.Position.X;

            Assert.That(dashed, Is.GreaterThan(walked),
                "a dash that does not clearly outrun a walk is not a dash");
        }

        [Test]
        public void ADashClearsYourOwnBlastByExactlyOneTile()
        {
            // The relationship these two numbers encode: escaping is a skill, not a formality.
            var config = Config();
            var reach = config.DashSpeedPerTick * config.DashDurationTicks;
            var blast = config.StartingBlastRange * SubTilePoint.UnitsPerTile;

            Assert.That(reach, Is.EqualTo(blast + SubTilePoint.UnitsPerTile),
                "dash distance and blast range are a pair; change one and re-tune the other");
        }

        [Test]
        public void ADashIgnoresSteeringWhileItLasts()
        {
            // Committing to the direction is what stops a dash being a strictly better walk.
            var simulation = Corridor();

            simulation.Tick(Dash(Direction.East));
            var startY = simulation.State.Player.Position.Y;

            // Push hard north for the rest of the dash. It must not turn.
            Advance(simulation, DashTicks - 1, PlayerIntent.FromDirection(Direction.North));

            Assert.That(simulation.State.Player.Position.Y, Is.EqualTo(startY),
                "steering mid-dash must be ignored");
        }

        [Test]
        public void ADashEndsAndControlReturns()
        {
            var simulation = Corridor();

            simulation.Tick(Dash(Direction.East));
            Advance(simulation, DashTicks);

            Assert.That(simulation.State.Player.IsDashing, Is.False);

            var before = simulation.State.Player.Position.Y;
            Advance(simulation, 10, PlayerIntent.FromDirection(Direction.North));

            Assert.That(simulation.State.Player.Position.Y, Is.GreaterThan(before),
                "the player must steer again once the dash is over");
        }

        [Test]
        public void ADashRunsOnACooldown()
        {
            var simulation = Corridor();

            simulation.Tick(Dash(Direction.East));
            Assert.That(simulation.State.Player.Skills[0].Charges, Is.Zero);

            // Release and press again well before the cooldown is up.
            Advance(simulation, 5);
            var beforeSecond = simulation.State.Player.Position;
            simulation.Tick(Dash(Direction.East));

            var moved = simulation.State.Player.Position.X - beforeSecond.X;
            Assert.That(moved, Is.LessThan(DashSpeed),
                "a dash on cooldown must not fire");

            Advance(simulation, DashCooldown);
            Assert.That(simulation.State.Player.Skills[0].Charges, Is.EqualTo(1));
        }

        [Test]
        public void HoldingTheDashButton_FiresOnceNotEveryTick()
        {
            var simulation = Corridor(Config(dashCooldown: 0));

            Advance(simulation, 20, Dash(Direction.East));

            Assert.That(simulation.State.Player.Skills[0].Charges, Is.EqualTo(1),
                "holding the button must not drain charges; a skill triggers on the press");
        }

        [Test]
        public void ADashIntoAWallStopsRatherThanGrinding()
        {
            // Boxed in on every side, so the dash jams on its second tick — well short of the six
            // it was given. Anything less cramped and this would pass simply by running out.
            var simulation = new GameSimulation(
                Config(),
                LevelLayout.Parse(
                    "###",
                    "#P#",
                    "###"),
                seed: 1u);

            simulation.Tick(Dash(Direction.East));
            simulation.Tick(Idle);

            Assert.That(simulation.State.Player.IsDashing, Is.False,
                "a dash held against a wall would hold the player's steering hostage");
        }

        [Test]
        public void ADashCollidesWithWallsExactlyAsWalkingDoes()
        {
            var simulation = new GameSimulation(
                Config(),
                LevelLayout.Parse(
                    "#####",
                    "#.P.#",
                    "#####"),
                seed: 1u);

            simulation.Tick(Dash(Direction.East));
            Advance(simulation, DashTicks);

            Assert.That(simulation.State.Player.Tile.X, Is.EqualTo(3),
                "no dash may phase through the board");
        }

        [Test]
        public void ADashWithNoHeadingUsesWhereThePlayerIsLooking()
        {
            var simulation = Corridor();

            // Face east, stop, then dash with no stick input at all.
            Advance(simulation, 4, PlayerIntent.FromDirection(Direction.East));
            var before = simulation.State.Player.Position.X;

            simulation.Tick(new PlayerIntent(0, 0, IntentButtons.Skill1));

            Assert.That(simulation.State.Player.Position.X, Is.GreaterThan(before + 200),
                "a standing dash must go somewhere, not be silently eaten");
        }

        [Test]
        public void ADashAfterADiagonalRunFollowsTheDiagonal()
        {
            // Reported in round two as "the dash went in a different direction than I thought".
            // A stick recentres faster than a thumb reaches a button, so a player running diagonally
            // routinely presses dash on a frame reading no input at all. Falling back to the
            // four-way facing threw them up to 45° off the line they were actually travelling.
            var simulation = Corridor();

            // Run north-east for long enough to establish a heading, then dash with nothing held.
            Advance(simulation, 6, new PlayerIntent(70, 70));

            var before = simulation.State.Player.Position;
            simulation.Tick(new PlayerIntent(0, 0, IntentButtons.Skill1));

            var movedX = simulation.State.Player.Position.X - before.X;
            var movedY = simulation.State.Player.Position.Y - before.Y;

            Assert.That(movedX, Is.GreaterThan(0), "the dash must keep going east");
            Assert.That(movedY, Is.GreaterThan(0), "and north — a diagonal run dashes diagonally");

            // Within a quarter of each other is comfortably diagonal; a cardinal fallback would put
            // one of these at zero.
            Assert.That(IntMath.Abs(movedX - movedY), Is.LessThan(IntMath.Abs(movedX) / 4 + 40),
                $"the dash should follow the diagonal, but went ({movedX}, {movedY})");
        }

        [Test]
        public void ADraggedDashGoesWhereItWasDragged()
        {
            // The gesture 07-CONCEPT-REVISION §4i designed and the simulation never read: on touch
            // each skill button is its own stick, so a drag on the dash button is a direction. It
            // was discarded for two weeks, which showed up the moment the ground arrow made it
            // visible — the arrow pointed one way and the dash went another.
            var simulation = Corridor();

            // Running east, then dashing on an aim drawn north. The aim must win.
            Advance(simulation, 6, new PlayerIntent(100, 0));

            var before = simulation.State.Player.Position;

            simulation.Tick(new PlayerIntent(
                0, 0, IntentButtons.Skill1 | IntentButtons.AimedCast, 0, 100));

            var movedX = simulation.State.Player.Position.X - before.X;
            var movedY = simulation.State.Player.Position.Y - before.Y;

            Assert.That(movedY, Is.GreaterThan(0), "the dash must follow the aim that was drawn for it");
            Assert.That(IntMath.Abs(movedX), Is.LessThan(IntMath.Abs(movedY) / 4 + 40),
                $"and not the direction of travel, but it went ({movedX}, {movedY})");
        }

        [Test]
        public void AStandingAimDoesNotSteerTheDash()
        {
            // The pad case, and the reason the flag exists rather than the dash simply reading the
            // aim. A right stick held on an enemy is a shot being lined up; a dash that obeyed it
            // would launch the player into the thing they were escaping.
            var simulation = Corridor();

            Advance(simulation, 6, new PlayerIntent(100, 0));

            var before = simulation.State.Player.Position;

            // Aim north, no AimedCast flag — exactly what GamepadInputSource produces.
            simulation.Tick(new PlayerIntent(0, 0, IntentButtons.Skill1, 0, 100));

            var movedX = simulation.State.Player.Position.X - before.X;
            var movedY = simulation.State.Player.Position.Y - before.Y;

            Assert.That(movedX, Is.GreaterThan(0), "the dash must keep following the stick");
            Assert.That(IntMath.Abs(movedY), Is.LessThan(IntMath.Abs(movedX) / 4 + 40),
                $"and ignore the standing aim, but it went ({movedX}, {movedY})");
        }

        [Test]
        public void AnAimedCastStillSteersTheShot()
        {
            // The skillshot has always honoured a free aim and must keep doing so — the flag adds a
            // case for the dash, it does not take one away from firing.
            var simulation = Corridor();

            Advance(simulation, 6, new PlayerIntent(100, 0));

            simulation.Tick(new PlayerIntent(
                0, 0, IntentButtons.Skill2 | IntentButtons.AimedCast, 0, 100));

            var projectile = simulation.State.Projectiles[0];

            Assert.That(projectile.IsActive, Is.True, "the shot must leave");
            Assert.That(projectile.VelocityY, Is.GreaterThan(0), "and follow the aim it was given");
        }

        [Test]
        public void AStandingPlayerWhoHasNeverMovedStillDashes()
        {
            // Facing is the last resort behind the recorded heading, and it has to survive: on the
            // very first tick of a run there is no heading to fall back on at all.
            var simulation = Corridor();

            var before = simulation.State.Player.Position;
            simulation.Tick(new PlayerIntent(0, 0, IntentButtons.Skill1));

            Assert.That(simulation.State.Player.Position, Is.Not.EqualTo(before),
                "a dash on the first tick of a run must still fire");
        }

        [Test]
        public void ADashAnnouncesItself()
        {
            var simulation = Corridor();
            simulation.Tick(Dash(Direction.East));

            Assert.That(CountEvents(simulation, SimEventType.DashStarted), Is.EqualTo(1));
            Assert.That(CountEvents(simulation, SimEventType.SkillUsed), Is.EqualTo(1));
        }

        // ---------- skillshot ----------

        [Test]
        public void AShotFliesInTheAimedDirection()
        {
            var simulation = Corridor();

            simulation.Tick(Shoot(0, 100));
            var start = simulation.State.Projectiles[0].Position;

            Advance(simulation, 2);
            var later = simulation.State.Projectiles[0].Position;

            Assert.That(simulation.State.Projectiles[0].IsActive, Is.True,
                "the shot must still be flying, or this measures nothing");

            Assert.That(later.Y, Is.GreaterThan(start.Y));
            Assert.That(later.X, Is.EqualTo(start.X), "an aimed shot must not drift sideways");
        }

        [Test]
        public void AShotIsAimedIndependentlyOfMovement()
        {
            // The whole reason aim is a separate pair of bytes.
            var simulation = Corridor();

            simulation.Tick(new PlayerIntent(100, 0, IntentButtons.Skill2, 0, 100));

            var projectile = simulation.State.Projectiles[0];
            Assert.That(projectile.IsActive, Is.True);
            Assert.That(projectile.VelocityY, Is.GreaterThan(0), "the shot follows aim");
            Assert.That(projectile.VelocityX, Is.Zero, "not the direction of travel");
        }

        [Test]
        public void WithNoAim_AShotFollowsTheDirectionOfTravel()
        {
            // Keyboard-only play never supplies an aim axis. Without this the skill would not exist.
            var simulation = Corridor();

            simulation.Tick(new PlayerIntent(0, 100, IntentButtons.Skill2));

            Assert.That(simulation.State.Projectiles[0].VelocityY, Is.GreaterThan(0));
        }

        [Test]
        public void AShotDamagesAnEnemyWithoutKillingItOutright()
        {
            var simulation = new GameSimulation(
                Config(),
                LevelLayout.Parse(
                    "#########",
                    "#P.....E#",
                    "#########"),
                seed: 1u);

            var before = simulation.State.Enemies[0].Health.Current;

            simulation.Tick(Shoot(100, 0));
            Advance(simulation, ShotTicks);

            var after = simulation.State.Enemies[0].Health.Current;

            Assert.That(after, Is.LessThan(before), "the shot must connect");
            Assert.That(simulation.State.Enemies[0].IsActive, Is.True,
                "a shot that killed outright would make the maze irrelevant");
        }

        [Test]
        public void AShotStopsWhenItHits()
        {
            var simulation = new GameSimulation(
                Config(),
                LevelLayout.Parse(
                    "#########",
                    "#P.....E#",
                    "#########"),
                seed: 1u);

            simulation.Tick(Shoot(100, 0));
            Advance(simulation, 16);

            // Well inside the shot's flight time, so it can only have gone by connecting.
            Assert.That(simulation.State.Enemies[0].Health.Current,
                Is.LessThan(simulation.State.Enemies[0].Health.Max), "the shot must have connected");
            Assert.That(simulation.State.Projectiles.ActiveCount, Is.Zero,
                "a shot must not carry on through what it hit");
        }

        [Test]
        public void AShotIsStoppedByADestructibleBlockButDoesNotBreakIt()
        {
            // Load-bearing: bombs stay the only way to open the arena, so the maze is real cover.
            var simulation = new GameSimulation(
                Config(),
                LevelLayout.Parse(
                    "#######",
                    "#P.X.E#",
                    "#######"),
                seed: 1u);

            var block = new GridCoord(3, 1);

            simulation.Tick(Shoot(100, 0));
            Advance(simulation, ShotTicks);

            Assert.That(simulation.State.Board[block], Is.EqualTo(TileType.Destructible),
                "a skillshot must not clear the maze");
            Assert.That(simulation.State.Enemies[0].Health.Current,
                Is.EqualTo(simulation.State.Enemies[0].Health.Max),
                "the block must have absorbed the shot");
        }

        [Test]
        public void AShotIsStoppedByAWall()
        {
            var simulation = new GameSimulation(
                Config(),
                LevelLayout.Parse(
                    "#####",
                    "#.P.#",
                    "#####"),
                seed: 1u);

            simulation.Tick(Shoot(100, 0));
            Advance(simulation, 10);

            Assert.That(simulation.State.Projectiles.ActiveCount, Is.Zero);
        }

        [Test]
        public void AShotIsNotEatenByTheBombAtYourFeet()
        {
            var simulation = Corridor();

            simulation.Tick(new PlayerIntent(0, 0, IntentButtons.Bomb));
            simulation.Tick(Shoot(100, 0));
            Advance(simulation, 3);

            Assert.That(simulation.State.Projectiles.ActiveCount, Is.EqualTo(1),
                "a shot swallowed by your own bomb reads as a bug every single time");
        }

        [Test]
        public void AShotExpiresAfterItsFlightTime()
        {
            var simulation = Corridor();

            simulation.Tick(Shoot(0, 100));
            Assert.That(simulation.State.Projectiles.ActiveCount, Is.EqualTo(1));

            Advance(simulation, ShotTicks + 2);

            Assert.That(simulation.State.Projectiles.ActiveCount, Is.Zero);
        }

        [Test]
        public void AShotThatCannotSpawn_DoesNotSpendItsCharge()
        {
            // A skill that costs a cooldown and does nothing is indistinguishable from a misfire.
            // One projectile slot and a two-tick cooldown, fired down the long axis of the room so
            // the first shot is certainly still in flight when the charge comes back.
            var simulation = Corridor(Config(shotCooldown: 2, maxProjectiles: 1));

            simulation.Tick(Shoot(100, 0));
            Assert.That(simulation.State.Projectiles.ActiveCount, Is.EqualTo(1));

            Advance(simulation, 4);
            Assert.That(simulation.State.Player.Skills[1].Charges, Is.EqualTo(1),
                "the charge must have come back, or this proves nothing");
            Assert.That(simulation.State.Projectiles.ActiveCount, Is.EqualTo(1),
                "the pool must still be full, or this proves nothing");

            simulation.Tick(Shoot(100, 0));

            Assert.That(simulation.State.Player.Skills[1].Charges, Is.EqualTo(1),
                "a shot with nowhere to go must not consume a charge");
        }

        [Test]
        public void FiringAnnouncesItself()
        {
            var simulation = Corridor();
            simulation.Tick(Shoot(100, 0));

            Assert.That(CountEvents(simulation, SimEventType.ProjectileFired), Is.EqualTo(1));
        }

        // ---------- guarantees ----------

        [Test]
        public void SkillsAreDeterministic()
        {
            GameSimulation Create() => new GameSimulation(
                Config(),
                LevelLayout.Parse(
                    "#########",
                    "#P..E...#",
                    "#.#.#.#.#",
                    "#...E...#",
                    "#########"),
                seed: 99u);

            var first = Create();
            var second = Create();

            for (var i = 0; i < 400; i++)
            {
                var buttons = IntentButtons.None;

                if (i % 37 == 0)
                {
                    buttons = buttons.With(IntentButtons.Skill1);
                }

                if (i % 23 == 0)
                {
                    buttons = buttons.With(IntentButtons.Skill2);
                }

                if (i % 53 == 0)
                {
                    buttons = buttons.With(IntentButtons.Bomb);
                }

                var direction = Directions.Cardinals[(i / 17) % 4].ToOffset();
                var intent = new PlayerIntent(
                    (sbyte)(direction.X * PlayerIntent.AxisRange),
                    (sbyte)(direction.Y * PlayerIntent.AxisRange),
                    buttons,
                    (sbyte)(i % 200 - 100),
                    (sbyte)(97 - (i % 200)));

                first.Tick(intent);
                second.Tick(intent);

                Assert.That(second.ComputeStateHash(), Is.EqualTo(first.ComputeStateHash()),
                    $"simulations diverged on tick {i + 1}");
            }
        }

        [Test]
        public void SkillsAllocateNothing()
        {
            var simulation = new GameSimulation(
                Config(dashCooldown: 4),
                LevelLayout.Parse(
                    "###########",
                    "#P..E.....#",
                    "#.#.#.#.#.#",
                    "#...E.....#",
                    "###########"),
                seed: 7u);

            PlayerIntent Intent(int i) => new PlayerIntent(
                (sbyte)(i % 3 == 0 ? PlayerIntent.AxisRange : 0),
                0,
                (i % 5 == 0 ? IntentButtons.Skill1 : IntentButtons.None)
                    .With(i % 7 == 0 ? IntentButtons.Skill2 : IntentButtons.None),
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
                "dashing and firing must not produce garbage mid-match");
        }
    }
}
