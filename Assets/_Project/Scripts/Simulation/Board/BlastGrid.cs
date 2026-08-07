using System;
using BomberLegends.Core;

namespace BomberLegends.Simulation.Board
{
    /// <summary>
    /// How much longer each tile stays lethal.
    /// </summary>
    /// <remarks>
    /// A grid of countdowns rather than a list of blast shapes. Whether a tile kills is the question
    /// the damage system asks most often, and this makes it one array read; overlapping blasts merge
    /// naturally by taking whichever lasts longer.
    /// </remarks>
    public struct BlastGrid
    {
        private readonly int[] _ticksRemaining;

        /// <summary>Creates a grid matching a board of the given size.</summary>
        /// <exception cref="ArgumentOutOfRangeException">Either dimension is not positive.</exception>
        public BlastGrid(int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "Grid dimensions must be positive.");
            }

            Width = width;
            Height = height;
            _ticksRemaining = new int[width * height];
        }

        /// <summary>Tiles across.</summary>
        public int Width { get; }

        /// <summary>Tiles up.</summary>
        public int Height { get; }

        /// <summary>Whether standing on this tile is currently fatal.</summary>
        public readonly bool IsLethal(GridCoord tile) =>
            tile.IsInside(Width, Height) && _ticksRemaining[tile.ToIndex(Width)] > 0;

        /// <summary>Ticks of blast left on a tile.</summary>
        public readonly int TicksRemainingAt(GridCoord tile) =>
            tile.IsInside(Width, Height) ? _ticksRemaining[tile.ToIndex(Width)] : 0;

        /// <summary>
        /// Sets a tile alight for the given duration, and reports whether it was previously clear.
        /// </summary>
        /// <remarks>
        /// Overlapping blasts keep the longer of the two, so a tile caught by a second explosion does
        /// not go out early. The return value lets the caller raise one effect per new blast tile
        /// rather than one per overlapping arm.
        /// </remarks>
        public bool Ignite(GridCoord tile, int ticks)
        {
            if (!tile.IsInside(Width, Height))
            {
                return false;
            }

            var index = tile.ToIndex(Width);
            var wasClear = _ticksRemaining[index] <= 0;

            if (ticks > _ticksRemaining[index])
            {
                _ticksRemaining[index] = ticks;
            }

            return wasClear;
        }

        /// <summary>Counts every burning tile down by one, and reports how many went out.</summary>
        public int Decay()
        {
            var extinguished = 0;

            for (var i = 0; i < _ticksRemaining.Length; i++)
            {
                if (_ticksRemaining[i] <= 0)
                {
                    continue;
                }

                _ticksRemaining[i]--;
                if (_ticksRemaining[i] == 0)
                {
                    extinguished++;
                }
            }

            return extinguished;
        }

        /// <summary>Extinguishes everything.</summary>
        public void ClearAll() => Array.Clear(_ticksRemaining, 0, _ticksRemaining.Length);
    }
}
