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
>
> ### ✅ Answered yes, both clauses — M4 (2026-08-07) and M5 (2026-08-07)
> First clause at M4; second at M5, on the designer's own report of having to **change how he played
> based on the items he was carrying**. That is the synergy pillar landing, and it is the bet the
> whole v2.0 revision was made on.
>
> **This is not the validation gate.** The gate below is measured against players who did not build
> the game, and it needs the run loop (M6) to exist first. What is settled is that the concept is
> worth taking to that gate — which was genuinely in question until now.

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

> **Outcome (2026-08-23, 28 testers across three rounds): called, and not on the item metric.**
> Metric 1 was never successfully measured — round 1 was too small, round 2 measured the controls,
> and round 3's runs grew long enough that half the testers played one instead of two (§4o). It is
> closed on converging evidence instead: **9 of 12 described their build unaided**, in playstyle
> language they chose themselves — *"a walking artillery"*, *"a dash-bomb hybrid with pierce and
> cooldown"* — with metric 4 at 100% making that session the first trustworthy one. Weaker than a
> clean number on metric 1 and stated as such. **Touch is excluded from this: 0 of 3 could describe
> a build**, which is an item-card legibility failure and is tracked as build work, not as an open
> gate.

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
| **M5** | Item framework + three items + two passive slots | ✅ **complete, verified in play** |
| **M6** | Run loop: arenas, item choice, death, restart | ✅ **complete, verified in play** |
| — | *Gate enablement* — procedural sectors, feedback layer, run persistence, touch controls, three platforms, deployment | ✅ complete (§4e–§4l) |
| — | **▶ VALIDATION GATE** — the question in §3 | ✅ **called 2026-08-23 on 28 testers over three rounds.** Not on metric 1, which was never measurable; on 9/12 unaided build descriptions with metric 4 at 100% (§4o). Rounds stopped. **Touch item cards and the bomb-primacy question carry forward as build work** |
| M7+ | Third skill, slots 3–4, bosses, meta, art, audio | **unblocked.** Note that M7's exit criterion in [04-ROADMAP](04-ROADMAP.md) — *"≥ 60% of playtesters change their loadout between runs"* — has the identical two-run flaw that sank metric 1, and needs redefining before it is measured |

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

### Play verdict (2026-08-07)

**Answered: yes.** All three items tried across both slots. The report — *"I needed to adapt, and
based on the skills I got I need to change my gameplay"* — is the second clause of the slice question,
in the designer's own words and unprompted.

Two things that carries beyond "it was fun":

- **The items are legible.** Adapting to a build requires being able to tell what the build *does*.
  Had the effects been numeric nudges, the honest report would have been "it felt about the same".
- **Adaptation was forced, not offered.** Changing playstyle because of what you are carrying is the
  synergy pillar working. It is also the strongest available evidence that three items into two slots
  is enough scarcity to make a choice matter, which was not obvious in advance.

**What this does and does not settle.** The vertical slice's question is fully answered and the hybrid
concept is validated at prototype scale. The §3 success thresholds remain unmeasured — they are about
players who did not build the thing, and they need M6. The risk now shifts from *"is this fun?"* to
*"does it stay fun across a whole run?"*, which is open question #2 and squarely M6's job.

---

## 4e. M6 notes (2026-08-07)

**Delivered.** The run loop: clear an arena, choose one of three items, carry the build and your
damage forward, die, restart. **358 EditMode + 10 PlayMode tests green, zero warnings.**

### The decision that shaped it

> *"After 2 or 3 tries players will want to restart fast, so clean restart is OK."* — 2026-08-07

Taken as two instructions, not one. Clean restart is the design; **"fast" is a technical
requirement**, and it is why a restart rebuilds in place rather than reloading the scene. Pools,
materials, the camera rig and the input stack all survive. Restarting costs about as much as walking
through a door, and a test asserts that two hundred restarts complete in well under a second — if a
single one were ever to become expensive, that is what would catch it.

### Where the decisions live

`GameRun` is engine-free and holds the entire loop: arena order, offers, carry-forward, death,
restart. `RunController` — the only new MonoBehaviour — watches for exactly one thing:

> **`GameRun.Current` is a different object than the one on screen.** Every transition that starts an
> arena produces that signal, so nothing has to enumerate which transitions those are, and a new one
> added later needs no change here at all.

That is the same split that has paid off at every milestone: the loop was written and proven before
any of the view work existed.

### Rules worth stating

- **Clear condition is "everything that spawned is dead."** An arena that spawns nothing has *no*
  clear condition rather than being instantly won — which is what keeps an empty test room a sandbox
  instead of a match that ends on tick one.
- **Damage carries between arenas**, with a partial heal (25) for clearing one. Full healing would
  remove the reason to play carefully; none at all makes a third arena arithmetic rather than a
  fight. **This is the number most likely to need tuning**, and it is Inspector-visible.
- **An empty choice is never presented.** With slots full the run rolls straight on.
- **Offers are rolled by the run's own generator**, so which items appeared is part of the
  reproducible run rather than a roll the replay cannot see.
- **A starting build now seeds the run** and survives a restart, occupying real slots so the run
  offers correspondingly fewer. It is a development aid for trying a pairing without playing up to it.

### The honest limit: this cannot answer open question #2

Three items into two slots means a run contains **exactly two meaningful choices**, after which it is
survival only. That is enough to prove the loop works, and it is *not* enough to answer *"does it
stay fun for twenty minutes?"*.

> **The item pool is the binding constraint on run length, and it is content work, not slice work.**
> Question #2 stays open until there are enough items that arena five still presents a decision. The
> gate can be attempted before then, but a tester who runs out of choices after two arenas is telling
> you about the pool size, not about the design.

### Three arenas authored

Text layouts in the installer, cycled in order: the original open grid, a chambered layout, and a
wide corridor arena. `T-025` still replaces authored text with a `LevelDefinition` asset; nothing
about the loop changes when it does.

### Play verdict (2026-08-07)

**The loop holds together.** Played end to end — clear, choose, carry forward, die, restart —
and reported as *"pretty good at this stage"*. Taken as what it says: the loop works mechanically and
is not yet claimed to be finished. Every system the vertical slice specified now exists and has been
played.

**Deliberately not claimed.** This verdict does not cover the things M6 most needs to learn, because
they were not reported and inferring them would be inventing data:

- whether a run goes flat after its two choices are spent;
- whether the restart is fast *enough* to keep a player going for a third attempt;
- whether 25 health per arena clear is the right number.

The first of those is the one that matters, and it is question #2 — which stays open and is now
blocked on content, not systems.

**Milestone status: the slice is built.** What stands between here and the validation gate is
playtesters and, on the evidence of §4e, more items to choose between.

---

## 4f. Item pool widening (2026-08-07)

**Delivered.** Nine items, offers that continue once slots are full, and the ability to decline one.
**373 EditMode + 10 PlayMode tests green, zero warnings.**

### The correction that shaped this

The plan was "widen the pool". Halfway in, that turned out not to be sufficient on its own:

> **More items does not give a run more decisions.** With two slots you get two choices whatever the
> pool size — arena five is still choiceless. Pool size drives *variety between runs*, which serves
> "picks differently on run 2". It does nothing for §4e's actual complaint.

So the offer had to keep coming once slots were full. That means **swaps**, which means removing a
held item — the exact thing the M5 notes recorded as impossible.

**It turned out to be free.** That limitation lived inside a single `GameSimulation`; a run already
rebuilds the loadout from scratch for each arena by re-granting the held list. Removing an item is
list manipulation, and the next arena simply never applies it. The recorded cost never came due, and
a test now proves a swapped-away item's *effect* is gone rather than merely unlisted.

### The pool

| Item | Effect | Axis |
|---|---|---|
| Overcharge | Skillshot sets off bombs it flies over | behaviour |
| Momentum | Dash injures what it passes through | behaviour |
| Piercing Rounds | Skillshot is not used up by the first enemy | behaviour |
| **Bomb Trail** | Dashing lays a bomb where you left | behaviour |
| Kinetic Core | Every skill +50% magnitude | numbers |
| Overclock | Every skill −25% cooldown | numbers |
| Quickstep | Dash −40% cooldown | numbers |
| Focusing Lens | Skillshot +30 power, −30% magnitude | **trade** |
| Twin Shot | Skillshot +1 charge, +25% cooldown | **trade** |

Two of them are deliberately *trades* rather than upgrades, so an offer can be genuinely declinable
rather than always-yes. Two new traits carry four of the behaviour items; the rest are numbers.

**Bomb Trail is the strongest pairing in the pool and is written down nowhere.** With Overcharge it
becomes place-and-trigger at will: the dash lays the bomb, the shot sets it off. That is two
independent effects composing, which is the whole thesis. It is bound by the same bomb capacity as
the button — an item may add a *way* to place bombs, never a way to place *more* of them, or it
quietly breaks the economy the Bomberman layer rests on. Tested.

### Rules the offers now follow

- **Full slots offer a swap, not nothing.** Late in a run the question stops being "what do I want?"
  and becomes "what am I willing to give up?" — a better question that costs nothing extra to ask.
- **Any offer can be declined.** Without a skip, a late run would force a player to break a build
  they were happy with, turning a decision into a penalty for having chosen well.
- **A swap is two steps** — take, then choose what to give up — and can be abandoned at either.

### Still honouring the M4 warning

No item grants a **dash** charge. Twin Shot banks a second *skillshot*, which is a different skill
and a different decision. The catalog-walking test still enforces it.

---

## 4g. Play feedback and disposition (2026-08-07)

Six items raised after playing the widened pool. **374 EditMode + 10 PlayMode green.**

### Fixed now

**Item descriptions on the choice and swap screens.** Cards now carry a sentence saying what changes
about how you play, with any cost stated in the same breath. This was not polish:

> *"I'm mainly swapping just to try every skill."*
>
> That is the gate metric **"deliberately picks a different item on run 2"** failing for a user
> interface reason rather than a design one. A player who cannot read what an item does picks at
> random, and random picks read in the data as the synergy pillar failing. Descriptions had to land
> before any playtest, or the test would have measured the wrong thing.

A test now requires every catalogued item to carry a description of real length, so a new item cannot
reach a choice screen nameless.

**On-screen controls no longer appear on desktop.** The stick and BOMB button are gated on an actual
touchscreen being present rather than on the platform name, so a Windows tablet still gets them and a
desktop build does not. A hidden stick is also no longer sampled — an invisible control feeding the
simulation whatever it was last left holding is a bug waiting to happen. Inspector override for
testing from the Editor.

### Sequencing note, deliberately recorded

The report that swapping felt exploratory rather than deliberate arrived **before** descriptions
existed. That observation should be re-taken now, not designed around. Building enemy variety to fix
a problem that a sentence of text may already have fixed would be the expensive way to learn that.

### Accepted, not yet built

| Item | Disposition |
|---|---|
| **More arenas via simple PCG** | Accepted. Deterministic generation from the run seed, engine-free, in `Simulation`. Must guarantee a safe spawn pocket and a connected board — an arena that walls the player in is worse than a repeated layout. |
| **Attacking towers / statues** | Accepted in principle, as an *arena feature* rather than a mob. See below. |
| **Squash-and-stretch on the arena border** | Accepted, view layer only. |
| **Skill-ready and recharge indication** | Accepted. Currently only the numeric charge count in the readout. |

### On towers: the argument for them is specific to this game

The generic case ("MOBAs have them") is weak. The strong case is that **a tower is the only threat
that would make destructible blocks matter defensively.**

Today the maze matters for skillshots, which it blocks, and for movement. Nothing makes a player
*want* cover. A tower that shoots on sight turns every destructible block into protection — and the
player's primary verb is a bomb that destroys protection. **Your main tool eats your own cover.**
That is a real tension the design does not currently have anywhere, and it costs one new entity type
to get.

It also answers the concern behind the swapping report in the right shape: a static zoning threat
changes *what a build has to answer*, without needing new mob AI at all.

Two constraints if it is built:

- **Chip damage, not lethality.** With a 34-damage own-bomb and an immunity window, a hard-hitting
  static threat would be punishing in a way the design has carefully avoided.
- **It must telegraph.** A wind-up the player can read and dodge, or it becomes an unfair tax on
  standing still.

Mechanically it is close to free: a tower fires a projectile, and `ProjectileSystem` already flies,
collides and damages. Placement is a natural fit for the PCG work.

### On "changes direction or stops after a long run"

Two readings, and they are very different jobs:

- **A visual flourish** — lean, skid, dust on a hard stop. View only, no simulation change, safe.
- **Real momentum** — acceleration and slide in the movement rules. That is a simulation change: it
  alters the 360° feel already validated at M2b/M4, and it invalidates any recorded run.

Worth settling which is meant before either is built. The first is polish; the second is a change to
the thing that has already been signed off.

---

## 4h. Movement: the gamepad toll (2026-08-08)

**Reported.** *"The player is slowed down by the obstacle when playing, it happens more when playing
with a gamepad; with the keyboard I have better precision so I don't feel that."*

**That comparison is the diagnosis.** It is the same tile-versus-box mismatch that was wedging
enemies, and the input device is what decides who pays for it:

> A one-tile corridor leaves a fraction of a tile of slack around the player's box. Pressed against
> one side, they clip the corner of every pillar they pass and stop dead for a few ticks at each.
> **Keys are perfectly axis-aligned, so a keyboard player never drifts off-lane and never pays the
> toll.** A stick is a degree or two off almost always, and pays it at every junction.

**Fix: lane assist proportional to axis alignment.** Full help running straight down a corridor,
fading to nothing by roughly 27° off-axis. A deliberate diagonal is untouched, so movement stays
continuous rather than railed — the thing the whole hybrid rests on. Inspector-tunable 0–1, because
this is feel work and the dial belongs in the designer's hands.

**Dash is deliberately excluded.** It has committed to a heading, and curving it onto a lane
mid-flight would undo the commitment that makes it read as a dash rather than a speed boost.

### Corner slip may now be partly redundant

`CornerSlip_CanBeDisabled` failed when assist landed — not because assist broke anything, but because
**assist recentres the player before a corner is ever clipped**, so corner slip was never asked to do
anything. The test now pins assist to zero so it measures the helper it names.

That is worth knowing rather than acting on. The two cover different cases: assist only engages when
travelling near-axis-aligned, so corner slip still owns everything diagonal, where assist is zero by
design. If a later pass wants to simplify, this is the pair to look at — with the warning that the
diagonal case is the one no test currently isolates.

---

## 4i. Touch controls (2026-08-08)

**Delivered.** Analogue touch movement and MLBB-style drag-to-aim skill buttons.
**397 EditMode + 14 PlayMode green, zero warnings.**

### Found on the way in: touch was still playing v1.0

`TouchInputSource` was snapping the stick to one of four grid directions with hysteresis and a
change buffer, and its documentation still described "the isometric control problem" — a concern
retired at M2b. Correct for the lane-based v1.0 game; **wrong from the moment the player gained free
360° travel.** A phone was playing a different game from a keyboard, and adding aiming on top of
cardinal movement would have built precision onto a control that could not express it.

Movement is now analogue and quantised exactly as every other source does it. `InputFeelConfig`'s
`SwitchRatio` and `ChangeBufferSeconds` are consequently unused; they belong to a movement model
that no longer exists.

### The control: each skill button is its own stick

Tap to cast with no aim — the simulation falls back to the direction of travel. Press and drag to
aim, release to fire, drag into the cancel zone to abandon it.

> **Why this rather than a second thumbstick.** With three skills, one shared aim stick cannot know
> which skill you meant, so aiming and choosing would take two gestures. Making each button its own
> stick collapses them into one. The tap path matters as much as the drag: most casts do not need
> precision, and requiring a drag for all of them would make the game feel slow.

A cast is **latched on release and held until the simulation reads it**. Ticks run at 30 Hz and
fingers do not; a press resolving between two ticks would otherwise be lost. It is delivered exactly
once, because skills trigger on the press edge and a repeat would waste a charge.

### A bug only a test could have found

`OnPointerUp` hid the aim indicator *and the cancel zone* before asking whether the release had
landed in the cancel zone — so the zone was always inactive by the time it was tested and
**cancelling could never work**. On device this would have read as "cancel is broken" with no clue
why. Ten gesture tests now drive the pointer events directly.

### Correction (2026-08-22): the dash never read its own drag

Recorded here rather than quietly fixed, because "delivered" above was two-thirds true for two
weeks. Every skill button offered the drag gesture, and `SkillSystem.TryDash` resolved its heading
from `intent.MoveX/MoveY` alone — so a drag on the **dash** button rotated the on-button indicator,
packed an aim into the intent, and was then discarded. Tapping and dragging the dash did the same
thing. The shot honoured its aim throughout, which is why nothing looked wrong.

It surfaced the moment the ground arrow (§4n) drew that aim in the world: the arrow pointed one way
and the dash went another, which is worse than no arrow at all.

**The missing piece was ownership, not aim.** The intent's two aim bytes cannot say *who the aim
belongs to*, and the answer differs by device — on touch each skill button is its own stick, so a
drag belongs to that skill; on a pad the right stick is a standing aim belonging to nothing, and a
dash that followed it would launch the player into the enemy they were escaping. `IntentButtons`
had a spare bit, which is now `AimedCast`: set by touch on a released drag, never by the pad or the
mouse. `TryDash` honours the aim only when it is set, so the pad keeps the twin-stick convention of
dodging along the movement stick and touch finally gets the control §4i designed.

### Still missing on touch

No pause control on screen — Escape maps to the Android back button, so hardware back pauses, but
there is no on-screen equivalent. Skill cooldown state is also invisible, which matters more on a
touch screen than anywhere else, since there is no controller rumble or key feel to fall back on.

---

## 4j. Device build (2026-08-08)

Built, installed and confirmed on a Galaxy S21 Ultra. Touch controls, pause, the run HUD and the
readability changes all verified running. **397 EditMode + 14 PlayMode green, zero warnings.**

### Two bugs that only a device could show

Both passed every test and looked perfect in the Editor.

**The Physics module is stripped from a player build.** `PlaceholderMeshes` built its meshes with
`GameObject.CreatePrimitive`, which attaches a collider — and nothing else in this project references
physics, because every collision is resolved on the grid. So the module was stripped and every mesh
logged `Can't add component because class 'MeshCollider' doesn't exist!`, which forced the
development console over the play area. The meshes themselves were fine, so it presented as log
noise rather than as a bug. Now loads the built-in meshes directly and pulls in no module at all.

> **The general lesson: the Editor never strips anything.** Any code that reaches for an engine
> feature the game does not otherwise use will work in the Editor and fail in a build. View-layer
> code written entirely against the Editor is exactly where this hides.

**The HUD was drawing the build off-screen.** The readout box is authored at 700×70 and the line had
quietly grown to carry arena, health, enemies, skill charges *and* the item list. It wrapped to a
second line the box then clipped, so on device the charges and the whole build were invisible —
which would have silently broken the "can describe their build" gate metric at the first playtest.

### Also on device

- The skill cluster overlapped the bomb button, because its offsets were fixed pixels against an
  anchor that is larger on a phone. Now derived from the anchor's own size.
- `PAUSE` reused the hub's 420×120 menu button size; reduced to 170×84. It keeps the word rather
  than a glyph: the greybox draws with Unity's built-in legacy font, which has no coverage for
  symbols like U+23F8, and a missing glyph renders as an empty box.
- **Blocks now fill 88% of their tile**, so floor shows between them and the maze reads as separate
  pieces rather than one mass. Purely visual — collision is still resolved on whole tiles. The inset
  is kept modest on purpose: a wide gap between two solid pillars looks like a route, and being
  stopped by an opening you can see is worse than being stopped by an obvious wall.

### Not yet reported

The layout is confirmed to *look* right. How drag-to-aim actually *feels* — button reach, drag
radius, the tap-versus-drag threshold — is still unmeasured, and only a thumb can answer it.

---

## 4k. Procedural arenas (2026-08-08)

**Delivered.** Seeded arena generation in three styles, wired into the run and verified on device.
**412 EditMode + 14 PlayMode green, zero warnings.**

### Three styles, because density alone is not variety

`Lattice` is classic Bomberman — a pillar every second tile, open and connected by construction.
`Scattered` places loose pillars, never adjacent to another, reading as open ground with cover.
`Chambers` runs long walls with at least two doorways each, making rooms and sightlines.

> Re-rolling block density produces the same room with more clutter in it. Changing the *structure*
> is what makes an arena feel like somewhere else.

Two rules are load-bearing rather than decorative. Scattered pillars are **never placed adjacent**,
because two neighbouring random pillars start closing corridors and four close a room. Chamber walls
get **at least two doors**, because a room with one exit can be sealed by a single bomb — that is a
trap, not a space.

### Guarantees, enforced rather than hoped for

- **No unreachable ground.** The board is walked from spawn and anything it cannot reach becomes
  solid rock. Sealing rather than carving: a corridor cut to an isolated pocket is a dead end nobody
  asked for, whereas making it wall simply means the arena is slightly smaller than the rectangle it
  was rolled in, which no one can perceive.
- **A clear pocket at spawn**, never refilled with blocks. A spawn with one exit is a death sentence
  dressed as a layout: the first bomb placed has nowhere to run to.
- **Enemies start at a distance**, on open ground, each with at least one free neighbour — an enemy
  sealed in by blocks turns clearing the arena into excavation.
- **Something to blow up.** A board without destructibles is a corridor crawl.

Every one of these is asserted **across 120 seeds**. A generator that produces a good board most of
the time is not a working generator: the one run that walls a player in is the run they remember,
and it will never be the seed a single-seed test happened to pick.

### Arenas get their own generator

Each arena is rolled from a generator seeded for that arena alone, not from the run's shared stream.
Otherwise the board would be reshaped by anything else that drew a random number first — one extra
item offer would silently change every layout after it.

### A C# trap worth remembering

`ArenaSettings.Default` was written as `new ArenaSettings()`. A struct always exposes an implicit
parameterless constructor that zeroes every field and **ignores the defaults declared on the real
one**, so it produced an arena of size zero. `Validate` caught it immediately, which is the argument
for having written `Validate` at all.

### Tuning

Generation is on by default and Inspector-exposed on `MatchInstaller`: size, destructible density
and starting enemy count. The authored layouts are kept rather than deleted — variety is what a run
wants, but tuning anything needs the *same* board twice, and a seed is a clumsier way to ask for
that than a list.

---

## 4l. Build targets (2026-08-08 / 09)

Three platforms build and run. **421 EditMode + 14 PlayMode green, zero warnings.**

| Target | Size | Build | State |
|---|---|---|---|
| Android | 86 MB dev | 237 s | Device-verified on a Galaxy S21 Ultra |
| Windows | 92 MB release | 36 s | Launches clean, no errors in the player log |
| **WebGL** | **10 MB release** | 322 s | Runs windowed and fullscreen |

**WebGL is the gate target.** For a validation gate, friction decides sample size: a link someone
clicks reaches an order of magnitude more testers than an installer or a sideloaded APK, and the
people put off by a download are exactly the ones whose "did they come back for a second run?"
answer is worth having. It is also, at 10 MB, by far the smallest of the three.

Brotli with the decompression fallback, so it serves from any static host without content-encoding
configuration. Data caching on, so a reload or a return visit does not pay the download again.

### A correction worth recording

WebGL was scoped as a port on the strength of two `Awaitable.BackgroundThreadAsync()` calls in
`SaveService` and the fact that WebGL has no threads. **That was wrong** — the file already carried
`#if UNITY_WEBGL && !UNITY_EDITOR → PlatformSupportsBackgroundIo = false`, designed in at T-005. The
grep hit was read without reading the guard directly below it.

### The one real bug: black screen on fullscreen

Caused by `runInBackground = false` in the build settings. On WebGL the player stops rendering when
the canvas loses focus, and going fullscreen swaps the canvas and moves focus with it — so it came
back black rather than visibly paused. Set to true, which is the correct default for the web anyway.

`Application.Quit()` also does nothing inside a browser tab, so the hub's QUIT control is compiled
out on WebGL rather than shipping a button that visibly fails.

### Still unverified on WebGL

Save persistence across a page refresh — `persistentDataPath` is IndexedDB there, and whether it
survives a reload is the one thing that cannot be inferred from a successful build.

---

## 4m. The competitive bet (2026-08-09)

Recorded because it shapes what gets built after the gate.

> *"I think players will be more focused on gameplay and game mechanics, leaderboard and
> multiplayer. It's like a bet."*

**The architecture already bets the same way**, and not by accident:

- The simulation is deterministic and engine-free, and a match is fully described by its seed, its
  layout and a sequence of `PlayerIntent`. That is a **replay format**, and a replay format is what
  makes a leaderboard score *verifiable* rather than merely reported. Most small games cannot do
  this, and it is why `PlayerIntent` was kept to a few bytes from the first milestone.
- Intent-as-input over a fixed tick is the shape lockstep and rollback netcode both need. Multiplayer
  would be a matter of delivering someone else's intents, not of rewriting gameplay.

**The cheapest competitive hook is already sitting there: a shared daily seed.** Everyone plays the
same generated sector on the same day, and the leaderboard compares like with like. Seeds are already
a first-class concept and arenas already regenerate exactly from one, so this is small — and it turns
a single-player roguelite into something with a reason to return tomorrow.

Two consequences to settle before that ships, both cheap now and awkward later:

- **Resume and competition do not mix.** A run that can be restored is a run that can be retried from
  a good position. A competitive mode either disables resume or records that it happened.
- **A verified score needs the intent log kept**, which nothing currently records. Small to add while
  the format is stable; unpleasant to retrofit once runs are long.

**What it does not change is the gate.** Whether people compete is a different question from whether
the moment-to-moment game is good, and a leaderboard cannot rescue a loop nobody wants a second run
of. The current gate validates the thing any competitive layer would stand on.

**Multiplayer remains the single largest item in the project** and should not precede validation. The
GDD already defers the backend to Milestone 9, and nothing here argues with that.

---

## 4n. Round 2: the gate measured the controls (2026-08-19 → 22)

Full sheets and metric working in [10-PLAYTEST-PROTOCOL.md §10b](10-PLAYTEST-PROTOCOL.md). Recorded
here because it changes what happens next, not because it settles the question in §3.

**12 testers, enough to call the gate, and the gate was not passed.** Metric 1 came in at 42%
against a 60% threshold — but metric 4 came in at 62% against 80%, and §1 of the protocol is
explicit that metric 4 is a filter rather than a verdict. A session where deaths are blamed on the
controls is a session that measured the controls. **The item number is therefore not a result about
items.**

What makes it worth acting on rather than re-running is that the failure was not spread evenly:

| Device | Deaths blamed on self | Voluntary second run |
|---|---|---|
| Keyboard | **100%** (8/8) | 4/4 |
| Gamepad | 50% (4/8) | **0/4** |
| Touch | 38% (3/8) | 4/4 |

**The design works; two of the three ways to play it do not.** Keyboard players are the only ones
with a cursor showing where a shot goes, and they are the only ones who passed. Every gamepad tester
declined a second run, and nobody else did.

The gamepad cause was physical rather than a matter of taste: skills sat on the face buttons, aiming
needs the right thumb on the right stick, and both cannot be true at once. Touch failed next door to
it — the aim indicator was drawn on the skill button, which is the one place on a phone guaranteed
to be under a thumb.

Fixed since: skills moved to LB/RT/LT with the face buttons kept as aliases; the aim held 0.35 s past
the stick centring; the deadzone cut to 0.2 and rescaled radially; a dash following the last analogue
heading instead of the 4-way facing; and a fat arrow drawn on the ground under the player, fed from
the aim already in `PlayerIntent` so touch and gamepad light it up through the same two bytes. The
arrow is tester 03's, in their words: *"I need a fat arrow on the ground oriented to the enemy when
shooting."*

**Metric 2 was not recorded at all** — twelve testers and no item-legibility number, which is the one
figure that would say whether 42% is a design result or an interface result. It is the first thing
round 3 must capture.

**Round 3 needs fresh testers on gamepad and touch.** The eight who blamed the controls were
measuring controls that no longer exist.

---

## 4o. Round 3: the controls stopped being the answer (2026-08-23)

Full sheets and metric working in [10-PLAYTEST-PROTOCOL.md §10c](10-PLAYTEST-PROTOCOL.md).

**12 testers. Metric 4 went from 62% to 100% — seventeen deaths, seventeen self-attributions, and
nobody blamed the controls out loud on any device.** Metric 5 stayed at zero. §1 makes metric 4 the
filter that says whether the rest of a session can be believed, so **round 3 is the first round
whose other numbers mean anything.**

The depth change is the clearest signal that the input work landed. Nothing was added between the
rounds except controls that work:

| | Round 2 | Round 3 |
|---|---|---|
| First run, mean length | 10.1 min | **22.4 min** |
| First run, mean arena at death | 3.0 | **7.2** |

**Metric 2 was measured for the first time and passes at 75%** (9/12 described their build unaided).
The answers are specific — *"a dash-bomb hybrid with pierce and cooldown"*, *"like a walking
artillery"* — and Leela's is metric 1 and metric 2 in one sentence: *"First run I had a long-range
shot build with cooldown, but second run I went all-in on dash and bombs; way more fun."*

**Metric 1 still cannot be called, and the reason has inverted.** Round 2 could not measure it
because the controls ruined the session. Round 3 cannot measure it because **the game got good
enough that half the testers played one long run instead of two short ones** — six of twelve, and
they are the 26-to-34-minute runs reaching arenas 8, 9 and 10. A metric defined as *"picks
differently on run 2"* has n = 6 against a floor of eight.

**That is a measurement problem, not a design result, and it is now the thing blocking the gate.**
The evidence for deliberate build-shaping is already sitting in the round unread: Farnsworth took
nine items and skipped three across ten arenas. Metric 1 has to be re-defined to read choices
*within* a run before round 4.

### The one clean failure is touch, and it is the item cards

Every RANDOM in the round is a touch tester, every touch tester is a RANDOM, and none of the three
described their build spontaneously where 9 of 9 keyboard and gamepad testers did. Both metrics
failing together is §7's *"they could not tell what the items do — interface first, then re-test"*
branch, and it is not the controls: no touch tester blamed them and all three aimed a skillshot at
something specific.

The item cards are text-only at phone size. Round 1 and round 2 both recorded testers clicking item
**names** expecting a description; this is the third round pointing at the same screen. **Icons on
the cards, and legible descriptions on a phone, are now the gate's critical path** — and they are
asset work, not code.

### A finding that outranks the gate in the long run

**"Placed a bomb and escaped it on purpose" fell from 12/12 in round 2 to 7/12.** Every build
description in round 3 leads with dash, pierce or shot; Bomb Trail is the only bomb-flavoured item
and sits mid-pack. Open question #3 asks whether the bomb stays the primary verb. **This is the
first evidence and it says no** — the game is drifting toward a twin-stick shooter with bombs in it.
A balance finding rather than a gate failure (§8), but it is the Bomberman half of the hybrid.

> **Refined the same day by the captured insights ([14-INSIGHTS §1](14-INSIGHTS.md)), and the
> reading above was half wrong.** Players report that bombs get roughly 80% of the kills and the
> shot feels useless — the opposite of "the bomb is losing primacy". Both observations are true at
> once: `EnemySystem` is greedy pursuit with **no awareness of live bombs**, so enemies walk into
> blasts, and bombs kill *without the player having to execute the play*. The lethality of bombing
> is total; the **skill** of bombing is what evaporated, which is what the 12/12 → 7/12 fall was
> actually measuring. The game is not drifting into a twin-stick shooter — it is drifting into an
> autopilot. Fix is on the enemy, not the bomb.

---

## 4p. Enemies learn what a bomb is (2026-08-24)

The fix for [14-INSIGHTS §1](14-INSIGHTS.md): the highest-leverage change identified in the whole
insight set, and the one every bomb-flavoured idea below it was waiting on.

### What changed

`EnemySystem` was greedy pursuit with no concept of a bomb. It now reads a **`ThreatGrid`**, rebuilt
every tick between the blast and the enemies, which stores for each tile *how many steps it is from
somewhere the fire will not reach*. Zero means safe; anything else is the shortest way out.

A distance field rather than a set of dangerous tiles, because knowing a tile is dangerous tells an
enemy to leave and not **which way** — and a greedy guess dithers at exactly the moment the player is
watching. One breadth-first sweep outward from every safe tile answers both questions at once, and
walking the number downhill is always the shortest exit however many blasts overlap.

Three rules follow from it, and nothing else in the simulation changed:

| | |
|---|---|
| Safety outranks pursuit outright | never weighed against it: no amount of chasing is worth dying for |
| Momentum never carries an enemy into fire | committing to a heading is an optimisation, and a bomb laid since invalidates it |
| An enemy clear of the fire **holds** rather than backing off | a blast is a wall that will not be there in a moment; giving up ground to it costs more than waiting — and this is what lets a player use a bomb as a wall |

### The dial is the whole design: `EnemyBombFearTicks`

**Only bombs close to going off count**, at forty-five ticks of a ninety-tick fuse. Fear a bomb from
the moment it lands and enemies simply never come near one again — which trades one broken extreme
for the other and would make bombs stop killing anything not already cornered.

The number is arithmetic, not taste: an enemy at the far end of a starting blast needs three tiles of
travel to get clear, which is thirty-eight ticks at its speed. So it escapes **with a clear path** and
dies **without one**, which is the exchange the game is supposed to be about. That margin narrows
every time the player takes a blast-range item, which is exactly the payoff a range item should have.

Zero restores the old oblivious behaviour and is kept reachable: a mob that does not understand bombs
is a legitimate archetype, just not the only one.

**Dormant Sentinels are exempt.** Something that has not noticed the player has no reason to know what
a bomb is, and bombing what has not seen you coming is the whole reward for approaching an arena
carefully.

### Found on the way in: the arena is doing a lot of the killing

The mechanism works, and measuring it in generated arenas turned up something that outranks it.

**At the shipping density, two bomb placements in five cover a pocket of floor with no walkable tile
outside it at all.** The blast fills a corridor segment bounded by destructible blocks, and there is
nowhere to run to. Every enemy death in the automated runs was of this kind — not one died because it
was too slow, which incidentally confirms forty-five ticks is a generous window.

| `destructiblePercent` | Bomb placements that seal a pocket |
|---|---|
| **55** *(shipping)* | **41%** |
| 45 | 26% |
| 35 | 14% |
| 25 | 6% |

So enemy blast awareness is necessary and **not sufficient**. Two out of five bombs still kill by
level generation rather than by play, which is a large part of what the "four kills in five come from
bombs" report was measuring — and the same density is why *"a bomb only breaks one block"* feels true
while [14-INSIGHTS §6](14-INSIGHTS.md) shows the rules destroy up to four. One cause, three
complaints.

Lowering the fill is a design decision with wide consequences — item drops, readability, arena
identity — so it is recorded here rather than taken. What is taken is a floor under it: a test asserts
the sealed fraction may not grow.

---

## 4q. Block clustering (2026-08-24)

**The density was never the problem. The distribution was.**

`ScatterDestructibles` rolled every eligible tile independently at 55%, and independent rolls produce
salt and pepper: blocks spread evenly enough to cut the maze into segments shorter than a blast. A
bomb then fills a whole segment, and the enemy standing in it dies regardless of how well it plays —
which is the "kills decided by the level" half of §4p, and, at the same time, why *"a bomb only breaks
one block"* feels true. Most arms face open floor because the blocks are not next to each other.

**The fix spends the same budget in runs.** `DestructiblePercent` now sets a target count over the
eligible tiles, and `GrowBlockRun` lays that budget down as short random walks of
`ArenaSettings.BlockClusterSize`. Seeds are drawn by the partial shuffle `PlaceEnemies` already uses,
so the loop cannot spin as the board fills.

| `blockClusterSize` | Placements that seal a pocket | Floor that is destructible |
|---|---|---|
| **1** *(the old scatter)* | **36%** | 51% |
| **3** *(shipping)* | **17%** | 51% |
| 5 | 15% | 51% |

Measured over 200 seeds. **The first row is the control**: cluster size 1 is uncorrelated placement,
and it reproduces the 41%/40% baseline recorded above, so the halving is attributable to distribution
and to nothing else. **The fill column is the claim that mattered** — it does not move, so this is a
distribution change and not the density cut that was deliberately left untaken.

Five buys two more points for visibly longer walls; three was taken. The non-monotonic bumps at four
and six look like parity against the lattice styles rather than a trend, and were not chased.

### Tests

Two, in `ArenaGeneratorTests`, guarding the two things that could quietly rot:

- **Density is independent of clustering.** If a clustered board also held fewer blocks, the
  improvement would be a difficulty cut wearing a disguise.
- **Blocks actually adjoin.** Counting *lone* blocks rather than total adjacency: at 55% fill,
  scattered blocks already touch by accident, so total adjacency moves only 1.34× and discriminates
  poorly, while lone blocks go **14% → 0%** because a block placed in a run has a neighbour by
  construction. The test asserts the scattered control stays above 10% as well, so it cannot pass by
  losing its basis for comparison.

The `EnemyThreatTests` ratchet moved with the fix, from `≤41` to `≤20`. A ratchet left at the old
number would not notice this being undone.

### Tests

Ten, in `EnemyThreatTests`. The load-bearing ones are the counterfactual — the same bomb with fear at
zero still kills the enemy where it stands, so the fix is provably doing the work — and
`WhatEnemiesFearIsExactlyWhatTheBlastWillReach`, which detonates a bomb and compares the predicted
footprint against what actually burned. The threat projection walks the same arms as `BlastSystem`
but cannot share its code, because the blast also destroys and ignites as it goes; an enemy fearing
the wrong tiles would be worse than one fearing nothing, so the agreement is proved rather than
promised in a comment.

---

## 5. Open questions

1. **Does the third active skill earn its slot?** Three actives plus movement plus aim is a lot of
   simultaneous decision-making. Worth validating two before committing to three.
2. **What does a run cost in time?** **Answered by round 2** (§4n): a first run averaged 10.1
   minutes and a second 17.6, across 24 runs. That straddles the 6–10 minute mobile session the
   original design targeted and the 20–40 minutes a roguelite usually asks for — which is why run
   persistence had to exist before the gate, and it did. Every one of the twelve got a longer run
   out of their second attempt and eleven got further, so the loop teaches. What remains unmeasured
   is whether the *decisions* stay interesting deep into a run, which needs a playtest, not code.
3. **Does the bomb stay the primary verb?** If the skillshot is more effective than bombing, the
   Bomberman layer becomes set dressing. Bombs must remain the highest-damage, highest-risk option.
   **First evidence, and it is bad** (§4o): testers who placed a bomb and escaped it on purpose fell
   from 12/12 in round 2 to 7/12 in round 3, and every build description in round 3 leads with dash,
   pierce or shot. **Re-read once the insights arrived**: the bomb had not lost primacy at all — it
   was getting roughly four kills in five while nothing on the board understood what it was, so the
   *skill* of bombing evaporated rather than its power. Enemies now run from bombs about to go off
   (§4p), which should give the shot a job and make bomb-and-escape a play again. **Both halves are
   now built:** block clustering (§4q) took sealed placements from two in five to roughly one in six
   at unchanged density, so the level is no longer doing most of the killing either. **Still not
   answered** — both changes are measured in automated runs, and whether bombing feels like a skill
   again is a playtest question. Re-measure in the next round rather than tuning further.
   **Author play cannot answer it** (2026-09-05): the person who set the fear window bombs inside
   a window players do not know exists. See [15-AUTHOR-SESSIONS.md](15-AUTHOR-SESSIONS.md).
4. **What fills the Awakening meter?** Introduced by the lore update (GDD §3.1) with no mechanic
   behind it. Damage dealt rewards aggression, damage taken rewards recklessness, and chain size
   rewards the Bomberman layer — three different games. Not required before the gate.
5. **What is a Bomb Art?** Used as a character's identity in one line of the lore and as a per-sector
   unlock in another. Character, loadout, single skill or skin — undecided.
6. **Is a Fractured Heart a currency?** Harvested from Sentinels by the lore; §9.1 already lists Data
   Coins and Cœurs Néon. Third currency, rename, or the Awakening resource?
7. **Hit-stop.** Deliberately not implemented: pausing a fixed-tick authoritative simulation on frame
   time breaks determinism and replay validation. It can be added as a simulation rule if wanted, but
   that is a gameplay change, not a visual one.
