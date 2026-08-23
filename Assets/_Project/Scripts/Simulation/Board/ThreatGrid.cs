using System;
using BomberLegends.Core;

namespace BomberLegends.Simulation.Board
{
    /// <summary>
    /// How far each tile is from somewhere a blast will not reach.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Enemies had no idea bombs existed. They chose whichever open direction closed the distance
    /// and walked into explosions, which is why playtesters reported roughly four kills in five
    /// coming from bombs while the aimed shot felt pointless: the bomb was not winning fights, it
    /// was winning them <i>without the player having to execute the play</i>. This grid is what
    /// gives them something to be afraid of.
    /// </para>
    /// <para>
    /// It stores a distance field rather than a set of dangerous tiles. Knowing a tile is dangerous
    /// tells an enemy to leave; it does not tell it <b>which way out</b>, and a greedy guess
    /// dithers at exactly the moment the player is watching. One breadth-first sweep outward from
    /// every safe tile answers both questions at once: zero means safe, and any other value is the
    /// number of steps to the nearest tile the fire will not reach. Walking down that number is
    /// always the shortest way out, however many bombs overlap.
    /// </para>
    /// <para>
    /// Derived state, rebuilt from the board, the bombs and the fire every tick by
    /// <see cref="Systems.ThreatSystem"/>. Nothing here is authoritative and nothing here is
    /// hashed — two simulations that agree on bombs necessarily agree on this.
    /// </para>
    /// </remarks>
    public struct ThreatGrid
    {
        /// <summary>The value of a tile no blast will reach.</summary>
        public const int Safe = 0;

        /// <summary>The value of a threatened tile with no way out at all.</summary>
        /// <remarks>
        /// Deliberately the worst possible score rather than a special case: an enemy that compares
        /// escape distances will rank a trapped tile below every other option and keep moving,
        /// which is what a cornered enemy should do. It is the player's reward for cutting off the
        /// exit, and it must not read as a freeze.
        /// </remarks>
        public const int Trapped = int.MaxValue;

        private readonly int[] _escapeSteps;

        // The breadth-first frontier. Every tile enters it at most once, so the board's own size is
        // an exact bound and the sweep never allocates.
        private readonly int[] _queue;

        /// <summary>Creates a grid matching a board of the given size.</summary>
        /// <exception cref="ArgumentOutOfRangeException">Either dimension is not positive.</exception>
        public ThreatGrid(int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "Grid dimensions must be positive.");
            }

            Width = width;
            Height = height;
            _escapeSteps = new int[width * height];
            _queue = new int[width * height];
        }

        /// <summary>Tiles across.</summary>
        public int Width { get; }

        /// <summary>Tiles up.</summary>
        public int Height { get; }

        /// <summary>Whether a blast is going to reach this tile. Tiles off the board never are.</summary>
        public readonly bool IsThreatened(GridCoord tile) => EscapeStepsFrom(tile) != Safe;

        /// <summary>
        /// Steps from this tile to the nearest one no blast will reach, or <see cref="Safe"/> when
        /// it is already clear.
        /// </summary>
        /// <remarks>
        /// Off the board reads as safe, which no actor can ever act on: everything outside the
        /// board is solid, so nothing can stand there or step there.
        /// </remarks>
        public readonly int EscapeStepsFrom(GridCoord tile) =>
            tile.IsInside(Width, Height) ? _escapeSteps[tile.ToIndex(Width)] : Safe;

        /// <summary>
        /// Marks a tile as one a blast will reach, and reports whether it was previously clear.
        /// </summary>
        /// <remarks>
        /// Marking is idempotent, and the return value is what lets chain reactions be followed to
        /// a fixed point without a second bookkeeping array — the same trick
        /// <see cref="BlastGrid.Ignite"/> uses to raise one effect per new blast tile.
        /// </remarks>
        public bool Mark(GridCoord tile)
        {
            if (!tile.IsInside(Width, Height))
            {
                return false;
            }

            var index = tile.ToIndex(Width);
            if (_escapeSteps[index] != Safe)
            {
                return false;
            }

            _escapeSteps[index] = Trapped;
            return true;
        }

        /// <summary>
        /// Works out how far every marked tile is from safety.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A breadth-first sweep seeded with every clear tile that borders a marked one, expanding
        /// only into marked tiles. The work is therefore proportional to the size of the fire, not
        /// to the size of the board, which matters because the late arenas are 651 tiles and this
        /// runs thirty times a second.
        /// </para>
        /// <para>
        /// Bomb tiles are impassable, exactly as they are for movement, so an escape route is never
        /// plotted through one. Anything still marked when the sweep finishes has no route out and
        /// keeps its <see cref="Trapped"/> score.
        /// </para>
        /// </remarks>
        public void Solve(in BoardState board, in BombGrid bombs)
        {
            var head = 0;
            var tail = 0;

            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    var tile = new GridCoord(x, y);

                    if (_escapeSteps[tile.ToIndex(Width)] != Safe ||
                        !board.IsWalkable(tile) ||
                        bombs.HasBomb(tile))
                    {
                        continue;
                    }

                    if (BordersAThreat(tile))
                    {
                        _queue[tail++] = tile.ToIndex(Width);
                    }
                }
            }

            var cardinals = Directions.Cardinals;

            while (head < tail)
            {
                var index = _queue[head++];
                var tile = GridCoord.FromIndex(index, Width);
                var next = _escapeSteps[index] + 1;

                for (var d = 0; d < cardinals.Length; d++)
                {
                    var step = tile.Neighbour(cardinals[d]);

                    if (!board.IsWalkable(step) || bombs.HasBomb(step))
                    {
                        continue;
                    }

                    var stepIndex = step.ToIndex(Width);

                    // Anything not still Trapped is either safe or already reached by a shorter
                    // route, and breadth-first means the first arrival is always the shortest.
                    if (_escapeSteps[stepIndex] != Trapped)
                    {
                        continue;
                    }

                    _escapeSteps[stepIndex] = next;
                    _queue[tail++] = stepIndex;
                }
            }
        }

        /// <summary>Clears every tile back to safe.</summary>
        public void ClearAll() => Array.Clear(_escapeSteps, 0, _escapeSteps.Length);

        private readonly bool BordersAThreat(GridCoord tile)
        {
            var cardinals = Directions.Cardinals;

            for (var d = 0; d < cardinals.Length; d++)
            {
                var neighbour = tile.Neighbour(cardinals[d]);

                if (neighbour.IsInside(Width, Height) &&
                    _escapeSteps[neighbour.ToIndex(Width)] == Trapped)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
