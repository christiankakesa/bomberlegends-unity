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

                    var exempt = GridMotion.OverlappedBombs(
                        enemy.Position, config.EnemyRadius, state.BombGrid);

                    enemy.Position = GridMotion.Move(
                        enemy.Position,
                        offset.X * speed,
                        offset.Y * speed,
                        config.EnemyRadius,
                        state.Board,
                        state.BombGrid,
                        exempt,
                        config.CornerSlipPerTick,
                        config.CornerSlipTolerance);
                }

                // Stuck against something despite having a heading: force a fresh decision next tick.
                if (enemy.Position == beforePosition)
                {
                    enemy.MoveDirection = Direction.None;
                }

                if (enemy.Tile != beforeTile)
                {
                    enemy.MoveDirection = Direction.None;
                }

                state.Enemies[slot] = enemy;
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
            var from = enemy.Tile;
            var toward = target.Tile;

            var best = Direction.None;
            var bestDistance = int.MaxValue;
            var tied = 0;

            var cardinals = Directions.Cardinals;
            for (var i = 0; i < cardinals.Length; i++)
            {
                var candidate = cardinals[i];
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
