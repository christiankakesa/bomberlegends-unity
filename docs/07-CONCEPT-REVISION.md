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
| **M3** | Health, damage, one enemy that fights back | ✅ **complete, verified in editor** |
| **M4** | Skill framework + dash + skillshot | ✅ **complete, verified in play** |
| **M5** | Item framework + three items + two passive slots | ✅ **complete, awaiting play verdict** |
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

**Views.** Enemies render as pooled meshes, interpolated between ticks like the player, and flash
white while immune — the immunity window is a rule the player has to be able to read, or hits that
land on nothing look like the game ignoring them. A minimal readout shows health and enemies left.

**Play verdict (2026-08-06).** All three design goals met on first play:
- Enemies read as *hunting*, not twitching. The predicted junction jitter did not materialise.
- **34 damage from your own bomb is right** — it produces real caution when placing. Number locked.
- **Enemies cannot be simply walked past.** They reroute and close in, so they clear the
  "must be fought, not merely avoided" bar the slice sets.

**Enemies avoid bombs but not blasts — and that is worth knowing.** Nothing in the AI reasons about
danger; bombs are simply solid, exactly as they are for the player, so a bomb between an enemy and its
target forces a reroute. An enemy will walk straight into a burning tile and die. That is correct for
a basic mob and it is why trapping works.

> **Design opening.** Danger-awareness is the natural axis for higher enemy tiers: a mob that reads
> `BlastGrid` and refuses to enter tiles about to catch fire is a genuinely different opponent, and
> the query is already a single array read. Cheap to build, and a far better difficulty lever than
> raising health or speed.

**Deviation on record.** `MatchHudView` lives in Gameplay rather than UI because it reads the live
simulation, and UI may not reference Gameplay. Acceptable for a greybox readout; the real HUD should
sit behind a Data event channel so UI can react without seeing gameplay at all.

---

## 4c. M4 notes (2026-08-07)

**Delivered.** A skill framework, a dash and an aimed skillshot. **316 EditMode + 10 PlayMode tests
green, zero warnings.**

### The framework, and why it is shaped this way

A skill is **an id that selects behaviour, plus four numbers**: cooldown, power, magnitude and
duration — held in a `SkillSlot`, three of which make a `SkillLoadout`.

Two decisions carry the whole thing:

- **Skill tuning lives in `SimulationState`, not `SimulationConfig`.** Config is immutable and shared
  across a run; if items rewrite skills, the numbers they rewrite have to be per-run state. Config
  now only *seeds* the loadout. Getting this backwards is what would have made M5 painful.
- **The numbers are generic on purpose.** `Magnitude` is dash speed on one skill and projectile speed
  on another. An item reading "+40% magnitude" therefore applies to both without knowing either
  exists. A bespoke config type per skill would force every item to switch over every skill — the
  combinatorial explosion the item system exists to avoid.

`SkillSystem` runs **first in the tick**, ahead of movement, so a dash pressed this tick moves the
player this tick. Charges recharge **one at a time**, so "more charges" and "shorter cooldown" stay
genuinely different items rather than the same one twice.

### Dash

Three tiles in 0.2 s, on a two-second cooldown, and it **collides normally** — no phasing through
walls or bombs. Two properties make it a dash rather than a better walk: it **ignores steering for its
duration** (you commit to the direction), and it **ends early if it jams against a wall** rather than
holding your controls hostage while you visibly go nowhere.

> **The tuning relationship to preserve.** Dash reach is `500 × 6 = 3000` units; the starting blast
> reaches `2 × 1000 = 2000`. A dash therefore clears your own explosion **by exactly one tile** —
> escaping is a skill, not a formality, and the 34-damage decision survives having a mobility button.
> A test asserts this pairing so the two numbers cannot drift apart silently.

### Skillshot

Aimed independently of movement, using the two aim bytes `PlayerIntent` has reserved since M1 — which
is exactly the payoff of having widened the replay format before there was anything to put in it.

- **Stopped by a destructible block, but does not break it.** Load-bearing, and it protects open
  question #3: bombs stay the only way to open the arena, so the maze becomes real cover and the
  Bomberman layer keeps its job.
- **Not stopped by bombs.** They sit low, and a shot swallowed by the bomb at your feet reads as a bug
  every single time.
- **50 damage — half an enemy.** A real weapon, not a replacement for the bomb.
- **Passes through an enemy still inside its immunity window** rather than being consumed for nothing.

### Input

`IntentButtons.Special`/`Sprint` were pre-revision names and are now `Skill1`/`Skill2`/`Skill3`, one
per loadout slot. **Bit values are unchanged**, so the replay and future wire format is untouched — a
test now says so explicitly.

Mouse aim arrives through `IAimSource`, declared in Input and implemented in Gameplay by
`PointerAimSource` — the same inversion already used for `IGridProjection`, because unprojecting the
pointer needs the camera and Input may not reference Gameplay. The ground plane is taken at the
player's own height, not at zero, so shots go where the cursor is rather than landing slightly short.

`CompositeInputSource` now merges **aim separately from movement and buttons**. Standing still while
lining up a shot is the most common thing a player will do with a skillshot, and treating aim as
"activity" would let a resting mouse outrank a held gamepad.

**Bindings.** Keyboard: `Shift` dash, `Q`/left-click skillshot, `E`/right-click third slot.
Gamepad: `B` dash, `X` skillshot, `Y` third slot, right stick aims. With no aim supplied the shot
follows the direction of travel, so keyboard-only play is never blocked.

### Known gaps

- **No mobile skill buttons.** The touch surface still offers only the stick and BOMB. Deliberate on a
  PC-first build; it becomes real work at the mobile port, not before.
- **The third slot is empty**, waiting for M5 to put an item in it — which is also how open question
  #1 gets answered.
- **No dedicated dash visual.** The movement itself is loud enough to read; a trail is polish for
  later. The HUD does show live charges, which is the readout that changes decisions.

### Play verdict (2026-08-07)

**The first clause of the slice question is answered: yes.** Moving freely, aiming a skillshot and
setting off grid-shaped explosions *do* feel good together. That is the hybrid premise itself, and it
was the one thing no amount of architecture could establish. The second clause — *"does changing one
item visibly change how I play?"* — remains open and is what M5 exists to answer.

Caveat for the record: this is the designer's verdict, not a playtest. The §3 success thresholds are
measured against players who did not build the thing.

**The dash is an offensive tool, not just an escape.** Reported unprompted: it is used to fight mobs,
not merely to clear your own blast. That was not designed for and it is the best news in the
milestone, because it means the dash carries **two competing uses on one charge** — you cannot dash in
*and* dash out. That tension is what makes it a skill rather than a button, and it means the escape
tuning recorded above was only half the story.

> **Design opening, and a warning for M5.** The obvious dash item — *a second charge* — is a far
> bigger power spike than it looks. It does not merely double the escapes; it converts "in **or**
> out" into "in **and** out" and deletes the decision that makes the dash good. It should be
> expensive, late, or carry a real cost. **Cooldown reduction is the safer dash item**: it shortens
> the committed window without removing the choice.
>
> Relatedly: **the dash currently grants no immunity frames**, and that is now load-bearing rather
> than incidental. Dashing past a mob is risky on purpose. Adding i-frames later would silently
> convert the skill from positional to defensive — so if an item grants them, that is a build
> identity to design deliberately, never a stat to tack on.

---

## 4d. M5 notes (2026-08-07)

**Delivered.** An item framework, three items, two passive slots, and a build readout.
**337 EditMode + 10 PlayMode tests green, zero warnings.**

### Synergy without a synergy table

An item is `ItemEffect`: a target skill (or *all* of them), some **traits** to graft on, and some
**numbers** to shift. Nothing in it names a dash or a skillshot.

Traits are the behavioural axis — `DetonatesBombs`, `DamagesContacts` — and they are generic in the
same way the numbers are: a trait says *what happens on contact*, not which skill is doing the
touching, so the same flag reads sensibly on a projectile and on a dash.

> **This is the whole design.** Synergy emerges from traits and numbers composing. There is no table
> of item pairs anywhere in the codebase, and there must never be one: such a table grows as the
> square of the item count and is the standard way this kind of system dies. Adding an item is a row
> in `ItemCatalog`, not a branch in a system.

### The three items

| Item | Effect | Axis |
|---|---|---|
| **Overcharge** | Skillshot sets off bombs it flies over | behaviour |
| **Momentum** | Dash injures what it passes through (+40 flat power) | behaviour |
| **Kinetic Core** | Every skill +50% magnitude | numbers |

Three items into two slots means every run **leaves one behind** — which is exactly the "deliberately
pick a different item on run 2" experiment the slice measures.

Any pair plays differently. Overcharge + Kinetic Core is a long-range remote detonator. Momentum +
Kinetic Core is a 4.5-tile damaging charge. Overcharge + Momentum is the full loop: dash through the
mob, drop a bomb behind you, dash clear, shoot it. **None of those combinations is written down
anywhere** — they fall out of two independent effects landing on the same loadout.

### Overcharge is nine lines, because it reuses everything

A detonating shot **sets the bomb's fuse to zero** rather than exploding it. `FuseSystem` then finds
it due later in the same tick, and the shared detonation queue, chain reactions and the queued-guard
against a ring of bombs triggering each other forever all apply unchanged. Detonating directly would
have meant a second copy of all of it.

Two consequences worth stating:

- **The tick order was reversed from M4.** Skillshots now run *before* fuses rather than after
  enemies, so a triggered bomb goes off in the same tick — otherwise shooting a bomb would feel like
  asking it politely. The cost is that a shot is judged against enemy positions from the end of the
  previous tick; at 80 units of enemy movement against a 660-unit overlap window, that is not
  detectable. This reverses a decision recorded in §4c, deliberately.
- **The bomb under your feet is exempt.** Without it, equipping Overcharge would turn every shot
  fired while standing over your own bomb into a suicide — and would quietly contradict the
  walk-off-your-own-bomb grace the game already grants. Same exemption, same reason.

**Overcharge also answers open question #3 in the right direction.** It does not compete with the
bomb; it makes the bomb *more* central by turning a timer you plan around into a trigger you hold.

### Applied once, not recomputed

Items permanently rewrite the loadout when taken. A run only ever adds items, so recomputing
effective stats every tick would cost work every frame to support a case that never happens, and
would need a stable ordering rule to stay deterministic.

**The cost, recorded honestly:** an item cannot be removed, and the loadout no longer remembers its
base values. The inventory is kept anyway — no tick reads it — because it is what a readout shows,
what a save would store, and what a recompute-from-base would need if removal is ever required.

Order-independence holds today because **no field takes both a flat addition and a percentage**
(power is flat, magnitude is percentage). A test compares the two grant orders and would catch it if
that ever stopped being true.

### Honouring the M4 warning

The M4 play verdict recorded that a second dash charge would convert "dash in **or** out" into "in
**and** out" and delete the decision that makes the dash good. **No starting item grants a dash
charge, and a test enforces it** by walking the catalog — so a future item cannot reintroduce it by
accident without someone deliberately deleting that test.

### How to try it

`Match` scene → `MatchInstaller` → **Loadout → Starting Items**. Set two and play; set a different
two and play again. Milestone 6 replaces this with a choice between arenas — granting from the
Inspector until then is what lets the slice's real question be answered before a run loop exists.

The build is shown in the HUD, because the slice measures whether players can describe their build
unprompted and one they cannot see is one they cannot describe.

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
