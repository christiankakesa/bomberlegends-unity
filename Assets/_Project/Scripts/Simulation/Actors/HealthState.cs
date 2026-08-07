namespace BomberLegends.Simulation.Actors
{
    /// <summary>
    /// How much punishment something can take, and how long it is briefly immune for.
    /// </summary>
    /// <remarks>
    /// The immunity window is not a nicety. A blast tile stays lethal for several ticks, so without
    /// one, standing in an explosion would deal a hit every tick and delete anything instantly. It is
    /// what turns "touching fire hurts" into "touching fire costs you a chunk".
    /// </remarks>
    public struct HealthState
    {
        /// <summary>Health remaining.</summary>
        public int Current;

        /// <summary>Health at full.</summary>
        public int Max;

        /// <summary>Ticks of immunity left after the last hit.</summary>
        public int InvulnerableTicks;

        /// <summary>Creates a fully healed actor.</summary>
        public static HealthState Full(int max) => new HealthState
        {
            Current = max,
            Max = max,
            InvulnerableTicks = 0
        };

        /// <summary>Whether there is any health left.</summary>
        public readonly bool IsAlive => Current > 0;

        /// <summary>Whether a hit would currently be ignored.</summary>
        public readonly bool IsInvulnerable => InvulnerableTicks > 0;

        /// <summary>Counts the immunity window down by one tick.</summary>
        public void Age()
        {
            if (InvulnerableTicks > 0)
            {
                InvulnerableTicks--;
            }
        }

        /// <summary>
        /// Applies damage, unless immune. Returns whether the hit landed.
        /// </summary>
        public bool TryTakeDamage(int amount, int invulnerabilityTicks)
        {
            if (amount <= 0 || IsInvulnerable || !IsAlive)
            {
                return false;
            }

            Current -= amount;
            if (Current < 0)
            {
                Current = 0;
            }

            InvulnerableTicks = invulnerabilityTicks;
            return true;
        }
    }
}
