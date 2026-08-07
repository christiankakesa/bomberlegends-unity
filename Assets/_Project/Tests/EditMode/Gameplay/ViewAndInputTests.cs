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

    /// <summary>Covers the board projection onto the ground plane.</summary>
    public sealed class BoardProjectorTests
    {
        private static readonly BoardProjector Projector = new BoardProjector();

        [Test]
        public void TheGridLiesFlatOnTheGroundPlane()
        {
            var origin = Projector.GridToWorld(0f, 0f);
            var east = Projector.GridToWorld(1f, 0f) - origin;
            var north = Projector.GridToWorld(0f, 1f) - origin;

            Assert.That(east.x, Is.GreaterThan(0f), "grid east is world +X");
            Assert.That(east.z, Is.EqualTo(0f));
            Assert.That(north.z, Is.GreaterThan(0f), "grid north is world +Z");
            Assert.That(north.x, Is.EqualTo(0f));
            Assert.That(origin.y, Is.EqualTo(0f), "the board sits on the ground, with Y as height");
        }

        [Test]
        public void HeightRaisesOnlyTheVerticalAxis()
        {
            var flat = Projector.TileToWorld(new GridCoord(3, 4));
            var raised = Projector.TileToWorld(new GridCoord(3, 4), 2.5f);

            Assert.That(raised.x, Is.EqualTo(flat.x));
            Assert.That(raised.z, Is.EqualTo(flat.z));
            Assert.That(raised.y, Is.EqualTo(2.5f));
        }

        [Test]
        public void TilesAreSpacedExactlyOneTileApart()
        {
            var a = Projector.TileToWorld(new GridCoord(2, 2));
            var b = Projector.TileToWorld(new GridCoord(3, 2));

            Assert.That(b.x - a.x, Is.EqualTo(Projector.TileSize).Within(0.0001f),
                "neighbouring tiles must meet exactly, with no overlap and no gap");
        }

        [Test]
        public void ScreenToGrid_LeavesTheStickUntouched()
        {
            // Identity: the camera looks down the board's axes, so pushing up the screen runs away
            // from the camera, which is grid north.
            foreach (var stick in new[]
                     {
                         new Vector2(1f, 0f), new Vector2(0f, 1f),
                         new Vector2(-0.4f, 0.9f), new Vector2(0.7f, -0.7f)
                     })
            {
                Assert.That(Projector.ScreenToGrid(stick), Is.EqualTo(stick));
            }
        }

        [Test]
        public void PushingDiagonally_RemainsAnExactTie()
        {
            var grid = Projector.ScreenToGrid(new Vector2(1f, 1f));

            Assert.That(Mathf.Abs(grid.x), Is.EqualTo(Mathf.Abs(grid.y)).Within(0.0001f),
                "a symmetric stick input must not favour an axis");
        }

        [Test]
        public void SubTilePositions_ConvertToContinuousGridSpace()
        {
            // A tile's centre in sub-tile units must land on that tile's integer grid coordinate.
            Assert.That(BoardProjector.ToGrid(SubTilePoint.CentreOf(0)), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(BoardProjector.ToGrid(SubTilePoint.CentreOf(5)), Is.EqualTo(5f).Within(0.0001f));
        }

        [Test]
        public void BoardBounds_CoverEveryTile()
        {
            var bounds = Projector.BoardBounds(13, 9);

            Assert.That(bounds.size.x, Is.EqualTo(13f * Projector.TileSize).Within(0.0001f));
            Assert.That(bounds.size.z, Is.EqualTo(9f * Projector.TileSize).Within(0.0001f));
            Assert.That(bounds.Contains(Projector.TileToWorld(new GridCoord(12, 8))), Is.True,
                "the far corner tile must sit inside the arena bounds");
            Assert.That(bounds.Contains(Projector.TileToWorld(GridCoord.Zero)), Is.True);
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
