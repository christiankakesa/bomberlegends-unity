using BomberLegends.Core;
using NUnit.Framework;

namespace BomberLegends.Tests.EditMode.Core
{
    /// <summary>Covers direction offsets, opposites and axis comparisons.</summary>
    public sealed class DirectionTests
    {
        [Test]
        public void Cardinals_AreTheFourDirectionsClockwiseFromNorth()
        {
            var cardinals = Directions.Cardinals;

            Assert.That(cardinals.Length, Is.EqualTo(4));
            Assert.That(cardinals[0], Is.EqualTo(Direction.North));
            Assert.That(cardinals[1], Is.EqualTo(Direction.East));
            Assert.That(cardinals[2], Is.EqualTo(Direction.South));
            Assert.That(cardinals[3], Is.EqualTo(Direction.West));
        }

        [TestCase(Direction.North, 0, 1)]
        [TestCase(Direction.East, 1, 0)]
        [TestCase(Direction.South, 0, -1)]
        [TestCase(Direction.West, -1, 0)]
        [TestCase(Direction.None, 0, 0)]
        public void ToOffset_MatchesGridAxes(Direction direction, int expectedX, int expectedY)
        {
            Assert.That(direction.ToOffset(), Is.EqualTo(new GridCoord(expectedX, expectedY)));
        }

        [TestCase(Direction.North, Direction.South)]
        [TestCase(Direction.South, Direction.North)]
        [TestCase(Direction.East, Direction.West)]
        [TestCase(Direction.West, Direction.East)]
        [TestCase(Direction.None, Direction.None)]
        public void Opposite_ReversesDirection(Direction direction, Direction expected)
        {
            Assert.That(direction.Opposite(), Is.EqualTo(expected));
        }

        [Test]
        public void Opposite_AppliedTwice_ReturnsOriginal()
        {
            foreach (var direction in Directions.Cardinals)
            {
                Assert.That(direction.Opposite().Opposite(), Is.EqualTo(direction));
            }
        }

        [Test]
        public void OppositeOffsets_CancelOut()
        {
            foreach (var direction in Directions.Cardinals)
            {
                Assert.That(direction.ToOffset() + direction.Opposite().ToOffset(), Is.EqualTo(GridCoord.Zero));
            }
        }

        [TestCase(Direction.North, true)]
        [TestCase(Direction.East, true)]
        [TestCase(Direction.South, true)]
        [TestCase(Direction.West, true)]
        [TestCase(Direction.None, false)]
        public void IsCardinal_ExcludesNone(Direction direction, bool expected)
        {
            Assert.That(direction.IsCardinal(), Is.EqualTo(expected));
        }

        [TestCase(Direction.North, Direction.South, true)]
        [TestCase(Direction.North, Direction.North, true)]
        [TestCase(Direction.East, Direction.West, true)]
        [TestCase(Direction.North, Direction.East, false)]
        [TestCase(Direction.West, Direction.South, false)]
        [TestCase(Direction.None, Direction.North, false)]
        [TestCase(Direction.None, Direction.None, false)]
        public void IsSameAxis_GroupsOpposingDirections(Direction left, Direction right, bool expected)
        {
            Assert.That(left.IsSameAxis(right), Is.EqualTo(expected));
        }
    }
}
