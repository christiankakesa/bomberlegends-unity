using BomberLegends.Core;
using BomberLegends.Simulation;
using BomberLegends.Simulation.Board;
using BomberLegends.Simulation.Events;
using NUnit.Framework;

namespace BomberLegends.Tests.EditMode.Simulation
{
    /// <summary>
    /// Covers health, the immunity window, blast and contact damage, and the pursuing enemy.
    /// </summary>
    public sealed class HealthAndEnemyTests
    {
        private const int Fuse = 30;
        private const int Linger = 12;
        private const int Immunity = 30;

        private static SimulationConfig Config(
            int range = 2,
            int fuse = Fuse,
            int blastToPlayer = 34,
            int contact = 10,
            int immunity = Immunity,
            int enemySpeed = 80) =>
            new SimulationConfig(
                moveSpeedPerTick: 133,
                laneSnapPerTick: 200,
                turnTolerance: 300,
                directionDeadzone: PlayerIntent.DefaultDeadzone,
                cornerAssistEnabled: true,
                fuseTicks: fuse,
                blastLingerTicks: Linger,
                bombCooldownTicks: 0,
                startingBombCapacity: 1,
                startingBlastRange: range,
                maxBombs: 16,
                playerRadius: 340,
                cornerSlipPerTick: 90,
                cornerSlipTolerance: 320,
                playerMaxHealth: 100,
                blastDamageToPlayer: blastToPlayer,
                enemyContactDamage: contact,
                invulnerabilityTicks: immunity,
                enemyMaxHealth: 100,
                blastDamageToEnemy: 100,
                enemySpeedPerTick: enemySpeed,
                enemyRadius: 320,
                maxEnemies: 32);

        private static PlayerIntent Bomb => new PlayerIntent(0, 0, IntentButtons.Bomb);

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

        /// <summary>An empty room with the player in the middle and no enemies.</summary>
        private static GameSimulation EmptyRoom(SimulationConfig? config = null) =>
            new GameSimulation(
                config ?? Config(),
                LevelLayout.Parse(
                    "#########",
                    "#.......#",
                    "#.......#",
                    "#.......#",
                    "#...P...#",
                    "#.......#",
                    "#.......#",
                    "#.......#",
                    "#########"),
                seed: 1u);

        [Test]
        public void ThePlayerStartsAtFullHealth()
        {
            var simulation = EmptyRoom();

            Assert.That(simulation.State.Player.Health.Current, Is.EqualTo(100));
            Assert.That(simulation.State.Player.Health.IsAlive, Is.True);
            Assert.That(simulation.State.Player.Health.IsInvulnerable, Is.False);
        }

        [Test]
        public void StandingInYourOwnBlast_TakesOneLargeHit_NotOnePerTick()
        {
            // The whole point of the immunity window. The blast lingers for many ticks; without it
            // the player would take a hit every one of them and die instantly.
            var simulation = EmptyRoom();

            simulation.Tick(Bomb);
            Advance(simulation, Fuse + Linger + 5);

            Assert.That(simulation.State.Player.Health.Current, Is.EqualTo(66),
                $"a single blast must cost one hit, not {Linger}");
        }

        [Test]
        public void YourOwnBombTakesALargeShareOfHealth()
        {
            var simulation = EmptyRoom();
            var max = simulation.State.Player.Health.Max;

            simulation.Tick(Bomb);
            Advance(simulation, Fuse + 2);

            var lost = max - simulation.State.Player.Health.Current;
            Assert.That(lost, Is.GreaterThanOrEqualTo(max / 4),
                "self-trapping must stay frightening, or the Bomberman layer loses its tension");
        }

        [Test]
        public void ThreeOwnBlasts_KillThePlayer()
        {
            var simulation = EmptyRoom();

            for (var i = 0; i < 3; i++)
            {
                simulation.Tick(Bomb);
                Advance(simulation, Fuse + Linger + Immunity + 2);
            }

            Assert.That(simulation.State.Player.Health.IsAlive, Is.False);
            Assert.That(simulation.Phase, Is.EqualTo(MatchPhase.Defeat));
        }

        [Test]
        public void RunningOutOfHealth_EndsTheMatchAndAnnouncesIt()
        {
            var simulation = EmptyRoom(Config(blastToPlayer: 100));

            simulation.Tick(Bomb);

            var died = 0;
            for (var i = 0; i < Fuse + 5; i++)
            {
                simulation.Tick(Idle);
                died += CountEvents(simulation, SimEventType.PlayerDied);
            }

            Assert.That(died, Is.EqualTo(1), "death must be announced exactly once");
            Assert.That(simulation.Phase, Is.EqualTo(MatchPhase.Defeat));
        }

        [Test]
        public void OnceDefeated_TheSimulationStopsAdvancingTheWorld()
        {
            var simulation = EmptyRoom(Config(blastToPlayer: 100));
            simulation.Tick(Bomb);
            Advance(simulation, Fuse + 5);

            var frozen = simulation.State.Player.Position;
            Advance(simulation, 60, PlayerIntent.FromDirection(Direction.East));

            Assert.That(simulation.State.Player.Position, Is.EqualTo(frozen),
                "a finished match must not keep simulating");
        }

        [Test]
        public void AnEnemySpawnsWhereTheLayoutPlacesIt()
        {
            var simulation = new GameSimulation(
                Config(),
                LevelLayout.Parse(
                    "#######",
                    "#P...E#",
                    "#######"),
                seed: 1u);

            Assert.That(simulation.State.Enemies.AliveCount, Is.EqualTo(1));
            Assert.That(simulation.State.Enemies[0].Tile, Is.EqualTo(new GridCoord(5, 1)));
            Assert.That(simulation.State.Enemies[0].Health.Current, Is.EqualTo(100));
        }

        [Test]
        public void AnEnemyStandingInABlast_Dies()
        {
            // The enemy is two tiles east, within a range-2 blast.
            var simulation = new GameSimulation(
                Config(range: 2, enemySpeed: 1),
                LevelLayout.Parse(
                    "#######",
                    "#P.E..#",
                    "#######"),
                seed: 1u);

            Assert.That(simulation.State.Enemies.AliveCount, Is.EqualTo(1));

            simulation.Tick(Bomb);

            var killed = 0;
            for (var i = 0; i < Fuse + 5; i++)
            {
                simulation.Tick(Idle);
                killed += CountEvents(simulation, SimEventType.EnemyKilled);
            }

            Assert.That(killed, Is.EqualTo(1));
            Assert.That(simulation.State.Enemies.AliveCount, Is.Zero);
        }

        [Test]
        public void AnEnemyPursuesThePlayer()
        {
            var simulation = new GameSimulation(
                Config(),
                LevelLayout.Parse(
                    "#########",
                    "#P.....E#",
                    "#########"),
                seed: 1u);

            var start = simulation.State.Enemies[0].Position.X;

            Advance(simulation, 40);

            Assert.That(simulation.State.Enemies[0].Position.X, Is.LessThan(start),
                "the enemy must close on the player, not wander");
        }

        [Test]
        public void AnEnemyTouchingThePlayer_ChipsThemRatherThanKillingThem()
        {
            var simulation = new GameSimulation(
                Config(),
                LevelLayout.Parse(
                    "#######",
                    "#P.E..#",
                    "#######"),
                seed: 1u);

            Advance(simulation, 60);

            var health = simulation.State.Player.Health;
            Assert.That(health.Current, Is.LessThan(health.Max), "contact must hurt");
            Assert.That(health.IsAlive, Is.True,
                "but a single enemy must not delete the player while they stand still");
        }

        [Test]
        public void ContactDamage_IsLimitedByTheImmunityWindow()
        {
            var simulation = new GameSimulation(
                Config(contact: 10, immunity: 60),
                LevelLayout.Parse(
                    "#######",
                    "#P.E..#",
                    "#######"),
                seed: 1u);

            Advance(simulation, 120);

            var lost = simulation.State.Player.Health.Max - simulation.State.Player.Health.Current;
            Assert.That(lost, Is.LessThanOrEqualTo(30),
                "immunity must cap how fast a touching enemy can drain the player");
        }

        [Test]
        public void AnEnemyIsBlockedByBombsJustAsThePlayerIs()
        {
            var simulation = new GameSimulation(
                Config(fuse: 600),
                LevelLayout.Parse(
                    "#########",
                    "#P.....E#",
                    "#########"),
                seed: 1u);

            // Drop a bomb, step aside, and let the enemy run into it.
            simulation.Tick(Bomb);
            Advance(simulation, 200);

            Assert.That(simulation.State.Enemies[0].Tile.X, Is.GreaterThan(simulation.State.Player.Tile.X),
                "the bomb must stop the enemy reaching the player, exactly as it would stop the player");
        }

        [Test]
        public void EnemiesAndDamage_AreDeterministic()
        {
            GameSimulation Create() => new GameSimulation(
                Config(),
                LevelLayout.Parse(
                    "#########",
                    "#P..E...#",
                    "#.#.#.#.#",
                    "#...E...#",
                    "#########"),
                seed: 4242u);

            var first = Create();
            var second = Create();

            for (var i = 0; i < 400; i++)
            {
                var intent = i % 53 == 0
                    ? Bomb
                    : PlayerIntent.FromDirection(Directions.Cardinals[(i / 17) % 4]);

                first.Tick(intent);
                second.Tick(intent);

                Assert.That(second.ComputeStateHash(), Is.EqualTo(first.ComputeStateHash()),
                    $"simulations diverged on tick {i + 1}");
            }
        }

        [Test]
        public void EnemiesAndDamage_AllocateNothing()
        {
            var simulation = new GameSimulation(
                Config(),
                LevelLayout.Parse(
                    "#########",
                    "#P..E...#",
                    "#.#.#.#.#",
                    "#...E...#",
                    "#########"),
                seed: 7u);

            for (var i = 0; i < 200; i++)
            {
                simulation.Tick(i % 40 == 0 ? Bomb : Idle);
            }

            var before = System.GC.GetAllocatedBytesForCurrentThread();

            for (var i = 0; i < 5000; i++)
            {
                simulation.Tick(i % 40 == 0 ? Bomb : Idle);
            }

            Assert.That(System.GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero,
                "chasing enemies and resolving damage must not produce garbage mid-match");
        }
    }
}
