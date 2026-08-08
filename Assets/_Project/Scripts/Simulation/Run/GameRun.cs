using System;
using BomberLegends.Core;
using BomberLegends.Simulation.Board;
using BomberLegends.Simulation.Items;

namespace BomberLegends.Simulation.Run
{
    /// <summary>
    /// A sequence of arenas, the build assembled along the way, and the death that ends it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Engine-free like everything else that decides anything, so the whole loop — clear, choose,
    /// carry forward, die, restart — is testable in milliseconds without a scene. The view layer
    /// watches <see cref="Phase"/> and rebuilds; it never decides what happens next.
    /// </para>
    /// <para>
    /// Each arena gets its own <see cref="GameSimulation"/>, seeded from the run seed and the arena
    /// index. Health and items are carried across by rebuilding them into the new simulation rather
    /// than by sharing state, which keeps a single arena exactly as testable in isolation as it was
    /// before runs existed.
    /// </para>
    /// </remarks>
    public sealed class GameRun
    {
        /// <summary>How many items are offered after clearing an arena.</summary>
        public const int OfferCount = 3;

        private readonly SimulationConfig _config;
        private readonly IArenaSource _arenas;
        private readonly ItemId[] _held;
        private readonly ItemId[] _offers = new ItemId[OfferCount];
        private readonly ItemId[] _startingItems;
        private uint _seed;

        private DeterministicRandom _random;
        private int _heldCount;
        private int _offerCount;
        private int _arenaIndex;
        private ItemId _pending;

        /// <summary>Starts a run on the first arena.</summary>
        /// <exception cref="ArgumentException">No arenas were supplied.</exception>
        /// <param name="startingItems">
        /// A build to begin every attempt with. A development aid for trying a specific pairing
        /// without playing up to it; it survives a restart, and occupies real slots so the run
        /// offers correspondingly fewer.
        /// </param>
        public GameRun(
            in SimulationConfig config,
            LevelLayout[] arenas,
            uint seed,
            ItemId[]? startingItems = null)
            : this(config, new AuthoredArenaSource(arenas), seed, startingItems)
        {
        }

        /// <summary>Starts a run drawing its arenas from the given source.</summary>
        /// <exception cref="ArgumentNullException">No source was supplied.</exception>
        public GameRun(
            in SimulationConfig config,
            IArenaSource arenas,
            uint seed,
            ItemId[]? startingItems = null)
        {
            if (arenas == null)
            {
                throw new ArgumentNullException(nameof(arenas));
            }

            config.Validate();

            _config = config;
            _arenas = arenas;
            _seed = seed;
            _held = new ItemId[config.ItemSlots];
            _startingItems = startingItems ?? Array.Empty<ItemId>();

            Restart();
        }

        /// <summary>The arena currently being fought, counting from one.</summary>
        public int ArenaNumber => _arenaIndex + 1;

        /// <summary>Where the run is in its lifecycle.</summary>
        public RunPhase Phase { get; private set; }

        /// <summary>The simulation for the current arena. Never null.</summary>
        public GameSimulation Current { get; private set; } = null!;

        /// <summary>Items on offer while <see cref="Phase"/> is <see cref="RunPhase.Choosing"/>.</summary>
        public ReadOnlySpan<ItemId> Offers => _offers.AsSpan(0, _offerCount);

        /// <summary>Items taken so far, in the order they were taken.</summary>
        public ReadOnlySpan<ItemId> Held => _held.AsSpan(0, _heldCount);

        /// <summary>
        /// The item waiting to be taken while <see cref="Phase"/> is
        /// <see cref="RunPhase.Discarding"/>.
        /// </summary>
        public ItemId Pending => _pending;

        /// <summary>
        /// Captures the run so it can be resumed in another session.
        /// </summary>
        public RunSnapshot CreateSnapshot() => new RunSnapshot(
            _seed,
            _arenaIndex,
            Current.State.Player.Health.Current,
            Held.ToArray(),
            _random.State);

        /// <summary>
        /// Puts the run back where a snapshot left it.
        /// </summary>
        /// <remarks>
        /// The current arena is rebuilt from the seed rather than restored, so play resumes at its
        /// start. Anything mid-arena — bombs ticking, enemies mid-stride — is deliberately lost.
        /// </remarks>
        /// <returns>Whether the snapshot described a run worth resuming.</returns>
        public bool TryResume(in RunSnapshot snapshot)
        {
            if (!snapshot.HasProgress)
            {
                return false;
            }

            _seed = snapshot.Seed;
            _arenaIndex = snapshot.ArenaIndex;
            _offerCount = 0;
            _pending = ItemId.None;
            _heldCount = 0;

            Array.Clear(_held, 0, _held.Length);

            for (var i = 0; i < snapshot.Held.Length && _heldCount < _held.Length; i++)
            {
                if (snapshot.Held[i] != ItemId.None)
                {
                    _held[_heldCount++] = snapshot.Held[i];
                }
            }

            // Restored to the exact position it had reached, never replayed. Rebuilding the offers
            // that led here would consume a different number of draws, because each shuffle sized
            // itself against however many items were held at that moment.
            _random = snapshot.OfferState != 0u
                ? DeterministicRandom.FromState(snapshot.OfferState)
                : new DeterministicRandom(_seed);

            BuildArena(snapshot.CarriedHealth);
            Phase = RunPhase.Fighting;

            return true;
        }

        /// <summary>
        /// Reacts to whatever the current arena's simulation has decided.
        /// </summary>
        /// <remarks>
        /// Called after ticking. Returns whether the phase changed, so the view knows a rebuild or
        /// an overlay is due without having to diff anything itself.
        /// </remarks>
        public bool Observe()
        {
            if (Phase != RunPhase.Fighting)
            {
                return false;
            }

            switch (Current.Phase)
            {
                case MatchPhase.Defeat:
                    Phase = RunPhase.Ended;
                    return true;

                case MatchPhase.Victory:
                    // Only genuinely nothing left to show — every item already held — rolls the run
                    // straight on. A full inventory still gets an offer; it is simply a swap.
                    if (BuildOffers() == 0)
                    {
                        AdvanceArena();
                        return true;
                    }

                    Phase = RunPhase.Choosing;
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Takes one of the offered items and moves on to the next arena.
        /// </summary>
        /// <returns>Whether the choice was valid.</returns>
        public bool TryChoose(ItemId id)
        {
            if (Phase != RunPhase.Choosing)
            {
                return false;
            }

            var offered = false;
            for (var i = 0; i < _offerCount; i++)
            {
                if (_offers[i] == id)
                {
                    offered = true;
                }
            }

            if (!offered)
            {
                return false;
            }

            // With a slot free this is simply a gain. With none, the run asks the better question:
            // not "what do I want?" but "what am I willing to give up?".
            if (_heldCount < _held.Length)
            {
                _held[_heldCount++] = id;
                AdvanceArena();
                return true;
            }

            _pending = id;
            Phase = RunPhase.Discarding;
            return true;
        }

        /// <summary>Gives up a held item to take the pending one.</summary>
        /// <returns>Whether the swap was valid.</returns>
        public bool TryDiscard(ItemId discard)
        {
            if (Phase != RunPhase.Discarding || _pending == ItemId.None)
            {
                return false;
            }

            for (var i = 0; i < _heldCount; i++)
            {
                if (_held[i] != discard)
                {
                    continue;
                }

                _held[i] = _pending;
                _pending = ItemId.None;
                AdvanceArena();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Declines the offer and moves on.
        /// </summary>
        /// <remarks>
        /// Necessary once offers become swaps. Without it a late run would force a player to break a
        /// build they are happy with, turning a decision into a penalty for having chosen well.
        /// </remarks>
        public bool Skip()
        {
            if (Phase != RunPhase.Choosing && Phase != RunPhase.Discarding)
            {
                return false;
            }

            _pending = ItemId.None;
            AdvanceArena();
            return true;
        }

        /// <summary>
        /// Throws the run away and starts a fresh one.
        /// </summary>
        /// <remarks>
        /// Deliberately cheap: no assets are touched and nothing is loaded, so a restart is a few
        /// allocations rather than a scene transition. Players who have just died want to be playing
        /// again immediately, and a loading screen between attempts is how a roguelite loses them.
        /// </remarks>
        public void Restart()
        {
            _heldCount = 0;
            _offerCount = 0;
            _arenaIndex = 0;
            _pending = ItemId.None;
            _random = new DeterministicRandom(_seed);

            Array.Clear(_held, 0, _held.Length);

            for (var i = 0; i < _startingItems.Length && _heldCount < _held.Length; i++)
            {
                if (_startingItems[i] != ItemId.None && !Holds(_startingItems[i]))
                {
                    _held[_heldCount++] = _startingItems[i];
                }
            }

            BuildArena(_config.PlayerMaxHealth);
            Phase = RunPhase.Fighting;
        }

        /// <summary>Moves to the next arena, carrying the build and whatever health is left.</summary>
        private void AdvanceArena()
        {
            // Carried forward, plus a partial recovery for clearing the arena. Full healing would
            // remove the reason to play carefully; none at all makes a third arena arithmetic
            // rather than a fight. This is the number most likely to need tuning.
            var health = Current.State.Player.Health.Current + _config.ArenaClearHealing;

            _arenaIndex++;
            _offerCount = 0;
            _pending = ItemId.None;

            BuildArena(health);
            Phase = RunPhase.Fighting;
        }

        /// <summary>Creates the simulation for the current arena and restores the build into it.</summary>
        private void BuildArena(int carriedHealth)
        {
            // Derived from the run seed and the arena index, so a run is reproducible end to end
            // while no two arenas roll the same sequence.
            var seed = _seed ^ (uint)((_arenaIndex + 1) * 2654435761u);

            // The arena gets its own generator rather than sharing the run's. Otherwise the board
            // would be reshaped by anything else that happened to draw a random number first —
            // an extra item offer would silently change every layout after it.
            var layoutRandom = new DeterministicRandom(seed);
            var layout = _arenas.Create(_arenaIndex, ref layoutRandom);

            Current = new GameSimulation(_config, layout, seed, carriedHealth);

            // Re-granted in acquisition order. Items apply once and mutate, so replaying the same
            // grants onto a fresh loadout reproduces the build exactly.
            for (var i = 0; i < _heldCount; i++)
            {
                Current.TryGrantItem(_held[i]);
            }
        }

        /// <summary>Fills the offer list with items not already held, and returns how many.</summary>
        private int BuildOffers()
        {
            _offerCount = 0;

            // Shuffled by the run's own generator so the offer is part of the reproducible run
            // rather than a roll made somewhere the replay cannot see.
            var pool = ItemCatalog.All;
            var available = 0;
            Span<ItemId> candidates = stackalloc ItemId[32];

            for (var i = 0; i < pool.Length && available < candidates.Length; i++)
            {
                if (!Holds(pool[i]))
                {
                    candidates[available++] = pool[i];
                }
            }

            for (var i = available - 1; i > 0; i--)
            {
                var j = _random.NextInt(i + 1);
                (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
            }

            var take = available < OfferCount ? available : OfferCount;
            for (var i = 0; i < take; i++)
            {
                _offers[_offerCount++] = candidates[i];
            }

            return _offerCount;
        }

        private bool Holds(ItemId id)
        {
            for (var i = 0; i < _heldCount; i++)
            {
                if (_held[i] == id)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
