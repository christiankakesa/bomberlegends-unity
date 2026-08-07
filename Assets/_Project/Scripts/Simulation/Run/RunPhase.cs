namespace BomberLegends.Simulation.Run
{
    /// <summary>Where a run is in its lifecycle.</summary>
    public enum RunPhase : byte
    {
        /// <summary>An arena is being fought.</summary>
        Fighting = 0,

        /// <summary>An arena is cleared and an item is being chosen.</summary>
        Choosing = 1,

        /// <summary>The player died. Only a restart leaves this state.</summary>
        Ended = 2
    }
}
