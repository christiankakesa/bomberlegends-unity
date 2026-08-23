# Player insights — triage

Captured 2026-08-23, from play sessions outside the formal rounds. **None of these are mandatory.**
Each is judged against the five questions in the capture note — is it fun, does it solve a real
problem, does it add satisfaction, does it keep the balance, does it fit the design — and against
the one thing the insights themselves cannot supply: **the playtest record**
([10-PLAYTEST-PROTOCOL §10a–10d](10-PLAYTEST-PROTOCOL.md)).

Where an insight is corroborated by measured data it is treated as evidence. Where it is a
proposed *solution*, the underlying *problem* is separated out first — players are reliable about
what hurts and unreliable about what would fix it.

---

## 1. The one that reframes three others: enemies do not know about bombs

> *"Mob AI could be improved. Around 80% of the time, bombs kill the mobs, which makes the Shot
> ability feel almost useless."*

**Confirmed in the code, and it is the whole story.** `EnemySystem` is greedy pursuit: an alerted
enemy picks whichever open direction closes the distance and commits to it until the tile changes or
something blocks the way. **A live bomb is not something it evaluates.** An enemy in a corridor
walks into a blast because closing distance is the only thing it can see.

### It resolves a contradiction in the round 3 record

§10c read the fall in *"placed a bomb and escaped it on purpose"* — 12/12 in round 2 down to 7/12 in
round 3 — as the bomb losing primacy, and concluded the game was drifting toward a twin-stick
shooter. This insight says the opposite: bombs get 80% of the kills.

**Both are true, and together they are far more useful than either alone.** Bombs kill *without the
player having to execute the play*. The lethality of bombing is total; the skill of bombing has
evaporated. Players stopped baiting because they never needed to bait — and they invested their item
slots in dash, pierce and shot because that is where the expression felt like it was, while the
bombs won the fight anyway.

That also explains why the shot feels useless. It is not underpowered. **It has no job**, because
the thing it would be for is already dead.

### The fix is on the enemy, not on the bomb

Treat the tiles a live bomb will cover as blocked when an enemy chooses its direction. Nothing else
changes — no damage number, no cooldown, no item.

| Consequence | |
|---|---|
| Bombs stop free-killing | the shot acquires a job: finishing, and flushing enemies out of cover |
| Bomb-and-escape becomes a *play* again | you bait an enemy into the one tile it cannot leave |
| The Bomberman half returns | without touching a single balance value |
| [Open question #3](07-CONCEPT-REVISION.md) resolves | toward *yes, the bomb stays primary* — by skill rather than by autopilot |

Cheap, deterministic and unit-testable: it is one system in the engine-free simulation.

> **This must land before any bomb buff.** Two of the four pickup ideas below amplify a verb that is
> already dominant, and they are held on exactly this reasoning.

### Built, 2026-08-24 — and it is not sufficient

Done: enemies read a threat grid and run from bombs about to go off, hold at the edge of fire rather
than walking in, and are exempt while dormant so stealth-bombing survives. Full write-up in
[07-CONCEPT-REVISION §4p](07-CONCEPT-REVISION.md); ten tests, including the counterfactual that the
same bomb with fear switched off still kills the enemy where it stands.

**Measuring it in generated arenas turned up the other half of the problem.** At the shipping
`destructiblePercent` of 55, **two bomb placements in five cover a pocket of floor with no walkable
tile outside it** — the blast fills a corridor segment bounded by blocks, and fear cannot help an
enemy with nowhere to go. Every death in the automated runs was of that kind. At 35% fill it is 14%,
at 25% it is 6%.

So the "four kills in five" figure has **two** causes, and only one of them is now fixed. The other
is level generation, which promotes §6's block-clustering item from a cosmetic fix for a perception
problem to a balance change — see §6.

**Both are now built, 2026-08-24.** Clustering the blocks — same 55% density, laid down in short runs
instead of tile by tile — took sealed placements from 36% to 17% measured over 200 seeds, with the
uncorrelated distribution kept as the control row so the improvement is attributable. Write-up in
[07-CONCEPT-REVISION §4q](07-CONCEPT-REVISION.md). What neither change can settle is whether bombing
*feels* like a skill again; that is round 4's question.

---

## 2. Large arenas: a real problem, and the proposed solution is the wrong one

> *"When the map gets bigger, there are long periods with little happening while players travel
> toward mobs."* · *"A zoom function is also needed."* · *"Starting from Arena 5, the arenas become
> large."* · *"Players want a way to see the entire map."*

Four mentions of one thing, which makes it the most-reported item in the set. It is also
**corroborated arithmetic** rather than a feeling. From `ArenaSettings`:

| Arena | Board | Enemies | Tiles per enemy |
|---|---|---|---|
| 1 | 21 × 15 | 3 | 105 |
| 5 | 29 × 21 | 7 | 87 |
| 7 | 31 × 21 *(capped)* | 9 | 72 |
| 10 | 31 × 21 | 12 | 54 |

651 tiles, 55% of them destructible, and **Sentinels are dormant until approached** — so they do not
come to you. The tail of a late arena is a search task, not a combat task.

### Why this appeared now and not before

Dormancy is the round 1 fix that made arena 2 playable — every Sentinel used to hunt from tick 0.
It has an unintended consequence at depth that **nobody could see until round 3**, because nobody
had ever got there: round 2 died at a mean arena of 3.0, round 3 at 7.2. The controls fix is what
exposed it.

### Take the problem, not the solution

A minimap or a zoom makes the search cheaper. It does not make the search interesting, and it adds a
screen element to a game whose one confirmed failure is already a screen element.

**As an arena empties, wake the remaining Sentinels and let them hunt.** The tail becomes a fight
instead of a sweep, it needs no new UI, and it reads as the sector closing in — which is what the
escalation in [13-MUSIC §5](13-MUSIC.md) is already scored for.

Pair it with a modest camera pull-back at depth. `MatchCameraRig` already has
`_scaleDistanceWithArena` and `_maxDistanceScale` (1.35): raising the ceiling is a value change, not
a feature.

**Verdict: accept the problem, decline the minimap, revisit zoom only if the wake-up does not fix
the pacing.**

---

## 3. The skill choice screen — a fourth independent signal

> *"When selecting skill cards, there is no clear indication that the player must choose a skill."*

This is now the fourth separate reading of the same screen:

| Round | Signal |
|---|---|
| 1 | Two testers clicked skill **names** expecting descriptions |
| 2 | Two more did the same |
| 3 | **0 of 3 touch testers could describe their build; 3 of 3 coded RANDOM** |
| Now | The screen does not announce itself as a decision |

It is already the item carried out of the gate ([§10d](10-PLAYTEST-PROTOCOL.md)), and this widens
it: the problem is not only that the cards are illegible on a phone, it is that **the moment does
not read as a choice at all.** Highest-confidence UX item in the set, and the only one with a
failing metric attached.

### Built 2026-08-24 — and the number explains the split

**The cards rendered their descriptions at about 8 dp against a ~14 dp floor.** The canvas scales
against a 1920×1080 reference at match 0.5, so on the S21 Ultra one canvas unit is **0.3975 dp**
(see the correction below) and the 20-unit description came out at well under two-thirds of the
minimum readable size. That is the whole touch/pad split: the same cards were fine on a monitor at
desk distance and unreadable in a hand.

Fixed by sizing from the conversion instead of by eye — description 36 units (**14.3 dp**), name 40,
headline 48, cards grown to 580×480 to hold the larger text. Three further changes came out of
looking closely:

- The status line and the instruction are now separate: a dim `ARENA 3 CLEARED` over a large
  **`CHOOSE ONE`**. Testers read this as a results screen, so the instruction is now the biggest
  thing on it.
- Each card carries a **`TAKE`** footer. Information alone reads as a readout; the verb is what makes
  it a control, and it works on touch and pad alike where "tap" would not.
- It says **`GIVE UP`** during a discard. Same card, same position, opposite consequence — a
  pre-existing trap for anyone who has stopped reading the headline by arena 3.

Silent truncation was also removed: descriptions used to be cut with no sign anything was missing,
so the card still looked complete. A test now holds new descriptions to a 150-character budget
(longest today is 110), and three PlayMode tests assert the dp floor and that the cards fit both
16:9 and 21:9 — the arithmetic above is enforced rather than recorded.

**This is built, not validated.** §10d asks for three people on a phone for ten minutes; none of the
above is evidence until that happens.

### Corrected 2026-08-24 — the conversion was wrong, and the fix was measured against itself

The figures above originally said one canvas unit was 0.46 dp, from 3.22 physical pixels per dp on
the S21 Ultra. **The device reports 3.75.** Attached over adb, it gives density 600 at its
1440×3200 panel, which makes a unit 1.4907 / 3.75 = **0.3975 dp** — sixteen per cent smaller than
recorded. The description sized at 32 units to reach "14.8 dp" was really at **12.7 dp**, still
under the floor. The screen was declared fixed on arithmetic nobody had checked against the phone.

Two things are worth keeping from that:

- **The display mode is a red herring, and a tempting one.** The S21 Ultra ships in FHD+, so the
  app actually renders at 1080×2400 at density 450 — a scale factor of 1.1180 against 2.8125 px/dp,
  which is the *same* 0.3975 dp per unit. It has to be: the screen is 384 dp wide in either mode
  and dp is what the eye measures. Checking a phone's panel resolution without its density is how
  the wrong number got written down in the first place.
- **A test that reproduces the arithmetic cannot catch a wrong constant.** The original tests
  passed throughout, because they asserted the same 3.22 the code was sized from. What caught it
  was plugging the phone in and reading `dumpsys display`. The conversion now lives in
  `TextLegibility`, sourced from the device, and every screen defers to it by name.

The floor is therefore **36 canvas units**, not 30, and the audit that followed found every other
greybox screen under it as well: the pause menu buttons at 9.5 dp, the control hints at 10 and 9,
the touch skill and cancel labels at 8, the in-match pause control at 10, the hub's QUIT at 11, and
the on-device log overlay — the readout that exists to diagnose device-only failures — at 9. All now sit at the floor, with their panels widened where the larger text no longer fit:
the control hints panel from 900×130 to 1700×160, the pause buttons from 320×84 to 360×96, the
pause control from 170×84 to 190×88.

### Found on device 2026-08-24 — the text was readable, and the card still could not be tapped

First run on the S21 Ultra with the sizes above: **the `SHOT` button was sitting on the right-hand
choice card.** The skill cluster is anchored to the bomb button in the bottom-right, which is where
the third card is drawn, and nothing hid it while the overlay was up. The numbers are unambiguous —
`SHOT` occupies x 468–599, y −137..−7 and the card x 310–890, y −170..310, so it is entirely
inside it, as is the third slot when one is equipped.

The visible half was the smaller half. `CreateOverlay()` runs before the skill cluster is built, so
the cluster is a **later sibling and wins both the draw order and the raycast**: taps aimed at the
right card were being taken by an invisible-in-intent skill button. A screen rebuilt specifically so
that touch players could choose deliberately had a card they could not reliably choose.

`TouchControlVisibility` now takes a `Covered` predicate from the match — true while the
between-arena screen or the pause menu is up — and stands the whole cluster down. It is a predicate
rather than a call because the class knows only about devices; what counts as covered is a question
about the match. Two PlayMode tests hold it: one that the controls go and *come back*, one that no
live control shares screen with a choice card however that is arranged.

The general lesson is the same one as the dp figure, one level up: **the desktop view cannot show
you this either.** On a monitor the cluster is hidden, because it is hidden off touch devices, so
every screenshot of the fixed choice screen looked correct.

---

## 4. Cheap, real, and already a known bug class

**Bomb drop is silent or too quiet.** Round 1's headline failure was that two of four testers never
discovered the bomb. Audio confirmation of the core verb is part of that fix, and it never landed.
Asset work.

**The dash sound is harsh.** Asset work, no argument.

**Bomb cooldown is not visible.** Round 1 found exactly this bug class for *skills* — "skills avoided
because recharge was invisible" — and fixed it with seconds in the readout. The bomb never got the
same treatment. Note that `bombCooldownSeconds` ships at **0** (the classic capacity model, per
[04-ROADMAP](04-ROADMAP.md)), so what the player is asking to see is most likely **how many bombs
they have left**, not a timer. Same fix either way, and it has already worked once.

**"Why only two skill slots?"** Not a complaint about the number — a complaint that the ceiling is
**unexplained**. Slots 3–4 are M7 scope already. Showing the locked slots costs nothing and turns a
wall into a promise. It is also the cheapest possible evidence for
[open question #1](07-CONCEPT-REVISION.md), *does the third active skill earn its slot*: someone
asking for it before being offered it says yes.

---

## 5. Overclock on the first pick — an offer problem, not a balance problem

> *"During the first skill selection, Overclock feels useless because the player doesn't have enough
> context or abilities to benefit from it."*

Correct, and a well-known roguelite failure: a scaling item offered before there is anything to
scale. **Round 3 confirms it is a good item at the wrong moment** — Overclock was taken by 8 of 12
testers, just never first.

Fix by gating the offer pool, not by rebalancing: keep cooldown reduction and other multipliers out
of the arena 1 pool.

> Worth noting for its own sake: a first pick where none of the options can mean anything yet is
> precisely the kind of choice that gets coded RANDOM. Part of metric 1's difficulty may have been
> the offer pool rather than the player.

---

## 6. The four pickup ideas

| Idea | Verdict |
|---|---|
| **Explosive Shot** — passive, the shot destroys blocks | ✅ **Strongest of the four** |
| **Long-range bomb** — throw a bomb at distance | ✅ Accept |
| **Temporary bomb supercharge** — next bomb clears a row/column | ⏸ Hold |
| **Stronger bombs** — *"a bomb only breaks one block"* | ❌ Reject as stated; the perception is real |

**Explosive Shot is the only idea in the whole set that fixes a measured problem rather than adding
surface.** It gives the shot a job — clearing blocks — that bombs currently monopolise, which is
exactly the complaint in §1. It also answers *"a bomb only breaks one block"* without touching
bombs at all. Take it.

**Long-range bomb** makes the bomb an *active decision* rather than a static trap, which is precisely
what has gone missing now that enemies suicide into stationary ones. Genre-honest — Bomberman has
throwing and kicking — and it drops into the skill framework that already exists.

**Bomb supercharge is held on §1's reasoning**: it amplifies the verb that is already doing 80% of
the killing. Revisit *after* enemy blast awareness, at which point it stops being a free win and
becomes a reward for a play. Separately, a row-or-column clear on a 31 × 21 board is enormous — it
would delete the maze, which is the thing the game is about.

**"A bomb only breaks one block" is not what the rules do.** `BlastSystem` runs four arms; each
travels until it hits permanent structure, **destroys one destructible block and stops**, or runs out
of range. A bomb can take four blocks. The player's lived experience is one because in a corridor
most arms face open floor.

**So the perception is real and the diagnosis is wrong.** Do not raise the number. Address it where
it actually lives: block clustering in generation so arms have something to hit, and blast VFX that
reads all four arms clearly. Raising bomb power would compound §1.

> **Upgraded after §1 was built.** The same 55% scatter that makes a bomb look like it breaks one
> block is what seals two placements in five into an inescapable pocket. It is one cause behind three
> separate complaints — bombs kill everything, bombs break nothing, and the shot has no job — which
> makes level generation a balance change rather than a presentation fix, and the next thing to do
> after this.
>
> **Done 2026-08-24** ([07 §4q](07-CONCEPT-REVISION.md)). Blocks now land in runs, so a blast arm
> meets a wall of them instead of open floor. Whether that is enough to retire *"a bomb only breaks
> one block"* on its own, or whether the four-arm VFX is still needed, is now an observation to make
> in round 4 rather than an argument to have here.

---

## 7. Where these land

| | Item | Why here |
|---|---|---|
| ~~**Now**~~ **Built** | ~~Skill choice screen~~ **2026-08-24, unverified by testers** | The gate's carried-forward failure (§3) |
| **Now** | Bomb drop audio · dash audio | Asset work, and one is the core verb (§4) |
| **Now** | Bomb capacity/cooldown in the readout | Known bug class, already fixed once for skills (§4) |
| **Now** | Show the locked skill slots | Costs nothing, answers a real frustration (§4) |
| ~~**M6**~~ **Done** | ~~Enemy blast awareness~~ **Built 2026-08-24** | The highest-leverage change in the set (§1) |
| ~~**Next**~~ **Done** | ~~Block clustering in generation~~ **Built 2026-08-24** | Sealed placements 36% → 17% at unchanged density (§1, §6) |
| **M6** | Wake remaining Sentinels as an arena empties · camera pull-back at depth | Late-arena pacing (§2) |
| **M6** | Offer-gating: no scaling items in the arena 1 pool | Overclock (§5) |
| **M6** | Four-arm blast VFX | The bomb-power perception (§6) |
| **M7** | Explosive Shot · long-range bomb | New skills, into the existing framework (§6) |
| **M7** | Slots 3–4 | Already scoped |
| **Hold** | Bomb supercharge · raising bomb power | Revisit after enemy blast awareness (§1, §6) |
| **Declined** | Minimap / zoom as a feature | Take the pacing problem instead (§2) |

**The order matters more than the list.** Enemy blast awareness changes what every bomb-flavoured
idea below it is worth, so it went first among the gameplay changes — and the four *Now* items are
all screen-and-sound work that can proceed in parallel because none of them touch the simulation.

**Building it moved the next item.** Level generation is no longer a presentation fix waiting behind
the interesting work; it is the remaining half of the same problem, and the bomb-flavoured pickups
stay held until it lands.

**Both halves have now landed, and what unblocks is a playtest rather than a feature.** The condition
on the held items was never "clustering ships" — it was knowing what a bomb is worth once enemies
avoid blasts and the level stops sealing pockets. That number exists only in automated runs so far.
**Round 4 measures it; the Hold row is decided after, not now.** The four *Now* items are untouched by
any of this and remain the parallel track.
