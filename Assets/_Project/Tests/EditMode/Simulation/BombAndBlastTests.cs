using BomberLegends.Core;
using BomberLegends.Simulation;
using BomberLegends.Simulation.Board;
using BomberLegends.Simulation.Events;
using NUnit.Framework;

namespace BomberLegends.Tests.EditMode.Simulation
{
    /// <summary>
    /// Covers the core verb: placing a bomb, the blast it throws, and everything that blast sets off.
    /// </summary>
    public sealed class BombAndBlastTests
    {
        private const int Fuse = 30;
        private const int Linger = 6;

        private static SimulationConfig Config(
            int range = 2, int capacity = 1, int cooldown = 0, int fuse = Fuse) =>
            new SimulationConfig(
                moveSpeedPerTick: 133,
                laneSnapPerTick: 200,
                turnTolerance: 300,
                directionDeadzone: PlayerIntent.DefaultDeadzone,
                cornerAssistEnabled: true,
                fuseTicks: fuse,
                blastLingerTicks: Linger,
                bombCooldownTicks: cooldown,
                startingBombCapacity: capacity,
                startingBlastRange: range,
                maxBombs: 16);

        /// <summary>An open room with a solid border, seven by seven of floor.</summary>
        private static GameSimulation OpenRoom(SimulationConfig? config = null) =>
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

        /// <summary>
        /// Ticks until the given event appears and returns how many fired on that tick.
        /// </summary>
        /// <remarks>
        /// Events live for exactly one tick, so counting ticks by hand to land on a detonation is
        /// brittle. Advancing until the event happens is both robust and closer to how the view
        /// actually consumes them.
        /// </remarks>
        private static int AdvanceUntilEvent(
            GameSimulation simulation, SimEventType type, int maxTicks = 800)
        {
            for (var i = 0; i < maxTicks; i++)
            {
                simulation.Tick(Idle);

                var count = CountEvents(simulation, type);
                if (count > 0)
                {
                    return count;
                }
            }

            return 0;
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

        [Test]
        public void PressingBomb_PlacesOneOnThePlayersTile()
        {
            var simulation = OpenRoom();
            var tile = simulation.State.Player.Tile;

            simulation.Tick(Bomb);

            Assert.That(simulation.State.BombGrid.HasBomb(tile), Is.True);
            Assert.That(simulation.State.Player.ActiveBombs, Is.EqualTo(1));
            Assert.That(CountEvents(simulation, SimEventType.BombPlaced), Is.EqualTo(1));
        }

        [Test]
        public void HoldingTheButton_PlacesOnlyOneBomb()
        {
            var simulation = OpenRoom(Config(capacity: 4));

            Advance(simulation, 10, Bomb);

            Assert.That(simulation.State.Player.ActiveBombs, Is.EqualTo(1),
                "placement triggers on the press, not on the button being down");
        }

        [Test]
        public void Capacity_LimitsHowManyBombsAreOnTheBoard()
        {
            var simulation = OpenRoom(Config(capacity: 2));

            // Place, release, move, place, release, try again.
            simulation.Tick(Bomb);
            simulation.Tick(Idle);
            Advance(simulation, 8, PlayerIntent.FromDirection(Direction.East));
            simulation.Tick(Bomb);
            simulation.Tick(Idle);
            Advance(simulation, 8, PlayerIntent.FromDirection(Direction.East));
            simulation.Tick(Bomb);

            Assert.That(simulation.State.Player.ActiveBombs, Is.EqualTo(2));
        }

        [Test]
        public void ABombReturnsToThePool_WhenItDetonates()
        {
            var simulation = OpenRoom();
            simulation.Tick(Bomb);
            Assert.That(simulation.State.Player.ActiveBombs, Is.EqualTo(1));

            Advance(simulation, Fuse + 1);

            Assert.That(simulation.State.Player.ActiveBombs, Is.EqualTo(0),
                "the classic model returns the bomb on detonation, so the placement rate is the fuse");
        }

        [Test]
        public void TwoBombsCannotShareATile()
        {
            var simulation = OpenRoom(Config(capacity: 2));
            simulation.Tick(Bomb);
            simulation.Tick(Idle);
            simulation.Tick(Bomb);

            Assert.That(simulation.State.Player.ActiveBombs, Is.EqualTo(1));
        }

        [Test]
        public void ThePlayerCanWalkOffTheBombTheyJustPlaced_ButNotBackOntoIt()
        {
            // A fuse long enough to outlive the walk; otherwise the bomb frees the tile mid-test and
            // the player walks back through empty space, proving nothing.
            var simulation = OpenRoom(Config(fuse: 600));
            var origin = simulation.State.Player.Tile;

            simulation.Tick(Bomb);
            Advance(simulation, 12, PlayerIntent.FromDirection(Direction.East));

            Assert.That(simulation.State.Player.Tile, Is.Not.EqualTo(origin),
                "a player must be able to escape their own bomb");

            Advance(simulation, 20, PlayerIntent.FromDirection(Direction.West));

            Assert.That(simulation.State.Player.Tile, Is.Not.EqualTo(origin),
                "and must not be able to walk back onto it");
        }

        [Test]
        public void ABlast_ReachesExactlyItsRangeInEachDirection()
        {
            var simulation = OpenRoom(Config(range: 2));
            var origin = simulation.State.Player.Tile;

            simulation.Tick(Bomb);
            Advance(simulation, Fuse);

            var blasts = simulation.State.BlastGrid;
            foreach (var direction in Directions.Cardinals)
            {
                Assert.That(blasts.IsLethal(origin.Step(direction, 1)), Is.True);
                Assert.That(blasts.IsLethal(origin.Step(direction, 2)), Is.True);
                Assert.That(blasts.IsLethal(origin.Step(direction, 3)), Is.False,
                    "the blast must stop at its range");
            }

            Assert.That(blasts.IsLethal(origin), Is.True, "the bomb's own tile burns too");
        }

        [Test]
        public void ABlast_StopsAtPermanentStructure()
        {
            // The player starts one tile from the west wall.
            var simulation = new GameSimulation(
                Config(range: 4),
                LevelLayout.Parse(
                    "#####",
                    "#P..#",
                    "#####"),
                seed: 1u);

            simulation.Tick(Bomb);
            Advance(simulation, Fuse);

            Assert.That(simulation.State.BlastGrid.IsLethal(new GridCoord(0, 1)), Is.False,
                "a blast must not reach through the wall itself");
            Assert.That(simulation.State.BlastGrid.IsLethal(new GridCoord(3, 1)), Is.True);
        }

        [Test]
        public void ABlast_DestroysOneBlockAndStops()
        {
            var simulation = new GameSimulation(
                Config(range: 4),
                LevelLayout.Parse(
                    "#######",
                    "#P.XX.#",
                    "#######"),
                seed: 1u);

            simulation.Tick(Bomb);
            Advance(simulation, Fuse);

            Assert.That(simulation.State.Board[new GridCoord(3, 1)], Is.EqualTo(TileType.Empty),
                "the first block in the arm is destroyed");
            Assert.That(simulation.State.Board[new GridCoord(4, 1)], Is.EqualTo(TileType.Destructible),
                "and the one behind it survives, because the arm stops there");
            Assert.That(simulation.State.BlastGrid.IsLethal(new GridCoord(4, 1)), Is.False);
        }

        [Test]
        public void ABlastTile_StopsBeingLethalAfterItsDuration()
        {
            var simulation = OpenRoom();
            var origin = simulation.State.Player.Tile;

            simulation.Tick(Bomb);
            Advance(simulation, Fuse);
            Assert.That(simulation.State.BlastGrid.IsLethal(origin), Is.True);

            Advance(simulation, Linger);

            Assert.That(simulation.State.BlastGrid.IsLethal(origin), Is.False);
        }

        [Test]
        public void OneEventIsRaisedPerBlastTile_NotPerArmThatReachesIt()
        {
            var simulation = OpenRoom(Config(range: 2));

            simulation.Tick(Bomb);

            // A range-2 cross covers its own tile plus two along each of four arms.
            Assert.That(AdvanceUntilEvent(simulation, SimEventType.BlastSpawned), Is.EqualTo(9));
        }

        [Test]
        public void ABlastSetsOffAnotherBomb_InTheSameTick()
        {
            var simulation = OpenRoom(Config(range: 2, capacity: 2, fuse: 200));

            simulation.Tick(Bomb);
            simulation.Tick(Idle);
            Advance(simulation, 8, PlayerIntent.FromDirection(Direction.East));
            simulation.Tick(Bomb);

            // The second bomb is ten ticks younger, so its own fuse is nowhere near done. It can
            // only go off because the first one reaches it.
            var detonations = AdvanceUntilEvent(simulation, SimEventType.BombDetonated);

            Assert.That(detonations, Is.EqualTo(2),
                "both bombs must detonate on the same tick, not one after the other");
            Assert.That(simulation.State.Player.ActiveBombs, Is.EqualTo(0));
        }

        [Test]
        public void ALongChain_ResolvesCompletelyInOneTick()
        {
            // Eight bombs in a row, each within range of the next.
            var simulation = new GameSimulation(
                Config(range: 2, capacity: 8, fuse: 300),
                LevelLayout.Parse(
                    "##########",
                    "#P.......#",
                    "##########"),
                seed: 1u);

            for (var i = 0; i < 8; i++)
            {
                simulation.Tick(Bomb);
                simulation.Tick(Idle);

                if (i < 7)
                {
                    Advance(simulation, 8, PlayerIntent.FromDirection(Direction.East));
                }
            }

            Assert.That(simulation.State.Player.ActiveBombs, Is.EqualTo(8));

            // The first bomb placed reaches its fuse first and should take the rest with it.
            var detonations = AdvanceUntilEvent(simulation, SimEventType.BombDetonated);

            Assert.That(detonations, Is.EqualTo(8),
                "every bomb in the chain detonates exactly once, all on the same tick");
            Assert.That(simulation.State.Player.ActiveBombs, Is.EqualTo(0),
                "a chain must run to its end, however long it is");
        }

        [Test]
        public void ARingOfBombs_Terminates()
        {
            // A closed loop: without a guard, each bomb would keep re-queueing its neighbours.
            var simulation = new GameSimulation(
                Config(range: 3, capacity: 8, fuse: 300),
                LevelLayout.Parse(
                    "#####",
                    "#P..#",
                    "#...#",
                    "#...#",
                    "#####"),
                seed: 1u);

            void PlaceThenMove(Direction direction, int ticks)
            {
                simulation.Tick(Bomb);
                simulation.Tick(Idle);
                Advance(simulation, ticks, PlayerIntent.FromDirection(direction));
            }

            PlaceThenMove(Direction.East, 16);
            PlaceThenMove(Direction.South, 16);
            PlaceThenMove(Direction.West, 16);
            simulation.Tick(Bomb);

            Assert.That(simulation.State.Player.ActiveBombs, Is.EqualTo(4));

            var detonations = AdvanceUntilEvent(simulation, SimEventType.BombDetonated);

            Assert.That(detonations, Is.EqualTo(4),
                "every bomb in a ring detonates once and the resolution terminates");
            Assert.That(simulation.State.Player.ActiveBombs, Is.EqualTo(0));
        }

        [Test]
        public void ABombDetonating_LeavesItsOwnTileBurningForTheFullDuration()
        {
            var simulation = OpenRoom();
            var origin = simulation.State.Player.Tile;

            simulation.Tick(Bomb);
            AdvanceUntilEvent(simulation, SimEventType.BombDetonated);

            Assert.That(simulation.State.BlastGrid.TicksRemainingAt(origin), Is.EqualTo(Linger),
                "a tile lit this tick must not lose a tick to the same pass that created it");
        }

        [Test]
        public void OverlappingBlasts_KeepTheLongerDuration()
        {
            // Timing two explosions to overlap through the simulation is brittle; the rule belongs
            // to the grid, so it is checked there directly.
            var grid = new BlastGrid(3, 3);
            var tile = new GridCoord(1, 1);

            Assert.That(grid.Ignite(tile, 6), Is.True, "a clear tile reports that it newly caught");
            grid.Decay();
            grid.Decay();
            grid.Decay();
            Assert.That(grid.TicksRemainingAt(tile), Is.EqualTo(3));

            Assert.That(grid.Ignite(tile, 6), Is.False,
                "an already-burning tile must not raise a second effect");
            Assert.That(grid.TicksRemainingAt(tile), Is.EqualTo(6),
                "a tile caught again must not go out early");

            grid.Ignite(tile, 2);
            Assert.That(grid.TicksRemainingAt(tile), Is.EqualTo(6),
                "and a shorter blast must not cut a longer one short");
        }

        [Test]
        public void ABombBlocksMovement_ForAPlayerApproachingIt()
        {
            // A long fuse, so the bomb is still there when the player tries to come back.
            var simulation = OpenRoom(Config(fuse: 600));

            simulation.Tick(Bomb);
            Advance(simulation, 10, PlayerIntent.FromDirection(Direction.North));
            var afterLeaving = simulation.State.Player.Tile;

            Advance(simulation, 30, PlayerIntent.FromDirection(Direction.South));

            Assert.That(simulation.State.Player.Tile.Y, Is.GreaterThanOrEqualTo(afterLeaving.Y),
                "the bomb should stop the player coming back down onto it");
        }

        [Test]
        public void TheCooldownModel_CanBeEnabledForComparison()
        {
            // The design document's alternative: a wait after placement even with a bomb free.
            var simulation = OpenRoom(Config(capacity: 4, cooldown: 20, fuse: 600));

            simulation.Tick(Bomb);
            simulation.Tick(Idle);
            Advance(simulation, 8, PlayerIntent.FromDirection(Direction.East));
            simulation.Tick(Bomb);

            Assert.That(simulation.State.Player.ActiveBombs, Is.EqualTo(1),
                "with a cooldown running, a second bomb is refused even though the pool has room");

            Advance(simulation, 20);
            simulation.Tick(Idle);
            simulation.Tick(Bomb);

            Assert.That(simulation.State.Player.ActiveBombs, Is.EqualTo(2));
        }

        [Test]
        public void BombsAndBlasts_AllocateNothing()
        {
            var simulation = OpenRoom(Config(range: 3, capacity: 4));

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
                "placing and detonating bombs must not produce garbage mid-match");
        }

        [Test]
        public void ChainsAreDeterministic()
        {
            var first = OpenRoom(Config(range: 2, capacity: 4));
            var second = OpenRoom(Config(range: 2, capacity: 4));

            for (var i = 0; i < 400; i++)
            {
                var intent = i % 37 == 0
                    ? Bomb
                    : PlayerIntent.FromDirection(
                        Directions.Cardinals[(i / 11) % 4]);

                first.Tick(intent);
                second.Tick(intent);

                Assert.That(second.ComputeStateHash(), Is.EqualTo(first.ComputeStateHash()),
                    $"simulations diverged on tick {i + 1}");
            }
        }
    }
}
