namespace BomberLegends.Simulation
{
    /// <summary>Fixed properties of the simulation that no level or config may vary.</summary>
    public static class SimulationConstants
    {
        /// <summary>
        /// Ticks the simulation advances per second.
        /// </summary>
        /// <remarks>
        /// Thirty is ample for a tile grid, halves the per-frame simulation cost on low-end phones
        /// compared with sixty, and keeps replay and network payloads small. Smooth motion comes
        /// from the view interpolating between ticks, not from ticking faster.
        /// </remarks>
        public const int TicksPerSecond = 30;
    }
}
