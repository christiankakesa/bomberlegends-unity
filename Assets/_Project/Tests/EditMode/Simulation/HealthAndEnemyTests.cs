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

        /// <summary>
        /// The arena the Match scene actually ships, which is where the wedging was reported.
        /// </summary>
        /// <remarks>
        /// Reproduced only in this shape. A plain pillar corridor never wedges, because the bug
        /// needs the tight alternating pillars <i>and</i> destructible blocks opening mid-match to
        /// knock an enemy off-centre in the first place.
        /// </remarks>
        private static GameSimulation ShippedArena(uint seed, int laneSnap = 200) =>
            new GameSimulation(
                new SimulationConfig(
                    moveSpeedPerTick: 133,
                    laneSnapPerTick: laneSnap,
                    turnTolerance: 300,
                    directionDeadzone: PlayerIntent.DefaultDeadzone,
                    cornerAssistEnabled: true,
                    fuseTicks: Fuse,
                    blastLingerTicks: Linger,
                    bombCooldownTicks: 0,
                    startingBombCapacity: 1,
                    startingBlastRange: 2,
                    maxBombs: 16,
                    playerRadius: 340,
                    cornerSlipPerTick: 90,
                    cornerSlipTolerance: 320,
                    playerMaxHealth: 100,
                    // The player is made unkillable so the match runs its full length. A finished
                    // match freezes every system, and counting past that measures nothing at all.
                    blastDamageToPlayer: 0,
                    enemyContactDamage: 0,
                    invulnerabilityTicks: Immunity,
                    enemyMaxHealth: 100,
                    blastDamageToEnemy: 100,
                    enemySpeedPerTick: 80,
                    enemyRadius: 320,
                    maxEnemies: 32),
                LevelLayout.Parse(
                    "#########################",
                    "#P...X...X...X...X...X..#",
                    "#.#.#.#.#.#.#.#.#.#.#.#.#",
                    "#..X...XE..X...X...X...X#",
                    "#.#.#.#.#.#.#.#.#.#.#.#.#",
                    "#X...X...X...X..EX...X..#",
                    "#.#.#.#.#.#.#.#.#.#.#.#.#",
                    "#..X...X...X...X...X...X#",
                    "#.#.#.#.#.#.#.#.#.#.#.#.#",
                    "#X...XE..X...X...X...X..#",
                    "#.#.#.#.#.#.#.#.#.#.#.#.#",
                    "#..X...X...X...X..EX...X#",
                    "#.#.#.#.#.#.#.#.#.#.#.#.#",
                    "#....X...X...X...X...X..#",
                    "#.#.#.#.#.#.#.#.#.#.#.#.#",
                    "#......X...XE..X...X...X#",
                    "#########################"),
                seed);

        /// <summary>
        /// The longest any enemy spends penned inside a single tile of the board.
        /// </summary>
        /// <remarks>
        /// Confinement, not a frozen position, is what "stuck" actually looks like. A wedged enemy
        /// is still moving — it jitters a few units against the corner it is caught on — so an
        /// equality check on position reports everything as fine while the player watches an enemy
        /// vibrate in a doorway for twenty seconds.
        /// </remarks>
        private static int LongestConfinement(GameSimulation simulation, int ticks)
        {
            var capacity = simulation.State.Enemies.Capacity;
            var anchor = new GridCoord[capacity];
            var held = new int[capacity];
            var worst = 0;

            for (var slot = 0; slot < capacity; slot++)
            {
                anchor[slot] = simulation.State.Enemies[slot].Tile;
            }

            for (var tick = 0; tick < ticks; tick++)
            {
                var direction = ((tick / 90) % 4) switch
                {
                    0 => Direction.East,
                    1 => Direction.South,
                    2 => Direction.West,
                    _ => Direction.North
                };

                // Bombing as well as walking, because the maze has to open up: the wedge needs a
                // destructible block to be gone before an enemy can reach the corner that traps it.
                var buttons = tick % 70 == 0 ? IntentButtons.Bomb : IntentButtons.None;
                simulation.Tick(PlayerIntent.FromDirection(direction, buttons));

                if (simulation.Phase != MatchPhase.Playing)
                {
                    break;
                }

                for (var slot = 0; slot < capacity; slot++)
                {
                    var enemy = simulation.State.Enemies[slot];
                    if (!enemy.IsActive)
                    {
                        continue;
                    }

                    var distance = enemy.Tile.ManhattanDistanceTo(anchor[slot]);

                    if (distance <= 1)
                    {
                        held[slot]++;
                        if (held[slot] > worst)
                        {
                            worst = held[slot];
                        }

                        continue;
                    }

                    held[slot] = 0;
                    anchor[slot] = enemy.Tile;
                }
            }

            return worst;
        }

        [Test]
        public void EnemiesDoNotWedgeOnPillarCorners()
        {
            // Reported from play as enemies stuck on the corner of a blue pillar. The chase reasons
            // in whole tiles but the body is a box: an enemy knocked off-centre reads the tile ahead
            // as open while its box clips the pillar beside it, so it shuffles against the corner
            // indefinitely. Lane centring keeps the two in agreement by never letting it drift.
            for (var seed = 1u; seed <= 4u; seed++)
            {
                var confined = LongestConfinement(ShippedArena(seed), ticks: 900);

                Assert.That(confined, Is.LessThan(150),
                    $"an enemy spent {confined} ticks penned in one tile on seed {seed}");
            }
        }

        [Test]
        public void LaneCentringIsWhatKeepsEnemiesMoving()
        {
            // Guards the fix against being quietly undone. Without centring an enemy spends almost
            // the entire match in one tile; with it, a few dozen ticks at most.
            var without = LongestConfinement(ShippedArena(1u, laneSnap: 0), ticks: 900);
            var with = LongestConfinement(ShippedArena(1u), ticks: 900);

            Assert.That(without, Is.GreaterThan(400),
                "the bug must still be reproducible, or this test proves nothing");
            Assert.That(with * 4, Is.LessThan(without),
                $"centring must dramatically unstick enemies, but went {without} -> {with}");
        }

        [Test]
        public void EnemiesInAMazeCloseOnThePlayer()
        {
            var simulation = new GameSimulation(
                Config(enemySpeed: 80),
                LevelLayout.Parse(
                    "#############",
                    "#P.........E#",
                    "#.#.#.#.#.#.#",
                    "#E..........#",
                    "#.#.#.#.#.#.#",
                    "#.........E.#",
                    "#############"),
                seed: 4u);

            var start = 0;
            var capacity = simulation.State.Enemies.Capacity;

            for (var slot = 0; slot < capacity; slot++)
            {
                var enemy = simulation.State.Enemies[slot];
                if (enemy.IsActive)
                {
                    start += enemy.Tile.ManhattanDistanceTo(simulation.State.Player.Tile);
                }
            }

            Advance(simulation, 300);

            var ended = 0;
            for (var slot = 0; slot < capacity; slot++)
            {
                var enemy = simulation.State.Enemies[slot];
                if (enemy.IsActive)
                {
                    ended += enemy.Tile.ManhattanDistanceTo(simulation.State.Player.Tile);
                }
            }

            Assert.That(ended, Is.LessThan(start), "a maze must slow the chase, not defeat it");
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
