using BomberLegends.Core;
using BomberLegends.Simulation.Actors;
using BomberLegends.Simulation.Board;
using BomberLegends.Simulation.Bombs;
using BomberLegends.Simulation.Skills;

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

        /// <summary>Every enemy in the arena.</summary>
        public Actors.EnemyBuffer Enemies;

        /// <summary>
        /// How many enemies this arena started with.
        /// </summary>
        /// <remarks>
        /// The clear condition is "everything that spawned is dead", so an arena that spawned
        /// nothing has no clear condition at all rather than being instantly won. That distinction
        /// is what keeps an empty test room a sandbox instead of a match that ends on tick one.
        /// </remarks>
        public int SpawnedEnemyCount;

        /// <summary>Every bomb on the board.</summary>
        public BombBuffer Bombs;

        /// <summary>Which tile each bomb occupies, for constant-time occupancy and chain lookups.</summary>
        public BombGrid BombGrid;

        /// <summary>How much longer each tile stays lethal.</summary>
        public BlastGrid BlastGrid;

        /// <summary>
        /// How far each tile is from somewhere a blast will not reach.
        /// </summary>
        /// <remarks>
        /// Derived rather than authoritative: rebuilt from the board, the bombs and the fire at the
        /// start of every enemy update, and therefore left out of the state hash. It lives here
        /// rather than inside the enemy system because it is a property of the board, and the
        /// player's own read-out of where it is unsafe to stand will want the same answer.
        /// </remarks>
        public ThreatGrid Threats;

        /// <summary>Every skillshot in flight.</summary>
        public ProjectileBuffer Projectiles;

        /// <summary>
        /// The match's random source. Part of the state because reproducing a match means
        /// reproducing every roll it made.
        /// </summary>
        public DeterministicRandom Random;
    }
}
