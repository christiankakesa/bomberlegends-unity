using BomberLegends.Core;
using BomberLegends.Simulation.Events;

namespace BomberLegends.Simulation.Systems
{
    /// <summary>
    /// Drives the enemies: pursue the player, and collide with the world exactly as the player does.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The chase is deliberately simple. It picks whichever open direction closes the distance and
    /// commits to it until the tile changes or something blocks the way. That reads as pursuit
    /// without pathfinding, and — more importantly — it is trivially deterministic, which a
    /// full-fledged planner would not be.
    /// </para>
    /// <para>
    /// Ties are broken with the match's own random source rather than a fixed preference, so two
    /// enemies in the same situation do not move as one, and a replay still reproduces exactly.
    /// </para>
    /// <para>
    /// Sentinels are <b>dormant until approached</b>. Every enemy hunting from the first tick meant
    /// an arena could only ever be fought as one mass, which is what made the second sector
    /// unplayable in testing. Waking on proximity turns the same enemies into a sequence of
    /// encounters the player chooses to start, and makes the maze worth reading.
    /// </para>
    /// </remarks>
    public static class EnemySystem
    {
        /// <summary>Advances every living enemy by one tick.</summary>
        public static void Tick(
            ref SimulationState state,
            in SimulationConfig config,
            SimEventBuffer events)
        {
            var target = state.Player.Position;

            for (var slot = 0; slot < state.Enemies.Capacity; slot++)
            {
                var enemy = state.Enemies[slot];
                if (!enemy.IsActive)
                {
                    continue;
                }

                enemy.Health.Age();

                // Waking is checked before anything else moves, so a Sentinel the player has just
                // walked up to reacts on the same tick rather than a tick late.
                if (!enemy.IsAlerted)
                {
                    if (enemy.Tile.ManhattanDistanceTo(state.Player.Tile) > config.EnemyAggroRadius)
                    {
                        // Dormant: still takes damage and still blocks, but does not hunt.
                        state.Enemies[slot] = enemy;
                        continue;
                    }

                    enemy.IsAlerted = true;
                }

                var beforeTile = enemy.Tile;
                var beforePosition = enemy.Position;

                // Re-decide on arriving somewhere new, or when the current heading is exhausted.
                if (enemy.MoveDirection == Direction.None || !CanContinue(ref state, enemy, config))
                {
                    enemy.MoveDirection = ChooseDirection(ref state, enemy, target, config);
                }

                if (enemy.MoveDirection != Direction.None)
                {
                    var offset = enemy.MoveDirection.ToOffset();
                    var speed = config.EnemySpeedPerTick;

                    CentreInLane(enemy, offset, config, out var driftX, out var driftY);

                    var exempt = GridMotion.OverlappedBombs(
                        enemy.Position, config.EnemyRadius, state.BombGrid);

                    enemy.Position = GridMotion.Move(
                        enemy.Position,
                        (offset.X * speed) + driftX,
                        (offset.Y * speed) + driftY,
                        config.EnemyRadius,
                        state.Board,
                        state.BombGrid,
                        exempt,
                        config.CornerSlipPerTick,
                        config.CornerSlipTolerance);
                }

                // Stuck against something despite having a heading: remember what failed, so the
                // next decision does not simply choose it again and wedge here permanently.
                if (enemy.Position == beforePosition)
                {
                    enemy.BlockedDirection = enemy.MoveDirection;
                    enemy.MoveDirection = Direction.None;
                }
                else
                {
                    enemy.BlockedDirection = Direction.None;
                }

                if (enemy.Tile != beforeTile)
                {
                    enemy.MoveDirection = Direction.None;
                    enemy.BlockedDirection = Direction.None;
                }

                state.Enemies[slot] = enemy;
            }
        }

        /// <summary>
        /// Pulls the enemy toward the middle of the lane it is travelling along.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The chase reasons in whole tiles but the body is a box, and those two disagree the moment
        /// an enemy sits off-centre: the tile ahead reads walkable while the box is clipping the
        /// pillar beside it. Corner slip, which exists to help the <i>player</i> round corners, is
        /// what knocks them off-centre in the first place — so the assist was quietly creating the
        /// condition that trapped them.
        /// </para>
        /// <para>
        /// Centred enemies never clip a corner, so the disagreement cannot arise. This is what
        /// <see cref="SimulationConfig.LaneSnapPerTick"/> was written for; it went unused when the
        /// player moved to free 360° travel, and it is exactly right here.
        /// </para>
        /// </remarks>
        private static void CentreInLane(
            in Actors.EnemyState enemy,
            GridCoord offset,
            in SimulationConfig config,
            out int driftX,
            out int driftY)
        {
            driftX = 0;
            driftY = 0;

            var snap = config.LaneSnapPerTick;
            if (snap <= 0)
            {
                return;
            }

            if (offset.X != 0)
            {
                var centre = SubTilePoint.CentreOf(enemy.Tile.Y);
                driftY = IntMath.Clamp(centre - enemy.Position.Y, -snap, snap);
                return;
            }

            if (offset.Y != 0)
            {
                var centre = SubTilePoint.CentreOf(enemy.Tile.X);
                driftX = IntMath.Clamp(centre - enemy.Position.X, -snap, snap);
            }
        }

        /// <summary>Whether the enemy's current heading is still open.</summary>
        private static bool CanContinue(
            ref SimulationState state, in Actors.EnemyState enemy, in SimulationConfig config)
        {
            var ahead = enemy.Tile.Neighbour(enemy.MoveDirection);
            return state.Board.IsWalkable(ahead) && !state.BombGrid.HasBomb(ahead);
        }

        /// <summary>
        /// Picks an open direction, preferring one that closes on the player.
        /// </summary>
        private static Direction ChooseDirection(
            ref SimulationState state,
            in Actors.EnemyState enemy,
            SubTilePoint target,
            in SimulationConfig config)
        {
            var chosen = Choose(ref state, enemy, target, enemy.BlockedDirection);

            // Everything else was walled off, so the failed heading is all that is left. Trying it
            // again beats standing still: whatever was clipping may since have moved.
            return chosen != Direction.None
                ? chosen
                : Choose(ref state, enemy, target, Direction.None);
        }

        private static Direction Choose(
            ref SimulationState state,
            in Actors.EnemyState enemy,
            SubTilePoint target,
            Direction avoid)
        {
            var from = enemy.Tile;
            var toward = target.Tile;

            var best = Direction.None;
            var bestDistance = int.MaxValue;
            var tied = 0;

            var cardinals = Directions.Cardinals;
            for (var i = 0; i < cardinals.Length; i++)
            {
                var candidate = cardinals[i];

                if (candidate == avoid)
                {
                    continue;
                }

                var step = from.Neighbour(candidate);

                if (!state.Board.IsWalkable(step) || state.BombGrid.HasBomb(step))
                {
                    continue;
                }

                var distance = step.ManhattanDistanceTo(toward);

                if (distance < bestDistance)
                {
                    best = candidate;
                    bestDistance = distance;
                    tied = 1;
                    continue;
                }

                if (distance != bestDistance)
                {
                    continue;
                }

                // Reservoir sampling over equally good options, so two enemies in identical
                // situations do not march in lockstep — and a replay still reproduces exactly.
                tied++;
                if (state.Random.NextInt(tied) == 0)
                {
                    best = candidate;
                }
            }

            return best;
        }
    }
}
