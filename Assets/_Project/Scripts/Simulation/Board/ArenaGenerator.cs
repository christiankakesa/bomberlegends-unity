using System;
using BomberLegends.Core;

namespace BomberLegends.Simulation.Board
{
    /// <summary>
    /// Builds an arena from a seed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Engine-free and deterministic: the same seed and arena index always produce the same board,
    /// on any platform, which is what keeps a whole run reproducible from its seed alone.
    /// </para>
    /// <para>
    /// Two guarantees are enforced rather than hoped for, because they are how generated levels
    /// actually fail: <b>a clear pocket around spawn</b>, and <b>a board with no unreachable
    /// ground</b>. A random arena that walls the player in is worse than the same handmade layout
    /// played twice.
    /// </para>
    /// </remarks>
    public static class ArenaGenerator
    {
        /// <summary>Generates the arena for a given position in a run.</summary>
        /// <exception cref="ArgumentException">The settings are unusable.</exception>
        public static LevelLayout Generate(
            int arenaIndex, in ArenaSettings settings, ref DeterministicRandom random)
        {
            settings.Validate();

            var width = Grow(settings.BaseWidth, settings.GrowthPerArena, arenaIndex, settings.MaxWidth);
            var height = Grow(settings.BaseHeight, settings.GrowthPerArena, arenaIndex, settings.MaxHeight);

            var tiles = new TileType[width * height];
            var style = (ArenaStyle)random.NextInt(3);

            BuildStructure(tiles, width, height, style, ref random);

            // Top-left, one tile in from the corner. A fixed corner keeps the opening moment
            // familiar however wildly the rest of the board is rolled.
            var spawn = new GridCoord(1, height - 2);

            ClearSafePocket(tiles, width, height, spawn, settings.SafePocketRadius);

            var reachable = SealUnreachableGround(tiles, width, height, spawn);

            ScatterDestructibles(
                tiles, width, height, spawn, settings, reachable, ref random);

            var enemies = PlaceEnemies(
                tiles, width, height, spawn, arenaIndex, settings, reachable, ref random);

            return new LevelLayout(width, height, tiles, spawn, enemies);
        }

        /// <summary>Sizes an arena for its position in the run, always odd so a lattice fits.</summary>
        private static int Grow(int start, int growth, int arenaIndex, int cap)
        {
            var size = start + (growth * arenaIndex);

            if (size > cap)
            {
                size = cap;
            }

            // An even dimension leaves a doubled row against one wall, which reads as a mistake.
            return size % 2 == 0 ? size - 1 : size;
        }

        private static void BuildStructure(
            TileType[] tiles, int width, int height, ArenaStyle style, ref DeterministicRandom random)
        {
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var border = x == 0 || y == 0 || x == width - 1 || y == height - 1;
                    tiles[(y * width) + x] = border ? TileType.Solid : TileType.Empty;
                }
            }

            switch (style)
            {
                case ArenaStyle.Lattice:
                    BuildLattice(tiles, width, height);
                    return;

                case ArenaStyle.Scattered:
                    BuildScattered(tiles, width, height, ref random);
                    return;

                default:
                    BuildChambers(tiles, width, height, ref random);
                    return;
            }
        }

        /// <summary>A pillar on every second tile. Open, readable, and connected by construction.</summary>
        private static void BuildLattice(TileType[] tiles, int width, int height)
        {
            for (var y = 2; y < height - 1; y += 2)
            {
                for (var x = 2; x < width - 1; x += 2)
                {
                    tiles[(y * width) + x] = TileType.Solid;
                }
            }
        }

        /// <summary>
        /// Loose pillars, never placed next to another.
        /// </summary>
        /// <remarks>
        /// The adjacency rule is what keeps this style from producing accidental walls. Two
        /// neighbouring random pillars start closing corridors, and four close a room.
        /// </remarks>
        private static void BuildScattered(
            TileType[] tiles, int width, int height, ref DeterministicRandom random)
        {
            for (var y = 2; y < height - 2; y++)
            {
                for (var x = 2; x < width - 2; x++)
                {
                    if (!random.Chance(14) || HasSolidNeighbour(tiles, width, x, y))
                    {
                        continue;
                    }

                    tiles[(y * width) + x] = TileType.Solid;
                }
            }
        }

        /// <summary>
        /// Long walls with doorways cut through them, producing rooms and sightlines.
        /// </summary>
        /// <remarks>
        /// Every wall gets at least two openings. One opening makes a room whose only exit can be
        /// blocked by a single bomb, which is a trap rather than a space.
        /// </remarks>
        private static void BuildChambers(
            TileType[] tiles, int width, int height, ref DeterministicRandom random)
        {
            for (var x = 3; x < width - 3; x += 4)
            {
                for (var y = 1; y < height - 1; y++)
                {
                    tiles[(y * width) + x] = TileType.Solid;
                }

                var doors = 2 + random.NextInt(2);
                for (var i = 0; i < doors; i++)
                {
                    var y = 1 + random.NextInt(height - 2);
                    tiles[(y * width) + x] = TileType.Empty;
                }
            }
        }

        private static bool HasSolidNeighbour(TileType[] tiles, int width, int x, int y)
        {
            for (var i = 0; i < Directions.Cardinals.Length; i++)
            {
                var offset = Directions.Cardinals[i].ToOffset();
                if (tiles[((y + offset.Y) * width) + x + offset.X] == TileType.Solid)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Clears a block of ground around spawn so the first bomb can always be escaped.</summary>
        private static void ClearSafePocket(
            TileType[] tiles, int width, int height, GridCoord spawn, int radius)
        {
            for (var y = spawn.Y - radius; y <= spawn.Y + radius; y++)
            {
                for (var x = spawn.X - radius; x <= spawn.X + radius; x++)
                {
                    if (x <= 0 || y <= 0 || x >= width - 1 || y >= height - 1)
                    {
                        continue;
                    }

                    tiles[(y * width) + x] = TileType.Empty;
                }
            }
        }

        /// <summary>
        /// Walks the board from spawn and turns anything it cannot reach into solid rock.
        /// </summary>
        /// <remarks>
        /// Sealing rather than carving, deliberately. Carving a corridor to an isolated pocket
        /// produces a dead-end nobody asked for; making it part of the wall simply means the arena
        /// is a little smaller than the rectangle it was rolled in, which nobody can tell.
        /// </remarks>
        /// <returns>Which tiles are reachable, indexed the same way as the board.</returns>
        private static bool[] SealUnreachableGround(
            TileType[] tiles, int width, int height, GridCoord spawn)
        {
            var reachable = new bool[width * height];
            var queue = new int[width * height];
            var count = 0;
            var head = 0;

            var start = (spawn.Y * width) + spawn.X;
            reachable[start] = true;
            queue[count++] = start;

            while (head < count)
            {
                var index = queue[head++];
                var x = index % width;
                var y = index / width;

                for (var i = 0; i < Directions.Cardinals.Length; i++)
                {
                    var offset = Directions.Cardinals[i].ToOffset();
                    var nextX = x + offset.X;
                    var nextY = y + offset.Y;

                    if (nextX < 0 || nextY < 0 || nextX >= width || nextY >= height)
                    {
                        continue;
                    }

                    var next = (nextY * width) + nextX;

                    // Destructible blocks are walked through: they are an obstacle in time, not in
                    // space, so ground behind one is reachable as soon as a bomb says so.
                    if (reachable[next] || tiles[next] == TileType.Solid)
                    {
                        continue;
                    }

                    reachable[next] = true;
                    queue[count++] = next;
                }
            }

            for (var i = 0; i < tiles.Length; i++)
            {
                if (!reachable[i])
                {
                    tiles[i] = TileType.Solid;
                }
            }

            return reachable;
        }

        private static void ScatterDestructibles(
            TileType[] tiles,
            int width,
            int height,
            GridCoord spawn,
            in ArenaSettings settings,
            bool[] reachable,
            ref DeterministicRandom random)
        {
            var pocket = settings.SafePocketRadius;

            for (var y = 1; y < height - 1; y++)
            {
                for (var x = 1; x < width - 1; x++)
                {
                    var index = (y * width) + x;

                    if (!reachable[index] || tiles[index] != TileType.Empty)
                    {
                        continue;
                    }

                    // The pocket stays open, or the guaranteed escape route is filled back in.
                    if (IntMath.Abs(x - spawn.X) <= pocket && IntMath.Abs(y - spawn.Y) <= pocket)
                    {
                        continue;
                    }

                    if (random.Chance(settings.DestructiblePercent))
                    {
                        tiles[index] = TileType.Destructible;
                    }
                }
            }
        }

        /// <summary>Places enemies on open ground, far enough away to be seen before they arrive.</summary>
        private static GridCoord[] PlaceEnemies(
            TileType[] tiles,
            int width,
            int height,
            GridCoord spawn,
            int arenaIndex,
            in ArenaSettings settings,
            bool[] reachable,
            ref DeterministicRandom random)
        {
            var wanted = settings.BaseEnemies + (settings.EnemiesPerArena * arenaIndex);

            if (wanted > settings.MaxEnemies)
            {
                wanted = settings.MaxEnemies;
            }

            var candidates = new GridCoord[width * height];
            var available = 0;

            for (var y = 1; y < height - 1; y++)
            {
                for (var x = 1; x < width - 1; x++)
                {
                    var index = (y * width) + x;

                    if (!reachable[index] || tiles[index] != TileType.Empty)
                    {
                        continue;
                    }

                    var tile = new GridCoord(x, y);

                    if (tile.ManhattanDistanceTo(spawn) < settings.MinEnemyDistance)
                    {
                        continue;
                    }

                    candidates[available++] = tile;
                }
            }

            if (available == 0)
            {
                return Array.Empty<GridCoord>();
            }

            // Partial shuffle: only as many as are needed are drawn, and never the same tile twice.
            var placed = wanted < available ? wanted : available;

            for (var i = 0; i < placed; i++)
            {
                var pick = i + random.NextInt(available - i);
                (candidates[i], candidates[pick]) = (candidates[pick], candidates[i]);

                FreeOneNeighbour(tiles, width, height, candidates[i]);
            }

            var result = new GridCoord[placed];
            Array.Copy(candidates, result, placed);

            return result;
        }

        /// <summary>
        /// Makes sure an enemy has somewhere to go.
        /// </summary>
        /// <remarks>
        /// An enemy walled in by destructibles cannot move until the player digs it out, which turns
        /// clearing the arena into excavation. One guaranteed opening keeps it a fight.
        /// </remarks>
        private static void FreeOneNeighbour(TileType[] tiles, int width, int height, GridCoord tile)
        {
            var blocked = -1;

            for (var i = 0; i < Directions.Cardinals.Length; i++)
            {
                var offset = Directions.Cardinals[i].ToOffset();
                var x = tile.X + offset.X;
                var y = tile.Y + offset.Y;

                if (x <= 0 || y <= 0 || x >= width - 1 || y >= height - 1)
                {
                    continue;
                }

                var index = (y * width) + x;

                if (tiles[index] == TileType.Empty)
                {
                    return;
                }

                if (tiles[index] == TileType.Destructible && blocked < 0)
                {
                    blocked = index;
                }
            }

            if (blocked >= 0)
            {
                tiles[blocked] = TileType.Empty;
            }
        }
    }
}
