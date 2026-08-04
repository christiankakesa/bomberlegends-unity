using BomberLegends.Core;
using BomberLegends.Simulation.Actors;
using BomberLegends.Simulation.Board;

namespace BomberLegends.Simulation
{
    /// <summary>
    /// Everything the simulation knows.
    /// </summary>
    /// <remarks>
    /// All value types with fixed-size storage, so a tick allocates nothing. Bombs, blasts, enemies,
    /// pickups and objectives join this as their milestones land; each arrives as another
    /// fixed-capacity buffer rather than a growable collection.
    /// </remarks>
    public struct SimulationState
    {
        /// <summary>Ticks elapsed since the match began.</summary>
        public int Tick;

        /// <summary>Where the match is in its lifecycle.</summary>
        public MatchPhase Phase;

        /// <summary>The tile grid.</summary>
        public BoardState Board;

        /// <summary>The player.</summary>
        public PlayerState Player;

        /// <summary>
        /// The match's random source. Part of the state because reproducing a match means
        /// reproducing every roll it made.
        /// </summary>
        public DeterministicRandom Random;
    }
}
