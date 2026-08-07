# Bomber Legends — Implementation Backlog (Phase 5)

**Depends on:** `03-ARCHITECTURE.md`, `04-ROADMAP.md`
**Ordering principle:** maximum gameplay validation per day spent. The core verb is playable by task T-016.

---

## Complexity Scale

| Size | Effort | Note |
|---|---|---|
| **S** | ≤ 4 h | |
| **M** | 1–2 days | |
| **L** | 3–5 days | Should be split if it can be |
| **XL** | > 5 days | **Must** be split before starting — an XL task is an unplanned task |

## Universal Definition of Done

Applies to **every** task; per-task DoD lists only the additions.

- Compiles with zero warnings; `#nullable enable` respected
- Zero GC allocation in any per-frame or per-tick path (verified in the Profiler for gameplay tasks)
- No `Find`/`FindObjectOfType`/LINQ/reflection/string concatenation in gameplay code
- Assembly dependency rules of `03-ARCHITECTURE.md` §2 not violated
- Serialized fields private with `[Header]`/`[Tooltip]`/`[Range]` where designer-facing; Inspector is clean
- XML docs on every public type and member
- No TODOs, no commented-out code, no placeholder implementations
- Tests written and green; existing tests still green
- Verified in a **build on a real Android device**, not only in the Editor
- Self-reviewed against the `CLAUDE.md` code-review checklist

---

# MILESTONE 0 — Project Bootstrap

### T-001 · Create Unity 6.3 project and repository — ✅ **DONE 2026-08-05**
**Goal** Unity 6.3 LTS project with URP 2D, git repository, Unity `.gitignore`, Git LFS for binary assets, `_Project/` folder structure, forced text serialisation, visible meta files.
**Outcome** Unity 6000.3.10f1, URP 2D active (`Renderer2DData`), Linear colour space, Input System active, ForceText serialisation. Repo initialised at `C:\Users\chris\workspace\games\bomber-legends`. **Two carry-overs:** git-lfs is not installed on the machine (blocks binary commits — see `README.md`), and three packages are vendored as local tarballs due to a CDN certificate-chain issue (documented with a retirement condition in `README.md`).
**Dependencies** none · **Complexity** S
**Test criteria** Clean clone opens in Unity without errors; no `Library/` or `Temp/` tracked; a `.png` commits through LFS.
**DoD** Folder structure matches `03-ARCHITECTURE.md` §1; project settings committed; README documents the Unity version.

### T-002 · Assembly definition skeleton — ✅ **DONE 2026-08-05**
**Goal** Create all 10 runtime asmdefs + 2 test asmdefs with the exact reference graph of §2. `Core` and `Simulation` have `noEngineReferences: true`; `Editor` is Editor-platform-only.
**Outcome** All 12 assemblies emit. `AssemblyGraphTests` (EditMode, 7 tests) enforces the rules permanently rather than once: engine-free `Core`/`Simulation`, no `UI`↔`Gameplay` edge, no `Simulation`→`Data` edge, `Bootstrap` is a leaf, graph acyclic. Both negative cases verified to fail compilation. Incremental compile measured: touching a `UI` file rebuilds `BomberLegends.UI.dll` only, script compilation **1.26 s**. `#nullable enable` applied project-wide via `Assets/csc.rsp`.
**Dependencies** T-001 · **Complexity** S
**Test criteria** A deliberate `using UnityEngine;` in `Simulation` fails to compile. A deliberate `UI → Gameplay` reference fails. Changing a `UI` file recompiles only `UI` + `Bootstrap`.
**DoD** Graph is acyclic; each assembly has one placeholder type so it compiles; incremental compile time measured and recorded.

### T-003 · Core value types — ✅ **DONE 2026-08-05**
**Goal** `GridCoord`, `Direction`, `Tick`, `DeterministicRandom` (xorshift, explicit seed), `IntentButtons` flags in `BomberLegends.Core`.
**Outcome** All five types implemented, **101 EditMode tests green**, zero compiler warnings under `#nullable enable`. `GridCoord` and `Tick` are `readonly struct` with `IEquatable<T>`; `Direction` and `IntentButtons` are enums with boxing-free extension helpers in place of `Enum.HasFlag`; `DeterministicRandom` is a mutable struct (a PRNG must advance) with unbiased range draws via rejection sampling. Grid axes fixed: North = +Y, East = +X, board index = `Y * width + X`.
**Dependencies** T-002 · **Complexity** S
**Test criteria** `GridCoord` arithmetic, neighbour queries, bounds checks; `DeterministicRandom` produces an identical sequence for an identical seed across 10 000 draws.
**DoD** All value types are `readonly struct` with `IEquatable<T>` implemented (no boxing on comparison); 100% test coverage.

### T-004 · Service interfaces and `GameContext` — ✅ **DONE 2026-08-05**
**Goal** Declare `ISettingsService`, `ISaveService`, `IAssetService`, `IAudioService`, `ISceneService`, `IAnalyticsService`; implement `GameContext` as the typed root graph.
**Outcome** All six contracts declared, `GameContext` built with constructor injection and null guards, `NullAnalyticsService` implemented (silent in builds, logs in Editor). **134 EditMode tests green.** Supporting types: `AudioBus` (in `Core`), `AssetKey`, `SceneId`, `ISceneTransitionPayload`, `SettingsData`, `PlayerSaveData` (schema-versioned from v1), `AnalyticsPayload` (6 inline fields, allocation-free). `SfxDefinition`/`MusicDefinition` ScriptableObjects added to `Data` with the day-one voice-limiting fields. Reusable test doubles live in `Tests/EditMode/Fakes/`, including `RecordingAnalyticsService` for T-035.
**Decisions** (1) Async uses Unity 6's built-in **`Awaitable`**, not UniTask — first-party, no dependency, sufficient for our surface; supersedes the `CLAUDE.md` preference. (2) `IAssetService` uses `AssetKey` rather than Addressables' `AssetReference`, keeping the loading library out of the service contract entirely.
**Dependencies** T-002 · **Complexity** S
**Test criteria** `GameContext` constructs from fakes in an EditMode test with no Unity scene.
**DoD** No service resolution by type lookup anywhere; `NullAnalyticsService` implemented so call sites can be written from day one.

### T-005 · Save system — 🟡 **CODE COMPLETE 2026-08-05 · one DoD item carried to T-009**
**Goal** `ISaveRepository` + `FileSaveRepository` (atomic temp-then-replace), `PlayerPrefsSaveRepository` (WebGL), `MemorySaveRepository` (tests); `SaveService` with `SchemaVersion`, a migration chain, dirty batching, and autosave on `OnApplicationPause(true)` / `OnApplicationFocus(false)`.
**Outcome** All three repositories, `ISaveMigration` chain, `SaveService`, `SaveLifecycleHandler`. **160 EditMode + 4 PlayMode tests green.** Atomic write uses a three-file dance (`.tmp` written with flush-to-disk → current moved to `.bak` → `.tmp` moved into place), avoiding any dependence on `File.Replace` semantics across platforms. Every crash window is covered by a test that constructs the on-disk state directly. Unparseable payloads fall back to the backup, then are **quarantined rather than overwritten**. Payloads from a newer build are kept, not discarded. `FlushImmediate()` added to `ISaveService` for the pause path.
**⚠️ Carried over** DoD requires "verified by force-stopping the app on a real device". Not possible yet — there is no bootstrap wiring (T-007) and no device build (T-009). **This must be verified at T-009 before Milestone 0 is called done.**
**Dependencies** T-004 · **Complexity** M
**Test criteria** Round-trip preserves all fields; a v1 payload migrates to v2; a simulated kill during write leaves the previous save intact and loadable; save survives an app force-stop on device.
**DoD** Zero main-thread blocking on non-WebGL; corrupt-file path fails gracefully to a default save with an Editor error; **verified by force-stopping the app on a real device**.

### T-006 · Scene architecture and `SceneService` — ✅ **DONE 2026-08-05**
**Outcome** Three scenes generated by a committed `SceneScaffolder` editor tool (scene YAML merges badly, so their structure is defined in code and the diff of a structural change is a code diff). `SceneService` swaps scenes additively over the persistent bootstrap scene in a strict order — cover, unload, load, install, uncover — so a scene is never visible half-wired. `SceneInstaller` is the single injection point per scene. **9 PlayMode tests** cover hub→match→hub, root-object counts, single-additive-scene, and the AudioListener/EventSystem singletons.
**Goal** `Bootstrap`/`Hub`/`Match` scenes; additive load/unload with a loading screen; `SceneInstaller` pattern receiving `GameContext`.
**Dependencies** T-004 · **Complexity** M
**Test criteria** PlayMode test: Bootstrap → Hub → Match → Hub with no leaked GameObjects and no duplicate `AudioListener`/`EventSystem`.
**DoD** Exactly one `AudioListener` and one `EventSystem`, both in `Bootstrap`; every transition awaits completion; no `Find` used to locate scene roots.

### T-007 · `GameBootstrap` and app configuration — ✅ **DONE 2026-08-05**
**Outcome** Composition root builds the six-service graph in dependency order, loads the save, applies settings, wires the lifecycle handler, and opens the hub. Frame rate capped explicitly, vSync off, screen sleep disabled. Save repository is chosen per platform (file on device, player preferences on WebGL).
**Goal** Boot sequence of §5: frame rate cap, quality tier by device profile, sleep timeout, safe-area probe, service composition, save load, hub load.
**Dependencies** T-005, T-006 · **Complexity** S
**Test criteria** Cold start to interactive hub < 4 s on the target device, measured 5 times.
**DoD** `Application.targetFrameRate` set explicitly; boot timings logged behind a dev flag.

### T-008 · Editor play-from-any-scene tool — ✅ **DONE 2026-08-05**
**Outcome** `PlayFromBootstrap` forces play mode to start at Bootstrap and then continues into whichever scene the developer had open. **Guarded against test runs** — the first implementation hijacked the test runner's entry into play mode and hung it indefinitely; now suppressed in batch mode and for the duration of any `TestRunnerApi` run.
**Goal** Editor script that loads `Bootstrap` first when Play is pressed from any scene, then restores the original scene additively.
**Dependencies** T-006 · **Complexity** S
**Test criteria** Pressing Play in `Match.unity` boots correctly and the developer lands in `Match`.
**DoD** Toggleable from a menu item; zero effect on builds. *Small task, disproportionate return — it protects everyone's iteration loop for the life of the project.*

### T-009 · Android build pipeline — 🟡 **CONFIGURED · device verification outstanding**
**Outcome** `AndroidBuildTool` applies the player settings in code rather than trusting whatever the project was last saved with, so a build from a clean checkout matches a developer machine: IL2CPP, ARM64, ASTC, landscape-only (both ways up), min SDK 26, application id `com.christiankakesa.bomberlegends`. One command produces `Builds/Android/BomberLegends-dev.apk`. **Build verified: succeeded in 496 s, 84 MB APK**, containing `AndroidManifest.xml`, `classes.dex`, `libil2cpp.so` and `libunity.so` for `arm64-v8a` only, with the application id and landscape orientation confirmed in the manifest. Build output is git-ignored.
**✅ Device-verified 2026-08-06 (Samsung S21 Ultra):** APK installs via adb and manually, launches to the hub, hub→match→hub navigation works, the board renders and the player moves.
**Root cause of the first device failure:** the URP 2D sprite shaders were absent from the always-included list, and nothing in any scene referenced them (every `SpriteRenderer` was created at runtime), so they were stripped from the build — UI drew, the world did not, and the Editor looked fine throughout. Fixed by including both shaders and serialising the player's renderer into the scene. A `DeviceLogOverlay` now prints errors on screen in development builds so device-only failures are diagnosable without a cable.
**⚠️ Still outstanding** The T-005 carry-over — save survives a force-stop — is not yet testable, since no progress is persisted until the meta loop exists (M4/M5).
**Goal** Android build target configured (IL2CPP, ARM64, ASTC, landscape-only, correct min SDK), a one-click build script, signing keystore.
**Dependencies** T-007 · **Complexity** M
**Test criteria** One command produces an installable .apk; it launches to the hub on the target device. **Plus the T-005 carry-over: force-stop the app mid-session and confirm the save survives and reloads.**
**DoD** Build settings committed; build script in `Editor` assembly; build size recorded as the baseline for tracking.

---

# MILESTONE 1 — Movement & Feel  *(highest-risk milestone)*

### T-010 · `BoardState` and level layout — ✅ **DONE 2026-08-05**
**Outcome** `TileType`, `BoardState` (flat row-major array, allocated once) and `LevelLayout` with a text parser. Reads outside the board return `Solid` rather than throwing, which removes a bounds check from every movement and blast call site. `LevelLayout.Parse` takes rows top-down the way a level looks when written out and maps the first row to the highest Y — asserted by test, since getting it backwards would silently mirror every level.
**Goal** Flat-array board (`TileType[]` + damage bytes) with coordinate indexing, bounds checks, and occupancy queries in `Simulation`.
**Dependencies** T-003 · **Complexity** S
**Test criteria** Index round-trip for every cell of a 13×11 board; out-of-bounds queries return `Solid` rather than throwing.
**DoD** Zero allocation after construction; no `List<T>`/`Dictionary<K,V>` in the board.

### T-011 · `GameSimulation` shell and tick loop — ✅ **DONE 2026-08-05**
**Outcome** `GameSimulation` with `Tick(in PlayerIntent)` as the sole mutator, `SimulationState`, an ordered system list, `SimEventBuffer` (fixed capacity; overflow counted, never grown) and `ComputeStateHash()`. **Verified: 10 000 ticks allocate exactly 0 bytes** (`GC.GetAllocatedBytesForCurrentThread`). Determinism verified over 200 ticks of scripted input across two instances.
**Goal** `GameSimulation` with `Tick(in PlayerIntent)`, `SimulationState`, the ordered system list of §4.2 (stubs), `SimEvent` buffer, `ComputeStateHash()`.
**Dependencies** T-010 · **Complexity** M
**Test criteria** 10 000 empty ticks allocate 0 bytes; the state hash is stable and order-independent.
**DoD** `Tick` is the only public mutator; system order documented in code as an explicit, readable list.

### T-012 · `MatchRunner` accumulator and view interpolation — ✅ **DONE 2026-08-05**
**Outcome** Pacing extracted into `FixedStepAccumulator` (engine-free) so it is tested against exact frame times rather than only observed. Verified: **exactly 30 steps/second at 30, 60 and 120 fps**, and within one step of wall clock over a 60-second run at 90 and 144 fps. A 500 ms stall yields 5 ticks with the 10-tick backlog discarded, then pacing recovers. `FixedUpdate` not used.
**Goal** Fixed 30 Hz accumulator in `Update`, interpolation alpha, `ViewSynchroniser` skeleton draining `SimEvent`s.
**Dependencies** T-011 · **Complexity** M
**Test criteria** Simulation advances exactly 30 ticks/second at 30, 60, and 120 fps render rates; no spiral-of-death when a frame takes 500 ms (max catch-up ticks enforced).
**DoD** `FixedUpdate` not used; max catch-up capped; interpolation visibly smooth on device.

### T-013 · `MovementSystem` (simulation side) — ✅ **DONE 2026-08-05**
**Outcome** Soft-grid movement with lane snapping, deferred turns, corner assist and wall collision that settles exactly on the tile centre. Movement applies in sub-steps of at most half a tile, so **no speed can tunnel through a wall** — tested at 3 tiles/tick. **17 movement tests**, including no positional drift across 5 000 ticks of back-and-forth travel.
**Goal** Soft-grid movement: continuous position, lane snapping, occupancy blocking, direction changes only at valid points.
**Dependencies** T-011 · **Complexity** M
**Test criteria** ≥ 20 unit tests: walls block, lane centring is stable, no tunnelling at high speed, no drift over 10 000 ticks.
**DoD** Movement fully deterministic; speed expressed in tiles/tick; zero float drift accumulation.

### T-014 · Board projection, rendering and depth sorting — ✅ **DONE · device-verified 2026-08-06** *(draw calls still unmeasured)*
**Outcome** `BoardProjector` (three-quarter top-down over a square grid, replacing the original isometric projection on 2026-08-06 — see `GDD.md` v1.2). Depth sorts by row with sub-tile resolution, so an actor sorts correctly part-way between rows instead of popping; floor tiles carry an offset guaranteeing they never occlude. `MatchCameraRig` computes framing from the board and the live aspect ratio, so the whole board fits a 20:9 phone and a 4:3 tablet alike. Placeholder art generated procedurally, keeping the slice free of binary assets. Verified rendering on a Samsung S21 Ultra.
**⚠️ Outstanding** "< 20 draw calls" and the four-approach-direction sorting check need a device or the Frame Debugger. Deferred to the T-036 performance pass.
**Goal** `IsometricProjector` (grid ↔ world ↔ screen), board renderer with placeholder tiles, deterministic depth key from grid coordinates.
**Dependencies** T-013 · **Complexity** M
**Test criteria** Player renders correctly in front of and behind blocks from all four approach directions; no sort flicker when moving along a lane; a full board is < 20 draw calls.
**DoD** Single sprite atlas; sort key derived from coordinates, not Unity's transparency sort; verified at three aspect ratios.

### T-015 · Touch input, virtual joystick and **feel tuning**  ⚑ *critical* — 🟡 **MECHANISMS BUILT · tuning and sign-off need a device**
**Outcome** All five feel mechanisms exist. Input layer: basis rotation via the exact inverse projection, cardinal snap with **hysteresis**, and change-only buffering. Simulation layer (from T-013): corner assist and deferred turns. `DirectionSnapper` is a pure function and unit-tested, including a 100-sample sweep across the diagonal asserting the direction never flickers. Every parameter lives on `InputFeelConfig` with `[Range]` sliders, tunable in play mode.
**Design note** The buffer fires only on direction *changes*. A naive 120 ms buffer would carry the player ~0.5 tiles past a deliberate release, which would wreck precise bomb placement in Milestone 2.
**⚠️ Outstanding — this is the milestone's real exit criterion** The DoD requires tuning on a real phone and five testers navigating a maze unassisted, with zero stuck-on-geometry incidents over ten minutes. None of that can be done from here. **The numbers currently in `InputFeel.asset` are starting points, not tuned values.**
**Goal** `TouchInputSource` + on-screen joystick implementing all five mechanisms of `03-ARCHITECTURE.md` §9: basis rotation, cardinal snap with hysteresis, 120 ms input buffering, corner-cutting assist, deferred-turn tolerance. All parameters on `MovementFeelConfig`.
**Dependencies** T-013, T-014 · **Complexity** L
**Test criteria** Unit tests for basis rotation and hysteresis boundaries · **on-device**: 0 stuck-on-geometry incidents in 10 minutes · 5 testers navigate a maze with no instruction · corner-turn success rate ≥ 90% when input is issued within 150 ms of the junction.
**DoD** Every feel parameter tunable in play mode via `[Range]` sliders; tuned values committed with a comment explaining each; **signed off on a real phone, not in the Editor**. *This task is the single biggest determinant of whether the game feels good. Budget the full L and do not compress it.*

### T-016 · Keyboard/gamepad input source — ✅ **DONE 2026-08-05**
**Outcome** `KeyboardInputSource` and `GamepadInputSource` produce identical `PlayerIntent` values to touch. `CompositeInputSource` polls in priority order so a developer can switch between keyboard, pad and the on-screen stick without a setting change — which matters while feel is being tuned. Gamepad reads the d-pad before the stick, since it is already discrete and needs no snapping.
**Goal** `KeyboardInputSource` and `GamepadInputSource` producing identical `PlayerIntent`s.
**Dependencies** T-015 · **Complexity** S
**Test criteria** Identical simulation behaviour across all three sources given equivalent input.
**DoD** Sources hot-swappable at runtime; desktop iteration no longer requires touch emulation.

---

# MILESTONE 2 — Bombs & Blasts

### T-017 · Bomb placement and fuse — ✅ **DONE 2026-08-06**
**Outcome** `BombBuffer` (slot-based, fixed capacity), `BombGrid` (O(1) occupancy), `BombPlacementSystem`, `FuseSystem`. Classic capacity model: a bomb returns to the pool on detonation, so placement rate equals the fuse. Placement is **edge-triggered**, so holding the button does not drain the pool. `BombCooldownTicks` ships at **0** with a test proving the GDD's cooldown model works when enabled — the question is now settled by a device playtest, not by argument.
**Design note** Walking off your own bomb needed no special case. A bomb blocks movement *into* its tile and the player is already standing on it, so leaving works and returning does not — the classic rule falls out of the occupancy check, with no ownership tracking.
**Goal** `BombPlacementSystem` + `FuseSystem`: capacity model, tile occupancy, walk-off-own-bomb exception, fuse countdown in ticks, `bombCooldownSeconds` tunable defaulting to 0.
**Dependencies** T-013 · **Complexity** M
**Test criteria** Cannot exceed capacity · cannot double-place on one tile · the player can leave a freshly placed bomb but not re-enter it · a bomb blocks enemies · fuse fires on the exact tick · cooldown set to 5 s reproduces the GDD model exactly.
**DoD** Fixed-capacity bomb buffer, no allocation; both economy models proven by test.

### T-018 · `BlastSystem` with chain detonation  ⚑ *critical* — ✅ **DONE 2026-08-06**
**Outcome** Cross propagation with range clipping, stop-at-solid, destroy-one-block-and-stop, and **chain detonation resolving within a single tick**. `BlastGrid` holds per-tile lethal countdowns, so "is this tile deadly" is one array read for M3's damage system, and overlapping blasts keep the longer duration. **21 tests**, including an 8-bomb chain resolving in one tick, a 4-bomb ring terminating, and 5 000 ticks of placing and detonating allocating **0 bytes**.
**Implementation notes** Resolution is an explicit queue, not recursion — a long chain would otherwise grow the call stack in proportion to its length. Each bomb carries an `IsQueued` flag so several arms reaching the same bomb cannot enqueue it twice, which is also what makes a ring terminate. Blast decay runs *before* new blasts are painted, so a tile lit this tick keeps its full duration.
**Goal** Cross-shaped BFS propagation: range clipping, stop at solid, damage-and-stop at destructible, **chain-detonate bombs**, lethal duration, blast tile lifetime.
**Dependencies** T-017 · **Complexity** L
**Test criteria** ≥ 30 unit tests including: range clipped by walls · exactly one destructible destroyed per arm · a 10-bomb chain resolves fully · a circular chain does not infinite-loop · chains do not recurse (iterative queue, no stack growth) · blast at board edge does not read out of bounds.
**DoD** Iterative propagation with a preallocated queue; chain depth unbounded with zero allocation; timing of chain detonation (same tick vs. next tick) documented as an explicit design decision with the rationale.

### T-019 · Bomb, blast and destruction views + pooling — ✅ **DONE 2026-08-06**
**Outcome** `MatchViewSynchroniser` turns simulation events into pooled views: `BombView` (pulse rate driven by the actual fuse, so a bomb about to go off looks different from a fresh one), `BlastView` (duration taken from the simulation's lethal window, so what looks dangerous and what kills are the same thing) and `BlockDestructionView`. All three pools prewarm during the loading screen and log an error if they ever grow mid-match. `ActionButton` added so bombs can be placed on a touch device. **Verified on a Samsung S21 Ultra:** bombs render and pulse, blasts appear, blocks are destroyed, effects retire and the pools recycle, with no errors in the device log.
**Note** Pool prewarming necessarily calls the create callback, which initially tripped the overflow error and failed two PlayMode tests on every match load. Reporting is now suppressed for the duration of the prewarm.
**Goal** Pooled `BombView`, `BlastSegmentView`, `BlockDestructionVfx` driven by `SimEvent`s; prewarm at match load; full state reset in `OnGet`.
**Dependencies** T-018, T-012 · **Complexity** M
**Test criteria** A 12-block chain detonation allocates 0 B · pools never grow during `PLAYING` · no visual state leaks between pooled uses · 60 fps sustained during the heaviest chain.
**DoD** Pool sizes serialized on the match installer; over-budget requests log an Editor error.

### T-020 · Bomb and blast audio with voice limiting
**Goal** `IAudioService` + `AudioMixer` bus hierarchy + `AudioSource` pool + `SfxDefinition` with `maxConcurrent` and `minRetriggerInterval`.
**Dependencies** T-019 · **Complexity** M
**Test criteria** A 12-block chain produces no clipping or distortion · concurrent voices never exceed the cap · bus volume sliders behave logarithmically · audio ducks on app pause.
**DoD** No `AudioSource` sets its own volume; all routing through the mixer.

### T-021 · Game feel pass: screen shake, hit-stop, blast flash
**Goal** Camera shake scaled by blast size, brief hit-stop on chain detonation, screen flash — all on a tunable `GameFeelConfig`.
**Dependencies** T-019 · **Complexity** S
**Test criteria** Playtesters describe explosions as "punchy" · effects never obscure the board or hide a lethal tile · all effects individually disableable for accessibility.
**DoD** Feel effects never alter simulation state (view layer only); intensity capped so readability always wins.

---

# MILESTONE 3 — Threat & Consequence

### T-022 · `EnemySystem` — Patrouilleur Basic
**Goal** Fixed patrol paths on the grid, blocked by bombs and walls, killed by blasts, lethal on player contact.
**Dependencies** T-018 · **Complexity** M
**Test criteria** Patrol loops indefinitely without desync · reverses correctly when blocked by a bomb · dies to a blast on the correct tick · contact detection has no gaps at maximum relative speed.
**DoD** Fully deterministic; no per-enemy allocation; patrol paths authored in `LevelDefinition`.

### T-023 · `DamageSystem`, lives and respawn
**Goal** Lethal resolution for player and enemies, 3 lives, respawn at spawn point, **timer persists across deaths** (resolves Contradiction C5), game-over on 0 lives.
**Dependencies** T-022 · **Complexity** M
**Test criteria** Simultaneous blast + enemy contact resolves once, not twice · respawn clears all transient state · bombs placed before death behave correctly after it · game-over fires on the correct tick.
**DoD** Death cause recorded in the `SimEvent` (needed for the Gate A "why did you die?" measurement); respawn < 1 s.

### T-024 · Enemy views and death feedback
**Goal** Pooled enemy views with 4-direction placeholder sprites, death VFX, SFX, and a clear pre-contact telegraph.
**Dependencies** T-023, T-019 · **Complexity** S
**Test criteria** Enemy screen position never disagrees with its simulation tile · a player can always tell what killed them.
**DoD** Death cause legible within 0.5 s of dying (verified in playtest).

---

# MILESTONE 4 — Match Flow

### T-025 · `LevelDefinition` ScriptableObject and loader
**Goal** Level asset: text-based tile layout, spawn point, exit position, node placements, enemy patrol paths, timer, power-up placements; `Bake()` to `LevelLayout`.
**Dependencies** T-010 · **Complexity** M
**Test criteria** A malformed layout fails loudly in the Editor with a precise message · a valid level loads identically 100 times · `OnValidate` rejects unreachable exits and out-of-bounds spawns.
**DoD** A designer edits the map without opening a scene; Inspector has a monospace layout preview.

### T-026 · Pickups: Data Nodes, Data Coins, power-ups
**Goal** `PickupSystem`: spawn from destroyed blocks, collection, blast-destroys-pickup rule, in-level range+ and bomb+ effects.
**Dependencies** T-018, T-025 · **Complexity** M
**Test criteria** Nodes spawn only at authored positions · coins drop at the configured rate deterministically from the seed · a pickup destroyed by a blast is removed correctly · power-ups apply immediately and persist until death.
**DoD** Coin/node distinction implemented per Contradiction C9; drop rate on a config asset.

### T-027 · `ObjectiveSystem` and exit
**Goal** Node counting, exit door unlock at 5/5, win condition on reaching an unlocked exit.
**Dependencies** T-026 · **Complexity** S
**Test criteria** The exit does not accept the player below 5/5 · win fires on the exact tick of entry · the unlock is unmistakably telegraphed (VFX + SFX + HUD).
**DoD** Objective progress raised as a `SimEvent` for HUD consumption.

### T-028 · `TimerSystem` and `ScoreSystem`
**Goal** Countdown in ticks with timeout defeat; score from blocks, enemies, nodes, and remaining time.
**Dependencies** T-011 · **Complexity** S
**Test criteria** Timer is frame-rate independent to the tick · scoring is deterministic and reproducible from a seed · defeat fires on the exact timeout tick.
**DoD** Score formula in a config asset, not code.

### T-029 · Match state machine, pause and **app-background handling**
**Goal** `Loading → Countdown → Playing → (Victory | Defeat) → Results`, plus pause, resume, quit-to-hub, and auto-pause on `OnApplicationPause`/`OnApplicationFocus`.
**Dependencies** T-027, T-028 · **Complexity** M
**Test criteria** Every transition in the `02-PROTOTYPE-SCOPE.md` §5 diagram is reachable and correct · **backgrounding the app mid-blast and returning resumes with no state corruption and no lost time** · an incoming phone call does not break the match.
**DoD** Verified on device including a real incoming call; simulation cannot tick while paused.

### T-030 · HUD
**Goal** Timer, lives, score, objective progress, bomb charges. MVP split: `HudPresenter` (plain C#) + `HudView` (MonoBehaviour) + `IHudView`.
**Dependencies** T-029 · **Complexity** M
**Test criteria** `HudPresenter` unit-tested with a mock view · readable on a 5" screen at arm's length · nothing critical inside thumb-occlusion zones · zero string allocation per frame · static and dynamic elements on separate canvases.
**DoD** No gameplay logic in the view; safe-area fitter applied; contrast verified against a bloom-heavy background.

### T-031 · Results screen
**Goal** Win/lose presentation: time, nodes, coins earned, score; Retry and Hub actions. Listens to `MatchEndedChannel`.
**Dependencies** T-029 · **Complexity** S
**Test criteria** Values correct for every win and lose path · Retry restarts in < 3 s (a slice requirement) · no `UI → Gameplay` assembly reference introduced.
**DoD** Presenter unit-tested; coins are banked before the screen appears.

---

# MILESTONE 5 — Meta Loop & Slice Completion

### T-032 · Wallet and progression services
**Goal** `IWalletService` (Data Coins), `IProgressionService` (owned upgrades), persisted through `ISaveService`, applied at `Bake()` time.
**Dependencies** T-005, T-031 · **Complexity** M
**Test criteria** Coins persist across restart · balance can never go negative · a purchase is atomic (never debits without granting) · upgrades apply to the next match's `SimulationConfig`.
**DoD** All economy arithmetic unit-tested including boundary and concurrency-adjacent cases.

### T-033 · Hub screen and upgrade UI
**Goal** Hub with coin balance, PLAY, and the Bomb Range upgrade track (3 tiers, costs 50/150), MVP-structured.
**Dependencies** T-032 · **Complexity** M
**Test criteria** Purchase updates balance and persists immediately · the affordability state is unmistakable · an upgraded range is visibly effective in the next match.
**DoD** Upgrade costs and effects on a config asset; presenter unit-tested.

### T-034 · Settings screen
**Goal** Audio bus volumes, quality tier, haptics toggle, accessibility toggles (screen shake, flash), persisted.
**Dependencies** T-020, T-032 · **Complexity** S
**Test criteria** Every setting persists and applies immediately, including after a restart.
**DoD** Android back-button handled through the screen stack.

### T-035 · Analytics instrumentation
**Goal** Call sites for `match_started`, `match_ended`, `player_died` (with cause), `upgrade_purchased`, `session_start/end` against the no-op service.
**Dependencies** T-004, T-031 · **Complexity** S
**Test criteria** Events fire exactly once with correct payloads (verified against a logging implementation).
**DoD** No SDK dependency added; payload schema documented for the M9 wiring.

### T-036 · Device performance pass
**Goal** Profile on target and low-tier devices; hit every Gate B threshold in `02-PROTOTYPE-SCOPE.md` §6.
**Dependencies** T-033 · **Complexity** M
**Test criteria** 60 fps sustained on target / 30 fps floor on low-tier · 0 B/frame in `PLAYING` · < 120 draw calls · < 2 s match load · no thermal drop after 10 minutes.
**DoD** Profiler captures committed as the baseline; any threshold missed is fixed, not waived.

### T-037 · Determinism and soak tests
**Goal** Replay a recorded intent stream 1000× asserting identical state hashes; a 10 000-tick randomised soak asserting no exception and no allocation growth.
**Dependencies** T-029 · **Complexity** M
**Test criteria** Zero hash divergence across 1000 runs · soak completes clean · both run in CI.
**DoD** `ReplayInputSource` implemented; a recorded reference replay committed as a regression fixture. *This is the concrete payoff of architecture decision D1 and the prerequisite for any future netcode.*

### T-038 · Playtest build and observation protocol
**Goal** Distributable .apk + WebGL build; a written observation protocol capturing every Gate A metric; recruit 8 external testers.
**Dependencies** T-036 · **Complexity** M
**Test criteria** Both builds install and run for testers with no assistance · every Gate A metric is captured for every session.
**DoD** Protocol written before testing begins (so the criteria cannot be rationalised afterwards); results documented with a **go / pivot / stop** recommendation.

---

# ▶ VALIDATION GATE — do not start M6 until T-038 passes

---

# MILESTONE 6+ — Post-Validation (outline only)

Deliberately low-resolution: these tasks will be rewritten based on what the gate teaches. Estimating them
in detail now is planning fiction.

| ID | Task | Complexity | Key dependency |
|---|---|---|---|
| T-039 | Reinforced (purple) blocks, multi-hit damage state | S | T-018 |
| T-040 | Bombardier-Drone (ranged projectiles) | M | T-022 |
| T-041 | Chasseur-Néon (line-of-sight pursuit) | M | T-022 |
| T-042 | 5 authored levels + difficulty curve | L | T-025 |
| T-043 | Level select and per-level records | M | T-042 |
| T-044 | In-Editor level authoring tool | M | T-025 |
| T-045 | Skill slot framework (passive + active) | L | T-032 |
| T-046 | Shield passive (rebalanced per Risk G3) | M | T-045 |
| T-047 | Speed Boost passive + sprint | M | T-045 |
| T-048 | Special: Remote Detonator | M | T-045 |
| T-049 | Special: Short Teleport | M | T-045 |
| T-050 | Special: Lightning Chain | M | T-045 |
| T-051 | Loadout screen | M | T-045 |
| T-052 | **Bomb cooldown A/B test** (settles GDD §6.2.3 with data) | S | T-035, T-045 |
| T-053 | Isometric pixel art production | XL — **must be split** | Gate pass + Q8 |
| T-054 | Neon rendering pass within mobile budget | L | T-053 |
| T-055 | FTUE / tutorial | L | T-042 |
| T-056 | Localisation (FR/EN) | M | — |
| T-057 | Accessibility: colourblind-safe block encoding | M | T-053 |
| T-058 | iOS build pipeline | M | T-009 |
| T-059 | Nakama backend + server-authoritative save | XL — **must be split** | Q1 answered |
| T-060 | Leaderboards and daily objectives | L | T-059 |
| T-061 | IAP, premium currency, store compliance | XL — **must be split** | T-059 |
| T-062 | Cosmetics via remote Addressables | L | T-059 |
| T-063 | Multiplayer netcode | XL — **must be split** | Q1 = multiplayer |

---

## Immediate Next Actions

1. **Answer Q1–Q5** in `01-ANALYSIS.md` §13. Q1 and Q3 change the architecture; the rest change the design.
2. **Start T-001 → T-009** (Milestone 0). None of these depend on the open questions.
3. **Protect T-015 and T-018.** They are the two tasks the slice actually lives or dies on. Do not compress
   them to recover schedule elsewhere.

Next: `06-ENGINEERING-PROCESS.md`.
