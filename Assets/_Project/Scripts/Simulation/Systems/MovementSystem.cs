using BomberLegends.Core;
using BomberLegends.Simulation.Actors;
using BomberLegends.Simulation.Board;
using BomberLegends.Simulation.Events;

namespace BomberLegends.Simulation.Systems
{
    /// <summary>
    /// Moves the player freely in any direction, colliding with the grid.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The MOBA half of the hybrid. The player is a box that travels continuously; the board it
    /// collides against is still made of whole tiles, and bombs and blasts still snap to them. That
    /// split is the whole design: fluid to control, readable to survive.
    /// </para>
    /// <para>
    /// Two behaviours carry the feel:
    /// </para>
    /// <list type="bullet">
    /// <item><b>Per-axis resolution</b> produces wall sliding. Running diagonally into a wall drops
    /// the blocked component and keeps the other, so the player slides along the surface instead of
    /// stopping dead. This is the continuous successor to the grid version's corner assist, and it
    /// is just as essential — sticking on walls is what makes a game feel cheap.</item>
    /// <item><b>Sub-stepping</b> caps how far the box travels before collision is re-checked, so no
    /// speed can carry it through a wall.</item>
    /// </list>
    /// <para>
    /// Every calculation is integer. Normalising a stick vector needs a square root, and the
    /// floating-point one is not bit-identical across platforms; one differing bit in a velocity
    /// compounds into a divergent match, which would break replays and any server-side validation.
    /// </para>
    /// </remarks>
    public static class MovementSystem
    {
        /// <summary>The most a sub-step may travel, so a wall can never be skipped.</summary>
        private const int MaxSubStep = SubTilePoint.HalfTile;

        /// <summary>Advances the player by one tick.</summary>
        public static void Tick(
            ref SimulationState state,
            in SimulationConfig config,
            in PlayerIntent intent,
            SimEventBuffer events)
        {
            ref var player = ref state.Player;

            var previousTile = player.Tile;
            var previousPosition = player.Position;

            ComputeVelocity(intent, config, out var velocityX, out var velocityY);

            if (velocityX != 0 || velocityY != 0)
            {
                player.MoveDirection = DominantDirection(velocityX, velocityY);
                player.Facing = player.MoveDirection;

                // Bombs the player is already standing on cannot block them this tick. That is the
                // whole of the classic "walk off your own bomb" rule, with no ownership tracking:
                // they can leave, and once clear the bomb blocks them like any other obstacle.
                var exempt = OverlappedBombs(player.Position, config.PlayerRadius, state.BombGrid);

                Move(ref player, velocityX, velocityY, state.Board, state.BombGrid, config, exempt);
            }
            else
            {
                player.MoveDirection = Direction.None;
            }

            player.IsMoving = player.Position != previousPosition;

            var tile = player.Tile;
            if (tile != previousTile)
            {
                events.Add(new SimEvent(SimEventType.PlayerTileEntered, tile));
            }
        }

        /// <summary>
        /// Converts the stick into a velocity in sub-tile units per tick.
        /// </summary>
        /// <remarks>
        /// Magnitude is clamped to the stick's full range before scaling, so pushing diagonally is
        /// not faster than pushing straight — the classic bug in twin-stick movement. Partial
        /// deflection is preserved below that, so the stick is genuinely analogue.
        /// </remarks>
        private static void ComputeVelocity(
            in PlayerIntent intent, in SimulationConfig config, out int velocityX, out int velocityY)
        {
            int x = intent.MoveX;
            int y = intent.MoveY;

            var lengthSquared = (x * x) + (y * y);
            var deadzone = config.DirectionDeadzone;

            if (lengthSquared < deadzone * deadzone)
            {
                velocityX = 0;
                velocityY = 0;
                return;
            }

            var length = IntMath.Sqrt(lengthSquared);
            var speed = config.MoveSpeedPerTick;

            if (length <= PlayerIntent.AxisRange)
            {
                velocityX = x * speed / PlayerIntent.AxisRange;
                velocityY = y * speed / PlayerIntent.AxisRange;
                return;
            }

            velocityX = x * speed / length;
            velocityY = y * speed / length;
        }

        /// <summary>Applies a velocity in sub-steps, resolving each axis separately.</summary>
        private static void Move(
            ref PlayerState player,
            int velocityX,
            int velocityY,
            BoardState board,
            BombGrid bombs,
            in SimulationConfig config,
            in ExemptTiles exempt)
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

                MoveAxis(ref player, targetX - appliedX, horizontal: true, board, bombs, config, exempt);
                MoveAxis(ref player, targetY - appliedY, horizontal: false, board, bombs, config, exempt);

                appliedX = targetX;
                appliedY = targetY;
            }
        }

        /// <summary>Moves along one axis and pushes back out of anything solid.</summary>
        private static void MoveAxis(
            ref PlayerState player,
            int delta,
            bool horizontal,
            BoardState board,
            BombGrid bombs,
            in SimulationConfig config,
            in ExemptTiles exempt)
        {
            if (delta == 0)
            {
                return;
            }

            var radius = config.PlayerRadius;
            var position = player.Position;

            var moved = horizontal
                ? position.X + delta
                : position.Y + delta;

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

                // Only one of the two tiles blocking means the player is clipping a corner rather
                // than facing a wall, so they can be helped around it.
                if (lowBlocks != highBlocks && minOther != maxOther)
                {
                    TrySlipPastCorner(
                        ref player, horizontal, otherCentre, radius,
                        blockedByHigh: highBlocks, highTile: maxOther, lowTile: minOther, config);

                    // The nudge changed the other axis, so re-read it before writing this one.
                    position = player.Position;
                }
            }

            player.Position = horizontal ? position.WithX(moved) : position.WithY(moved);
        }

        /// <summary>Builds a tile coordinate for whichever axis is being resolved.</summary>
        private static GridCoord Tile(bool horizontal, int edge, int other) =>
            horizontal ? new GridCoord(edge, other) : new GridCoord(other, edge);

        /// <summary>
        /// Nudges the player sideways so they clear a corner they are only just clipping.
        /// </summary>
        /// <remarks>
        /// Applied a little at a time rather than teleporting them clear, so the correction reads as
        /// the character rounding the corner rather than as the game snapping them somewhere.
        /// </remarks>
        private static void TrySlipPastCorner(
            ref PlayerState player,
            bool horizontal,
            int otherCentre,
            int radius,
            bool blockedByHigh,
            int highTile,
            int lowTile,
            in SimulationConfig config)
        {
            if (config.CornerSlipPerTick <= 0)
            {
                return;
            }

            // How far the player would have to move to stop overlapping the offending tile.
            var required = blockedByHigh
                ? (otherCentre + radius) - (highTile * SubTilePoint.UnitsPerTile) + 1
                : ((lowTile + 1) * SubTilePoint.UnitsPerTile) - (otherCentre - radius) + 1;

            if (required <= 0 || required > config.CornerSlipTolerance)
            {
                return;
            }

            var step = required < config.CornerSlipPerTick ? required : config.CornerSlipPerTick;
            var nudged = blockedByHigh ? otherCentre - step : otherCentre + step;

            player.Position = horizontal
                ? player.Position.WithY(nudged)
                : player.Position.WithX(nudged);
        }

        /// <summary>Whether a tile stops the player.</summary>
        private static bool Blocks(
            BoardState board, BombGrid bombs, GridCoord tile, in ExemptTiles exempt)
        {
            if (!board.IsWalkable(tile))
            {
                return true;
            }

            return bombs.HasBomb(tile) && !exempt.Contains(tile);
        }

        /// <summary>Records the bomb tiles the player's box already overlaps.</summary>
        private static ExemptTiles OverlappedBombs(SubTilePoint position, int radius, BombGrid bombs)
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

        /// <summary>The cardinal direction that best describes a velocity, for facing and animation.</summary>
        private static Direction DominantDirection(int velocityX, int velocityY)
        {
            if (IntMath.Abs(velocityX) >= IntMath.Abs(velocityY))
            {
                return velocityX > 0 ? Direction.East : velocityX < 0 ? Direction.West : Direction.None;
            }

            return velocityY > 0 ? Direction.North : Direction.South;
        }

        /// <summary>
        /// The handful of bomb tiles that do not block the player this tick.
        /// </summary>
        /// <remarks>
        /// A box smaller than a tile can overlap at most four of them, so four inline slots are
        /// enough and nothing needs allocating.
        /// </remarks>
        private struct ExemptTiles
        {
            private GridCoord _a;
            private GridCoord _b;
            private GridCoord _c;
            private GridCoord _d;
            private int _count;

            internal void Add(GridCoord tile)
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

            internal readonly bool Contains(GridCoord tile) =>
                (_count > 0 && _a.Equals(tile)) ||
                (_count > 1 && _b.Equals(tile)) ||
                (_count > 2 && _c.Equals(tile)) ||
                (_count > 3 && _d.Equals(tile));
        }
    }
}
