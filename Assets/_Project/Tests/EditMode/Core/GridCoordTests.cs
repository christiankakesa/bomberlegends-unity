using System;
using BomberLegends.Core;
using NUnit.Framework;

namespace BomberLegends.Tests.EditMode.Core
{
    /// <summary>Covers coordinate arithmetic, neighbour queries, bounds checks and index mapping.</summary>
    public sealed class GridCoordTests
    {
        [Test]
        public void Constructor_StoresComponents()
        {
            var coord = new GridCoord(3, -7);

            Assert.That(coord.X, Is.EqualTo(3));
            Assert.That(coord.Y, Is.EqualTo(-7));
        }

        [Test]
        public void Zero_IsOrigin()
        {
            Assert.That(GridCoord.Zero, Is.EqualTo(new GridCoord(0, 0)));
        }

        [TestCase(Direction.North, 2, 4)]
        [TestCase(Direction.East, 3, 3)]
        [TestCase(Direction.South, 2, 2)]
        [TestCase(Direction.West, 1, 3)]
        [TestCase(Direction.None, 2, 3)]
        public void Neighbour_MovesOneTile(Direction direction, int expectedX, int expectedY)
        {
            var result = new GridCoord(2, 3).Neighbour(direction);

            Assert.That(result, Is.EqualTo(new GridCoord(expectedX, expectedY)));
        }

        [Test]
        public void Step_MovesGivenDistance()
        {
            Assert.That(GridCoord.Zero.Step(Direction.East, 4), Is.EqualTo(new GridCoord(4, 0)));
            Assert.That(GridCoord.Zero.Step(Direction.North, 3), Is.EqualTo(new GridCoord(0, 3)));
        }

        [Test]
        public void Step_WithNegativeDistance_MovesOppositeWay()
        {
            Assert.That(GridCoord.Zero.Step(Direction.East, -2), Is.EqualTo(new GridCoord(-2, 0)));
        }

        [Test]
        public void Step_WithNoDirection_DoesNotMove()
        {
            Assert.That(new GridCoord(5, 5).Step(Direction.None, 9), Is.EqualTo(new GridCoord(5, 5)));
        }

        [Test]
        public void ManhattanDistance_CountsOrthogonalSteps()
        {
            Assert.That(new GridCoord(1, 1).ManhattanDistanceTo(new GridCoord(4, 5)), Is.EqualTo(7));
            Assert.That(new GridCoord(4, 5).ManhattanDistanceTo(new GridCoord(1, 1)), Is.EqualTo(7));
            Assert.That(new GridCoord(2, 2).ManhattanDistanceTo(new GridCoord(2, 2)), Is.EqualTo(0));
        }

        [TestCase(0, 0, true)]
        [TestCase(12, 10, true)]
        [TestCase(13, 10, false)]
        [TestCase(12, 11, false)]
        [TestCase(-1, 0, false)]
        [TestCase(0, -1, false)]
        public void IsInside_ChecksBoardBounds(int x, int y, bool expected)
        {
            Assert.That(new GridCoord(x, y).IsInside(13, 11), Is.EqualTo(expected));
        }

        [Test]
        public void ToIndex_UsesRowMajorOrder()
        {
            Assert.That(new GridCoord(0, 0).ToIndex(13), Is.EqualTo(0));
            Assert.That(new GridCoord(12, 0).ToIndex(13), Is.EqualTo(12));
            Assert.That(new GridCoord(0, 1).ToIndex(13), Is.EqualTo(13));
            Assert.That(new GridCoord(5, 3).ToIndex(13), Is.EqualTo(44));
        }

        [Test]
        public void FromIndex_RoundTripsForEveryTileOfABoard()
        {
            const int width = 13;
            const int height = 11;

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var original = new GridCoord(x, y);
                    var roundTripped = GridCoord.FromIndex(original.ToIndex(width), width);

                    Assert.That(roundTripped, Is.EqualTo(original), $"round trip failed for {original}");
                }
            }
        }

        [Test]
        public void FromIndex_WithInvalidWidth_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => GridCoord.FromIndex(0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => GridCoord.FromIndex(0, -1));
        }

        [Test]
        public void FromIndex_WithNegativeIndex_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => GridCoord.FromIndex(-1, 13));
        }

        [Test]
        public void Addition_AndSubtraction_AreComponentWise()
        {
            var left = new GridCoord(3, 4);
            var right = new GridCoord(1, 2);

            Assert.That(left + right, Is.EqualTo(new GridCoord(4, 6)));
            Assert.That(left - right, Is.EqualTo(new GridCoord(2, 2)));
        }

        [Test]
        public void Equality_ComparesComponents()
        {
            var a = new GridCoord(2, 3);
            var b = new GridCoord(2, 3);
            var c = new GridCoord(3, 2);

            Assert.That(a == b, Is.True);
            Assert.That(a != c, Is.True);
            Assert.That(a.Equals(b), Is.True);
            Assert.That(a.Equals((object)b), Is.True);
            Assert.That(a.Equals("not a coordinate"), Is.False);
        }

        [Test]
        public void GetHashCode_MatchesForEqualValues_AndSeparatesTransposedOnes()
        {
            Assert.That(new GridCoord(2, 3).GetHashCode(), Is.EqualTo(new GridCoord(2, 3).GetHashCode()));
            Assert.That(new GridCoord(2, 3).GetHashCode(), Is.Not.EqualTo(new GridCoord(3, 2).GetHashCode()));
        }

        [Test]
        public void ToString_IsReadable()
        {
            Assert.That(new GridCoord(2, -3).ToString(), Is.EqualTo("(2, -3)"));
        }
    }
}
