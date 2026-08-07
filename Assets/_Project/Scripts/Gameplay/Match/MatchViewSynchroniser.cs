using System.Collections.Generic;
using BomberLegends.Core;
using BomberLegends.Gameplay.Board;
using BomberLegends.Gameplay.Bombs;
using BomberLegends.Gameplay.Enemies;
using BomberLegends.Gameplay.Skills;
using BomberLegends.Gameplay.Vfx;
using BomberLegends.Simulation;
using BomberLegends.Simulation.Board;
using BomberLegends.Simulation.Events;
using UnityEngine;
using UnityEngine.Pool;

namespace BomberLegends.Gameplay.Match
{
    /// <summary>
    /// Turns simulation events into things you can see.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The one place the view reacts to the rules. The simulation never spawns an effect or knows
    /// one exists; it announces what happened and this decides what that looks like. That is what
    /// keeps the rules testable without a scene, and what will let the same rules run on a server
    /// with no view at all.
    /// </para>
    /// <para>
    /// Every view comes from a pool, prewarmed while the loading screen is up. A chain reaction is
    /// the single heaviest moment in the game — a dozen bombs, a hundred blast tiles and their
    /// debris, all in one tick — and it is exactly the wrong moment to be allocating.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class MatchViewSynchroniser : MonoBehaviour
    {
        [Header("Pool sizes")]
        [SerializeField, Min(1)]
        [Tooltip("Bombs alive at once. Prewarmed; growing beyond this mid-match logs an error.")]
        private int _bombPoolSize = 16;

        [SerializeField, Min(1)]
        [Tooltip("Blast tiles alive at once. A long chain is the worst case, not a single bomb.")]
        private int _blastPoolSize = 160;

        [SerializeField, Min(1)]
        [Tooltip("Block destruction effects alive at once.")]
        private int _debrisPoolSize = 64;

        [Header("Appearance")]
        [SerializeField]
        [Tooltip("Readout of health and enemies remaining. Optional.")]
        private MatchHudView? _hud;

        [SerializeField]
        [Tooltip("Colour of a blast tile.")]
        private Color _blastColour = new Color(1f, 0.72f, 0.25f);

        [SerializeField]
        [Tooltip("Colour of the flash left where a block was destroyed.")]
        private Color _debrisColour = new Color(0.95f, 0.55f, 0.22f);

        [SerializeField, Range(0.05f, 1f)]
        [Tooltip("How long the debris flash lasts, in seconds.")]
        private float _debrisSeconds = 0.28f;

        private readonly List<TimedMeshView> _activeEffects = new List<TimedMeshView>(256);

        private ObjectPool<BombView>? _bombPool;
        private ObjectPool<BlastView>? _blastPool;
        private ObjectPool<BlockDestructionView>? _debrisPool;
        private ObjectPool<ProjectileView>? _projectilePool;

        private BombView?[] _bombsBySlot = System.Array.Empty<BombView>();
        private EnemyView?[] _enemiesBySlot = System.Array.Empty<EnemyView>();
        private SubTilePoint[] _enemyPrevious = System.Array.Empty<SubTilePoint>();
        private SubTilePoint[] _enemyCurrent = System.Array.Empty<SubTilePoint>();
        private ProjectileView?[] _projectilesBySlot = System.Array.Empty<ProjectileView>();
        private SubTilePoint[] _projectilePrevious = System.Array.Empty<SubTilePoint>();
        private SubTilePoint[] _projectileCurrent = System.Array.Empty<SubTilePoint>();
        private BoardRenderer? _boardRenderer;
        private BoardProjector _projector = null!;

        private Material? _opaqueMaterial;
        private Material? _transparentMaterial;
        private float _blastSeconds;
        private int _fuseTicks;
        private bool _reportedOverflow;
        private bool _prewarming;

        /// <summary>The readout, so a run can tell it which arena this is.</summary>
        public MatchHudView? Hud => _hud;

        /// <summary>Prepares the pools for a match.</summary>
        public void Begin(
            BoardRenderer boardRenderer,
            BoardProjector projector,
            in SimulationConfig config)
        {
            _boardRenderer = boardRenderer;
            _projector = projector;
            _fuseTicks = config.FuseTicks;
            _blastSeconds = (float)config.BlastLingerTicks / SimulationConstants.TicksPerSecond;

            _bombsBySlot = new BombView?[config.MaxBombs];
            _enemiesBySlot = new EnemyView?[config.MaxEnemies];
            _enemyPrevious = new SubTilePoint[config.MaxEnemies];
            _enemyCurrent = new SubTilePoint[config.MaxEnemies];
            _projectilesBySlot = new ProjectileView?[config.MaxProjectiles];
            _projectilePrevious = new SubTilePoint[config.MaxProjectiles];
            _projectileCurrent = new SubTilePoint[config.MaxProjectiles];

            // One shared material per surface type; instances vary only by property block, so the
            // whole effect layer stays batchable and nothing leaks a material per pooled object.
            _opaqueMaterial = PlaceholderMeshes.CreateMaterial(Color.white);
            _transparentMaterial = PlaceholderMeshes.CreateTransparentMaterial(Color.white);

            _bombPool = CreatePool(CreateBombView, _bombPoolSize, view => view.ResetView());
            _blastPool = CreatePool(() => CreateEffect<BlastView>("Blast"), _blastPoolSize,
                view => view.ResetView());
            _debrisPool = CreatePool(() => CreateEffect<BlockDestructionView>("Debris"), _debrisPoolSize,
                view => view.ResetView());
            _projectilePool = CreatePool(CreateProjectileView, _projectilesBySlot.Length,
                view => view.ResetView());

            Prewarm();
        }

        /// <summary>
        /// Spawns a view for every enemy the level placed. Called once the simulation exists.
        /// </summary>
        public void SpawnEnemies(GameSimulation simulation)
        {
            for (var slot = 0; slot < _enemiesBySlot.Length && slot < simulation.State.Enemies.Capacity;
                 slot++)
            {
                var enemy = simulation.State.Enemies[slot];
                if (!enemy.IsActive)
                {
                    continue;
                }

                var view = CreateEnemyView();
                view.Begin(_projector.PositionToWorld(enemy.Position));

                _enemiesBySlot[slot] = view;
                _enemyPrevious[slot] = enemy.Position;
                _enemyCurrent[slot] = enemy.Position;
            }
        }

        /// <summary>
        /// Records where every enemy was before the coming tick, so the view can interpolate.
        /// </summary>
        /// <remarks>
        /// Enemies are continuous state rather than discrete events, so like the player they need a
        /// before and an after. Without this they would step at the simulation rate while everything
        /// around them moved smoothly.
        /// </remarks>
        public void BeforeTick(GameSimulation simulation)
        {
            for (var slot = 0; slot < _enemyCurrent.Length && slot < simulation.State.Enemies.Capacity;
                 slot++)
            {
                _enemyPrevious[slot] = _enemyCurrent[slot];
            }

            for (var slot = 0; slot < _projectileCurrent.Length; slot++)
            {
                _projectilePrevious[slot] = _projectileCurrent[slot];
            }
        }

        /// <summary>Reacts to everything the simulation announced this tick.</summary>
        public void Consume(GameSimulation simulation)
        {
            var events = simulation.Events;

            for (var i = 0; i < events.Count; i++)
            {
                var simEvent = events[i];

                switch (simEvent.Type)
                {
                    case SimEventType.BombPlaced:
                        SpawnBomb(simEvent.Coord, simEvent.EntityId);
                        break;

                    case SimEventType.BombDetonated:
                        ReleaseBomb(simEvent.EntityId);
                        break;

                    case SimEventType.BlastSpawned:
                        SpawnBlast(simEvent.Coord);
                        break;

                    case SimEventType.BlockDestroyed:
                        DestroyBlock(simEvent.Coord);
                        break;

                    case SimEventType.EnemyKilled:
                        ReleaseEnemy(simEvent.EntityId - 1);
                        break;

                    case SimEventType.ProjectileFired:
                        SpawnProjectile(simulation, simEvent.EntityId);
                        break;

                    case SimEventType.ProjectileEnded:
                        ReleaseProjectile(simEvent.EntityId);
                        break;
                }
            }
        }

        /// <summary>Advances every live effect and retires the ones that have finished.</summary>
        public void Render(GameSimulation simulation, float deltaSeconds, float alpha)
        {
            RenderEnemies(simulation, alpha);
            RenderProjectiles(simulation, alpha);
            _hud?.Render(simulation);

            for (var i = _activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = _activeEffects[i];

                if (effect.Advance(deltaSeconds))
                {
                    continue;
                }

                _activeEffects.RemoveAt(i);
                Release(effect);
            }

            AdvanceBombs(simulation, deltaSeconds);
        }

        /// <summary>Returns every view to its pool at the end of a match.</summary>
        public void Stop()
        {
            for (var i = 0; i < _activeEffects.Count; i++)
            {
                Release(_activeEffects[i]);
            }

            _activeEffects.Clear();

            for (var slot = 0; slot < _bombsBySlot.Length; slot++)
            {
                ReleaseBomb(slot);
            }

            for (var slot = 0; slot < _enemiesBySlot.Length; slot++)
            {
                ReleaseEnemy(slot);
            }

            for (var slot = 0; slot < _projectilesBySlot.Length; slot++)
            {
                ReleaseProjectile(slot);
            }
        }

        private void RenderEnemies(GameSimulation simulation, float alpha)
        {
            for (var slot = 0; slot < _enemiesBySlot.Length; slot++)
            {
                var view = _enemiesBySlot[slot];
                if (view == null)
                {
                    continue;
                }

                var enemy = simulation.State.Enemies[slot];
                if (!enemy.IsActive)
                {
                    continue;
                }

                var gridX = Mathf.LerpUnclamped(
                    BoardProjector.ToGrid(_enemyPrevious[slot].X),
                    BoardProjector.ToGrid(_enemyCurrent[slot].X),
                    alpha);
                var gridY = Mathf.LerpUnclamped(
                    BoardProjector.ToGrid(_enemyPrevious[slot].Y),
                    BoardProjector.ToGrid(_enemyCurrent[slot].Y),
                    alpha);

                view.Render(_projector.GridToWorld(gridX, gridY), enemy.Health.IsInvulnerable);
            }
        }

        private void RenderProjectiles(GameSimulation simulation, float alpha)
        {
            for (var slot = 0; slot < _projectilesBySlot.Length; slot++)
            {
                var view = _projectilesBySlot[slot];
                if (view == null)
                {
                    continue;
                }

                var gridX = Mathf.LerpUnclamped(
                    BoardProjector.ToGrid(_projectilePrevious[slot].X),
                    BoardProjector.ToGrid(_projectileCurrent[slot].X),
                    alpha);
                var gridY = Mathf.LerpUnclamped(
                    BoardProjector.ToGrid(_projectilePrevious[slot].Y),
                    BoardProjector.ToGrid(_projectileCurrent[slot].Y),
                    alpha);

                // Held at chest height so a shot reads as passing over the floor rather than
                // sliding along it, and never disappears behind a block it is flying past.
                view.Render(_projector.GridToWorld(gridX, gridY, _projector.BlockHeight * 0.5f));
            }
        }

        private void SpawnProjectile(GameSimulation simulation, int slot)
        {
            if (_projectilePool == null || slot < 0 || slot >= _projectilesBySlot.Length)
            {
                return;
            }

            ReleaseProjectile(slot);

            var position = simulation.State.Projectiles[slot].Position;

            // Spawned with no history, or the first frame would interpolate it in from wherever the
            // previous occupant of this slot died.
            _projectilePrevious[slot] = position;
            _projectileCurrent[slot] = position;

            var view = _projectilePool.Get();
            view.Begin(_projector.PositionToWorld(position, _projector.BlockHeight * 0.5f));

            _projectilesBySlot[slot] = view;
        }

        private void ReleaseProjectile(int slot)
        {
            if (slot < 0 || slot >= _projectilesBySlot.Length)
            {
                return;
            }

            var view = _projectilesBySlot[slot];
            if (view == null)
            {
                return;
            }

            _projectilesBySlot[slot] = null;
            _projectilePool?.Release(view);
        }

        private ProjectileView CreateProjectileView() =>
            CreateEffectObject("Skillshot", PlaceholderMeshes.Sphere, _opaqueMaterial)
                .AddComponent<ProjectileView>();

        private void ReleaseEnemy(int slot)
        {
            if (slot < 0 || slot >= _enemiesBySlot.Length)
            {
                return;
            }

            var view = _enemiesBySlot[slot];
            if (view == null)
            {
                return;
            }

            _enemiesBySlot[slot] = null;
            Destroy(view.gameObject);
        }

        private EnemyView CreateEnemyView()
        {
            var body = CreateEffectObject("Enemy", PlaceholderMeshes.Sphere, _opaqueMaterial);
            body.GetComponent<MeshRenderer>().shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.On;

            return body.AddComponent<EnemyView>();
        }

        private void AdvanceBombs(GameSimulation simulation, float deltaSeconds)
        {
            for (var slot = 0; slot < _enemyCurrent.Length && slot < simulation.State.Enemies.Capacity;
                 slot++)
            {
                _enemyCurrent[slot] = simulation.State.Enemies[slot].Position;
            }

            for (var slot = 0; slot < _projectileCurrent.Length &&
                 slot < simulation.State.Projectiles.Capacity; slot++)
            {
                if (_projectilesBySlot[slot] != null)
                {
                    _projectileCurrent[slot] = simulation.State.Projectiles[slot].Position;
                }
            }

            for (var slot = 0; slot < _bombsBySlot.Length; slot++)
            {
                var view = _bombsBySlot[slot];
                if (view == null)
                {
                    continue;
                }

                var bomb = simulation.State.Bombs[slot];
                if (!bomb.IsActive)
                {
                    continue;
                }

                // One at the moment of placement, rising to zero remaining as it is about to go off.
                var progress = 1f - ((float)bomb.FuseTicksRemaining / _fuseTicks);
                view.Advance(deltaSeconds, progress);
            }
        }

        private void SpawnBomb(GridCoord tile, int slot)
        {
            if (_bombPool == null || slot < 0 || slot >= _bombsBySlot.Length)
            {
                return;
            }

            ReleaseBomb(slot);

            var view = _bombPool.Get();
            view.Begin(_projector.TileToWorld(tile, _projector.BlockHeight * 0.35f));

            _bombsBySlot[slot] = view;
        }

        private void ReleaseBomb(int slot)
        {
            if (slot < 0 || slot >= _bombsBySlot.Length)
            {
                return;
            }

            var view = _bombsBySlot[slot];
            if (view == null)
            {
                return;
            }

            _bombsBySlot[slot] = null;
            _bombPool?.Release(view);
        }

        private void SpawnBlast(GridCoord tile)
        {
            if (_blastPool == null)
            {
                return;
            }

            var view = _blastPool.Get();

            // Sits just clear of the floor so it never fights the ground plane for depth.
            view.Begin(
                _projector.TileToWorld(tile, 0.02f),
                _blastSeconds,
                _blastColour,
                new Vector3(_projector.TileSize, 0.04f, _projector.TileSize));

            _activeEffects.Add(view);
        }

        private void DestroyBlock(GridCoord tile)
        {
            _boardRenderer?.SetTile(tile, TileType.Empty);

            if (_debrisPool == null)
            {
                return;
            }

            var view = _debrisPool.Get();

            view.BeginAt(
                _projector.TileToWorld(tile, _projector.BlockHeight * 0.5f),
                _debrisSeconds,
                _debrisColour,
                Vector3.one * _projector.TileSize * 0.9f);

            _activeEffects.Add(view);
        }

        private void Release(TimedMeshView effect)
        {
            switch (effect)
            {
                case BlastView blast:
                    _blastPool?.Release(blast);
                    break;
                case BlockDestructionView debris:
                    _debrisPool?.Release(debris);
                    break;
            }
        }

        private ObjectPool<T> CreatePool<T>(System.Func<T> factory, int size, System.Action<T> reset)
            where T : Component =>
            new ObjectPool<T>(
                createFunc: () =>
                {
                    ReportOverflow(typeof(T).Name, size);
                    return factory();
                },
                actionOnGet: view => view.gameObject.SetActive(true),
                actionOnRelease: view =>
                {
                    reset(view);
                    view.gameObject.SetActive(false);
                },
                actionOnDestroy: view => Destroy(view.gameObject),
                collectionCheck: true,
                defaultCapacity: size,
                maxSize: size * 4);

        private void Prewarm()
        {
            // Filling the pools necessarily runs the create callback, so overflow reporting is
            // suppressed for the duration. Without this, every match would report an overflow the
            // moment it loaded.
            _prewarming = true;

            PrewarmPool(_bombPool, _bombPoolSize);
            PrewarmPool(_blastPool, _blastPoolSize);
            PrewarmPool(_debrisPool, _debrisPoolSize);
            PrewarmPool(_projectilePool, _projectilesBySlot.Length);

            _prewarming = false;

            // Everything created up to here is expected; anything beyond it is not.
            _reportedOverflow = false;
        }

        private static void PrewarmPool<T>(ObjectPool<T>? pool, int count) where T : class
        {
            if (pool == null)
            {
                return;
            }

            var warmed = new T[count];
            for (var i = 0; i < count; i++)
            {
                warmed[i] = pool.Get();
            }

            for (var i = 0; i < count; i++)
            {
                pool.Release(warmed[i]);
            }
        }

        private void ReportOverflow(string typeName, int size)
        {
            if (_prewarming || _reportedOverflow)
            {
                return;
            }

            _reportedOverflow = true;
            Debug.LogError(
                $"[Match] The {typeName} pool ran past its prewarmed size of {size} and had to " +
                "allocate mid-match. Raise the size; a pool that grows is just Instantiate with " +
                "extra steps.");
        }

        private BombView CreateBombView() =>
            CreateEffectObject("Bomb", PlaceholderMeshes.Sphere, _opaqueMaterial)
                .AddComponent<BombView>();

        private T CreateEffect<T>(string name) where T : TimedMeshView =>
            CreateEffectObject(name, PlaceholderMeshes.Cube, _transparentMaterial).AddComponent<T>();

        private GameObject CreateEffectObject(string name, Mesh mesh, Material? material)
        {
            var child = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            child.transform.SetParent(transform, false);

            child.GetComponent<MeshFilter>().sharedMesh = mesh;

            var renderer = child.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            return child;
        }

        private void OnDestroy()
        {
            if (_opaqueMaterial != null)
            {
                Destroy(_opaqueMaterial);
            }

            if (_transparentMaterial != null)
            {
                Destroy(_transparentMaterial);
            }
        }
    }
}
