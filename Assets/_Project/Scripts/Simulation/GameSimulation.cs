using System;
using BomberLegends.Core;
using BomberLegends.Simulation.Actors;
using BomberLegends.Simulation.Board;
using BomberLegends.Simulation.Events;
using BomberLegends.Simulation.Systems;

namespace BomberLegends.Simulation
{
    /// <summary>
    /// The authoritative rules of a single match.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Deterministic and free of any engine reference: given the same configuration, layout, seed
    /// and sequence of intents, this produces byte-identical state every time, on any platform.
    /// That is what makes the rules testable in milliseconds without a scene, replays exact, and a
    /// future server-side simulation a matter of hosting this class rather than rewriting it.
    /// </para>
    /// <para>
    /// <see cref="Tick"/> is the only way to change anything. The view reads
    /// <see cref="State"/> and drains <see cref="Events"/>; it never writes back.
    /// </para>
    /// </remarks>
    public sealed class GameSimulation
    {
        private readonly SimulationConfig _config;
        private readonly SimEventBuffer _events;
        private SimulationState _state;

        /// <summary>Starts a match.</summary>
        /// <param name="config">Tuning, already baked to plain values.</param>
        /// <param name="layout">The level's starting state.</param>
        /// <param name="seed">Seeds every random decision the match will make.</param>
        /// <exception cref="ArgumentException">The configuration is unusable.</exception>
        public GameSimulation(in SimulationConfig config, in LevelLayout layout, uint seed)
        {
            config.Validate();

            _config = config;
            _events = new SimEventBuffer();

            _state = new SimulationState
            {
                Tick = 0,
                Phase = MatchPhase.Playing,
                Board = layout.CreateBoard(),
                Player = PlayerState.SpawnedAt(layout.PlayerSpawn),
                Random = new DeterministicRandom(seed)
            };

            _events.Add(new SimEvent(SimEventType.PlayerSpawned, layout.PlayerSpawn));
        }

        /// <summary>
        /// The current state, returned by reference so reading it copies nothing.
        /// </summary>
        public ref readonly SimulationState State => ref _state;

        /// <summary>
        /// Events produced by the most recent tick, or by construction until the first tick runs.
        /// Drain these every frame; the next tick clears them.
        /// </summary>
        public SimEventBuffer Events => _events;

        /// <summary>Ticks elapsed since the match began.</summary>
        public int CurrentTick => _state.Tick;

        /// <summary>Where the match is in its lifecycle.</summary>
        public MatchPhase Phase => _state.Phase;

        /// <summary>
        /// Advances the match by exactly one tick.
        /// </summary>
        /// <remarks>
        /// Systems run in the order listed below, and that order is part of the design rather than
        /// an accident of file layout. Later milestones insert their systems into this list, not
        /// into scattered update methods where the ordering would be implicit and unreviewable.
        /// </remarks>
        public void Tick(in PlayerIntent intent)
        {
            _events.Clear();

            if (_state.Phase != MatchPhase.Playing)
            {
                _state.Tick++;
                return;
            }

            // 1. Movement — soft-grid travel, turn rules, wall collision.
            MovementSystem.Tick(ref _state, _config, intent, _events);

            // Bombs, fuses, blasts, enemies, damage, pickups, objectives, timer and scoring
            // slot in here in Milestones 2 to 4.

            _state.Tick++;
        }

        /// <summary>
        /// A hash of everything that must match for two runs to be considered identical.
        /// </summary>
        /// <remarks>
        /// Used by the determinism tests, and by replay validation later. Deliberately covers the
        /// random generator's state as well: two runs that reached the same board by different
        /// sequences of rolls are not the same run.
        /// </remarks>
        public ulong ComputeStateHash()
        {
            unchecked
            {
                var hash = 14695981039346656037UL;

                hash = Fold(hash, (ulong)_state.Tick);
                hash = Fold(hash, (byte)_state.Phase);
                hash = Fold(hash, _state.Board.ComputeHash());
                hash = Fold(hash, (ulong)(uint)_state.Player.Position.X);
                hash = Fold(hash, (ulong)(uint)_state.Player.Position.Y);
                hash = Fold(hash, (byte)_state.Player.MoveDirection);
                hash = Fold(hash, (byte)_state.Player.Facing);
                hash = Fold(hash, _state.Player.IsMoving ? 1UL : 0UL);
                hash = Fold(hash, _state.Random.State);

                return hash;
            }
        }

        private static ulong Fold(ulong hash, ulong value)
        {
            unchecked
            {
                for (var i = 0; i < 8; i++)
                {
                    hash ^= (value >> (i * 8)) & 0xFF;
                    hash *= 1099511628211UL;
                }

                return hash;
            }
        }
    }
}
