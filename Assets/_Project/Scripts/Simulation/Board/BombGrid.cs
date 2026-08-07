using System;
using BomberLegends.Core;

namespace BomberLegends.Simulation.Board
{
    /// <summary>
    /// Which tile each live bomb occupies.
    /// </summary>
    /// <remarks>
    /// A parallel grid rather than a search through the bomb list. Movement asks "can I enter this
    /// tile" on every sub-step and blast propagation asks "is there a bomb here" on every tile it
    /// reaches, so both need to be a single array read rather than a scan.
    /// </remarks>
    public struct BombGrid
    {
        // Stores slot index + 1, so zero cleanly means "no bomb" without a separate flag array.
        private readonly int[] _slots;

        /// <summary>Creates a grid matching a board of the given size.</summary>
        /// <exception cref="ArgumentOutOfRangeException">Either dimension is not positive.</exception>
        public BombGrid(int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "Grid dimensions must be positive.");
            }

            Width = width;
            Height = height;
            _slots = new int[width * height];
        }

        /// <summary>Tiles across.</summary>
        public int Width { get; }

        /// <summary>Tiles up.</summary>
        public int Height { get; }

        /// <summary>Whether a bomb occupies this tile. Tiles outside the grid never hold one.</summary>
        public readonly bool HasBomb(GridCoord tile) => SlotAt(tile) >= 0;

        /// <summary>The bomb slot occupying this tile, or <c>-1</c> when there is none.</summary>
        public readonly int SlotAt(GridCoord tile) =>
            tile.IsInside(Width, Height) ? _slots[tile.ToIndex(Width)] - 1 : -1;

        /// <summary>Records that a bomb slot occupies a tile.</summary>
        public void Set(GridCoord tile, int slot)
        {
            if (tile.IsInside(Width, Height))
            {
                _slots[tile.ToIndex(Width)] = slot + 1;
            }
        }

        /// <summary>Clears a tile.</summary>
        public void Clear(GridCoord tile)
        {
            if (tile.IsInside(Width, Height))
            {
                _slots[tile.ToIndex(Width)] = 0;
            }
        }

        /// <summary>Clears every tile.</summary>
        public void ClearAll() => Array.Clear(_slots, 0, _slots.Length);
    }
}
