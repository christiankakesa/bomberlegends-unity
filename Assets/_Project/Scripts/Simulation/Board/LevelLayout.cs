using System;
using BomberLegends.Core;

namespace BomberLegends.Simulation.Board
{
    /// <summary>
    /// A level's starting state, as plain data.
    /// </summary>
    /// <remarks>
    /// This is the shape the simulation is handed. Authoring assets live in the Data layer and bake
    /// down to this, so the simulation never touches a ScriptableObject and can be built and tested
    /// without the engine.
    /// </remarks>
    public readonly struct LevelLayout
    {
        /// <summary>Character marking free floor in a text layout.</summary>
        public const char EmptyGlyph = '.';

        /// <summary>Character marking permanent structure in a text layout.</summary>
        public const char SolidGlyph = '#';

        /// <summary>Character marking a destructible block in a text layout.</summary>
        public const char DestructibleGlyph = 'X';

        /// <summary>Character marking the player's starting tile in a text layout.</summary>
        public const char SpawnGlyph = 'P';

        private readonly TileType[] _tiles;

        /// <summary>Creates a layout from tiles in row-major order, bottom row first.</summary>
        /// <exception cref="ArgumentException">The tile count does not match the dimensions.</exception>
        public LevelLayout(int width, int height, TileType[] tiles, GridCoord playerSpawn)
        {
            if (tiles == null)
            {
                throw new ArgumentNullException(nameof(tiles));
            }

            if (width <= 0 || height <= 0)
            {
                throw new ArgumentException("Level dimensions must be positive.", nameof(width));
            }

            if (tiles.Length != width * height)
            {
                throw new ArgumentException(
                    $"Expected {width * height} tiles for a {width}x{height} level but got {tiles.Length}.",
                    nameof(tiles));
            }

            if (!playerSpawn.IsInside(width, height))
            {
                throw new ArgumentException(
                    $"Player spawn {playerSpawn} is outside a {width}x{height} level.", nameof(playerSpawn));
            }

            Width = width;
            Height = height;
            _tiles = tiles;
            PlayerSpawn = playerSpawn;
        }

        /// <summary>Tiles across.</summary>
        public int Width { get; }

        /// <summary>Tiles up.</summary>
        public int Height { get; }

        /// <summary>Where the player begins, and returns to after losing a life.</summary>
        public GridCoord PlayerSpawn { get; }

        /// <summary>Writes this layout into a board of matching size.</summary>
        /// <exception cref="ArgumentException">The board is a different size.</exception>
        public void ApplyTo(BoardState board)
        {
            if (board.Width != Width || board.Height != Height)
            {
                throw new ArgumentException(
                    $"Board is {board.Width}x{board.Height} but the layout is {Width}x{Height}.",
                    nameof(board));
            }

            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    board[new GridCoord(x, y)] = _tiles[(y * Width) + x];
                }
            }
        }

        /// <summary>Creates a board already populated with this layout.</summary>
        public BoardState CreateBoard()
        {
            var board = new BoardState(Width, Height);
            ApplyTo(board);
            return board;
        }

        /// <summary>
        /// Parses a text layout, where the first row given is the <b>top</b> of the level.
        /// </summary>
        /// <remarks>
        /// Reading top-down matches how a level looks when written out, while grid space has Y
        /// increasing upwards — so the first row becomes the highest Y. Getting that backwards would
        /// silently mirror every level, so it is asserted by test.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// The rows are empty, ragged, contain an unknown glyph, or do not define exactly one spawn.
        /// </exception>
        public static LevelLayout Parse(params string[] rows)
        {
            if (rows == null || rows.Length == 0)
            {
                throw new ArgumentException("A level layout needs at least one row.", nameof(rows));
            }

            var height = rows.Length;
            var width = rows[0].Length;

            if (width == 0)
            {
                throw new ArgumentException("A level layout row cannot be empty.", nameof(rows));
            }

            var tiles = new TileType[width * height];
            var spawn = default(GridCoord);
            var spawnCount = 0;

            for (var row = 0; row < height; row++)
            {
                if (rows[row].Length != width)
                {
                    throw new ArgumentException(
                        $"Row {row} is {rows[row].Length} characters but row 0 is {width}.", nameof(rows));
                }

                // The first row given is the top of the level, which is the highest Y in grid space.
                var y = height - 1 - row;

                for (var x = 0; x < width; x++)
                {
                    var glyph = rows[row][x];
                    var index = (y * width) + x;

                    switch (glyph)
                    {
                        case EmptyGlyph:
                            tiles[index] = TileType.Empty;
                            break;
                        case SolidGlyph:
                            tiles[index] = TileType.Solid;
                            break;
                        case DestructibleGlyph:
                            tiles[index] = TileType.Destructible;
                            break;
                        case SpawnGlyph:
                            tiles[index] = TileType.Empty;
                            spawn = new GridCoord(x, y);
                            spawnCount++;
                            break;
                        default:
                            throw new ArgumentException(
                                $"Unknown glyph '{glyph}' at row {row}, column {x}.", nameof(rows));
                    }
                }
            }

            if (spawnCount != 1)
            {
                throw new ArgumentException(
                    $"A level must define exactly one '{SpawnGlyph}' spawn tile but found {spawnCount}.",
                    nameof(rows));
            }

            return new LevelLayout(width, height, tiles, spawn);
        }
    }
}
