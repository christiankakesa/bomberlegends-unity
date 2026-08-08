# What is actually built

**Updated 2026-08-08 · 421 EditMode + 14 PlayMode tests green, zero warnings · device-verified on a Galaxy S21 Ultra**

A plain inventory of what exists in the project right now, so proposals can be validated against
reality rather than against memory. Design rationale lives in
[07-CONCEPT-REVISION.md](07-CONCEPT-REVISION.md); this file only answers *"is it in there?"*.

Legend — ✅ built and played · 🧪 built, not yet played · ⬜ not built

---

## Simulation

| Feature | State | Notes |
|---|---|---|
| Fixed 30 Hz tick, view interpolation | ✅ | Explicit accumulator, never `FixedUpdate` |
| Deterministic, engine-free rules | ✅ | Integer maths throughout; state hash covers everything |
| Zero allocation per tick | ✅ | Asserted by test on every subsystem |
| 360° continuous movement, wall sliding | ✅ | Per-axis resolution, sub-stepped |
| Corner slip | ✅ | Player only; enemies use lane centring instead |
| Player lane assist | 🧪 | Scales with axis alignment; Inspector-tunable 0–1 |
| Bombs, fuses, blasts, chain detonation | ✅ | Shared detonation queue, loop-guarded |
| Destructible blocks | ✅ | Cleared by blasts only |
| Health, immunity window | ✅ | Own blast 34, enemy contact 10, 30-tick immunity |
| Pursuing enemy | ✅ | Greedy tile chase, ties broken by the run's own RNG |
| Enemy lane centring | ✅ | Fixes wedging on pillar corners |
| Arena clear condition | ✅ | Every spawned enemy dead |

## Skills

| Feature | State | Notes |
|---|---|---|
| Three active slots | ✅ | Slot 3 is empty and waiting for content |
| Dash | ✅ | 3 tiles, commits to direction, collides normally, **no i-frames** |
| Skillshot | ✅ | Aimed independently; stopped by blocks, not by bombs |
| Charges and cooldowns | ✅ | Sequential recharge, one charge at a time |
| Skill traits | ✅ | `DetonatesBombs`, `DamagesContacts`, `Pierces`, `LeavesBombs` |

## Items

Nine items, two passive slots. Adding one is a row in `ItemCatalog` — no system changes.

| Item | Effect |
|---|---|
| Overcharge | Skillshot sets off bombs it flies over |
| Momentum | Dash injures what it passes through |
| Piercing Rounds | Skillshot is not used up by the first enemy |
| Bomb Trail | Dashing lays a bomb where you left |
| Kinetic Core | Every skill +50% magnitude |
| Overclock | Every skill −25% cooldown |
| Quickstep | Dash −40% cooldown |
| Focusing Lens | Skillshot +30 power, −30% magnitude |
| Twin Shot | Skillshot +1 charge, +25% cooldown |

## Run loop

| Feature | State | Notes |
|---|---|---|
| Arena sequence | ✅ | Generated per run, or authored layouts when pinned |
| Procedural arenas | ✅ | Three styles, seeded, connectivity and safe spawn guaranteed |
| Item offer after each clear | ✅ | Three drawn from the run's own RNG |
| Swap when slots are full | ✅ | Two-step: take, then give up |
| Decline an offer | ✅ | |
| Health carries between arenas | ✅ | +25 restored per clear — **the number most likely wrong** |
| Death ends the run | ✅ | |
| Clean restart, in place | ✅ | No scene load; 200 restarts well under a second |
| Item descriptions on cards | ✅ | Added 2026-08-07, font raised to 20 |

## Presentation and platform

| Feature | State | Notes |
|---|---|---|
| 3D greybox, follower camera | ✅ | Camera distance scales with arena width |
| Pooled views for bombs, blasts, debris, shots | ✅ | Prewarmed; growth mid-match is an error |
| HUD: arena, health, enemies, charges, build | ✅ | |
| Keyboard + mouse aim | ✅ | `Shift` dash · `Q`/LMB shot · `E`/RMB slot 3 |
| Gamepad + right-stick aim | 🧪 | `B` dash · `X` shot · `Y` slot 3 — **never played** |
| Touch controls hidden off touch devices | ✅ | Added 2026-08-07 |
| Quit the application | ✅ | Hub QUIT; stops play mode in the Editor |
| Gamepad / keyboard menu navigation | ✅ | Focus set on arrival, kept when lost, visibly highlighted |
| Active-device arbitration | 🧪 | Last device *deliberately used* owns the whole tick, aim included |
| Touch: analogue 360° movement | ✅ | Replaced the v1.0 four-way snapping |
| Touch: drag-to-aim skill buttons | ✅ | Tap casts, drag aims, release fires, cancel zone aborts |
| No UI selection during a match | ✅ | Enforced by test — Submit and Bomb share a button on a pad |
| Pause menu | ✅ | Start / Escape / on-screen button; resume and quit to hub |
| Audio: pooled service, voice limiting, pitch variation | ✅ | Generated placeholder sounds; no assets needed |
| Feedback table (event → sound + shake) | ✅ | Designer-editable asset; falls back to placeholders |
| Camera kick scaled to the event | ✅ | View-only; never touches simulation state |
| Android build pipeline | ✅ | Device-verified again 2026-08-08; 86 MB dev APK |
| Block inset for readability | ✅ | Blocks fill 88% of their tile; collision unchanged |

---

## Not built

| Gap | Note |
|---|---|
| **Skill-ready / recharge indicator** | Only the numeric charge count in the HUD |
| Authored audio | Sounds are generated placeholders; real clips drop into the same slots |
| Chain pitch escalation | §3 of the feel plan; the cheapest remaining win |
| Dash visual | Movement alone currently carries it |
| Squash-and-stretch on arena border | Agreed, view layer only |
| Level assets | T-025; the authored fallback layouts are still text in the installer |
| Meta progression, save of a run in progress | Excluded from the slice by design |

---

## Proposed, awaiting validation

Nothing below exists. Listed so it can be accepted, changed or dropped before any of it is built.

### Sector towers
Static emplacements that fire on sight, placed pseudo-randomly per arena.

**Why it earns its place:** it is the only threat that would make destructible blocks matter
*defensively*. Today the maze blocks skillshots and shapes movement, but nothing makes a player want
cover — and their primary verb is a bomb that destroys cover. *Your main tool eats your own
protection.* That tension does not exist anywhere in the design yet.

**Constraints:** chip damage rather than lethality, and a readable wind-up. Reuses `ProjectileSystem`
essentially unchanged.

### Sentinel bosses
A larger single opponent ending a run of arenas.

**Open question before building:** what makes it a boss. Health alone produces a long fight, not a
hard one. The strongest available lever is the one the M3 verdict identified — **danger awareness**:
an opponent that reads `BlastGrid` and refuses to walk into fire cannot be beaten by the trap that
works on every basic mob, which forces a genuinely different plan.

### Enemy variety
Mobs differentiated by behaviour rather than statistics: a bomb-avoider, a charger, a ranged one.
The same danger-awareness lever applies, and `BlastGrid` already answers the question in one read.
