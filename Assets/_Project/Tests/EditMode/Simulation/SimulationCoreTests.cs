using System;
using BomberLegends.Core;
using BomberLegends.Simulation;
using BomberLegends.Simulation.Board;
using BomberLegends.Simulation.Events;
using NUnit.Framework;

namespace BomberLegends.Tests.EditMode.Simulation
{
    /// <summary>Covers the board grid, its bounds behaviour and its hash.</summary>
    public sealed class BoardStateTests
    {
        [Test]
        public void NewBoard_IsEntirelyEmpty()
        {
            var board = new BoardState(13, 11);

            Assert.That(board.TileCount, Is.EqualTo(143));
            for (var y = 0; y < board.Height; y++)
            {
                for (var x = 0; x < board.Width; x++)
                {
                    Assert.That(board[new GridCoord(x, y)], Is.EqualTo(TileType.Empty));
                }
            }
        }

        [Test]
        public void Constructor_WithNonPositiveDimensions_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new BoardState(0, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new BoardState(5, -1));
        }

        [Test]
        public void ReadingOutsideTheBoard_ReportsSolid()
        {
            var board = new BoardState(3, 3);

            Assert.That(board[new GridCoord(-1, 0)], Is.EqualTo(TileType.Solid));
            Assert.That(board[new GridCoord(3, 0)], Is.EqualTo(TileType.Solid));
            Assert.That(board[new GridCoord(0, 3)], Is.EqualTo(TileType.Solid));
            Assert.That(board.IsWalkable(new GridCoord(0, -1)), Is.False,
                "treating the outside as wall is what removes bounds checks from movement and blasts");
        }

        [Test]
        public void WritingOutsideTheBoard_Throws()
        {
            var board = new BoardState(3, 3);

            Assert.Throws<ArgumentOutOfRangeException>(() => board[new GridCoord(3, 3)] = TileType.Solid);
        }

        [Test]
        public void Walkability_FollowsTileType()
        {
            var board = new BoardState(3, 3);
            var coord = new GridCoord(1, 1);

            Assert.That(board.IsWalkable(coord), Is.True);

            board[coord] = TileType.Destructible;
            Assert.That(board.IsWalkable(coord), Is.False);
            Assert.That(board.IsBlocking(coord), Is.True);

            board[coord] = TileType.Solid;
            Assert.That(board.IsBlocking(coord), Is.True);
        }

        [Test]
        public void Hash_ChangesWithContentAndMatchesForIdenticalBoards()
        {
            var first = new BoardState(4, 4);
            var second = new BoardState(4, 4);

            Assert.That(second.ComputeHash(), Is.EqualTo(first.ComputeHash()));

            first[new GridCoord(2, 1)] = TileType.Destructible;
            Assert.That(first.ComputeHash(), Is.Not.EqualTo(second.ComputeHash()));

            second[new GridCoord(2, 1)] = TileType.Destructible;
            Assert.That(first.ComputeHash(), Is.EqualTo(second.ComputeHash()));
        }

        [Test]
        public void Hash_DistinguishesTilePositions()
        {
            var first = new BoardState(4, 4);
            var second = new BoardState(4, 4);

            first[new GridCoord(0, 1)] = TileType.Solid;
            second[new GridCoord(1, 0)] = TileType.Solid;

            Assert.That(first.ComputeHash(), Is.Not.EqualTo(second.ComputeHash()),
                "a position-blind hash would let mirrored boards compare equal");
        }

        [Test]
        public void CopyTo_DuplicatesEveryTile()
        {
            var source = new BoardState(3, 3);
            source[new GridCoord(1, 2)] = TileType.Destructible;

            var destination = new BoardState(3, 3);
            source.CopyTo(destination);

            Assert.That(destination.ComputeHash(), Is.EqualTo(source.ComputeHash()));
        }

        [Test]
        public void CopyTo_WithMismatchedSize_Throws()
        {
            Assert.Throws<ArgumentException>(() => new BoardState(3, 3).CopyTo(new BoardState(4, 4)));
        }
    }

    /// <summary>Covers text layout parsing, including the row-order convention.</summary>
    public sealed class LevelLayoutTests
    {
        [Test]
        public void Parse_ReadsTheFirstRowAsTheTopOfTheLevel()
        {
            var layout = LevelLayout.Parse(
                "X..",
                ".P.",
                "..#");

            var board = layout.CreateBoard();

            Assert.That(layout.Height, Is.EqualTo(3));
            Assert.That(board[new GridCoord(0, 2)], Is.EqualTo(TileType.Destructible),
                "the first row given is the highest Y");
            Assert.That(board[new GridCoord(2, 0)], Is.EqualTo(TileType.Solid),
                "the last row given is Y zero");
        }

        [Test]
        public void Parse_LocatesTheSpawnAndLeavesTheTileWalkable()
        {
            var layout = LevelLayout.Parse(
                "###",
                "#P#",
                "###");

            Assert.That(layout.PlayerSpawn, Is.EqualTo(new GridCoord(1, 1)));
            Assert.That(layout.CreateBoard().IsWalkable(layout.PlayerSpawn), Is.True);
        }

        [Test]
        public void Parse_WithRaggedRows_Throws()
        {
            Assert.Throws<ArgumentException>(() => LevelLayout.Parse("P..", ".."));
        }

        [Test]
        public void Parse_WithAnUnknownGlyph_Throws()
        {
            Assert.Throws<ArgumentException>(() => LevelLayout.Parse("P?."));
        }

        [TestCase("...")]
        [TestCase("PP.")]
        public void Parse_WithoutExactlyOneSpawn_Throws(string row)
        {
            Assert.Throws<ArgumentException>(() => LevelLayout.Parse(row));
        }

        [Test]
        public void Parse_WithNoRows_Throws()
        {
            Assert.Throws<ArgumentException>(() => LevelLayout.Parse());
        }

        [Test]
        public void ApplyTo_WithMismatchedBoard_Throws()
        {
            var layout = LevelLayout.Parse("P.", "..");

            Assert.Throws<ArgumentException>(() => layout.ApplyTo(new BoardState(3, 3)));
        }
    }

    /// <summary>Covers construction, tick counting, phase gating and the determinism hash.</summary>
    public sealed class GameSimulationTests
    {
        private static LevelLayout Layout => LevelLayout.Parse(
            "#####",
            "#...#",
            "#.P.#",
            "#...#",
            "#####");

        private static GameSimulation Create(uint seed = 7u) =>
            new GameSimulation(SimulationConfig.Default, Layout, seed);

        [Test]
        public void NewSimulation_StartsAtTickZeroAndPlaying()
        {
            var simulation = Create();

            Assert.That(simulation.CurrentTick, Is.EqualTo(0));
            Assert.That(simulation.Phase, Is.EqualTo(MatchPhase.Playing));
        }

        [Test]
        public void NewSimulation_AnnouncesTheSpawn()
        {
            var simulation = Create();

            Assert.That(simulation.Events.Count, Is.EqualTo(1));
            Assert.That(simulation.Events[0].Type, Is.EqualTo(SimEventType.PlayerSpawned));
            Assert.That(simulation.Events[0].Coord, Is.EqualTo(new GridCoord(2, 2)));
        }

        [Test]
        public void Tick_AdvancesTheCounterExactlyOnce()
        {
            var simulation = Create();

            for (var i = 1; i <= 10; i++)
            {
                simulation.Tick(PlayerIntent.None);
                Assert.That(simulation.CurrentTick, Is.EqualTo(i));
            }
        }

        [Test]
        public void Tick_ClearsThePreviousTicksEvents()
        {
            var simulation = Create();
            Assert.That(simulation.Events.Count, Is.EqualTo(1));

            simulation.Tick(PlayerIntent.None);

            Assert.That(simulation.Events.Count, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_WithAnInvalidConfig_Throws()
        {
            var broken = new SimulationConfig(
                moveSpeedPerTick: 0,
                laneSnapPerTick: 0,
                turnTolerance: 0,
                directionDeadzone: 0,
                cornerAssistEnabled: false);

            Assert.Throws<ArgumentException>(() => _ = new GameSimulation(broken, Layout, 1u));
        }

        [Test]
        public void IdenticalInputs_ProduceIdenticalStateHashes()
        {
            var first = Create();
            var second = Create();

            var script = new[]
            {
                Direction.East, Direction.East, Direction.North, Direction.None,
                Direction.West, Direction.South, Direction.South, Direction.East
            };

            for (var round = 0; round < 200; round++)
            {
                var intent = PlayerIntent.FromDirection(script[round % script.Length]);
                first.Tick(intent);
                second.Tick(intent);

                Assert.That(second.ComputeStateHash(), Is.EqualTo(first.ComputeStateHash()),
                    $"simulations diverged on tick {round + 1}");
            }
        }

        [Test]
        public void Ticking_AllocatesNothing()
        {
            var simulation = Create();
            var intent = PlayerIntent.FromDirection(Direction.East);

            // Warm up so any one-off JIT or first-call cost is excluded from the measurement.
            for (var i = 0; i < 200; i++)
            {
                simulation.Tick(intent);
            }

            var before = GC.GetAllocatedBytesForCurrentThread();

            for (var i = 0; i < 10_000; i++)
            {
                simulation.Tick(intent);
            }

            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.That(allocated, Is.Zero,
                $"ticking allocated {allocated} bytes; the simulation must not produce garbage mid-match");
        }

        [Test]
        public void StateHash_ChangesAsTheMatchProgresses()
        {
            var simulation = Create();
            var atStart = simulation.ComputeStateHash();

            simulation.Tick(PlayerIntent.FromDirection(Direction.East));

            Assert.That(simulation.ComputeStateHash(), Is.Not.EqualTo(atStart));
        }

        [Test]
        public void NonPlayingPhases_StillCountTicksButChangeNothingElse()
        {
            var simulation = Create();
            var stateBefore = simulation.State.Player.Position;

            // Reaching a terminal phase is Milestone 4's job; the gate itself is testable now by
            // driving the simulation and confirming Playing is what allows movement.
            Assert.That(simulation.Phase, Is.EqualTo(MatchPhase.Playing));
            simulation.Tick(PlayerIntent.FromDirection(Direction.East));

            Assert.That(simulation.State.Player.Position, Is.Not.EqualTo(stateBefore));
        }
    }

    /// <summary>Covers the fixed-capacity event buffer, including its overflow behaviour.</summary>
    public sealed class SimEventBufferTests
    {
        [Test]
        public void Add_StoresEventsInOrder()
        {
            var buffer = new SimEventBuffer(4);
            buffer.Add(new SimEvent(SimEventType.PlayerSpawned, new GridCoord(1, 1)));
            buffer.Add(new SimEvent(SimEventType.PlayerTileEntered, new GridCoord(2, 1)));

            Assert.That(buffer.Count, Is.EqualTo(2));
            Assert.That(buffer[0].Type, Is.EqualTo(SimEventType.PlayerSpawned));
            Assert.That(buffer[1].Coord, Is.EqualTo(new GridCoord(2, 1)));
        }

        [Test]
        public void Overflow_IsCountedRatherThanGrowingTheBuffer()
        {
            var buffer = new SimEventBuffer(2);
            for (var i = 0; i < 5; i++)
            {
                buffer.Add(new SimEvent(SimEventType.PlayerTileEntered, GridCoord.Zero));
            }

            Assert.That(buffer.Count, Is.EqualTo(2));
            Assert.That(buffer.Capacity, Is.EqualTo(2));
            Assert.That(buffer.DroppedCount, Is.EqualTo(3),
                "a silent allocation mid-match would be worse than a dropped effect");
        }

        [Test]
        public void Clear_EmptiesTheBufferButKeepsTheDroppedCount()
        {
            var buffer = new SimEventBuffer(1);
            buffer.Add(new SimEvent(SimEventType.PlayerSpawned, GridCoord.Zero));
            buffer.Add(new SimEvent(SimEventType.PlayerSpawned, GridCoord.Zero));

            buffer.Clear();

            Assert.That(buffer.Count, Is.EqualTo(0));
            Assert.That(buffer.DroppedCount, Is.EqualTo(1));

            buffer.ResetDroppedCount();
            Assert.That(buffer.DroppedCount, Is.EqualTo(0));
        }

        [Test]
        public void ReadingBeyondTheCurrentTick_Throws()
        {
            var buffer = new SimEventBuffer(4);

            Assert.Throws<ArgumentOutOfRangeException>(() => _ = buffer[0]);
        }

        [Test]
        public void Constructor_WithNonPositiveCapacity_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new SimEventBuffer(0));
        }
    }
}
