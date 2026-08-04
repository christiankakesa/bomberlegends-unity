using System;

namespace BomberLegends.Core
{
    /// <summary>
    /// A cardinal direction on the tile grid.
    /// </summary>
    /// <remarks>
    /// Grid space is independent of the isometric projection used for rendering:
    /// <see cref="North"/> is +Y, <see cref="South"/> is -Y, <see cref="East"/> is +X and
    /// <see cref="West"/> is -X. The conversion from screen space to grid space happens in the
    /// input layer, never in the simulation.
    /// </remarks>
    public enum Direction : byte
    {
        /// <summary>No direction. Used for "not moving" and for invalid results.</summary>
        None = 0,

        /// <summary>Positive Y.</summary>
        North = 1,

        /// <summary>Positive X.</summary>
        East = 2,

        /// <summary>Negative Y.</summary>
        South = 3,

        /// <summary>Negative X.</summary>
        West = 4
    }

    /// <summary>
    /// Allocation-free helpers for <see cref="Direction"/>.
    /// </summary>
    public static class Directions
    {
        private static readonly Direction[] CardinalArray =
        {
            Direction.North,
            Direction.East,
            Direction.South,
            Direction.West
        };

        /// <summary>
        /// The four cardinal directions in clockwise order, starting at <see cref="Direction.North"/>.
        /// Returned as a span so iteration allocates nothing.
        /// </summary>
        public static ReadOnlySpan<Direction> Cardinals => CardinalArray;

        /// <summary>
        /// Returns the single-tile offset represented by <paramref name="direction"/>.
        /// <see cref="Direction.None"/> maps to <see cref="GridCoord.Zero"/>.
        /// </summary>
        public static GridCoord ToOffset(this Direction direction) => direction switch
        {
            Direction.North => new GridCoord(0, 1),
            Direction.East => new GridCoord(1, 0),
            Direction.South => new GridCoord(0, -1),
            Direction.West => new GridCoord(-1, 0),
            _ => GridCoord.Zero
        };

        /// <summary>
        /// Returns the direction facing the opposite way.
        /// <see cref="Direction.None"/> is its own opposite.
        /// </summary>
        public static Direction Opposite(this Direction direction) => direction switch
        {
            Direction.North => Direction.South,
            Direction.East => Direction.West,
            Direction.South => Direction.North,
            Direction.West => Direction.East,
            _ => Direction.None
        };

        /// <summary>
        /// Returns <see langword="true"/> for the four cardinal directions and
        /// <see langword="false"/> for <see cref="Direction.None"/>.
        /// </summary>
        public static bool IsCardinal(this Direction direction) =>
            direction != Direction.None && direction <= Direction.West;

        /// <summary>
        /// Returns <see langword="true"/> when the two directions lie on the same axis,
        /// whether they point the same way or opposite ways. <see cref="Direction.None"/>
        /// shares an axis with nothing.
        /// </summary>
        public static bool IsSameAxis(this Direction direction, Direction other)
        {
            if (!direction.IsCardinal() || !other.IsCardinal())
            {
                return false;
            }

            var directionIsVertical = direction == Direction.North || direction == Direction.South;
            var otherIsVertical = other == Direction.North || other == Direction.South;
            return directionIsVertical == otherIsVertical;
        }
    }
}
