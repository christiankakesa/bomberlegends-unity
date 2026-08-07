using BomberLegends.Core;
using BomberLegends.Simulation.Board;
using BomberLegends.Simulation.Events;

namespace BomberLegends.Simulation.Systems
{
    /// <summary>
    /// Resolves explosions, including everything they set off in turn.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The heart of the game. A bomb throws four arms out from its tile; each arm runs until it hits
    /// permanent structure, destroys one destructible block and stops, or runs out of range. Any bomb
    /// an arm reaches goes off too.
    /// </para>
    /// <para>
    /// <b>Chains resolve within the same tick.</b> A row of bombs going off in sequence over several
    /// ticks would read as a stutter rather than a chain reaction, and the tactical value of laying a
    /// chain comes from it being one event. Resolution is an explicit queue rather than recursion:
    /// a long chain would otherwise grow the call stack in proportion to its length, and a ring of
    /// bombs would never terminate at all.
    /// </para>
    /// </remarks>
    public static class BlastSystem
    {
        /// <summary>
        /// Ages existing blasts and resolves every queued detonation, following chains.
        /// </summary>
        /// <param name="state">Simulation state.</param>
        /// <param name="config">Tuning.</param>
        /// <param name="queue">Detonation queue, pre-filled by the fuse system.</param>
        /// <param name="queuedCount">How many entries the fuse system wrote.</param>
        /// <param name="events">Where effects are announced.</param>
        public static void Tick(
            ref SimulationState state,
            in SimulationConfig config,
            int[] queue,
            int queuedCount,
            SimEventBuffer events)
        {
            // Existing blasts age before new ones are painted, so a tile lit this tick keeps its
            // full duration rather than losing a tick to the same pass that created it.
            state.BlastGrid.Decay();

            var head = 0;
            var tail = queuedCount;

            while (head < tail)
            {
                var slot = queue[head++];
                var bomb = state.Bombs[slot];

                if (!bomb.IsActive)
                {
                    continue;
                }

                state.Bombs.Remove(slot);
                state.BombGrid.Clear(bomb.Tile);

                if (state.Player.ActiveBombs > 0)
                {
                    state.Player.ActiveBombs--;
                }

                events.Add(new SimEvent(SimEventType.BombDetonated, bomb.Tile, slot, bomb.Range));
                Ignite(ref state, config, bomb.Tile, events);

                var cardinals = Directions.Cardinals;
                for (var d = 0; d < cardinals.Length; d++)
                {
                    Propagate(ref state, config, bomb, cardinals[d], queue, ref tail, events);
                }
            }
        }

        /// <summary>Throws one arm of a blast outwards from its bomb.</summary>
        private static void Propagate(
            ref SimulationState state,
            in SimulationConfig config,
            in Bombs.BombState bomb,
            Direction direction,
            int[] queue,
            ref int tail,
            SimEventBuffer events)
        {
            for (var distance = 1; distance <= bomb.Range; distance++)
            {
                var tile = bomb.Tile.Step(direction, distance);
                var type = state.Board[tile];

                // Outside the board reads as solid, so this also stops the arm at the edge.
                if (type == TileType.Solid)
                {
                    return;
                }

                if (type == TileType.Destructible)
                {
                    state.Board[tile] = TileType.Empty;
                    events.Add(new SimEvent(SimEventType.BlockDestroyed, tile));
                    Ignite(ref state, config, tile, events);

                    // A destroyed block absorbs the arm: the blast does not continue past it.
                    return;
                }

                Ignite(ref state, config, tile, events);

                var other = state.BombGrid.SlotAt(tile);
                if (other < 0)
                {
                    continue;
                }

                var chained = state.Bombs[other];
                if (!chained.IsActive || chained.IsQueued || tail >= queue.Length)
                {
                    continue;
                }

                chained.IsQueued = true;
                state.Bombs[other] = chained;
                queue[tail++] = other;
            }
        }

        /// <summary>Sets a tile alight, announcing it only the first time it catches.</summary>
        private static void Ignite(
            ref SimulationState state,
            in SimulationConfig config,
            GridCoord tile,
            SimEventBuffer events)
        {
            if (state.BlastGrid.Ignite(tile, config.BlastLingerTicks))
            {
                events.Add(new SimEvent(SimEventType.BlastSpawned, tile));
            }
        }
    }
}
