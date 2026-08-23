using BomberLegends.Core;
using BomberLegends.Simulation.Board;
using NUnit.Framework;

namespace BomberLegends.Tests.EditMode.Simulation
{
    /// <summary>
    /// Covers procedurally generated arenas.
    /// </summary>
    /// <remarks>
    /// Almost everything here is asserted across many seeds. A generator that produces a good board
    /// most of the time is not a working generator: the one run that walls a player in is the run
    /// they remember, and it will never be the seed a single-seed test happened to pick.
    /// </remarks>
    public sealed class ArenaGeneratorTests
    {
        private const int Seeds = 120;

        private static LevelLayout Generate(uint seed, int arenaIndex = 0, ArenaSettings? settings = null)
        {
            var random = new DeterministicRandom(seed);
            return ArenaGenerator.Generate(arenaIndex, settings ?? ArenaSettings.Default, ref random);
        }

        /// <summary>Every tile a player could ever stand on, found by walking from spawn.</summary>
        private static bool[] Reachable(in LevelLayout layout)
        {
            var board = layout.CreateBoard();
            var seen = new bool[board.Width * board.Height];
            var queue = new System.Collections.Generic.Queue<GridCoord>();

            seen[layout.PlayerSpawn.ToIndex(board.Width)] = true;
            queue.Enqueue(layout.PlayerSpawn);

            while (queue.Count > 0)
            {
                var tile = queue.Dequeue();

                for (var i = 0; i < Directions.Cardinals.Length; i++)
                {
                    var next = tile.Neighbour(Directions.Cardinals[i]);

                    if (!board.Contains(next) || board[next] == TileType.Solid)
                    {
                        continue;
                    }

                    var index = next.ToIndex(board.Width);
                    if (seen[index])
                    {
                        continue;
                    }

                    seen[index] = true;
                    queue.Enqueue(next);
                }
            }

            return seen;
        }

        // ---------- the guarantees ----------

        [Test]
        public void NoGroundIsEverUnreachable()
        {
            // The failure that ruins a run: ground the player can see and never stand on.
            for (var seed = 1u; seed <= Seeds; seed++)
            {
                var layout = Generate(seed);
                var board = layout.CreateBoard();
                var reachable = Reachable(layout);

                for (var y = 0; y < board.Height; y++)
                {
                    for (var x = 0; x < board.Width; x++)
                    {
                        var tile = new GridCoord(x, y);

                        if (board[tile] == TileType.Solid)
                        {
                            continue;
                        }

                        Assert.That(reachable[tile.ToIndex(board.Width)], Is.True,
                            $"seed {seed} left {tile} stranded");
                    }
                }
            }
        }

        [Test]
        public void SpawnAlwaysHasRoomToPlaceABombAndLeave()
        {
            // A spawn with one exit is a death sentence dressed up as a layout: the first bomb
            // placed has nowhere to run to.
            for (var seed = 1u; seed <= Seeds; seed++)
            {
                var layout = Generate(seed);
                var board = layout.CreateBoard();

                var exits = 0;
                for (var i = 0; i < Directions.Cardinals.Length; i++)
                {
                    if (board.IsWalkable(layout.PlayerSpawn.Neighbour(Directions.Cardinals[i])))
                    {
                        exits++;
                    }
                }

                Assert.That(exits, Is.GreaterThanOrEqualTo(2), $"seed {seed} boxed the spawn in");
                Assert.That(board[layout.PlayerSpawn], Is.EqualTo(TileType.Empty));
            }
        }

        [Test]
        public void TheSafePocketIsNeverFilledWithBlocks()
        {
            var settings = ArenaSettings.Default;

            for (var seed = 1u; seed <= Seeds; seed++)
            {
                var layout = Generate(seed);
                var board = layout.CreateBoard();
                var spawn = layout.PlayerSpawn;

                for (var y = spawn.Y - settings.SafePocketRadius; y <= spawn.Y + settings.SafePocketRadius; y++)
                {
                    for (var x = spawn.X - settings.SafePocketRadius; x <= spawn.X + settings.SafePocketRadius; x++)
                    {
                        var tile = new GridCoord(x, y);

                        if (!board.Contains(tile) || x == 0 || y == 0 ||
                            x == board.Width - 1 || y == board.Height - 1)
                        {
                            continue;
                        }

                        Assert.That(board[tile], Is.EqualTo(TileType.Empty),
                            $"seed {seed} put {board[tile]} at {tile}, inside the safe pocket");
                    }
                }
            }
        }

        [Test]
        public void EveryArenaIsSealedByAWall()
        {
            for (var seed = 1u; seed <= Seeds; seed++)
            {
                var layout = Generate(seed);
                var board = layout.CreateBoard();

                for (var x = 0; x < board.Width; x++)
                {
                    Assert.That(board[new GridCoord(x, 0)], Is.EqualTo(TileType.Solid));
                    Assert.That(board[new GridCoord(x, board.Height - 1)], Is.EqualTo(TileType.Solid));
                }

                for (var y = 0; y < board.Height; y++)
                {
                    Assert.That(board[new GridCoord(0, y)], Is.EqualTo(TileType.Solid));
                    Assert.That(board[new GridCoord(board.Width - 1, y)], Is.EqualTo(TileType.Solid));
                }
            }
        }

        // ---------- enemies ----------

        [Test]
        public void EveryArenaIsClearable()
        {
            // No enemies means no clear condition, so the run would stop dead on that arena.
            for (var seed = 1u; seed <= Seeds; seed++)
            {
                Assert.That(Generate(seed).EnemySpawns.Length, Is.GreaterThan(0), $"seed {seed}");
            }
        }

        [Test]
        public void EnemiesNeverStartOnTopOfThePlayer()
        {
            var settings = ArenaSettings.Default;

            for (var seed = 1u; seed <= Seeds; seed++)
            {
                var layout = Generate(seed);

                foreach (var enemy in layout.EnemySpawns.ToArray())
                {
                    Assert.That(enemy.ManhattanDistanceTo(layout.PlayerSpawn),
                        Is.GreaterThanOrEqualTo(settings.MinEnemyDistance),
                        $"seed {seed} spawned an enemy on the player's doorstep");
                }
            }
        }

        [Test]
        public void EveryEnemyCanMove()
        {
            // An enemy sealed in by destructibles turns clearing the arena into excavation.
            for (var seed = 1u; seed <= Seeds; seed++)
            {
                var layout = Generate(seed);
                var board = layout.CreateBoard();

                foreach (var enemy in layout.EnemySpawns.ToArray())
                {
                    var open = 0;
                    for (var i = 0; i < Directions.Cardinals.Length; i++)
                    {
                        if (board.IsWalkable(enemy.Neighbour(Directions.Cardinals[i])))
                        {
                            open++;
                        }
                    }

                    Assert.That(open, Is.GreaterThan(0), $"seed {seed} walled in the enemy at {enemy}");
                }
            }
        }

        [Test]
        public void EnemiesShareNoTile()
        {
            for (var seed = 1u; seed <= Seeds; seed++)
            {
                var spawns = Generate(seed).EnemySpawns.ToArray();

                for (var i = 0; i < spawns.Length; i++)
                {
                    for (var j = i + 1; j < spawns.Length; j++)
                    {
                        Assert.That(spawns[i], Is.Not.EqualTo(spawns[j]), $"seed {seed}");
                    }
                }
            }
        }

        // ---------- shape and progression ----------

        [Test]
        public void ArenasAreAlwaysOddSized()
        {
            // An even dimension leaves a doubled row against one wall, which reads as a mistake.
            for (var seed = 1u; seed <= 20u; seed++)
            {
                for (var arena = 0; arena < 8; arena++)
                {
                    var layout = Generate(seed, arena);

                    Assert.That(layout.Width % 2, Is.EqualTo(1));
                    Assert.That(layout.Height % 2, Is.EqualTo(1));
                }
            }
        }

        [Test]
        public void ArenasGrowAndFillUpAsTheRunGoesOn()
        {
            var first = Generate(9u, arenaIndex: 0);
            var later = Generate(9u, arenaIndex: 5);

            Assert.That(later.Width, Is.GreaterThanOrEqualTo(first.Width));
            Assert.That(later.EnemySpawns.Length, Is.GreaterThan(first.EnemySpawns.Length));
        }

        [Test]
        public void GrowthIsCapped()
        {
            var settings = ArenaSettings.Default;
            var layout = Generate(3u, arenaIndex: 500);

            Assert.That(layout.Width, Is.LessThanOrEqualTo(settings.MaxWidth));
            Assert.That(layout.Height, Is.LessThanOrEqualTo(settings.MaxHeight));
            Assert.That(layout.EnemySpawns.Length, Is.LessThanOrEqualTo(settings.MaxEnemies));
        }

        [Test]
        public void DifferentSeedsProduceDifferentArenas()
        {
            // Otherwise the whole exercise bought nothing.
            var shapes = new System.Collections.Generic.HashSet<string>();

            for (var seed = 1u; seed <= 30u; seed++)
            {
                var layout = Generate(seed);
                var board = layout.CreateBoard();
                shapes.Add(board.ComputeHash().ToString());
            }

            Assert.That(shapes.Count, Is.GreaterThan(25),
                "thirty seeds should not collapse into a handful of boards");
        }

        [Test]
        public void TheSameSeedAlwaysProducesTheSameArena()
        {
            for (var seed = 1u; seed <= 20u; seed++)
            {
                var first = Generate(seed, arenaIndex: 3).CreateBoard();
                var second = Generate(seed, arenaIndex: 3).CreateBoard();

                Assert.That(second.ComputeHash(), Is.EqualTo(first.ComputeHash()));
            }
        }

        [Test]
        public void ThereIsSomethingToBlowUp()
        {
            // Destructibles are the resource the bomb exists to spend itself on. A board without
            // them is a corridor crawl.
            for (var seed = 1u; seed <= Seeds; seed++)
            {
                var layout = Generate(seed);
                var board = layout.CreateBoard();
                var destructibles = 0;

                for (var y = 0; y < board.Height; y++)
                {
                    for (var x = 0; x < board.Width; x++)
                    {
                        if (board[new GridCoord(x, y)] == TileType.Destructible)
                        {
                            destructibles++;
                        }
                    }
                }

                Assert.That(destructibles, Is.GreaterThan(10), $"seed {seed} generated a bare board");
            }
        }

        // ---------- how the blocks are distributed ----------

        [Test]
        public void ClusteringChangesWhereTheBlocksGoAndNotHowManyThereAre()
        {
            // The whole point of clustering is that it is not a difficulty change. Blocks in runs
            // leave the floor between them open, which is what gives a cornered enemy somewhere to
            // run — but if the board also quietly held fewer blocks, the improvement would just be
            // a density cut wearing a disguise, and lowering the density is a separate decision
            // nobody has taken.
            var scattered = 0;
            var clustered = 0;

            for (var seed = 1u; seed <= Seeds; seed++)
            {
                scattered += CountBlocks(Generate(seed, settings: WithCluster(1)));
                clustered += CountBlocks(Generate(seed, settings: WithCluster(3)));
            }

            // Not equality: a run stops at a dead end, so the last one placed can come up short.
            var drift = System.Math.Abs(scattered - clustered) * 100 / scattered;

            Assert.That(drift, Is.LessThanOrEqualTo(3),
                $"clustered boards hold {clustered} blocks against {scattered} scattered — that is " +
                "a density change, not a distribution change");
        }

        [Test]
        public void BlocksActuallyEndUpNextToEachOther()
        {
            // Guards the mechanism rather than its effect. The sealed-placement measurement in
            // EnemyThreatTests says the fix works; this says *why* it works, so a refactor that
            // quietly returns to rolling each tile on its own fails here first, with a clear reason.
            //
            // The lone block is the right thing to count. Total adjacency barely moves — at 55%
            // fill, scattered blocks already touch by accident — but a block placed as part of a
            // run has a neighbour by construction, so the only lone blocks left are runs that died
            // at their seed. Measured across these seeds: 14% of blocks stand alone when scattered,
            // and none do when clustered.
            var scattered = LoneBlockPercent(WithCluster(1));
            var clustered = LoneBlockPercent(WithCluster(3));

            Assert.That(scattered, Is.GreaterThanOrEqualTo(10),
                $"only {scattered}% of scattered blocks stand alone, so this comparison no longer " +
                "has a control and the clustered number below proves nothing");

            Assert.That(clustered, Is.LessThanOrEqualTo(2),
                $"{clustered}% of blocks stand alone against {scattered}% scattered; the runs are " +
                "not forming");
        }

        /// <summary>What share of destructible blocks have no destructible neighbour.</summary>
        private static int LoneBlockPercent(ArenaSettings settings)
        {
            var blocks = 0;
            var lone = 0;

            for (var seed = 1u; seed <= Seeds; seed++)
            {
                var board = Generate(seed, settings: settings).CreateBoard();

                for (var y = 0; y < board.Height; y++)
                {
                    for (var x = 0; x < board.Width; x++)
                    {
                        var tile = new GridCoord(x, y);

                        if (board[tile] != TileType.Destructible)
                        {
                            continue;
                        }

                        blocks++;

                        if (!HasBlockNeighbour(board, tile))
                        {
                            lone++;
                        }
                    }
                }
            }

            return blocks == 0 ? 0 : lone * 100 / blocks;
        }

        private static bool HasBlockNeighbour(BoardState board, GridCoord tile)
        {
            for (var i = 0; i < Directions.Cardinals.Length; i++)
            {
                var neighbour = tile.Neighbour(Directions.Cardinals[i]);

                if (board.Contains(neighbour) && board[neighbour] == TileType.Destructible)
                {
                    return true;
                }
            }

            return false;
        }

        private static ArenaSettings WithCluster(int size) => new ArenaSettings(
            destructiblePercent: 55, blockClusterSize: size);

        private static int CountBlocks(in LevelLayout layout)
        {
            var board = layout.CreateBoard();
            var blocks = 0;

            for (var y = 0; y < board.Height; y++)
            {
                for (var x = 0; x < board.Width; x++)
                {
                    if (board[new GridCoord(x, y)] == TileType.Destructible)
                    {
                        blocks++;
                    }
                }
            }

            return blocks;
        }

        // ---------- settings ----------

        [Test]
        public void UnusableSettingsAreRefused()
        {
            Assert.Throws<System.ArgumentException>(
                () => new ArenaSettings(baseWidth: 5).Validate());
            Assert.Throws<System.ArgumentException>(
                () => new ArenaSettings(baseEnemies: 0).Validate());
            Assert.Throws<System.ArgumentException>(
                () => new ArenaSettings(maxEnemies: 999).Validate());
            Assert.Throws<System.ArgumentException>(
                () => new ArenaSettings(destructiblePercent: 140).Validate());
            Assert.Throws<System.ArgumentException>(
                () => new ArenaSettings(blockClusterSize: 0).Validate());
        }
    }
}
