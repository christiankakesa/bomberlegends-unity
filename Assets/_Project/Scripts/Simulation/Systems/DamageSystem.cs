using BomberLegends.Simulation.Events;
using BomberLegends.Simulation.Skills;

namespace BomberLegends.Simulation.Systems
{
    /// <summary>
    /// Works out what the explosions and the enemies just did to everyone.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Runs after the blast has been resolved, so it reads a finished picture of what is on fire
    /// rather than a half-built one.
    /// </para>
    /// <para>
    /// The damage numbers carry a design decision: <b>your own bomb hits hard and enemies chip.</b>
    /// Health plus a dash would otherwise quietly remove the reason the Bomberman layer exists —
    /// if blowing yourself up is a minor inconvenience, there is no tension in laying a trap and
    /// standing near it.
    /// </para>
    /// </remarks>
    public static class DamageSystem
    {
        /// <summary>Applies blast and contact damage for this tick.</summary>
        public static void Tick(
            ref SimulationState state,
            in SimulationConfig config,
            SimEventBuffer events)
        {
            state.Player.Health.Age();

            ApplyBlastToEnemies(ref state, config, events);
            ApplyDashToEnemies(ref state, config, events);
            ApplyBlastToPlayer(ref state, config, events);
            ApplyContactToPlayer(ref state, config, events);

            if (!state.Player.Health.IsAlive && state.Phase == MatchPhase.Playing)
            {
                state.Phase = MatchPhase.Defeat;
                events.Add(new SimEvent(SimEventType.PlayerDied, state.Player.Tile));
            }
        }

        private static void ApplyBlastToEnemies(
            ref SimulationState state, in SimulationConfig config, SimEventBuffer events)
        {
            for (var slot = 0; slot < state.Enemies.Capacity; slot++)
            {
                var enemy = state.Enemies[slot];
                if (!enemy.IsActive || !state.BlastGrid.IsLethal(enemy.Tile))
                {
                    continue;
                }

                if (!enemy.Health.TryTakeDamage(config.BlastDamageToEnemy, config.InvulnerabilityTicks))
                {
                    state.Enemies[slot] = enemy;
                    continue;
                }

                events.Add(new SimEvent(
                    SimEventType.DamageTaken, enemy.Tile, slot + 1, config.BlastDamageToEnemy));

                if (!enemy.Health.IsAlive)
                {
                    enemy.IsActive = false;
                    events.Add(new SimEvent(SimEventType.EnemyKilled, enemy.Tile, slot + 1));
                }

                state.Enemies[slot] = enemy;
            }
        }

        /// <summary>
        /// Injures every enemy a damaging dash is passing through.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Resolved before the enemy's own contact damage, so killing what you dash through is the
        /// reward for committing to it. Surviving enemies still land their hit: the dash grants no
        /// immunity, so passing through a mob is a trade rather than a free kill. That is what keeps
        /// the skill honest now that it is a weapon as well as an escape.
        /// </para>
        /// <para>
        /// A single overlap test per tick is enough, and safely so: a dash covers less ground in one
        /// tick than the combined width of the two boxes, so it cannot pass through an enemy
        /// unnoticed. <c>DashOutrunsContact</c> asserts that relationship holds.
        /// </para>
        /// </remarks>
        private static void ApplyDashToEnemies(
            ref SimulationState state, in SimulationConfig config, SimEventBuffer events)
        {
            if (!state.Player.DashedThisTick ||
                !state.Player.DashTraits.Has(SkillTraits.DamagesContacts) ||
                state.Player.DashPower <= 0)
            {
                return;
            }

            for (var slot = 0; slot < state.Enemies.Capacity; slot++)
            {
                var enemy = state.Enemies[slot];

                if (!enemy.IsActive)
                {
                    continue;
                }

                if (!GridMotion.Overlaps(
                        state.Player.Position, config.PlayerRadius, enemy.Position, config.EnemyRadius))
                {
                    continue;
                }

                if (!enemy.Health.TryTakeDamage(state.Player.DashPower, config.InvulnerabilityTicks))
                {
                    continue;
                }

                events.Add(new SimEvent(
                    SimEventType.DamageTaken, enemy.Tile, slot + 1, state.Player.DashPower));

                if (!enemy.Health.IsAlive)
                {
                    enemy.IsActive = false;
                    events.Add(new SimEvent(SimEventType.EnemyKilled, enemy.Tile, slot + 1));
                }

                state.Enemies[slot] = enemy;
            }
        }

        private static void ApplyBlastToPlayer(
            ref SimulationState state, in SimulationConfig config, SimEventBuffer events)
        {
            if (!state.BlastGrid.IsLethal(state.Player.Tile))
            {
                return;
            }

            if (state.Player.Health.TryTakeDamage(config.BlastDamageToPlayer, config.InvulnerabilityTicks))
            {
                events.Add(new SimEvent(
                    SimEventType.DamageTaken, state.Player.Tile, 0, config.BlastDamageToPlayer));
            }
        }

        private static void ApplyContactToPlayer(
            ref SimulationState state, in SimulationConfig config, SimEventBuffer events)
        {
            for (var slot = 0; slot < state.Enemies.Capacity; slot++)
            {
                var enemy = state.Enemies[slot];
                if (!enemy.IsActive)
                {
                    continue;
                }

                if (!GridMotion.Overlaps(
                        state.Player.Position, config.PlayerRadius, enemy.Position, config.EnemyRadius))
                {
                    continue;
                }

                if (state.Player.Health.TryTakeDamage(
                        config.EnemyContactDamage, config.InvulnerabilityTicks))
                {
                    events.Add(new SimEvent(
                        SimEventType.DamageTaken, state.Player.Tile, 0, config.EnemyContactDamage));
                }

                // One source of contact damage per tick; the immunity window covers the rest anyway.
                return;
            }
        }
    }
}
