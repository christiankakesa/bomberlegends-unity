# Bomber Legends — Technical Architecture (Phase 3)

**Engine:** Unity 6.3 LTS · URP (2D Renderer) · C# 9 · IL2CPP
**Depends on:** `01-ANALYSIS.md`, `02-PROTOTYPE-SCOPE.md`
**Status:** Proposed. No code written.

---

## 0. The Three Decisions That Define This Architecture

Everything else follows from these. Each is cheap now and expensive later.

### D1 — The simulation is a pure C# library with **no engine references**

The entire gameplay rules layer (grid, bombs, blasts, enemies, damage, objectives, scoring) lives in an
assembly with Unity's `noEngineReferences` flag set. It cannot call `Transform`, `Time`, `Debug`, or
`Instantiate` — the compiler enforces it.

**Why:**
- **Testability.** Thousands of simulation tests run in milliseconds in EditMode, headless, no scene.
- **Determinism.** No frame-rate coupling, no `Time.deltaTime` leaking into rules.
- **Network readiness.** If Q1 (`01-ANALYSIS.md` §13) answers "multiplayer", this assembly is already the
  server simulation. Retrofitting this later is a rewrite; doing it now costs one checkbox and some
  discipline.
- **Iteration speed.** Balance changes are validated by a test run, not by playing the game.

**Cost:** views must mirror simulation state rather than own it. That is the whole trade, and it is worth it.

### D2 — Zero Unity physics in gameplay

Movement, collision, and blast propagation are grid lookups and interval maths. No `Rigidbody2D`, no
`Physics2D.Overlap*`, no colliders except for UI raycasts. Physics is non-deterministic, allocates,
costs frame time, and buys nothing on a tile grid.

### D3 — Fixed simulation tick at 30 Hz + view interpolation

The simulation advances in fixed 33.33 ms steps driven by an explicit accumulator (not `FixedUpdate`, which
is owned by the physics system and mutable by other code). Views interpolate between the previous and
current tick states, so movement looks smooth at any render rate.

```
Update(): accumulator += deltaTime
          while (accumulator >= TickDuration) { Simulation.Tick(intent); accumulator -= TickDuration; }
          alpha = accumulator / TickDuration
          views.Render(previousState, currentState, alpha)
```

30 Hz is ample for a grid game, halves simulation CPU on low-tier devices, and makes replays and network
snapshots small.

---

## 1. Folder Structure

```
Assets/
├── _Project/                          # everything we author, one root — keeps third-party out
│   ├── Art/
│   │   ├── Characters/
│   │   ├── Tiles/
│   │   ├── Props/
│   │   ├── UI/
│   │   └── Atlases/                   # SpriteAtlas assets (one per screen-cohesive group)
│   ├── Audio/
│   │   ├── Music/
│   │   ├── Sfx/
│   │   └── Mixers/                    # MainAudioMixer.mixer
│   ├── Data/                          # ScriptableObject *instances* (not code)
│   │   ├── Levels/
│   │   ├── Enemies/
│   │   ├── Abilities/
│   │   ├── Balance/
│   │   ├── Audio/
│   │   └── EventChannels/
│   ├── Prefabs/
│   │   ├── Gameplay/
│   │   ├── UI/
│   │   └── Vfx/
│   ├── Scenes/
│   │   ├── Bootstrap.unity
│   │   ├── Hub.unity
│   │   ├── Match.unity
│   │   └── Dev/                       # scratch scenes, excluded from builds
│   ├── Settings/
│   │   ├── Rendering/                 # URP assets + 2D renderers per quality tier
│   │   ├── Input/                     # BomberLegends.inputactions
│   │   └── Addressables/
│   ├── Scripts/
│   │   ├── Core/                      # asmdef: BomberLegends.Core          (no engine refs)
│   │   ├── Simulation/                # asmdef: BomberLegends.Simulation    (no engine refs)
│   │   ├── Data/                      # asmdef: BomberLegends.Data
│   │   ├── Services/                  # asmdef: BomberLegends.Services
│   │   ├── Input/                     # asmdef: BomberLegends.Input
│   │   ├── Gameplay/                  # asmdef: BomberLegends.Gameplay
│   │   ├── UI/                        # asmdef: BomberLegends.UI
│   │   ├── Meta/                      # asmdef: BomberLegends.Meta
│   │   ├── Bootstrap/                 # asmdef: BomberLegends.Bootstrap     (composition root)
│   │   └── Editor/                    # asmdef: BomberLegends.Editor        (Editor platform only)
│   ├── Tests/
│   │   ├── EditMode/                  # asmdef: BomberLegends.Tests.EditMode
│   │   └── PlayMode/                  # asmdef: BomberLegends.Tests.PlayMode
│   └── Vfx/
├── Plugins/                           # third-party (UniTask, etc.)
└── StreamingAssets/
```

**Inside `Scripts/`, organisation is feature-based, not type-based.** This is the rule from `CLAUDE.md` and
it matters more than the folder list above:

```
Scripts/Gameplay/
├── Match/            MatchRunner, MatchContext, MatchInstaller, MatchStateMachine
├── Player/           PlayerView, PlayerAnimator, PlayerIntentBinder
├── Bombs/            BombView, BlastView, BlastVfxController
├── Enemies/          EnemyView, EnemyViewFactory
├── Board/            BoardRenderer, TileView, DestructibleView, IsometricProjector
├── Pickups/          PickupView, PickupCollectPresenter
├── Camera/           MatchCameraRig
└── Vfx/              ScorePopupPool, ScreenShake
```

Each feature folder owns its views, its prefab wiring, and its presenters. **`Match/` is the only folder
that knows about all the others** — it is the feature composition root.

---

## 2. Assembly Definitions

Ten runtime assemblies plus tests. The graph is a strict DAG; **cycles are a build error, which is the point.**

| Assembly | Engine refs | References | Contains |
|---|---|---|---|
| `BomberLegends.Core` | **No** | — | `GridCoord`, `Direction`, `Tick`, `DeterministicRandom`, `FixedPoint` (reserved), small collections, result types |
| `BomberLegends.Simulation` | **No** | Core | The entire game rules layer. **The most important assembly in the project.** |
| `BomberLegends.Data` | Yes | Core, Simulation | ScriptableObject *classes*, event channel types, config → sim-struct bakers |
| `BomberLegends.Services` | Yes | Core, Data | `ISaveService`, `IAudioService`, `ISceneService`, `IAssetService`, `ISettingsService`, `IAnalyticsService` + implementations |
| `BomberLegends.Input` | Yes | Core, Simulation, Data | `IInputSource`, touch/gamepad/keyboard/replay sources, joystick widget backend |
| `BomberLegends.Meta` | Yes | Core, Data, Services | Progression, wallet, upgrades, unlocks, save model |
| `BomberLegends.Gameplay` | Yes | Core, Simulation, Data, Services, Input | `MatchRunner`, all views, pooling, VFX, camera |
| `BomberLegends.UI` | Yes | Core, Simulation, Data, Services, Meta | Screens, HUD, presenters, view interfaces |
| `BomberLegends.Bootstrap` | Yes | **all of the above** | `GameBootstrap`, `GameContext`, installers, scene flow |
| `BomberLegends.Editor` | Yes (Editor only) | all | Level editor tooling, validators, build scripts |

**Critical constraints, enforced by the compiler:**

- `Gameplay` and `UI` **do not reference each other.** They communicate through `Data` event channels.
  This is the rule that keeps UI free of gameplay logic (SKILL.md) and keeps the HUD replaceable.
- Nothing references `Bootstrap`. It is a leaf, and it is the only place `new` is called on services.
- `Simulation` cannot reference `Data` — configuration flows *in* as plain structs, baked by `Data`.
- `Editor` is `includePlatforms: ["Editor"]` and is never in a build.

**Why ten assemblies and not one:** incremental compile time. A change to a HUD label recompiles `UI`
(seconds), not the whole project. On a project of this size, this is the difference between a 2-second and a
25-second iteration cycle, and iteration speed is the project's stated first principle.

### Dependency graph

```
                            ┌──────────────────┐
                            │    Bootstrap     │  composition root — references everything
                            └────────┬─────────┘
              ┌──────────────────────┼──────────────────────┐
              ▼                      ▼                      ▼
        ┌──────────┐           ┌──────────┐           ┌──────────┐
        │ Gameplay │           │    UI    │           │   Meta   │
        └────┬─────┘           └────┬─────┘           └────┬─────┘
             │   ╲                  │   ╱                  │
             │    ╲   ┌─────────┐   │  ╱                   │
             │     ╲─▶│  Input  │◀──┘ ╱                    │
             │        └────┬────┘    ╱                     │
             ▼             ▼        ▼                      ▼
        ┌─────────────────────────────────────────────────────┐
        │                     Services                        │
        └──────────────────────────┬──────────────────────────┘
                                   ▼
        ┌─────────────────────────────────────────────────────┐
        │                       Data                          │   (ScriptableObjects, event channels)
        └──────────┬──────────────────────────────┬───────────┘
                   ▼                              ▼
        ┌─────────────────────┐        ┌─────────────────────┐
        │    Simulation       │───────▶│        Core         │   ← both: NO ENGINE REFERENCES
        └─────────────────────┘        └─────────────────────┘
```

Dependencies point **downward only**. Nothing below the line ever knows Unity exists.

---

## 3. Namespaces

Mirror the assembly and feature structure exactly — a file's namespace tells you its assembly, and therefore
its allowed dependencies, at a glance.

```
BomberLegends.Core
BomberLegends.Core.Collections

BomberLegends.Simulation                 // GameSimulation, SimulationState, SimulationConfig
BomberLegends.Simulation.Board
BomberLegends.Simulation.Bombs
BomberLegends.Simulation.Actors
BomberLegends.Simulation.Objectives
BomberLegends.Simulation.Events

BomberLegends.Data
BomberLegends.Data.Levels
BomberLegends.Data.Balance
BomberLegends.Data.Events                // ScriptableObject event channels

BomberLegends.Services.Save
BomberLegends.Services.Audio
BomberLegends.Services.Assets
BomberLegends.Services.Scenes
BomberLegends.Services.Settings

BomberLegends.Input

BomberLegends.Gameplay.Match
BomberLegends.Gameplay.Player
BomberLegends.Gameplay.Bombs
BomberLegends.Gameplay.Board
BomberLegends.Gameplay.Enemies
BomberLegends.Gameplay.Vfx

BomberLegends.UI.Hud
BomberLegends.UI.Screens
BomberLegends.UI.Common

BomberLegends.Meta.Progression
BomberLegends.Meta.Economy

BomberLegends.Bootstrap
BomberLegends.Editor.*
```

`#nullable enable` project-wide (SKILL.md requires nullable reference types).

---

## 4. Feature-Based Architecture — the Simulation Layer

The most important design in the project. Sketched at interface level only; no implementation.

### 4.1 Shape

```csharp
namespace BomberLegends.Simulation;

/// <summary>Authoritative, deterministic, engine-free game simulation for a single match.</summary>
public sealed class GameSimulation
{
    public SimulationState State { get; }
    public IReadOnlyList<SimEvent> PendingEvents { get; }   // drained by the view each frame

    public GameSimulation(in SimulationConfig config, in LevelLayout layout, uint seed);

    /// <summary>Advances exactly one fixed tick. The only mutation entry point.</summary>
    public void Tick(in PlayerIntent intent);

    /// <summary>Order-independent hash of authoritative state. Used by determinism tests and, later, netcode.</summary>
    public ulong ComputeStateHash();
}
```

`Tick` is the *only* public mutator. That single constraint gives replays, determinism tests, save-state
debugging, and a future server tick for free.

### 4.2 Internal systems (composition, not inheritance)

`GameSimulation.Tick` runs a fixed, ordered list of systems over shared state. Order is explicit and
documented, because in a grid game **order is gameplay**:

```
1. IntentSystem        consume PlayerIntent → desired direction / actions
2. MovementSystem      soft-grid movement, corner assist, occupancy
3. BombPlacementSystem capacity check, tile occupancy, spawn bomb
4. FuseSystem          decrement fuses, queue detonations
5. BlastSystem         BFS propagation, chain detonation, tile damage    ← order-critical
6. EnemySystem         patrol advance, contact tests
7. DamageSystem        resolve lethal overlaps for player + enemies
8. PickupSystem        collection, blast destruction of pickups
9. ObjectiveSystem     node count, exit state, win condition
10. TimerSystem        countdown, timeout condition
11. ScoreSystem        accumulate
```

Each system is a `static` or stateless class taking `ref SimulationState`. No virtual dispatch, no
allocation, trivially unit-testable in isolation, and the tick order is one readable list rather than
scattered `Update` methods with implicit ordering — which is the single biggest source of "why did that
happen" bugs in Unity gameplay code.

### 4.3 State

```csharp
public struct SimulationState
{
    public int Tick;
    public BoardState Board;         // flat TileType[] + damage byte[], indexed by (y * width + x)
    public PlayerState Player;
    public BombBuffer Bombs;         // fixed-capacity array, no List<T> growth
    public BlastBuffer Blasts;
    public EnemyBuffer Enemies;
    public PickupBuffer Pickups;
    public ObjectiveState Objectives;
    public MatchPhase Phase;
}
```

All fixed-capacity, all value types, no `List<T>`, no `Dictionary<K,V>`, **no allocation after
construction**. This is what makes the 0 B/frame target in `02-PROTOTYPE-SCOPE.md` §6 achievable by
construction rather than by later optimisation.

### 4.4 Simulation events

Simulation systems never call view code. They append value-type events to a preallocated buffer:

```csharp
public readonly struct SimEvent
{
    public readonly SimEventType Type;   // BombPlaced, BlastSpawned, BlockDestroyed, EnemyKilled,
    public readonly GridCoord Coord;     // PickupCollected, PlayerDied, ObjectiveProgressed, MatchEnded
    public readonly int EntityId;
    public readonly int Value;
}
```

The view drains `PendingEvents` once per frame and reacts (spawn VFX, play SFX, animate). Zero allocation,
zero coupling, and a complete match is reproducible from `(seed, layout, intent stream)` alone — which is
also exactly what a replay file and a network packet stream need.

**This is the seam that makes future multiplayer a feature instead of a rewrite.**

---

## 5. Scene Architecture

Three shipped scenes. Deliberately few — scenes are the worst merge-conflict surface in Unity.

| Scene | Load mode | Lifetime | Contents |
|---|---|---|---|
| `Bootstrap` | Single, index 0 | **Persistent, never unloaded** | `GameBootstrap`, `GameContext` owner, `AudioListener`, audio source pool, persistent loading-screen canvas, `EventSystem` |
| `Hub` | Additive | While in hub | Hub environment, hub UI canvas, `HubInstaller` |
| `Match` | Additive | While in match | Board root, entity view roots, HUD canvas, `MatchInstaller`, camera rig |

**Rules:**
- Exactly one `AudioListener` and one `EventSystem`, both in `Bootstrap`. Duplicates are a top-5 source of
  silent bugs.
- Scene content is **structure only**. Every prefab reference is serialized on an installer, never resolved
  by `Find`.
- The `Match` scene contains *no level data* — layout comes from a `LevelDefinition` ScriptableObject, so a
  designer retunes the map without opening or merging a scene.
- `Dev/` scenes exist for isolated feature testing and are excluded from build settings.

### Bootstrap flow

```
App launch
   │
   ▼  [Bootstrap.unity, build index 0]
GameBootstrap.Awake()
   │  Application.targetFrameRate = 60      (explicit — never leave this to the platform default)
   │  QualitySettings tier selection from device profile
   │  Screen.sleepTimeout, safe-area probe
   ▼
Compose services (in dependency order, all explicit `new`):
   ISettingsService → ISaveService → IAssetService → IAudioService → ISceneService → IAnalyticsService
   │
   ▼
await SaveService.LoadAsync()               (UniTask)
   │
   ▼
Build GameContext { Settings, Save, Assets, Audio, Scenes, Analytics, Progression, Wallet }
   │
   ▼
await AssetService.WarmupAsync()            (Addressables catalog + preload label "boot")
   │
   ▼
await SceneService.LoadAsync(SceneId.Hub)   (additive; installer receives GameContext)
   │
   ▼
Hide loading screen → HUB interactive
```

Total budget: **< 4 s cold start** (`02-PROTOTYPE-SCOPE.md` §6, Gate B).

**Editor convenience (required for iteration speed):** an editor script detects play-mode entry from any
scene, loads `Bootstrap` first, then re-loads the original scene additively. Without this, a developer
cannot press Play in `Match.unity`, and the whole team's iteration time doubles. This is a Milestone 0 task.

---

## 6. Service Layer

### 6.1 Composition, not a service locator

```csharp
namespace BomberLegends.Bootstrap;

/// <summary>Root object graph. Constructed once at boot, passed down explicitly.</summary>
public sealed class GameContext
{
    public ISettingsService  Settings    { get; }
    public ISaveService      Save        { get; }
    public IAssetService     Assets      { get; }
    public IAudioService     Audio       { get; }
    public ISceneService     Scenes      { get; }
    public IAnalyticsService Analytics   { get; }
    public IProgressionService Progression { get; }
    public IWalletService    Wallet      { get; }
}
```

**Strongly-typed properties, not `Resolve<T>()`.** No reflection (banned by `CLAUDE.md`), no runtime
resolution failures, full IDE navigation, and the dependency set of the whole game is one readable class.

MonoBehaviours receive it through **scene installers**: each additive scene has one `SceneInstaller`
MonoBehaviour with serialized references to its scene roots; `SceneService` calls `installer.Install(context)`
after load, and the installer pushes what each root needs. No `FindObjectOfType`, no static singletons in
gameplay code.

*Upgrade path:* if the object graph outgrows manual wiring (realistically past 25–30 services), adopt
**VContainer** — it is codegen-based and fast on IL2CPP. Do not adopt it for the slice; it is complexity
without payoff at this size.

### 6.2 Service contracts

```csharp
public interface ISaveService
{
    PlayerSaveData Data { get; }
    UniTask LoadAsync();
    UniTask SaveAsync();
    void MarkDirty();                     // batched; flushed on interval + on pause
}

public interface IAssetService                       // wraps Addressables; gameplay never sees Addressables
{
    UniTask<T> LoadAsync<T>(AssetReference reference) where T : Object;
    UniTask<GameObject> InstantiateAsync(AssetReference reference, Transform parent);
    void Release(Object handle);
    UniTask WarmupAsync(string label);
}

public interface IAudioService
{
    void PlaySfx(SfxDefinition definition, Vector3? worldPosition = null);
    void PlayMusic(MusicDefinition definition, float fadeSeconds = 1f);
    void SetBusVolume(AudioBus bus, float normalized01);
    void StopAll();
}

public interface ISceneService
{
    SceneId Current { get; }
    UniTask TransitionToAsync(SceneId target, ITransitionPayload? payload = null);
}

public interface IAnalyticsService                   // no-op implementation until M9
{
    void Track(string eventName, in AnalyticsPayload payload);
}
```

Every service is an interface so it can be replaced by a null/fake implementation in tests and in the WebGL
build (where analytics, IAP, and file I/O differ). `IAnalyticsService` exists from day one as a no-op
specifically so instrumentation calls can be written now and wired later without touching gameplay code.

---

## 7. Event System — Three Tiers, Deliberately

Using one event mechanism for everything is the most common Unity architecture mistake. Three tiers, with a
hard rule for each:

| Tier | Mechanism | Frequency | Use for | Never use for |
|---|---|---|---|---|
| **1. Simulation events** | `SimEvent` struct buffer, drained per frame | Per-tick, high volume | Bomb placed, block destroyed, entity died, blast spawned | Anything a designer needs to wire |
| **2. Event channels** | `ScriptableObject` assets with `event Action<T>` | Low, cross-feature | Match started/ended, coins changed, upgrade purchased, scene transition | Per-tick gameplay — allocation and indirection will bite |
| **3. Local C# events** | Plain `event` / `Action` | Any, within one feature | View ↔ presenter inside a single feature | Cross-assembly communication |

```csharp
namespace BomberLegends.Data.Events;

public abstract class EventChannel<T> : ScriptableObject
{
    private event Action<T>? _raised;
    public void Raise(in T payload) => _raised?.Invoke(payload);
    public void Register(Action<T> handler)   => _raised += handler;
    public void Unregister(Action<T> handler) => _raised -= handler;
}

[CreateAssetMenu(menuName = "Bomber Legends/Events/Match Ended")]
public sealed class MatchEndedChannel : EventChannel<MatchResult> { }
```

**Tier 2 is how `Gameplay` and `UI` communicate without referencing each other.** `MatchRunner` raises
`MatchEndedChannel`; the results screen listens. Neither assembly knows the other exists.

**Mandatory discipline:** every `Register` in `OnEnable` has an `Unregister` in `OnDisable`. `ScriptableObject`
event channels survive scene unloads and leak listeners into destroyed objects otherwise — this is the known
failure mode of the pattern and the reason a `#if UNITY_EDITOR` leak assertion on domain reload is part of
the base class.

---

## 8. Save System

**Requirement from `CLAUDE.md`: gameplay code must never know the storage implementation.** Two layers:

```
Meta / Gameplay
      │  reads and writes typed data only
      ▼
ISaveService  ── versioning, migration, dirty batching, autosave policy
      │
      ▼
ISaveRepository  ── raw persistence, platform-specific
      ├── FileSaveRepository        Android / iOS / Desktop  (atomic: write .tmp → File.Replace)
      ├── PlayerPrefsSaveRepository WebGL  (no reliable filesystem sync)
      ├── MemorySaveRepository      tests
      └── NakamaSaveRepository      later, if Q1 → multiplayer (server-authoritative)
```

```csharp
[Serializable]
public sealed class PlayerSaveData
{
    public int SchemaVersion;                 // migration key — present from the first release
    public long DataCoins;
    public int BombRangeLevel;
    public List<LevelRecord> Levels;
    public SettingsData Settings;
}
```

**Rules:**
- `SchemaVersion` exists from v1 with a migration chain (`Migrate_1_to_2`, …). Adding it later means
  shipping a version that cannot be upgraded, which permanently orphans early players' saves.
- **Atomic writes.** Write to a temp file, then replace. A phone killed mid-write must never corrupt a save.
- **Autosave on `OnApplicationPause(true)` and `OnApplicationFocus(false)`.** On Android this is often the
  *only* callback received before the process is killed. Missing it is the #1 cause of "the game lost my
  progress" reviews.
- Serialisation via `JsonUtility` for the slice (zero dependency, IL2CPP-safe, works everywhere). Its
  limitations — no dictionaries, no polymorphism, no `null` distinction on value types — are acceptable for
  a flat save model and must be respected in the DTO design. Escalate to Newtonsoft only if the model needs
  polymorphism.
- Save writes never block the main thread: `UniTask.RunOnThreadPool` on non-WebGL, synchronous on WebGL.

---

## 9. Input Architecture

**Unity Input System**, one `.inputactions` asset, and one abstraction that carries the entire design:

```csharp
namespace BomberLegends.Input;

public interface IInputSource
{
    PlayerIntent Sample(int tick);
}
```

```csharp
namespace BomberLegends.Simulation;

/// <summary>One tick of player will. Serialisable, replayable, and network-ready by construction.</summary>
public readonly struct PlayerIntent
{
    public readonly sbyte MoveX;         // quantised -100..100 — deterministic across platforms
    public readonly sbyte MoveY;
    public readonly IntentButtons Buttons;   // [Flags] Bomb, Special, Sprint
}
```

Implementations: `TouchInputSource` (virtual joystick), `GamepadInputSource`, `KeyboardInputSource`,
`ReplayInputSource` (deterministic tests, automated soak runs), and later `NetworkInputSource`.

**Quantising to `sbyte` is not premature optimisation** — it removes float divergence across CPUs, which is
what makes the determinism test in Gate B pass, and it makes an intent 3 bytes on the wire.

### The isometric input problem (Risk G6 — the highest feel risk in the project)

Handled entirely inside `TouchInputSource` and `MovementSystem`, in this order:

1. **Basis rotation.** Rotate the raw stick vector by −45° into grid space, so screen-up-right = grid North.
2. **Cardinal snap with hysteresis.** Snap to the nearest of 4 directions. Entering a new direction requires
   crossing a wider angle than leaving it (e.g. enter at 30°, leave at 50°) — this is what stops the
   character stuttering when the thumb sits near a diagonal.
3. **Input buffering (~120 ms).** A direction or bomb press issued slightly before it is legal is honoured
   when it becomes legal. This is the single largest perceived-responsiveness win available.
4. **Corner-cutting assist.** When moving along a lane and pushing perpendicular near a junction, apply a
   small lateral correction toward the lane centre so the player rounds the corner instead of catching it.
   This is what makes the difference between "tight" and "sticky", and it is *not* in the GDD.
5. **Deferred-turn tolerance.** Hold the requested turn for a few ticks after it becomes impossible rather
   than dropping it.

All five parameters live on a `MovementFeelConfig` ScriptableObject with `[Range]` sliders, tunable in play
mode. Milestone 1 exists specifically to tune these against real thumbs on a real phone.

---

## 10. UI Architecture

**Model–View–Presenter.** Views are MonoBehaviours holding references and nothing else; presenters are plain
C# classes holding all logic and zero Unity types beyond data.

```
Model (Simulation state / Meta data)
   │
   ▼
Presenter (plain C#, unit-testable)   ── e.g. HudPresenter, ResultsPresenter, UpgradePresenter
   │  IHudView interface
   ▼
View (MonoBehaviour)                  ── e.g. HudView: sets text, fills, colours
```

```csharp
public interface IHudView
{
    void SetTimer(int secondsRemaining);
    void SetLives(int lives);
    void SetScore(int score);
    void SetObjectiveProgress(int collected, int required);
    void SetBombCharges(int available, int capacity);
}
```

`HudPresenter` is tested without a scene. `HudView` contains no branching logic.

**Toolkit choice: uGUI for the slice, everywhere.** Rationale: the in-match HUD needs a virtual joystick,
radial cooldown fills, safe-area anchoring, and world-space-anchored damage popups — all mature, well-trodden
uGUI territory with predictable mobile performance. UI Toolkit is a strong fit for the *meta* screens
(data-driven lists, upgrade trees) and should be adopted at **M8** when the hub grows real content. Running
two UI systems before then is cost without benefit.

**Mobile-mandatory rules:**
- **Split canvases.** Static HUD elements on one canvas, per-frame-changing elements (timer, score, fills) on
  another. A single canvas rebuilds *every element* when any element changes — this alone can cost several
  milliseconds per frame on mobile.
- **Safe area.** A `SafeAreaFitter` on every screen root. Notches and gesture bars will otherwise eat the HUD.
- **`raycastTarget = false`** on every non-interactive `Graphic`. The default is `true` and it is pure waste.
- **Thumb zones.** Nothing critical to read in the bottom-left or bottom-right ~15% of the screen.
- **Screen stack.** `IScreenService` with push/pop, so back-button behaviour on Android is one implementation
  rather than per-screen ad-hoc logic.

---

## 11. Audio Architecture

Per `CLAUDE.md`: an `AudioMixer` with a strict bus hierarchy; **no `AudioSource` sets its own volume.**

```
Master
├── Music
├── Sfx
│   ├── Gameplay        (bombs, blasts, destruction)
│   └── Feedback        (pickups, objective progress)
├── UI
├── Voice               (reserved)
└── Ambience
```

- `SfxDefinition` ScriptableObject: clip variants (random selection avoids ear fatigue), volume, pitch range,
  bus, and — critically — **`maxConcurrent` and `minRetriggerInterval`**. A chain detonation destroying 12
  blocks in one tick would otherwise fire 12 identical one-shots in the same frame, producing a clipped,
  distorted mess and a CPU spike. The voice limiter is a *day-one* requirement, not polish.
- `AudioSource` pool (16 sources) in the persistent `Bootstrap` scene. Never `Instantiate` an `AudioSource`.
- Bus volume set through exposed mixer parameters with **logarithmic** conversion
  (`dB = 20 * log10(clamp(v, 0.0001, 1))`) — linear sliders on a dB mixer feel broken.
- Music: streaming load type, compressed in memory. SFX: decompress on load, short clips only.
- Mandatory: duck all audio on `OnApplicationPause` and respect the device silent switch on iOS.

---

## 12. Addressables Strategy

`Addressables` is wrapped behind `IAssetService` so gameplay code never references the Addressables API
directly — which keeps the option of changing loading strategy without touching features.

| Group | Packing | Location | Contents |
|---|---|---|---|
| `Boot_Local` | Pack Together | Local | Loading screen, fonts, core UI atlas, default audio |
| `Gameplay_Local` | Pack Together | Local | Player, bombs, blasts, tiles, core VFX — everything a match needs |
| `Levels_Remote` | Pack Separately | **Local now, remote-ready** | `LevelDefinition` assets + level-specific props |
| `Audio_Remote` | Pack Together By Label | Local now, remote later | Music (largest single asset class) |
| `Cosmetics_Remote` | Pack Separately | Remote (M9) | Skins, trails, bomb VFX variants |

**Rules:**
- Everything is **local** for the slice. The remote *configuration* exists from day one so flipping a group
  to remote is a settings change, not a refactor.
- Preload the `Gameplay_Local` label during the match loading screen; never load during `PLAYING`.
- Release handles explicitly on match teardown; an Addressables handle leak on mobile is an OOM crash,
  not a warning.
- **WebGL:** no synchronous loading, remote catalogue must be CORS-configured, and total first-load size is
  the binding constraint. Keep `Boot_Local` minimal.
- Per-platform texture variants (ASTC mobile / DXT desktop) via labels, configured before content grows —
  retrofitting variants across hundreds of assets is miserable.

**Explicitly banned:** the `Resources/` folder. It is loaded in full at startup, bloats the build, and
cannot be patched. `CLAUDE.md` allows it "if justified" — there is no justification in this project.

---

## 13. ScriptableObject Strategy

**Hard rule (SKILL.md): never store mutable runtime state in a ScriptableObject.** In the Editor those
mutations persist across play sessions and produce bugs that vanish on a fresh build — the worst class of bug
there is.

Three categories, distinguished by suffix so the rule is visible at the call site:

| Suffix | Purpose | Mutability | Example |
|---|---|---|---|
| `*Definition` | Content identity | Immutable | `EnemyDefinition`, `AbilityDefinition`, `SfxDefinition` |
| `*Config` | Tuning / balance | Immutable | `MovementFeelConfig`, `BombConfig`, `BlastConfig` |
| `*Channel` | Event channel | Stateless | `MatchEndedChannel` |

Runtime state lives in `SimulationState` (structs) and `Meta` (plain classes backed by the save).

### The bake step

`Simulation` cannot reference `Data` (no engine refs). So at match start, `Data` **bakes** ScriptableObject
configuration into plain structs:

```csharp
namespace BomberLegends.Data.Balance;

public sealed class MatchConfigAsset : ScriptableObject
{
    [SerializeField, Min(1)] private int _startingBombCapacity = 1;
    [SerializeField, Min(1)] private int _startingBlastRange = 2;
    [SerializeField, Min(0.1f)] private float _fuseSeconds = 3f;
    [SerializeField, Tooltip("0 = classic capacity model (recommended). >0 enables GDD §6.2.3 cooldown gating.")]
    private float _bombCooldownSeconds;

    public SimulationConfig Bake() => new(_startingBombCapacity, _startingBlastRange,
                                          SecondsToTicks(_fuseSeconds), SecondsToTicks(_bombCooldownSeconds));
}
```

The simulation receives a plain struct with tick counts, not seconds and not Unity objects. Note that the
contested 5 s cooldown from `01-ANALYSIS.md` §3 is a single serialized field defaulting to 0 — the design
argument is settled by a playtest and an Inspector value, not by a code change.

**Inspector quality is a requirement, not a nicety** (`CLAUDE.md`): `[Header]`, `[Tooltip]`, `[Range]`,
`[Min]` on every designer-facing field, and an `OnValidate` that clamps invalid combinations.

---

## 14. Object Pooling Strategy

Pool everything spawned during a match. `UnityEngine.Pool.ObjectPool<T>` is the base; wrapped so pools are
declared as data, prewarmed at load, and never grow during `PLAYING`.

| Pooled object | Prewarm | Peak driver |
|---|---|---|
| `BombView` | 8 | Max simultaneous bombs |
| `BlastSegmentView` | 64 | A chain detonation of 8 bombs × range 4 × 4 arms |
| `BlockDestructionVfx` | 32 | Blocks destroyed in one tick |
| `PickupView` | 24 | Coins on the board |
| `ScorePopup` | 16 | Simultaneous scoring events |
| `EnemyView` | 12 | Level enemy count + spawn headroom |
| `AudioSource` | 16 | Voice limiter ceiling |

**Rules:**
- **Prewarm during the loading screen.** A pool that grows mid-match is just `Instantiate` with extra steps.
- Views reset all state in `OnGet` — leftover tween/particle/colour state from the previous use is the
  classic pooling bug.
- Pool capacities are serialized fields on the match installer, so a profiler run tunes them without a
  code change.
- `maxSize` is enforced and an over-budget request logs an error in the Editor (SKILL.md: fail loudly in
  Editor, gracefully in builds) — that error is the signal that a design change broke a budget.

---

## 15. Data Flow

### Per frame

```
  ┌────────────────────────────────────────────────────────────────────────┐
  │ Unity Update()                                                          │
  │                                                                         │
  │   IInputSource.Sample(tick) ──▶ PlayerIntent (3 bytes)                  │
  │            │                                                            │
  │            ▼                                                            │
  │   MatchRunner: accumulator loop                                         │
  │     while (acc >= 33.3ms):                                              │
  │         GameSimulation.Tick(intent)   ← pure C#, no allocation          │
  │         acc -= 33.3ms                                                   │
  │            │                                                            │
  │            ├──▶ SimulationState (authoritative)                         │
  │            └──▶ PendingEvents (SimEvent[])                              │
  │                      │                                                  │
  │            ┌─────────┴──────────┬──────────────────┐                    │
  │            ▼                    ▼                  ▼                    │
  │      ViewSynchroniser      AudioReactor       HudPresenter              │
  │      (spawn/despawn        (IAudioService,    (IHudView)                │
  │       pooled views,         voice-limited)                              │
  │       interpolate @alpha)                                               │
  └────────────────────────────────────────────────────────────────────────┘
```

The simulation never pushes to views. Views pull state and drain events. One direction, always.

### Per match

```
HUB  ──"PLAY"──▶  SceneService.TransitionToAsync(Match, LevelId)
                        │
                        ▼
                  MatchInstaller.Install(GameContext)
                        │  LevelDefinition ─┐
                        │  MatchConfigAsset ┼──▶ Bake() ──▶ SimulationConfig + LevelLayout (structs)
                        │  ProgressionService (owned upgrades) ─┘
                        ▼
                  new GameSimulation(config, layout, seed)   ← the only place the sim is constructed
                        │
                     [ PLAYING ]
                        │
                        ▼  MatchEnded SimEvent
                  MatchResult { won, timeRemaining, nodes, coins, score }
                        │
                        ├──▶ MatchEndedChannel.Raise()  ──▶  ResultsPresenter (UI)
                        ├──▶ IWalletService.Add(coins)   ──▶  ISaveService.MarkDirty()
                        └──▶ IAnalyticsService.Track("match_ended", …)
```

Note that `MatchResult` crosses the `Gameplay` → `UI` boundary through a `Data` event channel — the two
assemblies still do not reference each other.

---

## 16. Performance Rules (binding)

Derived from `CLAUDE.md`, `SKILL.md`, and the mobile budget in `01-ANALYSIS.md` §7. These are checked at
code review.

1. **Zero allocation in `PLAYING`.** No `new` on reference types, no LINQ, no closures capturing state, no
   `foreach` over interfaces, no string concatenation, no boxing. `SimulationState` is preallocated.
2. **String building:** all HUD numbers use cached string arrays or `TMP_Text.SetText(format, value)` — never
   `"Score: " + score` (allocates every frame; a top-3 mobile GC source).
3. **No `Find`, `FindObjectOfType`, `GetComponent` in `Update`.** Cache in `Awake`, prefer serialized
   references, `TryGetComponent` only outside hot paths.
4. **No reflection, no `dynamic`, no boxing of enums** (`Dictionary<Enum,T>` boxes without a custom comparer).
5. **Sprite atlases mandatory.** One draw-call target: < 120 per frame. Isometric depth sorting via an
   explicit sort key from grid coordinates, not Unity's default transparency sort.
6. **No real-time 2D lights for neon.** Emissive sprite channel + one tuned bloom pass in the URP 2D renderer.
   This is a locked rendering rule (`01-ANALYSIS.md` §7) that the art pipeline depends on.
7. **`Application.targetFrameRate = 60` explicitly.** Never render above the display target — it burns
   battery and triggers thermal throttling for zero perceived benefit.
8. **Profile on a real low-tier device every milestone.** Editor profiling on a desktop is not evidence.

---

## 17. Testing Architecture

| Layer | Test mode | What is tested |
|---|---|---|
| `Core`, `Simulation` | **EditMode**, headless, milliseconds | All rules: blast propagation, chain detonation, occupancy, win/lose, timer, scoring. **Target ≥ 90% coverage.** |
| `Meta` | EditMode | Upgrade costs, wallet arithmetic, save migration chains |
| `Services` | EditMode with fakes | Save round-trip, versioning, atomic write behaviour |
| `UI` presenters | EditMode with mock views | Formatting, state transitions |
| Integration | **PlayMode** | Bootstrap → hub → match → results; scene transitions; pooling leak checks |
| Determinism | EditMode | Replay a recorded intent stream 1000× ⇒ identical `ComputeStateHash()` |
| Soak | PlayMode, automated | `ReplayInputSource` drives 10 000 random-input ticks; assert no exception, no allocation growth |

The determinism and soak tests are the ones that catch the expensive bugs. Both are only possible because of
decision **D1** — which is the concrete return on the "no engine references" constraint.

---

## 18. Does This Scale to a Live-Service Mobile Game?

Assessed honestly against the stated ambition.

| Requirement | Ready? | Notes |
|---|---|---|
| Content cadence (new levels without a client update) | **Yes** | `LevelDefinition` assets in a remote Addressables group |
| Server-authoritative economy | **Yes, by substitution** | `ISaveRepository` → `NakamaSaveRepository`; no gameplay change |
| Leaderboards / ranked | **Yes** | Score already exists; Nakama leaderboards are a service addition |
| Multiplayer (PvP or co-op) | **Prepared, not built** | Deterministic tick + `PlayerIntent` stream = the two hard prerequisites are already satisfied |
| Live balancing without a client patch | **Yes, with one addition** | Remote-config override applied at `Bake()` — a single seam |
| A/B testing | **Yes** | `SimulationConfig` is constructed per match from data |
| Cosmetics / skins | **Yes** | Remote Addressables group; views are already data-driven |
| Seasons / events | **Partially** | Needs a content-schedule service; no architectural blocker |
| Analytics | **Yes** | `IAnalyticsService` no-op exists from day one; call sites written now |
| Anti-cheat | **Deferred, feasible** | Deterministic sim allows server-side replay validation — a major structural advantage |

**Honest caveat:** this architecture makes the *client* scale to live-service. It does not build the backend,
the CI/CD, the store integration, the content tooling, or the live-ops process — all of which are real
projects and none of which should start before `02-PROTOTYPE-SCOPE.md` Gate A passes.

Next: `04-ROADMAP.md`.
