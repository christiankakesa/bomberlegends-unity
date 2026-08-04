using System;
using BomberLegends.Core;

namespace BomberLegends.Simulation.Board
{
    /// <summary>
    /// The tile grid a match is played on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Backed by one flat array indexed row-major as <c>Y * Width + X</c>, allocated once at
    /// construction and never resized. Nothing in a tick allocates.
    /// </para>
    /// <para>
    /// This is a struct wrapping a reference to that array, so copying a board copies the handle,
    /// not the tiles. That is intentional: the board is held inside the simulation state by value
    /// while remaining cheap to pass around, and there is exactly one owner of the tiles.
    /// </para>
    /// <para>
    /// Reads outside the grid return <see cref="TileType.Solid"/> rather than throwing. Movement and
    /// blast propagation both walk off the edge by design, and treating the outside as wall removes
    /// a bounds check from every one of those call sites.
    /// </para>
    /// </remarks>
    public struct BoardState
    {
        private readonly TileType[] _tiles;

        /// <summary>Creates an empty board.</summary>
        /// <exception cref="ArgumentOutOfRangeException">Either dimension is not positive.</exception>
        public BoardState(int width, int height)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), width, "Board width must be positive.");
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height), height, "Board height must be positive.");
            }

            Width = width;
            Height = height;
            _tiles = new TileType[width * height];
        }

        /// <summary>Tiles across.</summary>
        public int Width { get; }

        /// <summary>Tiles up.</summary>
        public int Height { get; }

        /// <summary>Total number of tiles.</summary>
        public int TileCount => Width * Height;

        /// <summary>Reads or writes a tile. Reads outside the board return <see cref="TileType.Solid"/>.</summary>
        /// <exception cref="ArgumentOutOfRangeException">A write falls outside the board.</exception>
        public TileType this[GridCoord coord]
        {
            get => coord.IsInside(Width, Height) ? _tiles[coord.ToIndex(Width)] : TileType.Solid;
            set
            {
                if (!coord.IsInside(Width, Height))
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(coord), coord, "Cannot write to a tile outside the board.");
                }

                _tiles[coord.ToIndex(Width)] = value;
            }
        }

        /// <summary>Whether the coordinate lies on the board.</summary>
        public bool Contains(GridCoord coord) => coord.IsInside(Width, Height);

        /// <summary>Whether an actor may occupy this tile.</summary>
        public bool IsWalkable(GridCoord coord) => this[coord] == TileType.Empty;

        /// <summary>Whether this tile stops movement and blasts.</summary>
        public bool IsBlocking(GridCoord coord) => this[coord] != TileType.Empty;

        /// <summary>Replaces every tile on the board.</summary>
        public void Fill(TileType tile)
        {
            for (var i = 0; i < _tiles.Length; i++)
            {
                _tiles[i] = tile;
            }
        }

        /// <summary>Copies this board's tiles into <paramref name="destination"/>.</summary>
        /// <exception cref="ArgumentException">The boards are different sizes.</exception>
        public readonly void CopyTo(BoardState destination)
        {
            if (destination.Width != Width || destination.Height != Height)
            {
                throw new ArgumentException("Boards must be the same size to copy.", nameof(destination));
            }

            Array.Copy(_tiles, destination._tiles, _tiles.Length);
        }

        /// <summary>
        /// An order-dependent hash of every tile, folded into the simulation state hash used by the
        /// determinism tests.
        /// </summary>
        public readonly ulong ComputeHash()
        {
            // FNV-1a: cheap, allocation free, and good enough to catch any divergence in practice.
            unchecked
            {
                var hash = 14695981039346656037UL;
                for (var i = 0; i < _tiles.Length; i++)
                {
                    hash ^= (byte)_tiles[i];
                    hash *= 1099511628211UL;
                }

                return hash;
            }
        }
    }
}
