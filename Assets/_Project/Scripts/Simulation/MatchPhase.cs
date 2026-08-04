namespace BomberLegends.Simulation
{
    /// <summary>Where a match is in its lifecycle.</summary>
    public enum MatchPhase : byte
    {
        /// <summary>Board is shown, input is ignored, the opening count is running.</summary>
        Countdown = 0,

        /// <summary>Input is accepted and systems advance.</summary>
        Playing = 1,

        /// <summary>Objectives met. The simulation is frozen.</summary>
        Victory = 2,

        /// <summary>Lives or time exhausted. The simulation is frozen.</summary>
        Defeat = 3
    }
}
