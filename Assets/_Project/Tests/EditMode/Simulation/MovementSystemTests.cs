using BomberLegends.Core;
using BomberLegends.Simulation;
using BomberLegends.Simulation.Board;
using NUnit.Framework;

namespace BomberLegends.Tests.EditMode.Simulation
{
    /// <summary>
    /// Covers continuous 360° movement against a grid of solid tiles.
    /// </summary>
    /// <remarks>
    /// Replaces the four-directional lane tests written for v1.0. Lane snapping, deferred turns and
    /// cardinal hysteresis no longer exist — there are no lanes. What replaces them is wall sliding,
    /// which is what stops continuous movement feeling like it catches on everything.
    /// </remarks>
    public sealed class MovementSystemTests
    {
        private const int Radius = 340;

        private static SimulationConfig Config(float tilesPerSecond = 4f) =>
            SimulationConfig.FromTilesPerSecond(tilesPerSecond);

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

        private static PlayerIntent Stick(int x, int y) => new PlayerIntent((sbyte)x, (sbyte)y);

        private static void Hold(GameSimulation simulation, PlayerIntent intent, int ticks)
        {
            for (var i = 0; i < ticks; i++)
            {
                simulation.Tick(intent);
            }
        }

        [Test]
        public void Player_SpawnsAtTheCentreOfTheSpawnTile()
        {
            var simulation = OpenRoom();

            Assert.That(simulation.State.Player.Tile, Is.EqualTo(new GridCoord(4, 4)));
            Assert.That(simulation.State.Player.Position,
                Is.EqualTo(SubTilePoint.AtCentreOf(new GridCoord(4, 4))));
        }

        [Test]
        public void NoInput_LeavesThePlayerStill()
        {
            var simulation = OpenRoom();
            var before = simulation.State.Player.Position;

            Hold(simulation, PlayerIntent.None, 30);

            Assert.That(simulation.State.Player.Position, Is.EqualTo(before));
            Assert.That(simulation.State.Player.IsMoving, Is.False);
        }

        [TestCase(100, 0, 1, 0)]
        [TestCase(-100, 0, -1, 0)]
        [TestCase(0, 100, 0, 1)]
        [TestCase(0, -100, 0, -1)]
        public void PushingACardinalDirection_MovesAtFullSpeed(int x, int y, int signX, int signY)
        {
            var simulation = OpenRoom();
            var start = simulation.State.Player.Position;
            var speed = Config().MoveSpeedPerTick;

            simulation.Tick(Stick(x, y));

            var moved = simulation.State.Player.Position;
            Assert.That(moved.X - start.X, Is.EqualTo(signX * speed));
            Assert.That(moved.Y - start.Y, Is.EqualTo(signY * speed));
        }

        [Test]
        public void PushingDiagonally_IsNotFasterThanPushingStraight()
        {
            var simulation = OpenRoom();
            var start = simulation.State.Player.Position;
            var speed = Config().MoveSpeedPerTick;

            simulation.Tick(Stick(100, 100));

            var moved = simulation.State.Player.Position;
            var dx = moved.X - start.X;
            var dy = moved.Y - start.Y;
            var distance = IntMath.Sqrt((dx * dx) + (dy * dy));

            Assert.That(dx, Is.EqualTo(dy), "a symmetric push must move equally on both axes");
            Assert.That(distance, Is.EqualTo(speed).Within(2),
                "diagonal travel must match cardinal travel, not exceed it by 41 per cent");
        }

        [Test]
        public void PartialDeflection_MovesProportionallySlower()
        {
            var simulation = OpenRoom();
            var start = simulation.State.Player.Position;
            var speed = Config().MoveSpeedPerTick;

            simulation.Tick(Stick(50, 0));

            Assert.That(simulation.State.Player.Position.X - start.X, Is.EqualTo(speed / 2).Within(1),
                "the stick is analogue: half deflection is half speed");
        }

        [Test]
        public void InsideTheDeadzone_NothingMoves()
        {
            var simulation = OpenRoom();
            var before = simulation.State.Player.Position;

            Hold(simulation, Stick(20, 10), 10);

            Assert.That(simulation.State.Player.Position, Is.EqualTo(before));
        }

        [Test]
        public void RunningIntoAWall_StopsFlushAgainstIt()
        {
            var simulation = OpenRoom();

            Hold(simulation, Stick(100, 0), 200);

            var player = simulation.State.Player;
            var wallEdge = 8 * SubTilePoint.UnitsPerTile;

            Assert.That(player.Position.X + Radius, Is.LessThan(wallEdge),
                "the player's box must never overlap the wall");
            Assert.That(player.Position.X + Radius, Is.GreaterThan(wallEdge - 10),
                "and must come to rest flush against it, not short of it");
        }

        [Test]
        public void SlidingAlongAWall_KeepsTheUnblockedAxis()
        {
            var simulation = OpenRoom();

            // Press into the east wall first.
            Hold(simulation, Stick(100, 0), 200);
            var againstWall = simulation.State.Player.Position;

            // Now push diagonally into it. The blocked axis must not cancel the free one.
            Hold(simulation, Stick(100, 100), 10);

            var after = simulation.State.Player.Position;
            Assert.That(after.X, Is.EqualTo(againstWall.X).Within(2), "the blocked axis stays blocked");
            Assert.That(after.Y, Is.GreaterThan(againstWall.Y),
                "the free axis must keep moving, or the player sticks to every wall they touch");
        }

        [Test]
        public void SlidingWorksOnEveryWall()
        {
            foreach (var (into, along) in new[]
                     {
                         ((100, 0), (0, 100)),
                         ((-100, 0), (0, -100)),
                         ((0, 100), (100, 0)),
                         ((0, -100), (-100, 0))
                     })
            {
                var simulation = OpenRoom();
                Hold(simulation, Stick(into.Item1, into.Item2), 200);
                var pinned = simulation.State.Player.Position;

                Hold(simulation, Stick(into.Item1 + along.Item1, into.Item2 + along.Item2), 10);
                var after = simulation.State.Player.Position;

                Assert.That(after, Is.Not.EqualTo(pinned),
                    $"pressed into {into} the player should still slide towards {along}");
            }
        }

        [Test]
        public void HighSpeed_CannotTunnelThroughAWall()
        {
            // Three tiles per tick: without sub-stepping this would pass straight through.
            var reckless = SimulationConfig.FromTilesPerSecond(90f);
            var simulation = OpenRoom(reckless);

            Hold(simulation, Stick(100, 0), 20);

            Assert.That(simulation.State.Board.IsWalkable(simulation.State.Player.Tile), Is.True,
                "the player must never end up inside a wall");
            Assert.That(simulation.State.Player.Position.X + Radius,
                Is.LessThan(8 * SubTilePoint.UnitsPerTile));
        }

        [Test]
        public void ThePlayerFitsDownASingleTileCorridor()
        {
            var simulation = new GameSimulation(
                Config(),
                LevelLayout.Parse(
                    "#######",
                    "#P....#",
                    "#######"),
                seed: 1u);

            Hold(simulation, Stick(100, 0), 200);

            Assert.That(simulation.State.Player.Tile.X, Is.EqualTo(5),
                "the collision box must be small enough to travel a one-tile corridor");
        }

        [Test]
        public void Movement_DoesNotDriftOverALongSession()
        {
            var simulation = OpenRoom();
            var origin = simulation.State.Player.Position;

            for (var cycle = 0; cycle < 400; cycle++)
            {
                Hold(simulation, Stick(100, 0), 4);
                Hold(simulation, Stick(-100, 0), 4);
            }

            Assert.That(simulation.State.Player.Position, Is.EqualTo(origin),
                "integer positions must not accumulate error, however long the match runs");
        }

        [Test]
        public void DiagonalMovement_IsExactlyReversible()
        {
            var simulation = OpenRoom();
            var origin = simulation.State.Player.Position;

            for (var cycle = 0; cycle < 200; cycle++)
            {
                Hold(simulation, Stick(100, 100), 3);
                Hold(simulation, Stick(-100, -100), 3);
            }

            Assert.That(simulation.State.Player.Position, Is.EqualTo(origin));
        }

        [Test]
        public void Facing_FollowsTheDominantAxisOfTravel()
        {
            var simulation = OpenRoom();

            simulation.Tick(Stick(100, 30));
            Assert.That(simulation.State.Player.Facing, Is.EqualTo(Direction.East));

            simulation.Tick(Stick(30, -100));
            Assert.That(simulation.State.Player.Facing, Is.EqualTo(Direction.South));
        }

        [Test]
        public void TurningIntoASideCorridor_WhileClippingACorner_DoesNotStopThePlayer()
        {
            // A lattice like the real arena: one-tile corridors with pillars between them. The gap
            // is one tile and the player is 0.68 wide, so turning while slightly off-centre puts a
            // corner of their box inside a pillar.
            var simulation = new GameSimulation(
                Config(),
                LevelLayout.Parse(
                    "#######",
                    "#.....#",
                    "#.#.#.#",
                    "#..P..#",
                    "#.#.#.#",
                    "#.....#",
                    "#######"),
                seed: 1u);

            // Drift far enough off centre that the box genuinely straddles two rows — the pillar
            // row above and the clear row it is travelling along. Two ticks was not enough: the box
            // stayed inside the clear row, so there was no corner to catch on and the test passed
            // while proving nothing.
            Hold(simulation, Stick(0, 40), 4);
            var beforeTurn = simulation.State.Player.Position;

            Hold(simulation, Stick(100, 0), 25);

            Assert.That(simulation.State.Player.Position.X, Is.GreaterThan(beforeTurn.X + 200),
                "clipping a pillar must not stop the player dead; they should slip around it");
        }

        [Test]
        public void CornerSlip_DoesNotPushThePlayerThroughAWall()
        {
            var simulation = OpenRoom();

            // Pressed squarely into a wall, with no corner to round: the assistance must not fire.
            Hold(simulation, Stick(100, 0), 200);
            var pinned = simulation.State.Player.Position;

            Hold(simulation, Stick(100, 0), 60);

            Assert.That(simulation.State.Player.Position, Is.EqualTo(pinned),
                "a player walking straight into a wall should stay put, not be steered along it");
        }

        [Test]
        public void CornerSlip_CanBeDisabled()
        {
            // Lane assist is switched off as well, or this measures the wrong helper: assist
            // recentres the player before a corner is ever clipped, so corner slip would never be
            // asked to do anything and the test would pass without proving it exists.
            var withoutAssist = new SimulationConfig(
                moveSpeedPerTick: 133,
                laneSnapPerTick: 0,
                turnTolerance: 0,
                directionDeadzone: PlayerIntent.DefaultDeadzone,
                cornerAssistEnabled: false,
                cornerSlipPerTick: 0,
                playerLaneAssistPerTick: 0);

            var simulation = new GameSimulation(
                withoutAssist,
                LevelLayout.Parse(
                    "#######",
                    "#.....#",
                    "#.#.#.#",
                    "#..P..#",
                    "#.#.#.#",
                    "#.....#",
                    "#######"),
                seed: 1u);

            Hold(simulation, Stick(0, 40), 4);
            var beforeTurn = simulation.State.Player.Position;

            Hold(simulation, Stick(100, 0), 25);

            Assert.That(simulation.State.Player.Position.X, Is.LessThan(beforeTurn.X + 200),
                "with the assistance off the player should catch, which is what makes it worth having");
        }

        [Test]
        public void Movement_AllocatesNothing()
        {
            var simulation = OpenRoom();
            var intent = Stick(70, 70);

            for (var i = 0; i < 200; i++)
            {
                simulation.Tick(intent);
            }

            var before = System.GC.GetAllocatedBytesForCurrentThread();

            for (var i = 0; i < 10_000; i++)
            {
                simulation.Tick(i % 2 == 0 ? intent : Stick(-70, -70));
            }

            Assert.That(System.GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }
    }

    /// <summary>Covers the exact integer maths the simulation depends on.</summary>
    public sealed class IntMathTests
    {
        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(2, 1)]
        [TestCase(3, 1)]
        [TestCase(4, 2)]
        [TestCase(15, 3)]
        [TestCase(16, 4)]
        [TestCase(20000, 141)]
        [TestCase(1000000, 1000)]
        public void Sqrt_RoundsTowardsZero(int value, int expected)
        {
            Assert.That(IntMath.Sqrt(value), Is.EqualTo(expected));
        }

        [Test]
        public void Sqrt_IsExactAcrossAWideRange()
        {
            for (var root = 0; root < 1000; root++)
            {
                Assert.That(IntMath.Sqrt(root * root), Is.EqualTo(root));
                Assert.That(IntMath.Sqrt((root * root) + root), Is.EqualTo(root),
                    "values between consecutive squares must round down");
            }
        }

        [Test]
        public void Sqrt_OfNegative_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => IntMath.Sqrt(-1));
        }

        [Test]
        public void AbsAndClamp_BehaveAsExpected()
        {
            Assert.That(IntMath.Abs(-5), Is.EqualTo(5));
            Assert.That(IntMath.Abs(5), Is.EqualTo(5));
            Assert.That(IntMath.Clamp(10, 0, 5), Is.EqualTo(5));
            Assert.That(IntMath.Clamp(-10, 0, 5), Is.EqualTo(0));
            Assert.That(IntMath.Clamp(3, 0, 5), Is.EqualTo(3));
        }
        // ---------- lane assist ----------

        /// <summary>
        /// A corridor flanked by pillars, which is the shape every arena is built from.
        /// </summary>
        private static GameSimulation PillarCorridor(int laneAssist)
        {
            var config = new SimulationConfig(
                moveSpeedPerTick: 133,
                laneSnapPerTick: 200,
                turnTolerance: 300,
                directionDeadzone: PlayerIntent.DefaultDeadzone,
                cornerAssistEnabled: true,
                playerRadius: 340,
                cornerSlipPerTick: 90,
                cornerSlipTolerance: 320,
                playerLaneAssistPerTick: laneAssist);

            return new GameSimulation(
                config,
                LevelLayout.Parse(
                    "###############",
                    "#.#.#.#.#.#.#.#",
                    "#P............#",
                    "#.#.#.#.#.#.#.#",
                    "###############"),
                seed: 1u);
        }

        /// <summary>How far east the player gets on a given stick reading.</summary>
        private static int DistanceEast(int laneAssist, sbyte moveX, sbyte moveY, int ticks = 150)
        {
            var simulation = PillarCorridor(laneAssist);
            var start = simulation.State.Player.Position.X;

            for (var i = 0; i < ticks; i++)
            {
                simulation.Tick(new PlayerIntent(moveX, moveY));
            }

            return simulation.State.Player.Position.X - start;
        }

        [Test]
        public void AnOffAxisStickTravelsAsFastAsAKeyboard()
        {
            // Reported from play: the player is slowed by obstacles, and far more on a pad than on
            // a keyboard. Keys are perfectly axis-aligned so the box never drifts off-lane; a stick
            // is a couple of degrees off and clips the corner of every pillar it passes.
            var keyboard = DistanceEast(130, 100, 0);
            var gamepad = DistanceEast(130, 99, 14);

            Assert.That(gamepad, Is.GreaterThan(keyboard * 9 / 10),
                $"an off-axis stick covered {gamepad} against a keyboard's {keyboard}");
        }

        [Test]
        public void LaneAssistIsWhatClosesTheGamepadGap()
        {
            // Guards the fix from being quietly undone, and proves the problem is real rather than
            // assumed: without assist the same stick reading loses ground down the same corridor.
            var without = DistanceEast(0, 99, 14);
            var with = DistanceEast(130, 99, 14);

            Assert.That(with, Is.GreaterThan(without),
                $"assist must help an off-axis run, but went {without} -> {with}");
        }

        [Test]
        public void LaneAssistLeavesADeliberateDiagonalAlone()
        {
            // The help has to disappear well before a diagonal, or continuous movement quietly
            // becomes movement on rails — which is the thing the whole hybrid rests on not being.
            var simulation = new GameSimulation(
                new SimulationConfig(
                    moveSpeedPerTick: 133,
                    laneSnapPerTick: 200,
                    turnTolerance: 300,
                    directionDeadzone: PlayerIntent.DefaultDeadzone,
                    cornerAssistEnabled: true,
                    playerRadius: 340,
                    cornerSlipPerTick: 90,
                    cornerSlipTolerance: 320,
                    playerLaneAssistPerTick: 130),
                // Deliberately roomy. A 45° run covers nearly four tiles on each axis, and a wall
                // arriving first would clamp one of them and look exactly like flattening.
                LevelLayout.Parse(
                    "###########",
                    "#.........#",
                    "#.........#",
                    "#.........#",
                    "#.........#",
                    "#.........#",
                    "#.........#",
                    "#.........#",
                    "#.........#",
                    "#P........#",
                    "###########"),
                seed: 1u);

            var start = simulation.State.Player.Position;

            for (var i = 0; i < 40; i++)
            {
                simulation.Tick(new PlayerIntent(70, 70));
            }

            var movedX = simulation.State.Player.Position.X - start.X;
            var movedY = simulation.State.Player.Position.Y - start.Y;

            Assert.That(movedY, Is.GreaterThan(movedX * 8 / 10),
                "a 45° push must still travel diagonally, not be flattened onto a lane");
        }

    }
}
