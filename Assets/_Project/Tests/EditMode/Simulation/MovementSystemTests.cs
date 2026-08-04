using BomberLegends.Core;
using BomberLegends.Simulation;
using BomberLegends.Simulation.Board;
using BomberLegends.Simulation.Events;
using NUnit.Framework;

namespace BomberLegends.Tests.EditMode.Simulation
{
    /// <summary>
    /// Covers the movement rules that decide whether the game feels tight or sticky.
    /// </summary>
    /// <remarks>
    /// The layouts are written the way they look on screen: the first row is the top of the level.
    /// </remarks>
    public sealed class MovementSystemTests
    {
        private static GameSimulation Open3x3(SimulationConfig? config = null) =>
            new GameSimulation(
                config ?? SimulationConfig.Default,
                LevelLayout.Parse(
                    "#####",
                    "#...#",
                    "#.P.#",
                    "#...#",
                    "#####"),
                seed: 1u);

        private static void Hold(GameSimulation simulation, Direction direction, int ticks)
        {
            var intent = PlayerIntent.FromDirection(direction);
            for (var i = 0; i < ticks; i++)
            {
                simulation.Tick(intent);
            }
        }

        [Test]
        public void Player_SpawnsAtTheCentreOfTheSpawnTile()
        {
            var simulation = Open3x3();
            var player = simulation.State.Player;

            Assert.That(player.Tile, Is.EqualTo(new GridCoord(2, 2)));
            Assert.That(player.Position, Is.EqualTo(SubTilePoint.AtCentreOf(new GridCoord(2, 2))));
            Assert.That(player.IsMoving, Is.False);
        }

        [Test]
        public void NoInput_LeavesThePlayerStill()
        {
            var simulation = Open3x3();
            var before = simulation.State.Player.Position;

            for (var i = 0; i < 30; i++)
            {
                simulation.Tick(PlayerIntent.None);
            }

            Assert.That(simulation.State.Player.Position, Is.EqualTo(before));
            Assert.That(simulation.State.Player.IsMoving, Is.False);
        }

        [TestCase(Direction.East, 1, 0)]
        [TestCase(Direction.West, -1, 0)]
        [TestCase(Direction.North, 0, 1)]
        [TestCase(Direction.South, 0, -1)]
        public void HoldingADirection_MovesThatWay(Direction direction, int deltaX, int deltaY)
        {
            var simulation = Open3x3();
            var start = simulation.State.Player.Position;

            Hold(simulation, direction, 1);
            var moved = simulation.State.Player.Position;

            Assert.That(moved.X - start.X, Is.EqualTo(deltaX * SimulationConfig.Default.MoveSpeedPerTick));
            Assert.That(moved.Y - start.Y, Is.EqualTo(deltaY * SimulationConfig.Default.MoveSpeedPerTick));
            Assert.That(simulation.State.Player.Facing, Is.EqualTo(direction));
            Assert.That(simulation.State.Player.IsMoving, Is.True);
        }

        [Test]
        public void MovingIntoAWall_StopsExactlyAtTheTileCentre()
        {
            var simulation = Open3x3();

            Hold(simulation, Direction.East, 200);

            var player = simulation.State.Player;
            Assert.That(player.Tile, Is.EqualTo(new GridCoord(3, 2)), "should rest in the last free tile");
            Assert.That(player.Position.X, Is.EqualTo(SubTilePoint.CentreOf(3)),
                "a blocked player must settle on the tile centre, not against the wall edge");
        }

        [Test]
        public void MovingIntoAWall_ReportsBlockedAndStopsMoving()
        {
            var simulation = Open3x3();
            Hold(simulation, Direction.East, 200);

            simulation.Tick(PlayerIntent.FromDirection(Direction.East));

            Assert.That(simulation.State.Player.IsMoving, Is.False);

            var sawBlocked = false;
            for (var i = 0; i < simulation.Events.Count; i++)
            {
                sawBlocked |= simulation.Events[i].Type == SimEventType.PlayerBlocked;
            }

            Assert.That(sawBlocked, Is.True, "running into a wall should raise a blocked event");
        }

        [Test]
        public void HighSpeed_CannotTunnelThroughAWall()
        {
            // Three tiles per tick: without sub-stepping this would jump straight past the wall.
            var reckless = new SimulationConfig(
                moveSpeedPerTick: SubTilePoint.UnitsPerTile * 3,
                laneSnapPerTick: 200,
                turnTolerance: 300,
                directionDeadzone: PlayerIntent.DefaultDeadzone,
                cornerAssistEnabled: true);

            var simulation = Open3x3(reckless);

            Hold(simulation, Direction.East, 10);

            Assert.That(simulation.State.Player.Tile, Is.EqualTo(new GridCoord(3, 2)));
            Assert.That(simulation.State.Board.IsWalkable(simulation.State.Player.Tile), Is.True,
                "the player must never end up inside a wall");
        }

        [Test]
        public void ReversingDirection_IsAlwaysAllowed()
        {
            var simulation = Open3x3();
            Hold(simulation, Direction.East, 2);
            var afterEast = simulation.State.Player.Position.X;

            Hold(simulation, Direction.West, 1);

            Assert.That(simulation.State.Player.Position.X, Is.LessThan(afterEast));
            Assert.That(simulation.State.Player.MoveDirection, Is.EqualTo(Direction.West));
        }

        [Test]
        public void TurningFromRest_SnapsIntoTheLaneImmediately()
        {
            var simulation = Open3x3();

            // Drift off the vertical lane, then stop.
            Hold(simulation, Direction.East, 2);
            simulation.Tick(PlayerIntent.None);
            Assert.That(simulation.State.Player.Position.X,
                Is.Not.EqualTo(SubTilePoint.CentreOf(simulation.State.Player.Tile.X)));

            Hold(simulation, Direction.North, 1);

            Assert.That(simulation.State.Player.MoveDirection, Is.EqualTo(Direction.North));
            Assert.That(simulation.State.Player.Position.X,
                Is.EqualTo(SubTilePoint.CentreOf(simulation.State.Player.Tile.X)),
                "turning must place the player exactly in the lane they are entering");
        }

        [Test]
        public void TurnRequestedFarFromAJunction_IsDeferredRatherThanDropped()
        {
            var simulation = Open3x3();

            // Move well past the centre of the spawn tile so a turn is not yet legal.
            Hold(simulation, Direction.East, 3);
            var beforeX = simulation.State.Player.Position.X;

            simulation.Tick(PlayerIntent.FromDirection(Direction.North));

            Assert.That(simulation.State.Player.Position.X, Is.GreaterThan(beforeX),
                "the player should keep travelling rather than stopping at an illegal turn");

            // Held against the direction, the turn lands once the next lane centre is reached.
            Hold(simulation, Direction.North, 20);
            Assert.That(simulation.State.Player.MoveDirection, Is.EqualTo(Direction.North));
        }

        [Test]
        public void CornerAssist_LetsABlockedPlayerTurnRegardlessOfAlignment()
        {
            var simulation = Open3x3();

            Hold(simulation, Direction.East, 200);
            Assert.That(simulation.State.Player.Tile, Is.EqualTo(new GridCoord(3, 2)));

            Hold(simulation, Direction.North, 1);

            Assert.That(simulation.State.Player.MoveDirection, Is.EqualTo(Direction.North),
                "a player pressed against a wall must be able to turn out of it");
        }

        [Test]
        public void CornerAssistDisabled_StillNeverWedgesAPlayerPermanently()
        {
            var careful = new SimulationConfig(
                moveSpeedPerTick: 133,
                laneSnapPerTick: 200,
                turnTolerance: 300,
                directionDeadzone: PlayerIntent.DefaultDeadzone,
                cornerAssistEnabled: false);

            var simulation = Open3x3(careful);
            Hold(simulation, Direction.East, 200);

            // Stop first, then turn: a stationary player always gets their turn.
            simulation.Tick(PlayerIntent.None);
            Hold(simulation, Direction.North, 1);

            Assert.That(simulation.State.Player.MoveDirection, Is.EqualTo(Direction.North));
        }

        [Test]
        public void LaneSnap_PullsAWanderingPlayerBackToTheCorridorCentre()
        {
            var simulation = Open3x3();

            // Nudge off the horizontal lane, then travel along it.
            Hold(simulation, Direction.North, 1);
            Hold(simulation, Direction.East, 30);

            Assert.That(simulation.State.Player.Position.Y,
                Is.EqualTo(SubTilePoint.CentreOf(simulation.State.Player.Tile.Y)),
                "travelling along a corridor should settle the player on its centre line");
        }

        [Test]
        public void TurningIntoAWall_IsRefusedAndTravelContinues()
        {
            // A corridor one tile tall: north and south are walls throughout.
            var simulation = new GameSimulation(
                SimulationConfig.Default,
                LevelLayout.Parse(
                    "#####",
                    "#P..#",
                    "#####"),
                seed: 1u);

            Hold(simulation, Direction.East, 2);
            var beforeX = simulation.State.Player.Position.X;

            simulation.Tick(PlayerIntent.FromDirection(Direction.North));

            Assert.That(simulation.State.Player.MoveDirection, Is.EqualTo(Direction.East),
                "a turn into a wall must be refused, not obeyed");
            Assert.That(simulation.State.Player.Position.X, Is.GreaterThan(beforeX));
        }

        [Test]
        public void EnteringANewTile_RaisesExactlyOneEvent()
        {
            var simulation = Open3x3();
            var entries = 0;

            for (var tick = 0; tick < 60; tick++)
            {
                simulation.Tick(PlayerIntent.FromDirection(Direction.East));

                for (var i = 0; i < simulation.Events.Count; i++)
                {
                    if (simulation.Events[i].Type == SimEventType.PlayerTileEntered)
                    {
                        entries++;
                    }
                }
            }

            Assert.That(entries, Is.EqualTo(1), "spawn tile to the far tile is a single tile change");
        }

        [Test]
        public void Movement_DoesNotDriftOverALongSession()
        {
            var simulation = Open3x3();
            var origin = simulation.State.Player.Position;

            // Equal numbers of ticks each way must land exactly back on the starting position.
            for (var cycle = 0; cycle < 500; cycle++)
            {
                Hold(simulation, Direction.East, 5);
                Hold(simulation, Direction.West, 5);
            }

            Assert.That(simulation.State.Player.Position, Is.EqualTo(origin),
                "integer positions must not accumulate error over a long match");
        }

        [Test]
        public void DiagonalStick_RequestsNothing()
        {
            var simulation = Open3x3();
            var before = simulation.State.Player.Position;

            simulation.Tick(new PlayerIntent(100, 100));

            Assert.That(simulation.State.Player.Position, Is.EqualTo(before),
                "an exact diagonal must not pick an axis arbitrarily");
        }

        [Test]
        public void StickInsideTheDeadzone_RequestsNothing()
        {
            var simulation = Open3x3();
            var before = simulation.State.Player.Position;

            simulation.Tick(new PlayerIntent(10, 5));

            Assert.That(simulation.State.Player.Position, Is.EqualTo(before));
        }
    }
}
