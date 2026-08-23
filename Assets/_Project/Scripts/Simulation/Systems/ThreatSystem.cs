using BomberLegends.Core;
using BomberLegends.Simulation.Board;
using BomberLegends.Simulation.Bombs;

namespace BomberLegends.Simulation.Systems
{
    /// <summary>
    /// Works out which tiles are about to be lethal, so enemies can act on it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Runs after the blast has resolved and before the enemies move, so what they read is a
    /// finished picture of the fire on the board and the fuses still burning above it.
    /// </para>
    /// <para>
    /// <b>Only bombs close to going off count.</b> An enemy that feared a bomb the instant it was
    /// placed would never walk near one again, and bombs would stop killing anything that was not
    /// already cornered — which trades one broken extreme for the other. Fearing a bomb late gives
    /// the player the exchange the game is supposed to be about: the enemy comes, the bomb is laid,
    /// the enemy runs, and whether it lives depends on whether the way out was cut off.
    /// </para>
    /// <para>
    /// The projection follows <see cref="BlastSystem"/>'s arms exactly — same range, same stopping
    /// rules, same chain reactions — because an enemy that fears the wrong tiles is worse than one
    /// that fears nothing. That agreement is not enforced by sharing code, since the blast also
    /// destroys and ignites as it goes; it is enforced by a test that detonates a bomb and compares
    /// what was predicted against what actually burned.
    /// </para>
    /// </remarks>
    public static class ThreatSystem
    {
        /// <summary>Rebuilds the threat grid for this tick.</summary>
        public static void Tick(ref SimulationState state, in SimulationConfig config)
        {
            state.Threats.ClearAll();

            // Bombs first, then fire. Ordered that way so a bomb sitting in the embers of an older
            // explosion is not mistaken for one a chain is about to set off: only another bomb's
            // arm can do that, and lingering fire cannot.
            var marked = MarkBombs(ref state, config.EnemyBombFearTicks);

            marked |= MarkBurningTiles(ref state);

            if (marked)
            {
                state.Threats.Solve(state.Board, state.BombGrid);
            }
        }

        /// <summary>
        /// Paints every bomb that is going to go off soon, and everything those set off in turn.
        /// </summary>
        /// <remarks>
        /// Chains are followed by repeating the pass until nothing new is marked. A bomb standing
        /// on a tile another bomb's arm reaches will detonate with it, whatever its own fuse says,
        /// so the player who lays a chain gets enemies that respect the whole chain. Painting is
        /// idempotent and can only ever add tiles, so the repetition always terminates — in
        /// practice after two passes, and at worst after one per bomb on the board.
        /// </remarks>
        private static bool MarkBombs(ref SimulationState state, int fearTicks)
        {
            var marked = false;
            bool changed;

            do
            {
                changed = false;

                for (var slot = 0; slot < state.Bombs.Capacity; slot++)
                {
                    var bomb = state.Bombs[slot];

                    if (!bomb.IsActive)
                    {
                        continue;
                    }

                    if (bomb.FuseTicksRemaining > fearTicks && !state.Threats.IsThreatened(bomb.Tile))
                    {
                        continue;
                    }

                    changed |= Paint(ref state, bomb);
                }

                marked |= changed;
            }
            while (changed);

            return marked;
        }

        private static bool Paint(ref SimulationState state, in BombState bomb)
        {
            var marked = state.Threats.Mark(bomb.Tile);
            var cardinals = Directions.Cardinals;

            for (var d = 0; d < cardinals.Length; d++)
            {
                marked |= PaintArm(ref state, bomb, cardinals[d]);
            }

            return marked;
        }

        /// <summary>Throws one arm of a predicted blast outwards, by the rules the real one uses.</summary>
        private static bool PaintArm(
            ref SimulationState state, in BombState bomb, Direction direction)
        {
            var marked = false;

            for (var distance = 1; distance <= bomb.Range; distance++)
            {
                var tile = bomb.Tile.Step(direction, distance);
                var type = state.Board[tile];

                // Outside the board reads as solid, so this also stops the arm at the edge.
                if (type == TileType.Solid)
                {
                    return marked;
                }

                marked |= state.Threats.Mark(tile);

                if (type == TileType.Destructible)
                {
                    // A destroyed block absorbs the arm: the blast does not continue past it.
                    return marked;
                }
            }

            return marked;
        }

        /// <summary>
        /// Paints what is already alight.
        /// </summary>
        /// <remarks>
        /// Fire outlives the bomb that made it by a fraction of a second, and an enemy that walked
        /// into the embers would be handing back the free kill this whole system exists to remove.
        /// </remarks>
        private static bool MarkBurningTiles(ref SimulationState state)
        {
            var marked = false;

            for (var y = 0; y < state.Board.Height; y++)
            {
                for (var x = 0; x < state.Board.Width; x++)
                {
                    var tile = new GridCoord(x, y);

                    if (state.BlastGrid.IsLethal(tile))
                    {
                        marked |= state.Threats.Mark(tile);
                    }
                }
            }

            return marked;
        }
    }
}
