using BomberLegends.Simulation.Events;
using BomberLegends.Simulation.Items;
using BomberLegends.Simulation.Skills;

namespace BomberLegends.Simulation.Systems
{
    /// <summary>
    /// Grants items and folds their effects into the player's loadout.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Runs on acquisition, not every tick. Items are only ever added during a run, never removed,
    /// so recomputing effective stats each tick would cost work every frame to support a case that
    /// never happens — and would need a stable ordering rule to stay deterministic. Applying once
    /// and mutating the slot avoids both.
    /// </para>
    /// <para>
    /// The cost of that choice, recorded honestly: an item cannot be taken away, and the loadout no
    /// longer remembers its base values. If removal is ever needed, this becomes a recompute from
    /// base plus inventory — which is why the inventory is kept even though no tick reads it.
    /// </para>
    /// </remarks>
    public static class ItemSystem
    {
        /// <summary>
        /// Gives the player an item, returning whether it was taken.
        /// </summary>
        public static bool TryGrant(ref SimulationState state, ItemId id, SimEventBuffer? events = null)
        {
            if (id == ItemId.None || !state.Player.Items.IsCreated)
            {
                return false;
            }

            var slot = state.Player.Items.TryAdd(id);

            if (slot < 0)
            {
                return false;
            }

            Apply(ref state, id);

            events?.Add(new SimEvent(SimEventType.ItemAcquired, state.Player.Tile, slot, (int)id));

            return true;
        }

        /// <summary>Folds an item's effect into every skill it targets.</summary>
        private static void Apply(ref SimulationState state, ItemId id)
        {
            var effect = ItemCatalog.Effect(id);

            for (var index = 0; index < SkillLoadout.SlotCount; index++)
            {
                state.Player.Skills[index] = effect.ApplyTo(state.Player.Skills[index]);
            }
        }
    }
}
