using System;
using BomberLegends.Core;
using BomberLegends.Gameplay.Board;
using BomberLegends.Input;
using NUnit.Framework;
using UnityEngine;

namespace BomberLegends.Tests.EditMode.Gameplay
{
    /// <summary>
    /// Covers the frame pacing that drives the whole game.
    /// </summary>
    /// <remarks>
    /// Exact frame times are supplied here rather than measured at runtime, so the guarantee that
    /// the simulation runs at a fixed rate regardless of display rate is proven rather than assumed.
    /// </remarks>
    public sealed class FixedStepAccumulatorTests
    {
        private const double Step = 1.0 / 30.0;

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(120)]
        public void FrameRatesThatDivideTheStep_ProduceExactlyThirtyStepsPerSecond(int framesPerSecond)
        {
            var accumulator = new FixedStepAccumulator(Step);
            var frameTime = 1.0 / framesPerSecond;
            var steps = 0;

            for (var frame = 0; frame < framesPerSecond; frame++)
            {
                steps += accumulator.Advance(frameTime, maxSteps: 5, out _);
            }

            Assert.That(steps, Is.EqualTo(30), "the simulation rate must not depend on the display rate");
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(120)]
        [TestCase(144)]
        [TestCase(90)]
        public void OverALongRun_StepsTrackWallClockAtAnyFrameRate(int framesPerSecond)
        {
            const int seconds = 60;

            var accumulator = new FixedStepAccumulator(Step);
            var frameTime = 1.0 / framesPerSecond;
            var steps = 0;

            for (var frame = 0; frame < framesPerSecond * seconds; frame++)
            {
                steps += accumulator.Advance(frameTime, maxSteps: 5, out _);
            }

            // A frame time that cannot be represented exactly leaves the accumulator a fraction of a
            // step behind, which shows up as a fixed offset of at most one step. What matters is
            // that it stays an offset and never grows into drift, so this is measured over a minute.
            Assert.That(steps, Is.EqualTo(seconds * 30).Within(1),
                "the step count must track wall clock to within a single step, however long the match runs");
        }

        [Test]
        public void SlowFrames_StillProduceTheRightRateOverTime()
        {
            var accumulator = new FixedStepAccumulator(Step);
            var steps = 0;

            // Twenty frames of 50 ms is one second of wall time at an uneven rate.
            for (var frame = 0; frame < 20; frame++)
            {
                steps += accumulator.Advance(0.05, maxSteps: 5, out _);
            }

            Assert.That(steps, Is.EqualTo(30));
        }

        [Test]
        public void AStalledFrame_IsCappedAndTheBacklogIsDiscarded()
        {
            var accumulator = new FixedStepAccumulator(Step);

            var steps = accumulator.Advance(0.5, maxSteps: 5, out var discarded);

            Assert.That(steps, Is.EqualTo(5), "the burst must be capped");
            Assert.That(discarded, Is.EqualTo(10), "the rest must be dropped, not owed");
        }

        [Test]
        public void AfterAStall_ThePacingReturnsToNormal()
        {
            var accumulator = new FixedStepAccumulator(Step);
            accumulator.Advance(0.5, maxSteps: 5, out _);

            var steps = 0;
            for (var frame = 0; frame < 60; frame++)
            {
                steps += accumulator.Advance(1.0 / 60.0, maxSteps: 5, out var discarded);
                Assert.That(discarded, Is.Zero, "a recovered accumulator should not keep discarding");
            }

            Assert.That(steps, Is.EqualTo(30));
        }

        [Test]
        public void Alpha_StaysWithinTheStep()
        {
            var accumulator = new FixedStepAccumulator(Step);

            for (var frame = 0; frame < 500; frame++)
            {
                accumulator.Advance(1.0 / 137.0, maxSteps: 5, out _);
                Assert.That(accumulator.Alpha, Is.InRange(0f, 1f));
            }
        }

        [Test]
        public void NegativeOrZeroDelta_ProducesNoSteps()
        {
            var accumulator = new FixedStepAccumulator(Step);

            Assert.That(accumulator.Advance(-1.0, maxSteps: 5, out _), Is.Zero);
            Assert.That(accumulator.Advance(0.0, maxSteps: 5, out _), Is.Zero);
        }

        [Test]
        public void Reset_DropsPartialProgress()
        {
            var accumulator = new FixedStepAccumulator(Step);
            accumulator.Advance(Step * 0.75, maxSteps: 5, out _);
            Assert.That(accumulator.Alpha, Is.GreaterThan(0f));

            accumulator.Reset();

            Assert.That(accumulator.Alpha, Is.EqualTo(0f));
        }

        [Test]
        public void InvalidArguments_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new FixedStepAccumulator(0d));

            var accumulator = new FixedStepAccumulator(Step);
            Assert.Throws<ArgumentOutOfRangeException>(() => accumulator.Advance(1.0, 0, out _));
        }
    }

    /// <summary>Covers the isometric projection and the depth ordering derived from it.</summary>
    public sealed class IsometricProjectorTests
    {
        private static readonly IsometricProjector Projector = new IsometricProjector();

        [Test]
        public void GridDirections_ProjectToScreenDiagonals()
        {
            var origin = Projector.GridToWorld(0f, 0f);

            var east = Projector.GridToWorld(1f, 0f) - origin;
            var north = Projector.GridToWorld(0f, 1f) - origin;

            Assert.That(east.x, Is.GreaterThan(0f).And.Not.EqualTo(0f));
            Assert.That(east.y, Is.GreaterThan(0f), "east reads as up-and-right on screen");
            Assert.That(north.x, Is.LessThan(0f), "north reads as up-and-left on screen");
            Assert.That(north.y, Is.GreaterThan(0f));
        }

        [Test]
        public void ScreenToGrid_IsTheExactInverseOfGridToWorld()
        {
            foreach (var grid in new[]
                     {
                         new Vector2(1f, 0f), new Vector2(0f, 1f),
                         new Vector2(-1f, 0f), new Vector2(0f, -1f),
                         new Vector2(2.5f, -3.25f)
                     })
            {
                var screen = Projector.GridToWorld(grid.x, grid.y);
                var roundTripped = Projector.ScreenToGrid(screen);

                Assert.That(roundTripped.x, Is.EqualTo(grid.x).Within(0.0001f));
                Assert.That(roundTripped.y, Is.EqualTo(grid.y).Within(0.0001f));
            }
        }

        [Test]
        public void ScreenToGrid_MapsPushingUpToEqualPartsOfBothAxes()
        {
            var grid = Projector.ScreenToGrid(new Vector2(0f, 1f));

            Assert.That(grid.x, Is.EqualTo(grid.y).Within(0.0001f),
                "straight up the screen sits exactly between two grid directions");
        }

        [Test]
        public void SortingOrder_PutsDistantTilesBehind()
        {
            var near = IsometricProjector.SortingOrder(0f, 0f);
            var far = IsometricProjector.SortingOrder(5f, 5f);

            Assert.That(far, Is.LessThan(near), "greater grid depth must draw first");
        }

        [Test]
        public void SortingOrder_HasSubTileResolution()
        {
            var atTile = IsometricProjector.SortingOrder(2f, 2f);
            var partWay = IsometricProjector.SortingOrder(2.5f, 2f);

            Assert.That(partWay, Is.Not.EqualTo(atTile),
                "an actor between tiles must sort between them, not pop at the boundary");
        }

        [Test]
        public void FloorTiles_AlwaysDrawBehindEverything()
        {
            // The nearest possible floor against the furthest possible actor on a large board.
            var nearestFloor = IsometricProjector.FloorSortingOrder(GridCoord.Zero);
            var furthestActor = IsometricProjector.SortingOrder(40f, 40f);

            Assert.That(nearestFloor, Is.LessThan(furthestActor),
                "floor must never occlude an actor or a block");
        }
    }

    /// <summary>
    /// Covers the stick-to-direction snapping, including the hysteresis that stops a thumb resting
    /// near a diagonal from stuttering the character.
    /// </summary>
    public sealed class DirectionSnapperTests
    {
        private const float SwitchRatio = 1.4f;

        [Test]
        public void FromRest_PicksTheDominantAxis()
        {
            Assert.That(DirectionSnapper.Snap(new Vector2(1f, 0.2f), Direction.None, SwitchRatio),
                Is.EqualTo(Direction.East));
            Assert.That(DirectionSnapper.Snap(new Vector2(-0.1f, 1f), Direction.None, SwitchRatio),
                Is.EqualTo(Direction.North));
            Assert.That(DirectionSnapper.Snap(new Vector2(0.2f, -1f), Direction.None, SwitchRatio),
                Is.EqualTo(Direction.South));
            Assert.That(DirectionSnapper.Snap(new Vector2(-1f, 0f), Direction.None, SwitchRatio),
                Is.EqualTo(Direction.West));
        }

        [Test]
        public void AZeroStick_RequestsNothing()
        {
            Assert.That(DirectionSnapper.Snap(Vector2.zero, Direction.East, SwitchRatio),
                Is.EqualTo(Direction.None));
        }

        [Test]
        public void JustPastTheDiagonal_DoesNotStealTheDirection()
        {
            // The new axis leads, but not by the required margin.
            var result = DirectionSnapper.Snap(new Vector2(1f, 1.2f), Direction.East, SwitchRatio);

            Assert.That(result, Is.EqualTo(Direction.East),
                "without hysteresis a thumb near the diagonal flickers between two directions");
        }

        [Test]
        public void ClearlyPastTheDiagonal_ChangesDirection()
        {
            var result = DirectionSnapper.Snap(new Vector2(1f, 1.8f), Direction.East, SwitchRatio);

            Assert.That(result, Is.EqualTo(Direction.North));
        }

        [Test]
        public void HoveringAroundTheDiagonal_NeverFlickers()
        {
            var current = Direction.East;

            // Sweep back and forth across the boundary the way a resting thumb does.
            for (var i = 0; i < 100; i++)
            {
                var wobble = 1f + (Mathf.Sin(i * 0.37f) * 0.25f);
                current = DirectionSnapper.Snap(new Vector2(1f, wobble), current, SwitchRatio);

                Assert.That(current, Is.EqualTo(Direction.East),
                    $"direction changed on sample {i}, which would read as the character stuttering");
            }
        }

        [Test]
        public void Reversing_NeedsNoMargin()
        {
            var result = DirectionSnapper.Snap(new Vector2(-1f, 0f), Direction.East, SwitchRatio);

            Assert.That(result, Is.EqualTo(Direction.West),
                "a reversal stays in the same lane, so it should be immediate");
        }

        [Test]
        public void ContinuingTheSameWay_IsAlwaysHonoured()
        {
            Assert.That(DirectionSnapper.Snap(new Vector2(0.4f, 0f), Direction.East, SwitchRatio),
                Is.EqualTo(Direction.East));
        }

        [Test]
        public void ARatioOfOne_RemovesTheHysteresisEntirely()
        {
            var result = DirectionSnapper.Snap(new Vector2(1f, 1.01f), Direction.East, switchRatio: 1f);

            Assert.That(result, Is.EqualTo(Direction.North));
        }
    }
}
