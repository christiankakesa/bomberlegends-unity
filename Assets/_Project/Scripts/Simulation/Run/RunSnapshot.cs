using System;
using BomberLegends.Simulation.Items;

namespace BomberLegends.Simulation.Run
{
    /// <summary>
    /// Everything needed to put a run back exactly where it was.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Four values, because a run really is only four values: the seed, how far through it is, the
    /// build assembled so far, and the health left. Arenas are generated deterministically from the
    /// seed and the index, so this reconstructs the board rather than storing it.
    /// </para>
    /// <para>
    /// It deliberately captures nothing from inside an arena — no bomb positions, no enemy
    /// positions, no player position. Resuming therefore restarts the current arena rather than the
    /// exact moment. That is the honest trade: serialising live simulation state would tie the save
    /// format to every future gameplay change, so a run in progress would break on every update.
    /// </para>
    /// </remarks>
    [Serializable]
    public readonly struct RunSnapshot
    {
        /// <summary>Creates a snapshot.</summary>
        public RunSnapshot(
            uint seed, int arenaIndex, int carriedHealth, ItemId[]? held, uint offerState = 0u)
        {
            Seed = seed;
            ArenaIndex = arenaIndex;
            CarriedHealth = carriedHealth;
            Held = held ?? Array.Empty<ItemId>();
            OfferState = offerState;
        }

        /// <summary>The seed the whole run was rolled from.</summary>
        public uint Seed { get; }

        /// <summary>Which arena was being fought, counting from zero.</summary>
        public int ArenaIndex { get; }

        /// <summary>Health the player had on entering it.</summary>
        public int CarriedHealth { get; }

        /// <summary>Items held, in the order they were taken.</summary>
        public ItemId[] Held { get; }

        /// <summary>
        /// Where the offer generator had reached.
        /// </summary>
        /// <remarks>
        /// Stored rather than replayed. Rebuilding the offers that led here consumes a different
        /// number of draws, because how many candidates each one shuffled depended on how many items
        /// were held <i>at the time</i> — so replaying with the final build silently diverges. This
        /// was found by a test that expected a resumed run to continue the same sequence and got a
        /// different one.
        /// </remarks>
        public uint OfferState { get; }

        /// <summary>Whether this describes a run worth resuming.</summary>
        /// <remarks>
        /// A run on its first arena with nothing taken has no progress to lose, so restoring it
        /// would be indistinguishable from starting fresh — and would quietly pin every session to
        /// one seed.
        /// </remarks>
        public bool HasProgress => CarriedHealth > 0 && (ArenaIndex > 0 || Held.Length > 0);

        /// <summary>Nothing to resume.</summary>
        public static RunSnapshot None => new RunSnapshot(0u, 0, 0, null);

        /// <summary>Four values plus a generator position: the whole of a run.</summary>
        public override string ToString() =>
            $"arena {ArenaIndex + 1}, {Held.Length} items, {CarriedHealth} health";
    }
}
