using BomberLegends.Core;
using BomberLegends.Simulation.Events;

namespace BomberLegends.Simulation.Systems
{
    /// <summary>
    /// Flies skillshots, stops them on terrain, and applies what they hit.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A skillshot is stopped by a destructible block but does not break it. That is deliberate and
    /// load-bearing: bombs stay the only way to open the arena, so the Bomberman layer keeps its
    /// job and the skill layer stays about hitting things rather than clearing them. It also makes
    /// the destructible maze a genuine cover system rather than scenery.
    /// </para>
    /// <para>
    /// Bombs, by contrast, do not stop a shot. They sit low, and a projectile eaten by the player's
    /// own bomb at their feet would read as a bug every single time.
    /// </para>
    /// </remarks>
    public static class ProjectileSystem
    {
        /// <summary>
        /// The furthest a projectile may travel between collision checks, in sub-tile units.
        /// </summary>
        /// <remarks>
        /// Well under the width of the narrowest thing worth hitting, so no speed an item can grant
        /// lets a shot step over an enemy or through a wall.
        /// </remarks>
        private const int MaxStep = SubTilePoint.UnitsPerTile / 4;

        /// <summary>Half the width of a projectile's contact box.</summary>
        private const int Radius = 120;

        /// <summary>Advances every projectile in flight by one tick.</summary>
        public static void Tick(
            ref SimulationState state,
            in SimulationConfig config,
            SimEventBuffer events)
        {
            for (var slot = 0; slot < state.Projectiles.Capacity; slot++)
            {
                var projectile = state.Projectiles[slot];

                if (!projectile.IsActive)
                {
                    continue;
                }

                if (Advance(ref state, config, ref projectile, slot, events))
                {
                    state.Projectiles[slot] = projectile;
                    continue;
                }

                state.Projectiles[slot] = projectile;
                state.Projectiles.Remove(slot);
            }
        }

        /// <summary>Moves one projectile, returning whether it is still in flight.</summary>
        private static bool Advance(
            ref SimulationState state,
            in SimulationConfig config,
            ref Skills.ProjectileState projectile,
            int slot,
            SimEventBuffer events)
        {
            var origin = projectile.Position;
            var steps = StepCount(projectile.VelocityX, projectile.VelocityY);

            for (var step = 1; step <= steps; step++)
            {
                // Always measured from where the tick began, so splitting the travel into more
                // steps can never change where the shot ends up.
                var next = origin.Offset(
                    projectile.VelocityX * step / steps,
                    projectile.VelocityY * step / steps);

                if (next.Equals(projectile.Position))
                {
                    continue;
                }

                projectile.Position = next;

                if (state.Board.IsBlocking(next.Tile))
                {
                    events.Add(new SimEvent(SimEventType.ProjectileEnded, next.Tile, slot));
                    return false;
                }

                if (TryHitEnemy(ref state, config, projectile, slot, events))
                {
                    return false;
                }
            }

            projectile.TicksRemaining--;

            if (projectile.TicksRemaining > 0)
            {
                return true;
            }

            events.Add(new SimEvent(SimEventType.ProjectileEnded, projectile.Tile, slot));
            return false;
        }

        /// <summary>
        /// Applies a hit to the first enemy the projectile is touching.
        /// </summary>
        /// <remarks>
        /// A shot that lands on an enemy still inside its immunity window passes straight through
        /// rather than being eaten for nothing. Losing a cooldown to a hit that dealt no damage is
        /// indistinguishable from the skill misfiring.
        /// </remarks>
        private static bool TryHitEnemy(
            ref SimulationState state,
            in SimulationConfig config,
            in Skills.ProjectileState projectile,
            int slot,
            SimEventBuffer events)
        {
            for (var index = 0; index < state.Enemies.Capacity; index++)
            {
                var enemy = state.Enemies[index];

                if (!enemy.IsActive)
                {
                    continue;
                }

                if (!GridMotion.Overlaps(
                        projectile.Position, Radius, enemy.Position, config.EnemyRadius))
                {
                    continue;
                }

                if (!enemy.Health.TryTakeDamage(projectile.Damage, config.InvulnerabilityTicks))
                {
                    continue;
                }

                events.Add(new SimEvent(
                    SimEventType.DamageTaken, enemy.Tile, index + 1, projectile.Damage));

                if (!enemy.Health.IsAlive)
                {
                    enemy.IsActive = false;
                    events.Add(new SimEvent(SimEventType.EnemyKilled, enemy.Tile, index + 1));
                }

                state.Enemies[index] = enemy;

                events.Add(new SimEvent(
                    SimEventType.ProjectileEnded, projectile.Tile, slot, projectile.Damage));

                return true;
            }

            return false;
        }

        /// <summary>How many collision checks this tick's travel needs.</summary>
        private static int StepCount(int velocityX, int velocityY)
        {
            var absX = IntMath.Abs(velocityX);
            var absY = IntMath.Abs(velocityY);
            var longest = absX > absY ? absX : absY;

            return longest <= MaxStep ? 1 : ((longest + MaxStep - 1) / MaxStep);
        }
    }
}
