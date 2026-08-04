# Bomber Legends — Vertical Slice Definition (Phase 2)

**Depends on:** `01-ANALYSIS.md`
**Status:** Proposed scope lock

---

## The One Question

> **"Does placing a bomb, escaping it, and watching the board unzip feel good enough that a player wants
> one more level?"**

Nothing in this slice exists for any other reason. Every feature below is justified by that question, and
every feature in the GDD that is *not* below has been cut — not deleted, **postponed**.

**Guiding rule:** the slice must be playable on a real Android device, end to end, with placeholder art.
If a feature cannot change the answer to the question above, it is not in the slice.

---

## 1. Minimum Gameplay Systems

Nine systems. That is the whole game.

| # | System | Scope in the slice | Explicitly excluded |
|---|---|---|---|
| 1 | **Grid simulation** | Fixed-tick authoritative grid (occupancy, tile types, entity registry). Pure C#, no engine references. | Moving platforms, multi-floor, ramps |
| 2 | **Player movement** | Soft-grid 4-directional movement, corner-cutting assist, input buffering, angular hysteresis on the joystick | Diagonal movement, dash, wall-kick |
| 3 | **Bomb placement** | Capacity-based (starts at 1), fuse 3.0 s, blocked tile, walk-off-your-own-bomb exception | Cooldown gating (tunable exists, set to 0), remote detonation, kick, throw |
| 4 | **Explosion** | Cross BFS, range 2, stops at solid, destroys one destructible per arm, **chain-detonates bombs**, lethal for 0.4 s | Piercing blasts, custom shapes, blast-vs-blast interactions |
| 5 | **Damage & lives** | 3 lives; blast or enemy contact kills; respawn at level start; timer **persists** across lives | Shield, invulnerability upgrades, revives |
| 6 | **One enemy** | "Patrouilleur Basic" — fixed patrol path, lethal on contact, killed by blasts | Drone, Chasseur, ranged attacks, pathfinding AI |
| 7 | **Objective & match flow** | Collect N Data Nodes → the exit door opens → reach it before the timer | Survival mode, elimination mode, multi-objective levels |
| 8 | **Pickups** | Data Nodes (objective) + Data Coins (currency) + 2 in-level power-ups (range+, bomb+) | Skins, consumables, rare drops |
| 9 | **Meta: one upgrade track** | Spend Data Coins on **starting bomb range** (3 tiers) in a minimal hub screen | Tech tree, actives, passives, premium currency, IAP, skins |

### What is deliberately *not* a system yet

Skills (both passives, both actives), shield, sprint, specials, tech tree, economy, backend, accounts,
leaderboards, ads, tutorial, localisation, audio mixing tiers, VFX authoring.

**On the skill system specifically:** the GDD's headline differentiator is the skill loadout. It is *still*
cut from the slice — because a skill system layered on a core verb that does not yet feel good is
unmeasurable. Skills are Milestone 7, immediately after the slice validates.

---

## 2. One Playable Map

**`Sector-01 — Marché Néon`**

| Property | Value | Rationale |
|---|---|---|
| Dimensions | **13 × 11 tiles**, single screen, no camera scroll | Classic Bomberman proportions; fits landscape phone with room for HUD; answers Q3 with the cheap option |
| Border | Indestructible wall ring | Standard |
| Pillars | Indestructible on even/even coordinates | Classic lattice; guarantees escape routes exist |
| Destructibles | ~45 orange blocks, hand-authored (not random) | Authored = repeatable playtests = comparable data |
| Data Nodes | 5, placed under specific blocks | Forces board traversal |
| Data Coins | Drop from ~40% of destructibles | Feeds the one upgrade track |
| Power-ups | 1× range+, 1× bomb+ under fixed blocks | Tests in-level power growth |
| Enemies | 3 Patrouilleurs on authored patrol loops | Enough to create pressure, few enough to reason about |
| Exit | Fixed position, opens on 5/5 Nodes | Clear goal state |
| Timer | 150 s | Generous — pressure comes from enemies, not the clock |

**Art:** flat-coloured placeholder tiles and capsules. **Zero pixel art in the slice.** Art begins only
after the fun question is answered — otherwise the most expensive asset in the project is produced against
unvalidated mechanics.

The level ships as a **ScriptableObject `LevelDefinition`** with a text-based tile layout, so a designer can
retune the map in seconds without touching a scene.

---

## 3. One Game Mode

**`Data Heist`** — collect all Data Nodes, then reach the Porte de Données before the timer expires.

Chosen over the alternatives because it exercises every core system at once: it forces destruction (nodes
are buried), forces traversal (nodes are spread), forces threat management (enemies patrol), and produces a
clean win/lose state. "Eliminate all Sentinelles" tests only combat; "Survival" tests only endurance and
runs 3× longer than a mobile session.

---

## 4. One Progression Loop

```
  MATCH (Sector-01)
        │  win or lose, coins are kept either way
        ▼
  RESULTS SCREEN — time, nodes, coins earned, score
        │
        ▼
  HUB (one screen, one button)
        │  spend Data Coins:  Bomb Range  Lv1 → Lv2 → Lv3
        │                     (50)         (150)
        ▼
  REPLAY Sector-01, stronger
```

That is the entire meta. One currency, one upgrade, three tiers, one screen.

Coins are kept on loss deliberately: it guarantees forward progress, which is what makes a player accept a
second attempt — and "do players attempt a second run?" is the retention signal the slice needs to measure.

**Note the deliberate compromise:** per `01-ANALYSIS.md` §4 the long-term recommendation is a *horizontal*
meta. A single vertical upgrade track is acceptable here purely because there is one level and the tuning
surface is trivial. It must not be extended to a full tech tree without revisiting that decision.

---

## 5. One Complete Match Flow

Every state below must be implemented — no dead ends, no "returns to nothing".

```
BOOT  ─▶  [Bootstrap scene]  services initialise, save loads
   │
   ▼
HUB  ─▶  "PLAY"  ─▶  MATCH LOADING  ─▶  COUNTDOWN (3-2-1)
                                             │
                                             ▼
                                        ┌──────────┐
                                        │ PLAYING  │◀────────────┐
                                        └──────────┘             │
                                          │   │   │              │
                    nodes 5/5 + exit ─────┘   │   └──── death, lives > 0
                            │                 │              (respawn, timer persists)
                            ▼                 ▼                  │
                        VICTORY          DEFEAT                  │
                    (timer stopped)  (lives = 0 OR timer = 0)    │
                            │                 │                  │
                            └────────┬────────┘                  │
                                     ▼                           │
                              RESULTS SCREEN ───── "RETRY" ──────┘
                                     │
                                  "HUB"
                                     ▼
                                    HUB
```

**Also required (and routinely forgotten):** pause, app-backgrounded auto-pause (mandatory on mobile),
resume, and quit-to-hub. A mobile match flow without background handling is not shippable, even in a slice.

---

## 6. Success Criteria

The slice succeeds only if it passes **both** gates. These are the acceptance criteria for the whole phase.

### Gate A — Feel (subjective, but measured)

Minimum **8 external playtesters**, on real phones, unassisted, no explanation given.

| Metric | Pass threshold | How measured |
|---|---|---|
| **Voluntary replay** | ≥ 60% start a 2nd match without being asked | Observation |
| **"One more" moment** | ≥ 50% start a 3rd match | Observation |
| **Blame attribution on death** | ≥ 80% of deaths blamed on *themselves*, not the controls | Post-death question: "why did you die?" |
| **Control comprehension** | 100% move and bomb correctly within 15 s, no instruction | Observation |
| **Stuck-on-geometry incidents** | **0** | Session recording |
| **Fun rating** | Median ≥ 7/10 on "would you play more of this?" | Exit survey |

**Gate A is the real gate.** If deaths are blamed on the controls, the isometric input mapping is wrong and
nothing else matters until it is fixed.

### Gate B — Technical (objective)

| Metric | Pass threshold | How measured |
|---|---|---|
| Frame rate | ≥ 60 fps sustained on a mid-tier Android (Snapdragon 7-series class) | On-device profiler, 5-minute run |
| Frame rate floor | ≥ 30 fps on the defined low-tier device | Same |
| GC allocation in `PLAYING` | **0 B/frame** steady-state | Unity Profiler, allocation callstacks on |
| Match load time | < 2 s from HUB tap to countdown | Stopwatch + timeline capture |
| App-cold-start to HUB | < 4 s | Same |
| Draw calls | < 120 | Frame Debugger |
| Build size | < 60 MB (placeholder art) | Build report |
| Simulation unit tests | ≥ 90% coverage of the `Simulation` assembly, all green | Unity Test Framework |
| Simulation determinism | Same tick + same intent sequence ⇒ identical state hash, 1000 runs | Automated test |
| Thermal | No frame-rate drop after 10 min sustained play | On-device sustained run |

### Explicit failure response

- **Gate A fails on controls** → stop all content work, return to Milestone 1 (feel), re-test.
- **Gate A fails on fun with good controls** → this is the valuable outcome. Change the design (bomb
  economy, level shape, enemy pressure) before spending a single day on art or backend.
- **Gate B fails** → fix before adding any content; performance debt compounds.

---

## 7. Features Explicitly Postponed

Postponed, with the milestone where each is reconsidered. Nothing here is cancelled.

| GDD ref | Feature | Reconsidered at | Why postponed |
|---|---|---|---|
| §6.1.1 | Speed Boost passive + sprint | M7 | Layered on unvalidated core feel |
| §6.1.2 | Shield passive | M7 | Actively removes the tension being validated (Risk G3) |
| §6.2.4 | Special actives (teleport / lightning / remote) | M7 | Each is a mechanic-sized feature; one may replace bombing (Risk G7) |
| §5.2 | Bomb cooldown gating | M7 | Implemented as a tunable set to 0; A/B tested only after baseline feel exists |
| §8.1 | Reinforced (purple) blocks | M6 | Second block type adds tuning surface, not fun validation |
| §8.1 | Moving platform blocks | Post-1.0 | Hostile to a tick-based grid sim and blast propagation |
| §8.2 | Bombardier-Drone, Chasseur-Néon | M6 | One enemy is enough to create pressure |
| §8.3 | "Eliminate all" and "Survival" modes | M8 | Mode variety is content, not validation |
| §9.2 | Full tech tree (Bomb/Passive/Active) | M8 | Needs skills to exist first |
| §9.1 | Cœurs Néon premium currency | M9 | Monetisation before validation is wasted work |
| §9.3 | Skins & cosmetics | M9 | Convert poorly without multiplayer visibility |
| §3.2 | Isometric pixel art production | M5 | The most expensive asset in the project; must not precede validation |
| §3.1 | Lore, narrative, cutscenes | M8 | Zero effect on the fun question |
| header | Nakama / PostgreSQL / Go backend | M9+, only if Q1 answers "multiplayer" | Months of infrastructure for zero pre-validation value (Risk S4) |
| header | iOS, Desktop, Console builds | M8+ | Android + WebGL prove the game |
| — | Multiplayer / PvP | Post-validation, gated on Q1 | *Architecture is prepared for it now; nothing is implemented.* |
| — | Tutorial / FTUE | M8 | The slice is deliberately tested without instruction — that *is* the control test |
| — | Localisation | M8 | Cheap to add late if strings are externalised from the start |
| — | Ads, analytics SDK, IAP | M9 | Requires legal/store compliance work; no value pre-validation |

---

## 8. Slice Budget

| Area | Estimate |
|---|---|
| M0 Bootstrap + M1 Feel + M2 Bombs + M3 Enemies + M4 Match flow + M5 Meta | See `04-ROADMAP.md` |
| Placeholder art | ~0.5 day (coloured quads, one atlas) |
| Audio | 6 placeholder SFX, no music |
| **Total target** | **~25 working days (5 weeks) for one engineer**, ending in a testable Android build — milestone breakdown in `04-ROADMAP.md` |

If the slice is tracking beyond 7 weeks, cut the meta loop (§4) and enemies to 1 — the bomb feel test is
the irreducible core.

Next: `03-ARCHITECTURE.md`.
