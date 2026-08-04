# Bomber Legends — Production Roadmap (Phase 4)

**Depends on:** `02-PROTOTYPE-SCOPE.md`, `03-ARCHITECTURE.md`

**Governing rule:** every milestone ends in an **installable Android build that a stranger can play**. If a
milestone cannot produce one, it is scoped wrong and must be split. No milestone exceeds one working week.

**Estimates** assume one full-time engineer. They are engineering time only and exclude final art
production (Q8 in `01-ANALYSIS.md` §13 is unanswered).

---

## Milestone Map

```
  ┌─── SLICE: answers "is it fun?" ────────────────────────────────────────┐
  │  M0 ──▶ M1 ──▶ M2 ──▶ M3 ──▶ M4 ──▶ M5                                 │
  │  boot   feel   bombs  threat  match  meta                              │
  └────────────────────────────────────────────┬───────────────────────────┘
                                               ▼
                                   ╔═══════════════════════╗
                                   ║   VALIDATION GATE     ║  Gate A + Gate B
                                   ║  go / pivot / stop    ║  (02-PROTOTYPE-SCOPE §6)
                                   ╚═══════╤═══════════════╝
                                           ▼
                      M6 ──▶ M7 ──▶ M8 ──▶ M9 ──▶ M10
                    content skills  prod  live-  multiplayer
                                          service  (gated on Q1)
```

---

# Phase I — The Vertical Slice (M0–M5)

## M0 — Project Bootstrap — 🟡 **COMPLETE pending device check (2026-08-05)**
**Duration:** 3 days · **Build:** boots to a placeholder hub on a real phone

> **Status.** T-001 → T-009 delivered. 160 EditMode + 9 PlayMode tests green. The app boots, composes
> its services, loads the save and moves hub ↔ match. Two exit criteria need hardware that is not
> attached to the build machine: the APK launching on a device, and the save surviving a force-stop.
> Everything else is verified.

| | |
|---|---|
| **Delivers** | Unity 6.3 project, URP 2D pipeline, 10 assembly definitions wired, `GameContext` composition root, `Bootstrap`/`Hub`/`Match` scenes, `ISaveService` + `FileSaveRepository`, editor play-from-any-scene tooling, Android build pipeline, git + `.gitignore` + LFS |
| **Playable** | Tap the app icon → loading screen → hub screen with a non-functional PLAY button. That is a real build on a real device. |
| **Exit criteria** | Cold start < 4 s on target device · save round-trips across app restart · all assemblies compile with the dependency rules enforced · a .apk exists |
| **Why first** | Every later milestone deploys through this pipeline. A day-one Android build means device performance is never a late surprise. |

## M1 — Movement & Feel
**Duration:** 5 days · **Build:** walk around a grid, on a phone, with your thumb

| | |
|---|---|
| **Delivers** | `Simulation` + `Core` assemblies, `GameSimulation.Tick`, `BoardState`, `MovementSystem`, `PlayerIntent`, `TouchInputSource` + virtual joystick, isometric projection & depth sorting, view interpolation, `MovementFeelConfig`, EditMode test suite for movement |
| **Playable** | A capsule moves on a 13×11 greybox isometric board with walls. Nothing else. |
| **Exit criteria** | **The five feel mechanisms of `03-ARCHITECTURE.md` §9 are implemented and tuned on a real device** · 0 stuck-on-geometry incidents in a 10-minute session · movement is smooth at 60 fps · ≥ 20 movement unit tests green |
| **Why here** | This is the highest-risk item in the project (Risk G6). If isometric grid movement cannot be made to feel good, that must be discovered in week 2, not week 8. **Do not proceed to M2 until this feels right.** |

## M2 — Bombs & Blasts
**Duration:** 5 days · **Build:** the core verb, complete

| | |
|---|---|
| **Delivers** | `BombPlacementSystem`, `FuseSystem`, `BlastSystem` (BFS + **chain detonation**), bomb capacity model, walk-off-own-bomb rule, destructible blocks, `SimEvent` buffer, `ViewSynchroniser`, object pools, placeholder bomb/blast/destruction VFX + SFX, screen shake |
| **Playable** | Place bombs, destroy the board, blow yourself up, trigger chain reactions. |
| **Exit criteria** | Chain detonation works to arbitrary depth without stack growth · 0 B/frame during heavy blasts · blast propagation fully unit-tested including chains and range clipping · audio voice limiter prevents distortion on a 12-block chain |
| **Note** | `bombCooldownSeconds` ships as a serialized field defaulting to **0** (classic capacity model). The GDD's 5 s model is one Inspector value away and gets A/B tested at M7, not argued about now. |

## M3 — Threat & Consequence
**Duration:** 4 days · **Build:** you can die, and it matters

| | |
|---|---|
| **Delivers** | `EnemySystem` (Patrouilleur patrol paths), `DamageSystem`, lives, death, respawn, enemy death by blast, enemy views + pooling, death/respawn feedback |
| **Playable** | A hostile board. Deaths, respawns, 3 lives. |
| **Exit criteria** | Enemies never desync from the grid · a player killed by their own blast reads as *their* mistake (observed in playtest) · timer **persists** across deaths (resolves Contradiction C5) · enemy behaviour fully unit-tested |

## M4 — Match Flow
**Duration:** 4 days · **Build:** a complete, winnable, losable match

| | |
|---|---|
| **Delivers** | `ObjectiveSystem` (Data Nodes), exit door, `TimerSystem`, `ScoreSystem`, pickups (coins, range+, bomb+), match state machine, countdown, pause, **app-background auto-pause**, results screen, `MatchEndedChannel`, HUD (timer/lives/score/objective/bomb charges) |
| **Playable** | The complete `Data Heist` loop from `02-PROTOTYPE-SCOPE.md` §5, start to finish, with no dead ends. |
| **Exit criteria** | Every arrow in the §5 flow diagram is reachable · backgrounding the app mid-match pauses and resumes correctly · HUD readable on a 5" screen at arm's length · results are correct for every win/lose path |

## M5 — Meta Loop & Slice Completion
**Duration:** 4 days · **Build:** the testable slice — **this is the one that goes to playtesters**

| | |
|---|---|
| **Delivers** | Hub screen, wallet, `Data Coins` persistence, Bomb Range upgrade track (3 tiers), hub↔match transitions, results→hub→retry loop, settings (audio, quality), analytics instrumentation (no-op service, real call sites), device performance pass, **playtest build + observation protocol** |
| **Playable** | Play → earn → upgrade → replay stronger. The full slice. |
| **Exit criteria** | **Gate A and Gate B of `02-PROTOTYPE-SCOPE.md` §6, both passed** · a distributable .apk + a WebGL build for remote testers |

> ### ▶ VALIDATION GATE
> **Total to here: ~25 working days (5 weeks).**
>
> **Pass both gates** → proceed to M6.
> **Fail Gate A on controls** → return to M1. Content work is forbidden until controls pass.
> **Fail Gate A on fun with good controls** → this is the *valuable* failure. Redesign the bomb economy,
> board shape, or threat model and re-test. Do not proceed. Do not commission art.
> **Fail Gate B** → fix before adding content. Performance debt compounds and never gets cheaper.

---

# Phase II — Depth (M6–M8)
*Only begins after the gate passes.*

## M6 — Content Depth
**Duration:** 5 days · **Build:** 5 levels, 3 enemies, 2 block types

Reinforced (purple) blocks, Bombardier-Drone, Chasseur-Néon, 5 authored levels, difficulty curve, level select,
per-level records. Delivers the first real answer to "does it stay fun for 20 minutes?"
**Exit:** median session ≥ 12 minutes in playtest · a designer authors a new level end-to-end in < 30 min without engineering help.

## M7 — Skill System
**Duration:** 5 days · **Build:** the GDD's headline differentiator

Passive slots (Speed Boost, Shield — Shield rebalanced per Risk G3), active slots, 3 Special abilities
(Teleport, Lightning Chain, Remote Detonator), skill HUD with cooldowns and gauges, pre-match loadout screen,
**and the bomb-cooldown A/B test that finally settles GDD §6.2.3 with data.**
**Exit:** ≥ 60% of playtesters change their loadout between runs (if they don't, the differentiator isn't one and the design needs rework).

## M8 — Production Quality
**Duration:** 3–4 weeks (art-dependent — see Q8)
**Build:** something that could go on a store page

Isometric pixel art production, neon rendering pass within budget, animation, full audio pass + original
music, UI Toolkit hub rebuild, FTUE/tutorial, localisation (FR/EN), extra modes (Elimination, Survival),
lore/narrative framing, iOS build, accessibility pass (**colourblind-safe block encoding is mandatory** —
the design currently encodes block behaviour in colour alone).

---

# Phase III — Commercial (M9–M10)

## M9 — Live-Service Foundations
**Duration:** 3–4 weeks · **Build:** monetisable soft-launch candidate

Nakama backend, server-authoritative save (`NakamaSaveRepository`), leaderboards, daily objectives, the full
tech tree, premium currency, IAP + store compliance, cosmetics via remote Addressables, real analytics,
remote config for live balancing, CI/CD, crash reporting, soft-launch in one small market.

## M10 — Multiplayer *(gated on Q1)*
**Duration:** 6–8 weeks · **Build:** the product this may actually want to be

Server-authoritative match simulation reusing the *existing* `Simulation` assembly, matchmaking, rollback or
delay-based netcode, lobbies, ranked, spectating, replay validation anti-cheat.

**This milestone is only affordable because of architecture decisions D1–D3 taken at M1.** Without them it
is a rewrite of the entire gameplay layer.

---

## Schedule Summary

| Phase | Milestones | Engineering time | Cumulative |
|---|---|---|---|
| **I — Slice** | M0–M5 | ~5 weeks | 5 weeks |
| **II — Depth** | M6–M8 | ~6–7 weeks (+ art) | ~12 weeks |
| **III — Commercial** | M9–M10 | ~9–12 weeks | ~22–24 weeks |

### Risks to the schedule

1. **Art production (Q8)** is unestimated and will likely dominate M8. It is the most probable cause of slip.
2. **M1 overrun.** If isometric feel proves hard, M1 can double. This is the correct place to spend extra time.
3. **Gate failure.** A pivot at the gate is a *success* of the process, but it resets Phase II. Budget for it.
4. **Q1 remaining unanswered.** M9 and M10 are contradictory products; building M9's single-player economy
   and then pivoting to multiplayer wastes most of it.

Next: `05-BACKLOG.md`.
