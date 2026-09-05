# What is actually built

**Updated 2026-09-05 · 526 EditMode + 28 PlayMode tests green, zero warnings · device-verified on a Galaxy S21
Ultra, a Solana Seeker 2 and a RedMagic (NP05J) tablet**

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
| Enemy blast awareness | ✅ | `ThreatGrid` distance field; alerted enemies run, hold at the edge, dormant ones are exempt |
| `EnemyBombFearTicks` | 🧪 | 45 of a 90-tick fuse; zero restores the oblivious mob as an archetype |
| The arena closes in as it empties | ✅ | Waking distance grows across an arena's tail until the last survivors hunt from anywhere on the board. `ArenaTailShare` 50; 0 hunts from the first kill, 100 is plain dormancy |
| Arena clear condition | ✅ | Every spawned enemy dead |

## Skills

| Feature | State | Notes |
|---|---|---|
| Three active slots | ✅ | Slot 3 is empty and waiting for content, and now says so: `LOCKED` in the readout and a dimmed, unpressable outline in the touch cluster |
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
| Block clustering | 🧪 | Density spent in runs, not per tile; sealed placements 36% → 17% at unchanged 55% fill |
| Item offer after each clear | ✅ | Three drawn from the run's own RNG |
| First-offer gating | 🧪 | An offer made to a player carrying nothing withholds the items that only multiply a build — Kinetic Core and Overclock. Classified from the effect, not by name |
| Swap when slots are full | ✅ | Two-step: take, then give up |
| Decline an offer | ✅ | |
| Health carries between arenas | ✅ | +25 restored per clear — **the number most likely wrong** |
| Death ends the run | ✅ | |
| Clean restart, in place | ✅ | No scene load; 200 restarts well under a second |
| Fresh seed per attempt | 🧪 | Scene ships seed 0: every attempt draws its own, shown in the pause menu. Any other value fixes the run for replaying one board. Rounds 1–3 ran on seed 1 |
| Start on a chosen arena | 🧪 | `Starting arena` on the installer: full health, starting items, neither resumes nor writes the saved run. For measuring arena 9 without the climb |
| Item descriptions on cards | ✅ | Added 2026-08-07, font raised to 20 |

## Presentation and platform

| Feature | State | Notes |
|---|---|---|
| 3D greybox, follower camera | ✅ | Distance scales with arena width against the **first** arena's 21 tiles, so depth is framed in proportion: 17 units at arena 1, 25 at 31 tiles wide |
| Pooled views for bombs, blasts, debris, shots | ✅ | Prewarmed; growth mid-match is an error |
| HUD: arena, health, enemies, bombs, charges, build | ✅ | Bombs read as a count, or as the countdown to the next one. The counters line is **full**: 1706 units against the 1739 a tablet has |
| HUD build on its own line | ✅ | The single line wanted 2320 units against the 1740 a tablet has, and the build was the end that fell off |
| Item choice screen, phone-legible | ✅ | Body text 9 → 14.3 dp, `TAKE`/`GIVE UP` on the card, `CHOOSE ONE` headline; read and tapped cleanly on all three devices 2026-08-24 |
| Every other greybox screen, phone-legible | ✅ | Pause menu, control hints, touch buttons, pause control, hub QUIT, device log — all were 9–13 dp; verified on all three devices 2026-08-24 |
| Keyboard + mouse aim | ✅ | `Shift` dash · `Q`/LMB shot · `E`/RMB slot 3 |
| Gamepad + right-stick aim | 🧪 | `B` dash · `X` shot · `Y` slot 3 — **never played** |
| Touch controls hidden off touch devices | ✅ | Added 2026-08-07 |
| Touch controls stand down while a screen covers the match | ✅ | Found on device 2026-08-24: `SHOT` sat on the right choice card and took its taps. Fixed twice — the controls stand down, and the overlay now outranks them — and the card takes its own taps on all three devices |
| Quit the application | ✅ | Hub QUIT; stops play mode in the Editor |
| Gamepad / keyboard menu navigation | ✅ | Focus set on arrival, kept when lost, visibly highlighted |
| Active-device arbitration | 🧪 | Last device *deliberately used* owns the whole tick, aim included |
| Touch: analogue 360° movement | ✅ | Replaced the v1.0 four-way snapping |
| Movement stick answers the whole thumb quarter | ✅ | **Fixed 2026-09-05**: the stick was one 300-unit object that both listened and was drawn, so a thumb landing beside it moved nothing — on device that reads as the game ignoring you. The listening area is now the bottom-left quarter and the circle moves to meet the thumb. Device-verified on a Galaxy S21 Ultra |
| Touch: drag-to-aim skill buttons | ✅ | Tap casts, drag aims, release fires, cancel zone aborts. **Fixed 2026-09-05**: the tap decision was measured from the button's centre, so a still press anywhere outside a 26 px disc in a ~200 px button fired as an aimed shot towards the thumb; now measured from where the finger landed. Also fixed the same day: the aim arrow showed the instant a finger touched down, so an unavoidable thumb wobble under the tap threshold flashed it on and off on a plain tap; it now only appears once a real drag crosses that threshold — device-verified on a Galaxy S21 Ultra |
| No UI selection during a match | ✅ | Enforced by test — Submit and Bomb share a button on a pad |
| Pause menu | ✅ | Start / Escape / on-screen button; resume and quit to hub |
| Audio: pooled service, voice limiting, pitch variation | ✅ | Generated placeholder sounds; no assets needed |
| AudioMixer bus hierarchy | ✅ | `Settings/Audio/MainMixer` — Master over Music, Sfx, Ui, Voice, Ambience, each level an exposed parameter. Falls back to per-source multipliers when no mixer is assigned |
| Bomb drop audible on a phone | ✅ | Retuned 2026-08-24: body, harmonics and a tap instead of a bare 150→80 Hz sine. Keeps 75% of its level through a 300 Hz high-pass, against 43% |
| Placeholder mix levelled for a phone | ✅ | Every generated clip normalised to one loudness measured above 400 Hz, so a feedback volume states intent. The dash was 3.2× the bomb drop on a Seeker 2 and is now 0.6× |
| Feedback table (event → sound + shake) | ✅ | Designer-editable asset; falls back to placeholders |
| Camera kick scaled to the event | ✅ | View-only; never touches simulation state |
| Android build pipeline | ✅ | Device-verified 2026-08-08; 86 MB dev APK |
| Build stamped with its commit | 🧪 | Every build writes `1.0+<sha>` into the player version and restores it after; `*` when the tree was dirty. Shown with the seed in the pause menu as `SEED n · sha · DEV\|REL`. Round 1 lost a defect to an unknown build; the round-3 deploy recorded none |
| Frame time on the device overlay | 🧪 | `FRAME p50 · p99` over the last ten seconds, development builds only. Android's `gfxinfo` reads its own view frames, not Unity's |
| Development APK connects to the profiler | 🧪 | `ConnectWithProfiler` on the dev build; silent when nothing is listening |
| Windows build pipeline | ✅ | Mono; release 92 MB in 36 s; launches clean |
| WebGL build pipeline | ✅ | Brotli + fallback, 10 MB; runs windowed and fullscreen |
| Block inset for readability | ✅ | Blocks fill 88% of their tile; collision unchanged |

---

## Naming

The build uses generic names; the lore (GDD §3.1) names the same things. Bomb → **Soul Orb**,
destructible block → **Corrupted Data Cube**, enemy → **Sombra-Corps Sentinel**, boss → **Sentinel
Lord**, arena → **Sector**, active skill → **Bomb Art**.

**The simulation keeps the generic names on purpose.** `BombState` and `TileType.Destructible`
describe rules rather than fiction, and that layer is deliberately free of anything a designer might
rename. Lore names belong in the view, the interface and content assets — see
[11-ART-DIRECTION.md](11-ART-DIRECTION.md) §1.

---

## Deployment

| Environment | URL | Branch |
|---|---|---|
| Dev | `playdev-bl.funtest.fr` | `main` |
| Release | `play-bomberlegends.kakesa.net` *(unverified)* | `release/*` |

Build output goes into the `bomberlegends-play` repo (`Build/`, `TemplateData/`, `index.html`); a
push triggers a GitHub Action that runs a deploy script on the VPS. Traefik terminates TLS and
proxies to nginx.

**Publish with `tools/publish-webgl`** rather than by hand. It builds the *Release* flavour — the
default is Development, whose artifacts are named differently and whose `.wasm` is 134 MB — replaces
the three payload entries only after verifying the new build, and records the source commit in the
deploy commit message, so `git log -1` in the play repo answers what is live. The push is opt-in
(`--push`). The site drifted two fixes behind before this existed.

**The build depends on server headers and will not degrade without them.** `decompressionFallback`
is off, so Unity emits `.br` files that the browser decompresses natively and `.wasm.br` served as
`application/wasm` lets WebAssembly compile while it downloads. Turning the fallback on instead costs
about 90 KB of loader script and gives up stream compilation, and is only worth it for a host that
cannot set `Content-Encoding`. Verify after any infrastructure change:

```
curl -sI https://playdev-bomberlegends.kakesa.net/Build/Release.wasm.br
```

`content-encoding: br` and `content-type: application/wasm` are both required.

**Two things that bite when syncing a build in.** `rsync` from the Windows drive carries `777`
across and turns every unchanged asset into a file-mode change, so `chmod 644` afterwards keeps the
commit to the real diff. And the deploy `rsync`s the whole repo into the web root, so `.git` needs
excluding — nginx also denies hidden paths, verified returning 403.

---

## Not built

| Gap | Note |
|---|---|
| **Skill-ready / recharge indicator** | Only the numeric charge count in the HUD |
| Authored audio | Sounds are generated placeholders; real clips drop into the same slots |
| Cel-shaded look | Greybox draws with URP/Lit. Any new shader must join `ShaderInclusionTool` or it is stripped from device builds |
| Neon palette and bloom | Unmeasured on device; the look leans on the most expensive mobile post-process there is |
| Locked → liberated palette shift | May be a runtime system rather than authoring; see 11 §2 |
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
hard one. Danger awareness *was* the strongest lever here — and **it has been spent**: every alerted
enemy now refuses to walk into fire, so it is the floor rather than the ceiling. A boss needs a
different axis. The nearest one still unused is making it *deny space* rather than merely survive it.

### Enemy variety
Mobs differentiated by behaviour rather than statistics: a bomb-avoider, a charger, a ranged one.
The bomb-avoider **is now the default**, which inverts this list: the interesting archetype is the one
that does *not* care, and `EnemyBombFearTicks: 0` already produces it without a line of new code. A
mob that walks into your trap is a different threat from one that reads it, and the pair is more
variety than either alone. Everything else here still needs building.
