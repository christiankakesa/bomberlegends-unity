# Bomber Legends — Concept Revision (v2.0)

**Date:** 2026-08-06
**Supersedes:** parts of `01-ANALYSIS.md`, `02-PROTOTYPE-SCOPE.md`, `03-ARCHITECTURE.md`, `04-ROADMAP.md`
**Source:** GDD v2.0 §1, §2, §2b, §2c

---

## 1. What changed

The game is now a **hybrid**: Bomberman grid destruction, MOBA skillshot precision, roguelite item
synergy. Runs are single-player, stage-based, and end on death.

Three decisions were taken with it:

| Decision | Choice | Why it could not wait |
|---|---|---|
| Render pipeline | **Low-poly 3D** | 360° movement needs a rotating model, not a sprite sheet per facing. Free to change now with no art; a full re-do later. |
| Platform lead | **PC-first**, mobile later | Mouse aiming suits skillshots; seven loadout slots need screen space. Supersedes Android-first (2026-08-05). |
| Lethality | **Own bombs hit hard, enemies chip** | A dash plus evenly-scaled HP damage silently removes the self-trapping tension the Bomberman layer exists to create. |
| Camera | **Follows the player** | Supersedes the single-screen arena decision (Q3, 2026-08-05). Arenas may now exceed one screen, which in turn means off-screen threats eventually need telegraphing — an edge indicator or minimap, deferred until arenas are actually larger than a screen. |

---

## 2. What survives

Roughly **85% of written code is untouched.** This is the return on keeping the simulation
engine-free and event-driven.

**Untouched**
- `Core` — grid coordinates, integer sub-tile positions, deterministic RNG, tick types, accumulator
- `Simulation` — board, bombs, fuses, blast propagation, chain detonation, event buffer, state hashing
- Services, scenes, bootstrap, save, camera rig, pooling, view synchronisation
- The grid remains authoritative for bombs, blocks and blast shape

**Rewritten**
- `MovementSystem` — 4-directional soft-grid → 360° continuous with collision against grid blocks
- `BoardProjector` sorting — replaced by real depth once rendering is 3D
- `PlaceholderArt` — replaced by primitives

**Partly carried over from the movement work**
- Sub-stepping so speed cannot tunnel a wall — **still required**
- Integer sub-tile positions — **still required**, and now doubly so for projectiles
- Corner assist — **survives in spirit**: sliding along a wall rather than sticking to it is its 360°
  equivalent, and it is just as essential
- Lane snapping, deferred turns, cardinal snapping with hysteresis — **obsolete**; there are no lanes

**Superseded design decisions**
- Q2 platform ladder (Android-first) → PC-first
- The vertical meta progression debate → replaced by roguelite run-scoped items plus meta unlocks
- The five-second bomb cooldown question → still live, still one Inspector value, now competing with
  skill cooldowns for the same design space

---

## 3. The revised vertical slice

The old slice asked *"is placing a bomb and escaping it fun?"*. That is no longer sufficient, because
the **hybrid is the product**. A slice that proves classic Bomberman feels good proves nothing about
this game.

> ### The question
> **"Does moving freely, aiming a skillshot, and setting off grid-shaped explosions feel good
> together — and does changing one item visibly change how I play?"**

### Minimum systems

| # | System | Scope |
|---|---|---|
| 1 | Grid simulation | ✅ done |
| 2 | Bombs, fuses, blasts, chain detonation | ✅ done |
| 3 | **360° movement** with collision and wall sliding | rewrite |
| 4 | **Health and damage** — player and enemies; own blast takes a large share | new |
| 5 | **One dash** (mobility active) | new |
| 6 | **One skillshot** (directional projectile, blocked by destructible blocks) | new |
| 7 | **Two passive slots, three items**, at least one that visibly alters a skill | new |
| 8 | **One enemy** with health that must be fought, not merely avoided | new |
| 9 | **Run loop** — clear arena → choose one of three items → next arena → death ends the run | new |
| 10 | One arena, greybox 3D | new |

### Deliberately excluded

Third active skill, slots three and four, bosses, meta progression between runs, multiple biomes,
authored art, audio, mobile controls.

### Success criteria

The technical gates from `02-PROTOTYPE-SCOPE.md` §6 still apply. The feel gate changes:

| Metric | Threshold |
|---|---|
| Voluntary second run | ≥ 60% |
| **Players who deliberately pick a different item on run 2** | **≥ 60%** |
| Players who can describe their build unprompted | ≥ 50% |
| Deaths blamed on self rather than controls | ≥ 80% |
| Stuck-on-geometry incidents | 0 |

The item metric is the one that matters. If players pick items at random and cannot describe what
their build does, the synergy pillar has not landed and no amount of content will fix it.

---

## 4. Revised milestone plan

| Milestone | Content | Status |
|---|---|---|
| M0 | Bootstrap | ✅ complete, device-verified |
| M1 | Movement & feel (4-directional) | ✅ complete — superseded by M2b |
| M2 | Bombs, blasts, chain detonation, views | ✅ T-017 → T-019 done; T-020/T-021 deferred |
| **M2b** | 3D migration + 360° movement + wall sliding + corner slip | ✅ complete, verified in editor |
| **M3** | Health, damage, one enemy that fights back | 🟡 **simulation done and tested; enemies have no view yet** |
| M4 | Skill framework + dash + skillshot | |
| M5 | Item framework + three items + two passive slots | |
| M6 | Run loop: arenas, item choice, death, restart | |
| — | **▶ VALIDATION GATE** — the question in §3 | |
| M7+ | Third skill, slots 3–4, bosses, meta, art, audio, mobile port | |

Audio (T-020) and screen shake (T-021) move behind the hybrid work. They add polish to a loop whose
shape is about to change.

---

## 4b. M3 notes (2026-08-06)

**Delivered.** `HealthState` with an immunity window, `EnemyState`/`EnemyBuffer`, a pursuing
`EnemySystem`, and a `DamageSystem` that runs after the blast so it reads a finished picture of what
is on fire. `'E'` places an enemy in a text layout. Death ends the match. The state hash covers health
and every enemy, so determinism survives their addition. **288 EditMode + 10 PlayMode tests green.**

**Tuning, all Inspector-visible:** player 100 health, own blast **34**, enemy contact **10**, immunity
30 ticks, blast kills a basic enemy outright.

**`GridMotion` extracted.** The player and every enemy now collide through one implementation —
wall sliding, sub-stepping, corner slip and the bomb-exemption rule. Two implementations would have
meant two sets of bugs, and an enemy catching on a corner the player rounds would have been felt long
before it could be described. The refactor was validated by the 274 pre-existing tests.

**Outstanding**
- Enemies render nothing yet; they exist only in the simulation.
- The chaser commits to a heading until its tile changes. Distance-closing is tested, but *looking*
  good while doing it is not the same question — watch for jitter at junctions when the player is
  diagonal. The fix, if needed, is to bias towards the previous heading on ties rather than sampling
  the random source afresh.

---

## 5. Open questions

1. **Does the third active skill earn its slot?** Three actives plus movement plus aim is a lot of
   simultaneous decision-making. Worth validating two before committing to three.
2. **What does a run cost in time?** Roguelite runs are typically 20–40 minutes. That is far longer
   than the 6–10 minute mobile session the original design targeted, and it changes the save model
   (a run must survive being interrupted).
3. **Does the bomb stay the primary verb?** If the skillshot is more effective than bombing, the
   Bomberman layer becomes set dressing. Bombs must remain the highest-damage, highest-risk option.
4. **Hit-stop.** Deliberately not implemented: pausing a fixed-tick authoritative simulation on frame
   time breaks determinism and replay validation. It can be added as a simulation rule if wanted, but
   that is a gameplay change, not a visual one.
