using BomberLegends.Core;

namespace BomberLegends.Simulation.Bombs
{
    /// <summary>One placed bomb.</summary>
    public struct BombState
    {
        /// <summary>The tile it sits on.</summary>
        public GridCoord Tile;

        /// <summary>Ticks left before it detonates.</summary>
        public int FuseTicksRemaining;

        /// <summary>How many tiles its blast reaches along each arm.</summary>
        public int Range;

        /// <summary>Whether this slot holds a live bomb.</summary>
        public bool IsActive;

        /// <summary>
        /// Whether this bomb is already waiting in the detonation queue.
        /// </summary>
        /// <remarks>
        /// Several arms of the same explosion can reach one bomb. Without this flag it would be
        /// queued once per arm, and a ring of bombs could enqueue each other without bound.
        /// </remarks>
        public bool IsQueued;
    }
}
