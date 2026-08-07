using BomberLegends.Core;
using BomberLegends.Simulation.Board;

namespace BomberLegends.Simulation.Systems
{
    /// <summary>
    /// Moves a box through the tile grid, colliding with anything solid.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Shared by the player and by every enemy. Two implementations of "walk into a wall" would mean
    /// two sets of bugs and two different feels, and the difference would show the moment an enemy
    /// caught on a corner the player glides around.
    /// </para>
    /// <para>
    /// Resolution is per axis, which is what produces wall sliding: run diagonally into a wall and the
    /// blocked component drops while the other continues. Movement is applied in sub-steps no larger
    /// than half a tile, so no speed can carry a box through a wall.
    /// </para>
    /// </remarks>
    public static class GridMotion
    {
        private const int MaxSubStep = SubTilePoint.HalfTile;

        /// <summary>
        /// The tiles a moving box is allowed to pass through despite them being occupied.
        /// </summary>
        /// <remarks>
        /// A box smaller than a tile overlaps at most four, so four inline slots are enough and
        /// nothing needs allocating. Used for the bomb an actor is already standing on.
        /// </remarks>
        public struct ExemptTiles
        {
            private GridCoord _a;
            private GridCoord _b;
            private GridCoord _c;
            private GridCoord _d;
            private int _count;

            /// <summary>Records a tile that should not block.</summary>
            public void Add(GridCoord tile)
            {
                switch (_count)
                {
                    case 0: _a = tile; break;
                    case 1: _b = tile; break;
                    case 2: _c = tile; break;
                    case 3: _d = tile; break;
                    default: return;
                }

                _count++;
            }

            /// <summary>Whether a tile is exempt.</summary>
            public readonly bool Contains(GridCoord tile) =>
                (_count > 0 && _a.Equals(tile)) ||
                (_count > 1 && _b.Equals(tile)) ||
                (_count > 2 && _c.Equals(tile)) ||
                (_count > 3 && _d.Equals(tile));
        }

        /// <summary>Records the occupied tiles a box currently overlaps, so they cannot trap it.</summary>
        public static ExemptTiles OverlappedBombs(SubTilePoint position, int radius, BombGrid bombs)
        {
            var exempt = default(ExemptTiles);

            var minX = SubTilePoint.ToTile(position.X - radius);
            var maxX = SubTilePoint.ToTile(position.X + radius);
            var minY = SubTilePoint.ToTile(position.Y - radius);
            var maxY = SubTilePoint.ToTile(position.Y + radius);

            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var tile = new GridCoord(x, y);
                    if (bombs.HasBomb(tile))
                    {
                        exempt.Add(tile);
                    }
                }
            }

            return exempt;
        }

        /// <summary>Whether two boxes overlap.</summary>
        public static bool Overlaps(SubTilePoint a, int radiusA, SubTilePoint b, int radiusB)
        {
            var reach = radiusA + radiusB;
            return IntMath.Abs(a.X - b.X) < reach && IntMath.Abs(a.Y - b.Y) < reach;
        }

        /// <summary>Applies a velocity, resolving each axis separately and sub-stepping.</summary>
        /// <returns>The position after collision.</returns>
        public static SubTilePoint Move(
            SubTilePoint position,
            int velocityX,
            int velocityY,
            int radius,
            BoardState board,
            BombGrid bombs,
            in ExemptTiles exempt,
            int cornerSlipPerTick,
            int cornerSlipTolerance)
        {
            var longest = IntMath.Abs(velocityX) > IntMath.Abs(velocityY)
                ? IntMath.Abs(velocityX)
                : IntMath.Abs(velocityY);

            var steps = 1 + ((longest - 1) / MaxSubStep);
            if (steps < 1)
            {
                steps = 1;
            }

            var appliedX = 0;
            var appliedY = 0;

            for (var step = 1; step <= steps; step++)
            {
                // Targets are recomputed from the total each time rather than accumulated, so
                // integer division leaves no remainder unspent.
                var targetX = velocityX * step / steps;
                var targetY = velocityY * step / steps;

                position = MoveAxis(position, targetX - appliedX, true, radius, board, bombs, exempt,
                    cornerSlipPerTick, cornerSlipTolerance);
                position = MoveAxis(position, targetY - appliedY, false, radius, board, bombs, exempt,
                    cornerSlipPerTick, cornerSlipTolerance);

                appliedX = targetX;
                appliedY = targetY;
            }

            return position;
        }

        private static SubTilePoint MoveAxis(
            SubTilePoint position,
            int delta,
            bool horizontal,
            int radius,
            BoardState board,
            BombGrid bombs,
            in ExemptTiles exempt,
            int cornerSlipPerTick,
            int cornerSlipTolerance)
        {
            if (delta == 0)
            {
                return position;
            }

            var moved = horizontal ? position.X + delta : position.Y + delta;

            // The span the box covers on the other axis decides which tiles it can touch.
            var otherCentre = horizontal ? position.Y : position.X;
            var minOther = SubTilePoint.ToTile(otherCentre - radius);
            var maxOther = SubTilePoint.ToTile(otherCentre + radius);

            var leadingEdge = delta > 0 ? moved + radius : moved - radius;
            var edgeTile = SubTilePoint.ToTile(leadingEdge);

            var lowBlocks = Blocks(board, bombs, Tile(horizontal, edgeTile, minOther), exempt);
            var highBlocks = minOther == maxOther
                ? lowBlocks
                : Blocks(board, bombs, Tile(horizontal, edgeTile, maxOther), exempt);

            if (lowBlocks || highBlocks)
            {
                // Rest flush against the obstacle, one unit clear so the boxes never share an edge.
                moved = delta > 0
                    ? (edgeTile * SubTilePoint.UnitsPerTile) - radius - 1
                    : ((edgeTile + 1) * SubTilePoint.UnitsPerTile) + radius;

                // Only one of the two tiles blocking means the box is clipping a corner rather than
                // facing a wall, so it can be helped around.
                if (lowBlocks != highBlocks && minOther != maxOther && cornerSlipPerTick > 0)
                {
                    position = SlipPastCorner(
                        position, horizontal, otherCentre, radius, highBlocks, maxOther, minOther,
                        cornerSlipPerTick, cornerSlipTolerance);
                }
            }

            return horizontal ? position.WithX(moved) : position.WithY(moved);
        }

        /// <summary>
        /// Nudges a box sideways so it clears a corner it is only just clipping.
        /// </summary>
        /// <remarks>
        /// Applied a little at a time rather than teleporting it clear, so the correction reads as
        /// rounding the corner rather than as the game snapping things about.
        /// </remarks>
        private static SubTilePoint SlipPastCorner(
            SubTilePoint position,
            bool horizontal,
            int otherCentre,
            int radius,
            bool blockedByHigh,
            int highTile,
            int lowTile,
            int slipPerTick,
            int tolerance)
        {
            var required = blockedByHigh
                ? (otherCentre + radius) - (highTile * SubTilePoint.UnitsPerTile) + 1
                : ((lowTile + 1) * SubTilePoint.UnitsPerTile) - (otherCentre - radius) + 1;

            if (required <= 0 || required > tolerance)
            {
                return position;
            }

            var step = required < slipPerTick ? required : slipPerTick;
            var nudged = blockedByHigh ? otherCentre - step : otherCentre + step;

            return horizontal ? position.WithY(nudged) : position.WithX(nudged);
        }

        private static GridCoord Tile(bool horizontal, int edge, int other) =>
            horizontal ? new GridCoord(edge, other) : new GridCoord(other, edge);

        private static bool Blocks(
            BoardState board, BombGrid bombs, GridCoord tile, in ExemptTiles exempt)
        {
            if (!board.IsWalkable(tile))
            {
                return true;
            }

            return bombs.HasBomb(tile) && !exempt.Contains(tile);
        }
    }
}
