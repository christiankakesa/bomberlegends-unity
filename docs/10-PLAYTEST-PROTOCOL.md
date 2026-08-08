# Playtest protocol — the validation gate

**For the gate defined in [07-CONCEPT-REVISION.md §3](07-CONCEPT-REVISION.md).** Everything that
document specified is built and playable. This one is about collecting the answer honestly.

The build is a browser link. Testers need no install, and that matters: it is why the sample can be
large enough for a percentage to mean anything.

---

## 1. What this measures

Five numbers, and they are not equally important.

| # | Metric | Threshold | What a failure indicts |
|---|---|---|---|
| 1 | **Deliberately picks a different item on run 2** | ≥ 60% | **The synergy pillar** — the whole v2.0 bet |
| 2 | Can describe their build unprompted | ≥ 50% | Item legibility |
| 3 | Voluntary second run | ≥ 60% | The loop is not compelling |
| 4 | Deaths blamed on self, not controls | ≥ 80% | Controls or feedback, not design |
| 5 | Stuck on geometry | 0 incidents | A bug, and a fatal one |

**Metric 1 is the gate.** The others are supporting or diagnostic. Bomber Legends was rebuilt around
one claim — *items change how you play* — and a tester who picks at random is saying that claim is
not landing. Everything else can be fixed later; that cannot.

**Metric 4 is a filter, not a verdict.** It exists to tell you whether the other four are
trustworthy. If a quarter of deaths are blamed on the controls, the session measured the controls.

### Also worth capturing

Not gate metrics, but this is the cheapest chance to answer them:

- **How long is a run?** Open question #2. Time from first arena to death.
- **How far do they get?** Arena number at death, and whether it climbs across attempts.
- **Which pairings appear?** If everyone converges on the same two items, that is a balance finding
  worth having before content is built on top of it.

---

## 2. Sample size, honestly

| Testers | What you can conclude |
|---|---|
| 3–5 | Directional only. Treat as bug-finding, not as a gate. |
| **8–12** | **Enough to run the gate.** A 60% threshold is 5–7 people; a single outlier will not flip it. |
| 15+ | Better, and rarely worth waiting for at this stage. |

Below six people, a percentage is theatre. Two testers behaving oddly move it twenty points.

**Who.** People who play games but have not played *this* one, and who have never heard you describe
it. Anyone who has watched development cannot answer metric 2 — they already know what Bomb Trail
does.

Avoid stacking the sample with Bomberman veterans. They will read the grid instantly and tell you
nothing about whether the hybrid teaches itself.

---

## 3. Before the session

- The link, open and loaded. Never let a tester watch a download.
- Somewhere to write. One sheet per tester (§5).
- **Clear the saved run between testers.** A run persists now, so tester B would resume tester A's.
  A fresh browser profile, a private window, or clearing site data all work.
- Decide gamepad or keyboard/mouse in advance and keep it constant. Do not offer a choice mid-run.

---

## 4. Running it

### Say exactly this

> "This is an early prototype. The graphics and sound are placeholders — coloured blocks and
> generated bleeps. I am testing whether the game is fun, not whether it is pretty.
>
> Play however you like. I am not going to help, and that is not me being unkind — anything I tell
> you is something the game failed to. Stop whenever you want."

Then stop talking.

**Naming the placeholders is not an apology, it is a filter.** Without it a third of the session is
spent on the art, and you lose the feedback you came for.

### Rules for you

- **Do not explain the controls.** Not the dash, not the aim, not the swap screen. Whether those
  teach themselves *is* the test.
- **Do not answer questions during play.** "I want to see what you do" is enough.
- **Do not react.** No wincing when they walk into their own bomb. They read your face faster than
  the HUD.
- **Do not say "try again".** Metric 3 is destroyed the moment you ask for it.

### The critical moment

When they die the first time, **say nothing and look away for thirty seconds.** Check your phone.
Whatever they do in that gap is metric 3.

A facilitator leaning forward expectantly produces a second run every time — and a number you cannot
use.

---

## 5. What to record

One sheet per tester. Most of it is filled in while watching, not afterwards.

```
Tester ___   Date ___   Input: keyboard / gamepad / touch

RUN 1   items taken: ______________________   died on arena ___   length ___ min
RUN 2   items taken: ______________________   died on arena ___   length ___ min
        Started run 2 unprompted?   YES / NO          <- metric 3
        Chose differently on run 2? DELIBERATE / RANDOM / SAME   <- metric 1

OBSERVED (tick as it happens)
  [ ] Placed a bomb and escaped it on purpose
  [ ] Used the dash to escape
  [ ] Used the dash to attack
  [ ] Aimed a skillshot at something specific
  [ ] Read an item description before choosing (watch their eyes)
  [ ] Swapped an item when slots were full
  [ ] Skipped an offer
  [ ] Stuck on geometry            <- metric 5, note where
  [ ] Blamed the controls out loud

DEATHS   cause of each, in their words:
```

**"Deliberate" versus "random" on metric 1** is a judgement, so make it on evidence, not impression:
did they read the cards, hesitate, or say anything about why? A pick made in under two seconds
without their eyes moving is random, whatever they say afterwards.

---

## 6. Afterwards

Ask in this order. Do not skip ahead — the early questions must not be contaminated by the later
ones.

1. **"Talk me through what happened."** Say nothing else. Let them ramble; this is where the real
   finding usually is.
2. **"What was your character good at?"** — *metric 2.* A pass names a mechanic: "I could blow up
   the bombs from far away", "dashing hurt them". A fail is "it was fast" or a shrug. **They do not
   have to use the item's name.**
3. **"What killed you, that last time?"** — *metric 4.* Record their words. "I got greedy" is a
   pass. "It didn't dash" is a fail, and follow it: *"what did you press?"*
4. **"Was there a moment you knew what you wanted to do but the game wouldn't let you?"** Better
   than asking about frustration, because it asks about a specific event rather than a mood.
5. Only now: **"Did you notice you could choose items?"** — if this is a surprise, metric 1 is not a
   design result, it is a UI one.

### Questions not to ask

- ~~"Did you like it?"~~ — everyone says yes to your face.
- ~~"Was it fun?"~~ — unanswerable and unactionable.
- ~~"Would you play this again?"~~ — metric 3 is *behaviour*, and you already measured it. Asking
  invites politeness to overwrite data you have.
- ~~"Did you understand the items?"~~ — leads the witness. Question 2 measures it without naming it.

---

## 7. Reading the result

**Metric 1 passes.** The concept is validated. The next question is content, not design.

**Metric 1 fails but metric 2 passes.** They understood their build and still chose at random —
which means the items are legible but not *differentiating*. That is a balance problem: the pairings
are not producing playstyles distinct enough to be worth aiming for. Fixable, and expensive.

**Metrics 1 and 2 both fail.** They could not tell what items do. Interface first, then re-test.
**Do not conclude anything about the design from this** — you would be reading a screen problem as a
concept problem, which is exactly the false negative this protocol exists to avoid.

**Metric 4 fails.** Stop. Nothing else from that session is usable. Fix the controls or the feedback
and run it again with fresh people.

**Metric 5 fails even once.** A bug, and one that has been chased before — the enemy wedging in
§4g/§4h. Get the seed and the arena number.

### One thing to hold onto

A tester who finishes and immediately says *"can I try the other one?"* is worth more than any number
on this page. Write down what they said, exactly.

---

## 8. Known ways this can mislead

| Trap | Guard |
|---|---|
| Testers who have watched development | Exclude them. They cannot answer metric 2. |
| Facilitator hovering after a death | The thirty-second rule in §4. |
| Explaining a control "just once" | You have spent that tester. Note it and discount their metrics 2 and 4. |
| Placeholder art dominating feedback | The opening script. Redirect once, then let it go. |
| A previous tester's run resuming | Clear site data between sessions. |
| Only three testers | Report it as directional. Do not call the gate. |
| A dominant item pairing | Not a gate failure — a balance finding. Record which, and keep going. |

---

## 9. When it is over

Write the outcome into [07-CONCEPT-REVISION.md §3](07-CONCEPT-REVISION.md) with the sample size next
to it. A gate result without its sample size is unreadable six months later, and this project has
been careful about exactly that kind of record.
