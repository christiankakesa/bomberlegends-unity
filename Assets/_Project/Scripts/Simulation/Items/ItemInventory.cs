using System;

namespace BomberLegends.Simulation.Items
{
    /// <summary>
    /// The passive items a player is carrying.
    /// </summary>
    /// <remarks>
    /// Small and fixed, because scarcity is the design. Items permanently rewrite the loadout when
    /// they are taken, so this list is not consulted during a tick — it exists so the player can be
    /// shown what they built, and so a run can be saved and resumed.
    /// </remarks>
    public struct ItemInventory
    {
        private readonly ItemId[] _items;

        private ItemInventory(ItemId[] items) => _items = items;

        /// <summary>How many items can be carried.</summary>
        public readonly int Capacity => _items?.Length ?? 0;

        /// <summary>Reads a slot.</summary>
        public readonly ItemId this[int index] => _items[index];

        /// <summary>Whether this inventory has been created.</summary>
        public readonly bool IsCreated => _items != null;

        /// <summary>How many items are held.</summary>
        public readonly int Count
        {
            get
            {
                if (_items == null)
                {
                    return 0;
                }

                var count = 0;
                for (var i = 0; i < _items.Length; i++)
                {
                    if (_items[i] != ItemId.None)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>Whether every slot is filled.</summary>
        public readonly bool IsFull => Count >= Capacity;

        /// <summary>Creates an empty inventory.</summary>
        /// <exception cref="ArgumentOutOfRangeException">The capacity is not positive.</exception>
        public static ItemInventory WithSlots(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(capacity), capacity, "A player must have at least one item slot.");
            }

            return new ItemInventory(new ItemId[capacity]);
        }

        /// <summary>Whether the given item is already held.</summary>
        public readonly bool Contains(ItemId id)
        {
            if (_items == null || id == ItemId.None)
            {
                return false;
            }

            for (var i = 0; i < _items.Length; i++)
            {
                if (_items[i] == id)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Puts an item in the first free slot, returning its index or <c>-1</c> if it would not fit.
        /// </summary>
        /// <remarks>
        /// Duplicates are refused. Taking the same item twice would stack its percentages while
        /// adding nothing to the build, which is the shape of upgrade that makes a choice screen
        /// feel like arithmetic rather than a decision.
        /// </remarks>
        public int TryAdd(ItemId id)
        {
            if (_items == null || id == ItemId.None || Contains(id))
            {
                return -1;
            }

            for (var i = 0; i < _items.Length; i++)
            {
                if (_items[i] == ItemId.None)
                {
                    _items[i] = id;
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Swaps a held item for another, returning whether it happened.
        /// </summary>
        /// <remarks>
        /// Replacement exists so a run keeps presenting decisions once every slot is full. Late in a
        /// run the interesting question stops being "what do I want?" and becomes "what am I willing
        /// to give up?", which is a better question and costs nothing extra to ask.
        /// </remarks>
        public bool TryReplace(ItemId discard, ItemId take)
        {
            if (_items == null || discard == ItemId.None || take == ItemId.None ||
                discard == take || Contains(take))
            {
                return false;
            }

            for (var i = 0; i < _items.Length; i++)
            {
                if (_items[i] == discard)
                {
                    _items[i] = take;
                    return true;
                }
            }

            return false;
        }
    }
}
