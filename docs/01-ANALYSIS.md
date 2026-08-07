# Bomber Legends — Technical & Design Analysis (Phase 1)

**Author:** Lead Technical Director
**Date:** 2026-08-05
**Source documents:** `docs/GDD.md` v1.0 (2026-03-23), `CLAUDE.md`, `.claude/skills/unity-6.3/SKILL.md`
**Status:** Pre-production. No Unity project exists yet.

> This document does not accept the GDD at face value. Where the design is unproven, contradictory, or
> expensive, it says so and proposes an alternative.

---

## 1. High-Level Vision

**Bomber Legends** is a modernised Bomberman: a grid-based tactical action game where the player clears
destructible terrain with timed explosives under a countdown, wrapped in an original **Afro-Futurist neon
cyberpunk** identity ("Ébène-Prime", the "Génération Néon" rebels vs. the "Sombra-Corps" data corporation).

The differentiator claimed by the GDD is **replacing random power-ups with a persistent skill system**
(2 passive + 2 active slots) fed by a meta progression tech tree, monetised F2P.

### Honest assessment of the vision

The vision is **coherent but under-specified in exactly the place that decides success**. Bomberman's
appeal is a solved, well-understood mechanic; the risk here is not "can we build it" but "why would a
player choose this over the twenty other Bomberman clones on mobile stores." The GDD's answer is
*aesthetic* (afro-futurism) plus *build-crafting* (skill tree). The aesthetic is genuinely differentiating
and under-served — that is a real asset. The skill tree is not yet differentiating, because as specified it
mostly reproduces classic power-ups (range+, bomb count+, speed+) with a persistent wrapper.

**Recommendation:** the single-player campaign is the *least* defensible product in this space. The
strongest commercial version of this concept is **short-session competitive/co-op multiplayer** with the
afro-futurist identity and cosmetic monetisation — which is exactly what the listed Nakama backend implies,
and exactly what the GDD never describes. This gap must be resolved before production (see §13, Q1).

---

## 2. Core Player Fantasy

**Stated (implicit) fantasy:** *"I am a neon street-runner who out-thinks a corporate security grid — I read
the board, place one perfect charge, and the whole sector unzips while I slip through the gap."*

Three fantasy layers, in order of strength:

| Layer | Strength | Notes |
|---|---|---|
| **Spatial mastery** — reading blast lanes, escaping your own trap by one tile | **Strong.** Proven for 40 years. | This is the real product. Everything else is decoration. |
| **Cool identity** — dreadlocked runner, plasma orbs, tribal-tech neon | **Strong, under-exploited.** | Highest-value differentiator; also the most expensive to produce. |
| **Build crafting** — my loadout plays differently from yours | **Weak as specified.** | +25% speed and +1 range are stat inflation, not identity. Needs archetype-defining skills. |

**Challenge:** the GDD's own pillar list puts "Action Contre-la-Montre" (time pressure) as pillar 4, but the
timer as specified (§5) is not a fantasy — it is an anxiety tax. Time pressure in Bomberman works as an
*anti-camping* mechanic, not as a core pillar. Treating it as a pillar risks designing levels around
speed-running when the fantasy above is about *thinking*.

---

## 3. Core Gameplay Loop

As written in GDD §4, formalised:

```
SPAWN on isometric grid  →  timer starts, objectives shown
      ↓
MOVE (soft-grid, 4 directions)
      ↓
PLACE BOMB  →  3s fuse  →  cross-shaped blast, range 2
      ↓
REACT: escape own blast / dodge Sentinelles / spend active skill
      ↓
LOOT: destroyed blocks drop Data Coins + hidden Data Nodes
      ↓
OBJECTIVE MET  →  reach "Porte de Données" before timer expires
      ↓
RESULTS: score, coins  →  HUB
```

**Loop cadence problem (critical).** The GDD specifies a **3s fuse and a 5s per-slot cooldown starting at
placement** (§6.2.3). With one bomb slot that is:

```
t=0.0  place bomb
t=3.0  bomb detonates
t=5.0  slot available again      ← 2.0s of enforced dead time, 40% of the cycle
```

Classic Bomberman returns the bomb to your pool **on detonation**, so the cadence equals the fuse and the
skill is *chain placement*. The GDD's model makes the player stand still and wait, which:

- directly contradicts **Pillar 1** ("planifier leurs chaînes d'explosion"),
- directly contradicts **Pillar 4** (time pressure, while the design forces waiting),
- makes early-game (1 slot) the *worst-feeling* version of the game, which is what every new player sees.

**Recommendation:** ship the prototype with the **classic model** — capacity-based, slot returns on
detonation, no cooldown. Keep `bombCooldown` as a serialized tunable set to 0 so the GDD model is one
Inspector value away and can be A/B tested. If a cooldown is retained for F2P reasons, it must be ≤ fuse
duration so it never gates the player.

**Missing from the loop entirely:** **chain detonation** (a blast triggering adjacent bombs). The GDD never
mentions it. It is the single most important tactical mechanic in the genre and Pillar 1 is meaningless
without it. Treat as a required prototype feature, not a nice-to-have.

---

## 4. Meta Progression Loop

As written in GDD §9:

```
MATCH  →  Data Coins + Score
      ↓
ÉBÈNE-PRIME HUB
      ├─ Bomb Tech    (range, damage, simultaneous slots)
      ├─ Passive Tech (shield cooldown, max speed)
      ├─ Active Tech  (unlock new Special abilities)
      └─ Skins        (Cœurs Néon / premium only)
      ↓
NEXT SECTOR (harder)  →  MATCH
```

### Critique

1. **The loop is purely vertical.** Every upgrade makes the player numerically stronger with no trade-off.
   In a puzzle-action game, permanent power creep *destroys the puzzle*: level 1 designed for range 2
   becomes trivial at range 5, so levels must be balanced against upgrade state — which is a combinatorial
   authoring nightmare across a campaign.
   **Recommendation:** make the meta tree **horizontal** (unlock *options*: teleport / remote detonator /
   lightning chain, each ~equally powerful, differently shaped) and keep raw stats (range, slots) as
   **in-level pickups** that reset every match — the classic model. This preserves level balance, preserves
   the moment-to-moment power fantasy, and is dramatically cheaper to tune.
2. **No sinks, no retention hooks.** There is no daily loop, no energy, no season, no events, no reason to
   open the app on day 2. A "live-service mobile game" (the stated target) without a daily objective and a
   ranked/leaderboard hook has no retention spine. Nakama gives leaderboards essentially for free and the
   scoring system already exists — that is the cheapest retention hook available and should be in v1.
3. **Monetisation is unproven.** Cosmetics-only monetisation works when players *see each other*
   (multiplayer). In a single-player campaign, skin sales convert at a fraction of a percent. Another
   reason §13 Q1 is the most important open question in the project.
4. **Lives economy is undefined.** 3 hearts is a classic F2P monetisation surface (refill with premium
   currency), but the GDD never says what happens when they hit zero. Undefined fail state = undefined
   economy.

---

## 5. Session Length

**Not specified in the GDD.** This is a significant omission — session length determines level length,
timer values, energy design, and ad placement.

**Recommended target, derived from the mobile-first brief:**

| Unit | Target | Rationale |
|---|---|---|
| Single level | **90–150 s** | Fits the 3-life / countdown structure; one commute-stop of play. |
| Retry cost | **< 3 s** to restart | Death must be cheap or time pressure becomes rage. |
| Session (typical) | **6–10 min** (3–5 levels) | Standard mobile arcade session. |
| Session (committed) | **20–25 min** | Cap with soft friction (energy or diminishing rewards), not a hard wall. |

Note the GDD's "Survival: survive 3 minutes" objective (§8.3) is a **single level longer than an entire
typical session** and inverts the timer's meaning. Postpone it.

---

## 6. Target Audience

**Not specified in the GDD.** Inferred from mechanics, aesthetic, and business model:

- **Primary:** 22–40, mobile, nostalgic for Bomberman/Super Bomberman/Dyna Blaster; plays in short bursts;
  responds to strong visual identity. Skews male but the aesthetic broadens this.
- **Secondary:** afro-futurism / cyberpunk aesthetic audience (Black Panther, Afrofuturism art scene,
  Hyper Light Drifter / Sable visual-identity crowd) — this group is *culturally* under-served and is where
  organic marketing reach lives. It is the audience most likely to share screenshots. Art direction is
  therefore a **marketing investment**, not just a production cost.
- **Tertiary:** competitive party-game players — *only if* multiplayer ships.

**Challenge:** the audiences above want *different products*. The nostalgia audience wants tight classic
mechanics. The aesthetic audience wants presentation and vibe. The competitive audience wants PvP and
fairness. Prioritise: **mechanics first, aesthetic second, competition third.**

---

## 7. Platform Constraints (Mobile-First)

### Contradiction to resolve first

| Source | Platform priority |
|---|---|
| `GDD.md` §header | Mobile (iOS/Android), PC Win/Linux/Mac, **PS5, Xbox, Switch 2** |
| `CLAUDE.md` | **Windows, WebGL (itch.io)**, Android *(future)*, Steam *(future)* |
| This task brief | **Mobile-first** |

Three documents, three different priorities. Consoles in a v1 scope list from a small team is not a plan,
it is a wish. **Assumption adopted for all subsequent phases: Android-first, WebGL as the cheap
playtest/distribution channel, iOS at soft-launch, desktop as a near-free port, consoles out of scope
until the game is proven.** This must be confirmed (§13 Q2).

### Binding mobile constraints

| Constraint | Budget | Consequence for design |
|---|---|---|
| Target device | Snapdragon 7-series / Apple A12 class | 60 fps target, 30 fps floor on low tier |
| Frame budget | **16.6 ms** total, ~8 ms CPU main thread | No per-frame allocation, no LINQ, no physics queries in gameplay |
| Draw calls | **< 120** per frame | Sprite atlases mandatory; one material for the whole tile set |
| Real-time lights | **Effectively zero** | "Neon" must be **emissive sprites + a single bloom pass**, not URP 2D lights per neon element |
| Memory | **< 700 MB** working set on 2 GB devices | Streaming via Addressables; no giant uncompressed pixel-art atlases |
| Install size | **< 150 MB** base | Above ~200 MB Google Play requires Play Asset Delivery; avoid at v1 |
| Texture format | ASTC (Android/iOS), DXT (desktop), **ASTC/ETC2 fallback** for WebGL | Per-platform atlas variants via Addressables |
| Input | Thumbs occlude ~15% of screen, bottom corners | Landscape; keep critical readability out of thumb zones |
| Thermal | Sustained load throttles after ~10 min | Cap frame rate explicitly; do not render at 120 Hz on high-end |
| WebGL | No threads, no `System.IO` persistence, slow first load | Save layer must abstract storage; Addressables need a remote catalog |

### Aesthetic vs. platform tension (important)

"Detailed isometric pixel art + vibrant neon" is a **direct conflict with the mobile budget** if implemented
naively (one 2D light per neon element). It is entirely achievable if implemented as: emissive channel baked
into the sprites + a single tuned bloom in the URP 2D renderer + a small number of animated shader effects.
This must be a locked technical rule from day one, because the art pipeline depends on it.

---

## 8. Technical Challenges

Ranked by risk × cost.

1. **Isometric input mapping (highest feel risk).** In a 2:1 isometric projection, the four grid directions
   are *screen diagonals*. A joystick pushed "up" is ambiguous. This is the #1 reason isometric Bomberman
   clones feel bad. Requires: rotate input into grid basis, snap to nearest cardinal with a **dead zone +
   angular hysteresis** so the character does not flip-flop at 45°, plus **input buffering** (~120 ms) and
   **corner-cutting assist** (auto-slide into the lane when approaching a junction). The GDD's "soft-grid"
   note (§5.1) is the right instinct but names none of this.
2. **Simulation/view separation for a possible networked future.** Nakama is listed as a technical brick.
   If PvP ever ships, gameplay must be deterministic and replicable. Retrofitting that is a rewrite.
   Mitigation is cheap *if done now*: put the whole grid simulation in a `no-engine-references` assembly,
   drive it on a fixed tick, and express input as a serialisable `PlayerIntent` struct. Cost today: near
   zero. Cost later: months.
3. **Explosion propagation & chain reactions.** Must be a grid BFS, allocation-free, resolving: solid stop,
   destructible stop-and-damage, bomb → chain detonation (same tick or next tick — a *design* decision with
   large feel consequences), player/enemy damage, pickup destruction rules. Never use Unity physics for this.
4. **Sprite sorting in isometric pixel art.** Tall props, the player behind/in front of blocks, bombs on
   ramps. Requires a deterministic depth key from grid coordinates (`depth = (x + y)` plus a sub-layer),
   not Unity's default transparency sort. Getting this wrong late is a full art re-export.
5. **Neon rendering within budget.** See §7. Emissive + single bloom; a strict "no per-object 2D light"
   rule; a validated URP 2D renderer asset per quality tier.
6. **Mobile HUD readability under action.** The GDD flags this itself (§10). Four skill widgets with radial
   cooldowns + joystick + timer + lives + score, on a 5" screen, in a scene full of bloom. Requires a
   dedicated readability pass and a "HUD contrast" rule (solid backing plates, no neon-on-neon text).
7. **Grid entity occupancy.** Bombs block movement (GDD §5.2) but the player must be able to step *off* the
   tile they just bombed. Classic solution: bombs are non-blocking to any entity currently standing on them
   until it leaves. Undocumented in the GDD; a classic source of "I'm stuck" bugs.
8. **Content pipeline for 4-direction isometric pixel art.** Every character needs idle/walk/death × 4
   facings. Blocks need intact/damaged/destroy frames × variants. This scales linearly with roster size and
   is the #1 *schedule* risk (see §10).
9. **Save integrity and future server authority.** Local save now, server-authoritative later (F2P +
   leaderboards demand it). Requires an `ISaveRepository` abstraction from the first commit, versioned
   payloads, and atomic writes.
10. **Addressables on WebGL.** Remote catalogs, CORS, no synchronous loads, cold-start size. Solvable but
    must be configured before content grows.

---

## 9. Gameplay Risks

| # | Risk | Severity | Mitigation |
|---|---|---|---|
| G1 | **5 s bomb cooldown makes the core verb feel bad** (§3) | **Critical** | Ship classic capacity model; cooldown as a tunable defaulting to 0. Validate by playtest. |
| G2 | **No chain detonation specified** — Pillar 1 is unbuildable without it | **Critical** | Make it a prototype requirement. |
| G3 | **Shield trivialises the core tension.** An auto-block that recharges *during* a level removes the "don't blow yourself up" stakes that the entire game is built on | **High** | Not a starting passive. Move to meta unlock, one charge per life, no in-level recharge. |
| G4 | **Timer resets on death** (§5.3) — a player can farm the level by dying, which nullifies Pillar 4 | **High** | Timer persists across lives, or death costs a fixed time penalty. Pick one and state it. |
| G5 | **Permanent stat upgrades break level balance** (§4) | **High** | Horizontal meta; keep range/slots as in-level pickups. |
| G6 | ~~**Isometric controls feel imprecise**~~ | ~~High~~ → **RETIRED 2026-08-06** | Resolved by switching to a three-quarter top-down **square** grid: the four grid directions now map to screen axes instead of diagonals, so the ambiguity no longer exists. Cost one view class and its tests — the simulation was untouched. See `GDD.md` v1.2. |
| G7 | **"Lightning Chain" special destroys 3 blocks instantly** — bypasses the core verb; if it is ever the efficient play, the game stops being Bomberman | **Medium** | Specials must reshape tactics, not replace bombs. Cost them in a resource that competes with bombing. |
| G8 | **Speed +25% permanent passive** interacts violently with grid alignment and dodge windows | **Medium** | Speed as a bounded in-level pickup with tuned tiers, not a permanent multiplier. |
| G9 | **Time pressure + puzzle thinking are opposed motivations** | **Medium** | Generous timers as a failsafe/anti-camp, not as the difficulty source. |
| G10 | **Single-player Bomberman is a crowded, low-converting category** | **Medium-High** | Strategic, not technical. See §13 Q1. |
| G11 | **Moving platform blocks** (§8.1) interact badly with a tick-based grid sim and blast propagation | **Medium** | Explicitly postponed past the slice. |

---

## 10. Scope Risks

| # | Risk | Assessment |
|---|---|---|
| S1 | **Platform list (mobile + PC + Mac + Linux + 3 consoles)** | Unrealistic. Console certification alone is months of work per platform and requires devkits and a publisher. Cut to Android/WebGL. |
| S2 | **Engine list: "Unity3D/Axmol/S&Box"** | Three mutually exclusive engines in one document. `CLAUDE.md` resolves this to **Unity 6.3 LTS**. Treated as settled; the GDD line should be corrected. |
| S3 | **Detailed isometric pixel art** | The largest and least compressible cost in the project. A single animated character at 4 facings is days of skilled work. Content plan must be built around a **tile/prop kit + palette swaps**, and the prototype must run on greybox placeholders so gameplay is never blocked by art. |
| S4 | **Nakama + PostgreSQL/CockroachDB + Go backend** | A full self-hosted backend stack for a game with no validated fun. This is months of infrastructure serving zero gameplay value pre-validation. **Postpone entirely.** Local save first; Nakama when there is a reason (leaderboards or PvP). |
| S5 | **Full skill tree + economy + skins + premium currency at v1** | Classic pre-validation over-build. Each is real production work (UI, balancing, store, IAP compliance, receipt validation, refunds). Reduce to one upgrade track in the slice. |
| S6 | **Three enemy archetypes + four block types + three objective modes** | ~10 content systems before "is it fun?" is answered. Slice to 1 enemy, 2 block types, 1 objective. |
| S7 | **F2P live-service ambition vs. team size** | Live-service means content cadence forever. Unless the team can commit to a content pipeline, target premium/ad-supported instead. Strategic decision, needed before the economy is built. |
| S8 | **GDD §1 (Executive Summary / Pitch) is empty** | The document has no elevator pitch. Everything downstream — marketing, store page, scope arbitration — inherits that gap. |

---

## 11. Missing Design Details

Grouped by system. Each of these blocks implementation of the system it belongs to.

**Core mechanics**
- **Chain detonation:** does a blast detonate other bombs? Same tick or with a delay?
- **Blast/blast interaction:** do two blasts stack, cancel, or pass through?
- **Bomb occupancy exception:** can the player leave the tile of a bomb they just placed? (Yes, classically — must be specified.)
- **Blast vs. pickups:** are Data Coins destroyed by explosions? (Classic: yes, and it is a real tactical layer.)
- **Reinforced (purple) blocks:** do the two hits need to be from separate bombs? Does damage persist across the level or reset?
- **Movement speed model:** tiles/second, and how acceleration/deceleration interacts with grid snapping.
- **Blast propagation timing:** instantaneous full cross, or expanding tile-by-tile? (Affects dodge windows enormously.)
- **Blast lifetime:** how long does a blast tile remain lethal?

**Skills**
- Are the two passive slots **fixed** (Speed/Shield) or **selectable**? §6.1 implies fixed; §9.2 implies unlockable.
- The Special's "token gauge" vs. "cooldown" — §6.1 describes gauges for passives, §6.2 describes cooldowns for actives; the Speed passive has *both* a passive effect and an active sprint. What input triggers the sprint on mobile? ("double-tap the joystick" is stated as an example, not a spec.)
- Special abilities have no numbers at all (teleport distance, lightning range, remote detonator rules).

**Level & progression**
- Level count, sector count, difficulty curve.
- Level dimensions (grid width × height) and how they map to a landscape phone screen — **does the whole level fit on screen, or does the camera scroll?** This is a fundamental design decision with enormous consequences and is not mentioned anywhere.
- Timer values per level.
- Failure state when all 3 lives are consumed.
- Whether score has any function beyond display.
- Data Coins vs. Data Nodes: two collectibles with overlapping roles and no stated distinction in value.
- Reward amounts, upgrade costs, currency conversion rates.

**Meta & business**
- Retention loop: daily objectives, energy, events, seasons — none exist.
- Monetisation surfaces beyond skins: no ads, no IAP catalogue, no lives refill.
- Tutorial / onboarding / first-time-user experience: not mentioned.
- Audio direction: not mentioned *at all* despite a defined art direction. For an afro-futurist game, the soundtrack is half the identity.
- Localisation scope (the GDD is in French; `CLAUDE.md` lists Unity Localization).
- Accessibility: colourblind support is critical for a game that encodes block behaviour in **colour** (orange = 1 hit, purple = 2 hits).

---

## 12. Contradictions Inside the GDD

| # | Contradiction | Resolution proposed |
|---|---|---|
| C1 | **Nakama / PostgreSQL / CockroachDB / Go backend** are listed as technical bricks, but the GDD describes a **100% offline single-player campaign**. No multiplayer, no leaderboard, no social feature appears anywhere. | Blocking. See §13 Q1. Assume single-player v1, architected so PvP is possible. |
| C2 | **Engine: "Unity3D/Axmol/S&Box"** — three incompatible engines. | ✅ **DECIDED 2026-08-05: Unity 6.3 LTS or newer.** Recorded in `CLAUDE.md`; the GDD §header line is superseded and should be corrected at the next GDD revision. |
| C3 | **Platform list** in the GDD (incl. PS5/Xbox/Switch 2) vs. `CLAUDE.md` (Windows + WebGL, Android/Steam later) vs. the mobile-first brief. | ✅ **DECIDED 2026-08-05: Android → WebGL → iOS → Desktop, consoles out of scope.** Recorded in `CLAUDE.md` and in the GDD header (v1.1). See §13 Q2. |
| C4 | **Pillar 1 "chaînes d'explosion"** vs. **§6.2.3's 5 s per-slot cooldown**, which enforces idle time and makes chaining nearly impossible early. | Classic capacity model; cooldown tunable defaulting to 0. |
| C5 | **Pillar 4 "Action Contre-la-Montre"** vs. **§5.3's timer reset on death** — dying refreshes the clock, so the clock is not a real constraint. | Timer persists across lives, or a fixed time penalty on death. |
| C6 | **Pillar 4 (countdown pressure)** vs. **§8.3 "Survival: survive 3 minutes"** — the timer means the opposite thing in that mode. | Postpone Survival past the slice; if it ships, give it a distinct HUD treatment. |
| C7 | **§6.1 passives are fixed (Speed/Shield)** vs. **§9.2 "Passives Tech" implying unlockable passives**. | Fixed passives at v1; unlockable is a post-validation feature. |
| C8 | **§6.2.3 "1 bomb slot at start"** vs. **§5.2 default range 2** — the GDD's own reference-image HUD shows a mid-game state (`02:34`, 3 hearts) which is being read as if it were the starting state. Starting values are never actually specified. | Author a canonical starting loadout table. |
| C9 | **§9.1 Data Coins drop from blocks** vs. **§8.3 objective "collect 10 Nodes" hidden in blocks** — two collectibles from the same source with no differentiation. | Coins = meta currency (always drop, low value). Nodes = objective items (authored placement, not random). |
| C10 | **§3.2 "detailed pixel art + vibrant neon"** vs. **§10 "mobile performance is a risk"** — the document identifies the conflict but does not resolve it. | Lock the emissive + single-bloom rule now (§7). |

---

## 13. Questions to Answer Before Production

Ordered by how much downstream work each one blocks.

> **Q1, Q2 and Q3 were resolved on 2026-08-05.** Decisions recorded inline below. Q4–Q10 remain open.

**Q1 — Is Bomber Legends single-player, or is multiplayer the actual product?** *(blocks: everything)*
> ### ✅ DECIDED — **Single-player now, multiplayer-ready architecture.**
> Build and validate the single-player slice. The `Simulation` assembly is deterministic, engine-free, and
> driven by a serialisable `PlayerIntent` stream from commit #1, so PvP remains a feature (M10) rather than a
> rewrite. The Nakama/PostgreSQL/Go stack stays postponed until M9 at the earliest.

Nakama + PostgreSQL/CockroachDB + Go only make sense for multiplayer, leaderboards, or server-authoritative
economy. The GDD describes none of these. This decision determines the business model (cosmetics only
convert with an audience), the retention spine, the netcode requirements, and roughly 60% of the
architecture. *My recommendation: build the single-player slice to validate feel, but architect the
simulation as network-ready from commit #1 (near-zero cost), and treat PvP as the most likely v1.0 product.*

**Q2 — Confirm the platform ladder.** *(blocks: rendering, input, build pipeline, store compliance)*
> ### ✅ DECIDED — **Android → WebGL → iOS → Desktop. Consoles out of scope.**
> Android is the primary target from Milestone 0, so device performance and touch feel are never a late
> surprise. WebGL is the cheap remote-playtest channel. `CLAUDE.md`'s platform section is superseded by this
> and should be updated. The GDD's console list is removed from scope until the game is proven.

**Q3 — Does a level fit on one screen, or does the camera scroll?** *(blocks: level design, camera, art, UI)*
> ### ✅ DECIDED — **Single screen, ~13×11 tiles. No camera scrolling.**
> Classic Bomberman framing. No camera system, no minimap, no off-screen threat telegraphing, dramatically
> cheaper level authoring, and the best fit for a landscape phone. The GDD's "traverse a quartier" framing is
> satisfied narratively by sector-to-sector progression rather than by scrolling within a level.

**Q4 — Classic bomb capacity, or the 5 s cooldown model?** *(blocks: core feel, HUD, tuning, level design)*
See §3. *Recommendation: classic; validate with playtest data.*

**Q5 — Is the meta progression horizontal or vertical?** *(blocks: level balance, economy, tech tree UI)*
See §4. *Recommendation: horizontal unlocks in the meta, vertical power as in-level pickups.*

**Q6 — What is the retention loop?** *(blocks: economy, backend, live-ops)*
Daily objectives? Leaderboards? Seasons? Currently there is no reason to return on day 2.

**Q7 — What happens when all 3 lives are lost?** *(blocks: economy, session design)*
Restart sector? Lose run rewards? Pay to continue? This is a monetisation decision, not a UX detail.

**Q8 — What is the art production capacity?** *(blocks: the entire schedule)*
Isometric pixel art at the fidelity described is the dominant cost. Who produces it, at what rate? The
answer determines whether the roadmap in `04-ROADMAP.md` is realistic.

**Q9 — Is this F2P live-service or premium/ad-supported?** *(blocks: economy, backend, content cadence)*
Live-service commits the team to a permanent content treadmill. Confirm the team can sustain it.

**Q10 — Fill GDD §1.** *(blocks: scope arbitration)*
A one-paragraph pitch is the tool used to reject features. Without it, scope has no defence.

---

## Summary of Position

The **core mechanic is proven**, the **art direction is a genuine competitive asset**, and the **technical
plan is achievable in Unity 6.3** — provided three corrections are made before production:

1. **Fix the bomb economy** (drop the 5 s cooldown, add chain detonation) — otherwise the core verb is bad.
2. **Cut pre-validation scope by ~80%** (no backend, no economy, no skins, one enemy, one map) — otherwise
   fun is discovered too late to act on it.
3. **Decide single-player vs. multiplayer now** — because it is the only decision here that cannot be
   deferred cheaply.

Next: `02-PROTOTYPE-SCOPE.md`.
