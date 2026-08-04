using System;

namespace BomberLegends.Core
{
    /// <summary>
    /// An integer coordinate on the tile grid.
    /// </summary>
    /// <remarks>
    /// The grid is the authoritative space for all gameplay: movement, bomb placement, blast
    /// propagation and occupancy are expressed in <see cref="GridCoord"/>, never in world or screen
    /// units. Board storage is a flat array indexed by <c>Y * width + X</c>, matching
    /// <see cref="ToIndex"/>.
    /// </remarks>
    public readonly struct GridCoord : IEquatable<GridCoord>
    {
        /// <summary>Column, increasing eastward.</summary>
        public readonly int X;

        /// <summary>Row, increasing northward.</summary>
        public readonly int Y;

        /// <summary>Creates a coordinate at <paramref name="x"/>, <paramref name="y"/>.</summary>
        public GridCoord(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>The origin, <c>(0, 0)</c>.</summary>
        public static GridCoord Zero => new GridCoord(0, 0);

        /// <summary>
        /// Returns the adjacent coordinate in <paramref name="direction"/>.
        /// <see cref="Direction.None"/> returns this coordinate unchanged.
        /// </summary>
        public GridCoord Neighbour(Direction direction) => this + direction.ToOffset();

        /// <summary>
        /// Returns the coordinate <paramref name="distance"/> tiles away in
        /// <paramref name="direction"/>. Negative distances move the opposite way.
        /// </summary>
        public GridCoord Step(Direction direction, int distance)
        {
            var offset = direction.ToOffset();
            return new GridCoord(X + (offset.X * distance), Y + (offset.Y * distance));
        }

        /// <summary>Returns the number of orthogonal steps between this coordinate and <paramref name="other"/>.</summary>
        public int ManhattanDistanceTo(GridCoord other) => Math.Abs(X - other.X) + Math.Abs(Y - other.Y);

        /// <summary>
        /// Returns <see langword="true"/> when this coordinate lies within a board of
        /// <paramref name="width"/> by <paramref name="height"/> tiles.
        /// </summary>
        public bool IsInside(int width, int height) => X >= 0 && Y >= 0 && X < width && Y < height;

        /// <summary>
        /// Converts this coordinate to a flat array index for a board <paramref name="width"/> tiles wide.
        /// The caller is responsible for bounds checking; see <see cref="IsInside"/>.
        /// </summary>
        public int ToIndex(int width) => (Y * width) + X;

        /// <summary>
        /// Converts a flat array index back to a coordinate for a board <paramref name="width"/> tiles wide.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="width"/> is zero or negative, or <paramref name="index"/> is negative.
        /// </exception>
        public static GridCoord FromIndex(int index, int width)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), width, "Board width must be positive.");
            }

            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Index must not be negative.");
            }

            return new GridCoord(index % width, index / width);
        }

        /// <summary>Adds two coordinates component-wise.</summary>
        public static GridCoord operator +(GridCoord left, GridCoord right) =>
            new GridCoord(left.X + right.X, left.Y + right.Y);

        /// <summary>Subtracts <paramref name="right"/> from <paramref name="left"/> component-wise.</summary>
        public static GridCoord operator -(GridCoord left, GridCoord right) =>
            new GridCoord(left.X - right.X, left.Y - right.Y);

        /// <summary>Returns <see langword="true"/> when both coordinates are equal.</summary>
        public static bool operator ==(GridCoord left, GridCoord right) => left.Equals(right);

        /// <summary>Returns <see langword="true"/> when the coordinates differ.</summary>
        public static bool operator !=(GridCoord left, GridCoord right) => !left.Equals(right);

        /// <inheritdoc />
        public bool Equals(GridCoord other) => X == other.X && Y == other.Y;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is GridCoord other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        /// <inheritdoc />
        public override string ToString() => $"({X}, {Y})";
    }
}
