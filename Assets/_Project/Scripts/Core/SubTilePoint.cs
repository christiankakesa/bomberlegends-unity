using System;

namespace BomberLegends.Core
{
    /// <summary>
    /// A position on the board expressed in integer sub-tile units.
    /// </summary>
    /// <remarks>
    /// <para>
    /// One tile is <see cref="UnitsPerTile"/> units across, so a position is exact and every
    /// operation on it is integer arithmetic. Floating point would work for a single session and
    /// then betray us: accumulated drift over a long match, and results that differ between CPUs,
    /// which breaks replay validation and any future server-side simulation.
    /// </para>
    /// <para>
    /// Tile <c>(tx, ty)</c> covers <c>[tx * UnitsPerTile, (tx + 1) * UnitsPerTile)</c> on each axis,
    /// and its centre sits at <c>tx * UnitsPerTile + UnitsPerTile / 2</c>. Conversion to world units
    /// happens in the view layer only.
    /// </para>
    /// </remarks>
    public readonly struct SubTilePoint : IEquatable<SubTilePoint>
    {
        /// <summary>Sub-tile units spanning one tile.</summary>
        public const int UnitsPerTile = 1000;

        /// <summary>Sub-tile units from a tile edge to its centre.</summary>
        public const int HalfTile = UnitsPerTile / 2;

        /// <summary>Horizontal position in sub-tile units.</summary>
        public readonly int X;

        /// <summary>Vertical position in sub-tile units.</summary>
        public readonly int Y;

        /// <summary>Creates a position from raw sub-tile units.</summary>
        public SubTilePoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>The tile containing this position.</summary>
        public GridCoord Tile => new GridCoord(ToTile(X), ToTile(Y));

        /// <summary>The centre of the given tile.</summary>
        public static SubTilePoint AtCentreOf(GridCoord tile) =>
            new SubTilePoint(CentreOf(tile.X), CentreOf(tile.Y));

        /// <summary>The centre of a tile index along one axis, in sub-tile units.</summary>
        public static int CentreOf(int tileIndex) => (tileIndex * UnitsPerTile) + HalfTile;

        /// <summary>
        /// The tile index containing a sub-tile coordinate, rounding towards negative infinity so
        /// positions left of or below the origin land in the tile that actually contains them.
        /// </summary>
        public static int ToTile(int units) =>
            units >= 0 ? units / UnitsPerTile : ((units + 1) / UnitsPerTile) - 1;

        /// <summary>How far this position sits from the centre of its own tile, per axis.</summary>
        public SubTilePoint OffsetFromTileCentre()
        {
            var tile = Tile;
            return new SubTilePoint(X - CentreOf(tile.X), Y - CentreOf(tile.Y));
        }

        /// <summary>Returns this position moved by the given sub-tile amounts.</summary>
        public SubTilePoint Offset(int deltaX, int deltaY) => new SubTilePoint(X + deltaX, Y + deltaY);

        /// <summary>Returns this position with the horizontal coordinate replaced.</summary>
        public SubTilePoint WithX(int x) => new SubTilePoint(x, Y);

        /// <summary>Returns this position with the vertical coordinate replaced.</summary>
        public SubTilePoint WithY(int y) => new SubTilePoint(X, y);

        /// <inheritdoc />
        public bool Equals(SubTilePoint other) => X == other.X && Y == other.Y;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is SubTilePoint other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        /// <summary>Returns <see langword="true"/> when both positions are identical.</summary>
        public static bool operator ==(SubTilePoint left, SubTilePoint right) => left.Equals(right);

        /// <summary>Returns <see langword="true"/> when the positions differ.</summary>
        public static bool operator !=(SubTilePoint left, SubTilePoint right) => !left.Equals(right);

        /// <inheritdoc />
        public override string ToString() => $"[{X}, {Y}]";
    }
}
