using System;
using BomberLegends.Core;
using BomberLegends.Simulation.Actors;
using BomberLegends.Simulation.Board;
using BomberLegends.Simulation.Bombs;
using BomberLegends.Simulation.Events;
using BomberLegends.Simulation.Skills;
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
        private readonly int[] _detonationQueue;
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

            // Sized to the bomb pool: a bomb can only enter the queue once, guarded by its queued
            // flag, so this can never overflow however long a chain runs.
            _detonationQueue = new int[config.MaxBombs];

            _state = new SimulationState
            {
                Tick = 0,
                Phase = MatchPhase.Playing,
                Board = layout.CreateBoard(),
                Player = PlayerState.SpawnedAt(
                    layout.PlayerSpawn,
                    config.StartingBombCapacity,
                    config.StartingBlastRange,
                    config.PlayerMaxHealth,
                    config.CreateStartingLoadout()),
                Enemies = new EnemyBuffer(config.MaxEnemies),
                Bombs = new BombBuffer(config.MaxBombs),
                BombGrid = new BombGrid(layout.Width, layout.Height),
                BlastGrid = new BlastGrid(layout.Width, layout.Height),
                Projectiles = new ProjectileBuffer(config.MaxProjectiles),
                Random = new DeterministicRandom(seed)
            };

            var spawns = layout.EnemySpawns;
            for (var i = 0; i < spawns.Length; i++)
            {
                _state.Enemies.Spawn(spawns[i], config.EnemyMaxHealth);
            }

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

            // 1. Skills — recharge, then turn presses into effects. Ahead of movement so a dash
            //    pressed this tick moves the player this tick; latency on a dash is unforgivable.
            SkillSystem.Tick(ref _state, _config, intent, _events);

            // 2. Movement — soft-grid travel, turn rules, wall and bomb collision. Reads whatever
            //    dash the step above may have started.
            MovementSystem.Tick(ref _state, _config, intent, _events);

            // 3. Placement — turn a button press into a bomb on the board.
            BombPlacementSystem.Tick(ref _state, _config, intent, _events);

            // 4. Fuses — burn down, and queue whatever is due.
            var queued = FuseSystem.Tick(ref _state, _detonationQueue);

            // 5. Blasts — age existing fire, then resolve detonations and everything they chain into.
            BlastSystem.Tick(ref _state, _config, _detonationQueue, queued, _events);

            // 6. Enemies — pursue, colliding with the world exactly as the player does.
            EnemySystem.Tick(ref _state, _config, _events);

            // 7. Skillshots — fly against final positions, so a shot is judged against where the
            //    enemy actually ended the tick rather than where it started.
            ProjectileSystem.Tick(ref _state, _config, _events);

            // 8. Damage — read a finished picture of what is on fire and who is touching whom.
            //    Must follow the blast, or it would judge a half-resolved explosion. Also follows
            //    skillshots, so an enemy killed by one does not still land a contact hit.
            DamageSystem.Tick(ref _state, _config, _events);

            // Items, pickups, objectives and scoring slot in here in Milestone 5.

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
                hash = Fold(hash, (ulong)_state.Player.ActiveBombs);
                hash = Fold(hash, (ulong)_state.Player.BombCapacity);
                hash = Fold(hash, (ulong)_state.Player.BlastRange);
                hash = Fold(hash, (ulong)(uint)_state.Player.DashTicksRemaining);
                hash = Fold(hash, (ulong)(uint)_state.Player.DashVelocityX);
                hash = Fold(hash, (ulong)(uint)_state.Player.DashVelocityY);

                for (var index = 0; index < SkillLoadout.SlotCount; index++)
                {
                    var skill = _state.Player.Skills[index];
                    hash = Fold(hash, (byte)skill.Id);
                    hash = Fold(hash, (ulong)(uint)skill.CooldownRemaining);
                    hash = Fold(hash, (ulong)(uint)skill.Charges);
                }

                for (var slot = 0; slot < _state.Projectiles.Capacity; slot++)
                {
                    var projectile = _state.Projectiles[slot];
                    hash = Fold(hash, projectile.IsActive ? 1UL : 0UL);
                    if (!projectile.IsActive)
                    {
                        continue;
                    }

                    hash = Fold(hash, (ulong)(uint)projectile.Position.X);
                    hash = Fold(hash, (ulong)(uint)projectile.Position.Y);
                    hash = Fold(hash, (ulong)(uint)projectile.TicksRemaining);
                }

                for (var slot = 0; slot < _state.Bombs.Capacity; slot++)
                {
                    var bomb = _state.Bombs[slot];
                    hash = Fold(hash, bomb.IsActive ? 1UL : 0UL);
                    if (!bomb.IsActive)
                    {
                        continue;
                    }

                    hash = Fold(hash, (ulong)(uint)bomb.Tile.X);
                    hash = Fold(hash, (ulong)(uint)bomb.Tile.Y);
                    hash = Fold(hash, (ulong)(uint)bomb.FuseTicksRemaining);
                }

                hash = Fold(hash, (ulong)(uint)_state.Player.Health.Current);
                hash = Fold(hash, (ulong)(uint)_state.Player.Health.InvulnerableTicks);

                for (var slot = 0; slot < _state.Enemies.Capacity; slot++)
                {
                    var enemy = _state.Enemies[slot];
                    hash = Fold(hash, enemy.IsActive ? 1UL : 0UL);
                    if (!enemy.IsActive)
                    {
                        continue;
                    }

                    hash = Fold(hash, (ulong)(uint)enemy.Position.X);
                    hash = Fold(hash, (ulong)(uint)enemy.Position.Y);
                    hash = Fold(hash, (ulong)(uint)enemy.Health.Current);
                    hash = Fold(hash, (byte)enemy.MoveDirection);
                }

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
