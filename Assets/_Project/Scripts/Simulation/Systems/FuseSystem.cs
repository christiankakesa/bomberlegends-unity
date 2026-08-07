namespace BomberLegends.Simulation.Systems
{
    /// <summary>Burns down every live fuse and reports which bombs are due to go off.</summary>
    /// <remarks>
    /// Kept separate from the blast so that detonations caused by a fuse and detonations caused by
    /// another explosion enter the same queue and are resolved by the same code.
    /// </remarks>
    public static class FuseSystem
    {
        /// <summary>
        /// Advances every fuse by a tick, appending the slot of each expired bomb to
        /// <paramref name="detonations"/>.
        /// </summary>
        /// <returns>How many bombs were queued.</returns>
        public static int Tick(ref SimulationState state, int[] detonations)
        {
            var queued = 0;

            for (var slot = 0; slot < state.Bombs.Capacity; slot++)
            {
                var bomb = state.Bombs[slot];
                if (!bomb.IsActive)
                {
                    continue;
                }

                bomb.FuseTicksRemaining--;
                state.Bombs[slot] = bomb;

                if (bomb.FuseTicksRemaining <= 0 && queued < detonations.Length)
                {
                    detonations[queued++] = slot;
                }
            }

            return queued;
        }
    }
}
