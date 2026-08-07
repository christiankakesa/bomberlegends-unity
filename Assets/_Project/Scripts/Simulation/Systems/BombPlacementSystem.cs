using BomberLegends.Core;
using BomberLegends.Simulation.Events;

namespace BomberLegends.Simulation.Systems
{
    /// <summary>
    /// Turns a bomb button press into a bomb on the board.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Capacity is the classic model: a bomb comes back to the player when it detonates, so the rate
    /// they can be placed is the fuse length and the skill is chaining placements. The alternative —
    /// a cooldown that starts when the bomb is placed — leaves the player standing still waiting,
    /// which is why <see cref="SimulationConfig.BombCooldownTicks"/> defaults to zero. It exists so
    /// the other model can be tried on a device rather than argued about.
    /// </para>
    /// <para>
    /// Nothing here needs to let the player step off the bomb they just placed. A bomb blocks
    /// movement <i>into</i> its tile, and the player is already standing on it, so leaving works and
    /// coming back does not — which is exactly the classic rule, for free.
    /// </para>
    /// </remarks>
    public static class BombPlacementSystem
    {
        /// <summary>Places a bomb if the player asked for one and is allowed one.</summary>
        public static void Tick(
            ref SimulationState state,
            in SimulationConfig config,
            in PlayerIntent intent,
            SimEventBuffer events)
        {
            ref var player = ref state.Player;

            if (player.BombCooldownTicksRemaining > 0)
            {
                player.BombCooldownTicksRemaining--;
            }

            if (!intent.Buttons.Has(IntentButtons.Bomb))
            {
                player.BombHeldLastTick = false;
                return;
            }

            // Placement is edge-triggered. Holding the button down should not empty the pool the
            // instant a bomb becomes available again.
            var pressedThisTick = !player.BombHeldLastTick;
            player.BombHeldLastTick = true;

            if (!pressedThisTick ||
                player.BombCooldownTicksRemaining > 0 ||
                player.ActiveBombs >= player.BombCapacity)
            {
                return;
            }

            var tile = player.Tile;

            if (state.BombGrid.HasBomb(tile) || !state.Board.IsWalkable(tile))
            {
                return;
            }

            var slot = state.Bombs.Place(tile, config.FuseTicks, player.BlastRange);
            if (slot < 0)
            {
                return;
            }

            state.BombGrid.Set(tile, slot);
            player.ActiveBombs++;
            player.BombCooldownTicksRemaining = config.BombCooldownTicks;

            events.Add(new SimEvent(SimEventType.BombPlaced, tile, slot, player.BlastRange));
        }
    }
}
