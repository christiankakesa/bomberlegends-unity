using BomberLegends.Core;
using BomberLegends.Simulation.Actors;
using BomberLegends.Simulation.Board;
using BomberLegends.Simulation.Events;

namespace BomberLegends.Simulation.Systems
{
    /// <summary>
    /// Moves the player across the board.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implements the "soft grid" the design calls for: the player's position is continuous, but
    /// they travel along lane centres and every rule that matters reads the whole tile they occupy.
    /// Three behaviours make the difference between this feeling tight and feeling sticky, and all
    /// three live here:
    /// </para>
    /// <list type="bullet">
    /// <item><b>Lane snapping</b> pulls the player towards the centre of the corridor they are
    /// travelling along, so they never scrape an edge and miss an opening.</item>
    /// <item><b>Deferred turns</b> keep the player moving when they ask to turn while still short of
    /// a junction, rather than stopping them. Held against a direction, they turn the moment they
    /// arrive.</item>
    /// <item><b>Corner assist</b> lets a player who has run into a wall turn immediately regardless
    /// of alignment, which is what stops them wedging in a corner.</item>
    /// </list>
    /// <para>
    /// Movement is applied in sub-steps no larger than half a tile so a high speed can never skip
    /// over a wall.
    /// </para>
    /// </remarks>
    public static class MovementSystem
    {
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

            var requested = intent.ToDirection(config.DirectionDeadzone);
            player.MoveDirection = ResolveDirection(ref player, requested, state.Board, config);

            if (player.MoveDirection != Direction.None)
            {
                player.Facing = player.MoveDirection;

                if (!Advance(ref player, player.MoveDirection, state.Board, config))
                {
                    events.Add(new SimEvent(
                        SimEventType.PlayerBlocked, player.Tile, 0, (int)player.MoveDirection));
                }

                ApplyLaneSnap(ref player, player.MoveDirection, config);
            }

            player.IsMoving = player.Position != previousPosition;

            var tile = player.Tile;
            if (tile != previousTile)
            {
                events.Add(new SimEvent(SimEventType.PlayerTileEntered, tile));
            }
        }

        /// <summary>
        /// Decides which way the player travels this tick, applying the turn rules.
        /// </summary>
        private static Direction ResolveDirection(
            ref PlayerState player,
            Direction requested,
            BoardState board,
            in SimulationConfig config)
        {
            if (requested == Direction.None)
            {
                return Direction.None;
            }

            var current = player.MoveDirection;

            // Continuing or reversing needs no alignment: both stay in the same lane.
            if (requested == current || requested == current.Opposite())
            {
                return requested;
            }

            var tile = player.Tile;
            if (!board.IsWalkable(tile.Neighbour(requested)))
            {
                // Nothing to turn into. Keep whatever we were already doing.
                return current;
            }

            // A stationary player always gets their turn; there is no momentum to preserve.
            if (current == Direction.None)
            {
                player.Position = AlignForTurn(player.Position, tile, requested);
                return requested;
            }

            if (Misalignment(player.Position, tile, requested) <= config.TurnTolerance)
            {
                player.Position = AlignForTurn(player.Position, tile, requested);
                return requested;
            }

            if (config.CornerAssistEnabled && !board.IsWalkable(tile.Neighbour(current)))
            {
                // Running into a wall. Turning is the only thing left, so allow it however
                // misaligned the player is rather than leaving them stuck against the corner.
                player.Position = AlignForTurn(player.Position, tile, requested);
                return requested;
            }

            // Too far from the junction to turn yet. Carry on; the turn lands on arrival.
            return current;
        }

        /// <summary>
        /// How far the player is from the lane they would need to be in to travel
        /// <paramref name="direction"/>.
        /// </summary>
        private static int Misalignment(SubTilePoint position, GridCoord tile, Direction direction)
        {
            var offset = direction.ToOffset();

            // Travelling vertically constrains the horizontal position, and the other way round.
            var value = offset.X != 0
                ? position.Y - SubTilePoint.CentreOf(tile.Y)
                : position.X - SubTilePoint.CentreOf(tile.X);

            return value < 0 ? -value : value;
        }

        /// <summary>Places the player exactly in the lane for <paramref name="direction"/>.</summary>
        private static SubTilePoint AlignForTurn(SubTilePoint position, GridCoord tile, Direction direction)
        {
            var offset = direction.ToOffset();

            return offset.X != 0
                ? position.WithY(SubTilePoint.CentreOf(tile.Y))
                : position.WithX(SubTilePoint.CentreOf(tile.X));
        }

        /// <summary>
        /// Moves the player along <paramref name="direction"/>, stopping at the centre of the last
        /// free tile if something blocks the way.
        /// </summary>
        /// <returns><see langword="false"/> when a wall stopped the player short.</returns>
        private static bool Advance(
            ref PlayerState player,
            Direction direction,
            BoardState board,
            in SimulationConfig config)
        {
            var offset = direction.ToOffset();
            var horizontal = offset.X != 0;
            var sign = horizontal ? offset.X : offset.Y;

            var remaining = config.MoveSpeedPerTick;

            while (remaining > 0)
            {
                // Never advance more than half a tile at once, so the tile under the player cannot
                // change by more than one step and a wall can never be skipped over.
                var step = remaining < SubTilePoint.HalfTile ? remaining : SubTilePoint.HalfTile;
                remaining -= step;

                var tile = player.Position.Tile;
                var blocked = !board.IsWalkable(tile.Neighbour(direction));

                var current = horizontal ? player.Position.X : player.Position.Y;
                var proposed = current + (step * sign);

                if (blocked)
                {
                    var limit = SubTilePoint.CentreOf(horizontal ? tile.X : tile.Y);
                    var overshoots = sign > 0 ? proposed > limit : proposed < limit;

                    if (overshoots)
                    {
                        proposed = limit;
                    }
                }

                player.Position = horizontal
                    ? player.Position.WithX(proposed)
                    : player.Position.WithY(proposed);

                if (blocked && proposed == SubTilePoint.CentreOf(horizontal ? tile.X : tile.Y))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Pulls the player towards the centre of the lane they are travelling along.</summary>
        private static void ApplyLaneSnap(
            ref PlayerState player,
            Direction direction,
            in SimulationConfig config)
        {
            if (config.LaneSnapPerTick <= 0)
            {
                return;
            }

            var tile = player.Tile;
            var offset = direction.ToOffset();

            if (offset.X != 0)
            {
                var centre = SubTilePoint.CentreOf(tile.Y);
                player.Position = player.Position.WithY(
                    MoveToward(player.Position.Y, centre, config.LaneSnapPerTick));
            }
            else
            {
                var centre = SubTilePoint.CentreOf(tile.X);
                player.Position = player.Position.WithX(
                    MoveToward(player.Position.X, centre, config.LaneSnapPerTick));
            }
        }

        /// <summary>Steps <paramref name="value"/> towards <paramref name="target"/> without overshooting.</summary>
        private static int MoveToward(int value, int target, int maxDelta)
        {
            var delta = target - value;

            if (delta > maxDelta)
            {
                return value + maxDelta;
            }

            return delta < -maxDelta ? value - maxDelta : target;
        }
    }
}
