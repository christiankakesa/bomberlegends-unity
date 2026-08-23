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

---

## 7. Where these land

| | Item | Why here |
|---|---|---|
| **Now** | Skill choice screen: reads as a decision, legible on a phone, descriptions on the cards | The gate's carried-forward failure (§3) |
| **Now** | Bomb drop audio · dash audio | Asset work, and one is the core verb (§4) |
| **Now** | Bomb capacity/cooldown in the readout | Known bug class, already fixed once for skills (§4) |
| **Now** | Show the locked skill slots | Costs nothing, answers a real frustration (§4) |
| **M6** | **Enemy blast awareness** | The highest-leverage change in the set (§1) |
| **M6** | Wake remaining Sentinels as an arena empties · camera pull-back at depth | Late-arena pacing (§2) |
| **M6** | Offer-gating: no scaling items in the arena 1 pool | Overclock (§5) |
| **M6** | Block clustering in generation · four-arm blast VFX | The bomb-power perception (§6) |
| **M7** | Explosive Shot · long-range bomb | New skills, into the existing framework (§6) |
| **M7** | Slots 3–4 | Already scoped |
| **Hold** | Bomb supercharge · raising bomb power | Revisit after enemy blast awareness (§1, §6) |
| **Declined** | Minimap / zoom as a feature | Take the pacing problem instead (§2) |

**The order matters more than the list.** Enemy blast awareness changes what every bomb-flavoured
idea below it is worth, so it goes first among the gameplay changes — and the four *Now* items are
all screen-and-sound work that can proceed in parallel because none of them touch the simulation.
