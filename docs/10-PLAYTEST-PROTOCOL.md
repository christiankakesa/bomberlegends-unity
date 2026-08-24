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
| 1 | **Picks an item deliberately rather than at random** | ≥ 60% of offers | **The synergy pillar** — the whole v2.0 bet |
| 2 | Can describe their build unprompted | ≥ 50% | Item legibility |
| 3 | Voluntary second run | ≥ 60% | The loop is not compelling |
| 4 | Deaths blamed on self, not controls | ≥ 80% | Controls or feedback, not design |
| 5 | Stuck on geometry | 0 incidents | A bug, and a fatal one |

**Metric 1 is the gate.** The others are supporting or diagnostic. Bomber Legends was rebuilt around
one claim — *items change how you play* — and a tester who picks at random is saying that claim is
not landing. Everything else can be fixed later; that cannot.

**It is scored per offer, inside a single run, as of round 4.** It used to ask whether a tester
picked differently on a second run, which cost the gate two rounds running — see [§5](#5-what-to-record).
A run already produces nine or ten of these decisions; there was never any need to ask for a second
one.

**Metric 4 is a filter, not a verdict.** It exists to tell you whether the other four are
trustworthy. If a quarter of deaths are blamed on the controls, the session measured the controls.
Round 2 is the worked example: metric 1 came in at 42%, and it means nothing, because metric 4 came
in at 62% (§10b).

**Score metric 4 per death, not per tester**, and split every metric by input device before reading
any of them. Round 2's overall numbers hid a scheme that passed at 100% next to two that failed.

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
- Decide gamepad, keyboard/mouse or touch in advance and keep it constant. Do not offer a choice
  mid-run, and balance the devices across the sample — round 2's result was invisible until it was
  split by device (§10b).
- **On a pad in the browser, press a button before handing it over.** A browser does not report a
  gamepad until it has been used once, so a tester's first press looks to them like a dead game.
  Doing it yourself while the menu is up costs nothing and is not explaining a control.

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

### The one question you may ask, and only at the end

When they have stopped playing for good — not between runs — ask exactly once:

> "Tell me about the character you ended up with."

Name nothing. Not an item, not a skill, not the word *build*. Then write down what they say in their
words. That is metric 2's fallback reading (§5), and it is the only sentence in this protocol you are
allowed to initiate.

---

## 5. What to record

One sheet per tester. Most of it is filled in while watching, not afterwards.

```
Tester ___   Date ___   Input: keyboard / gamepad / touch

RUN 1   died on arena ___   length ___ min
RUN 2   died on arena ___   length ___ min      (leave blank if there was none)
        Started run 2 unprompted?   YES / NO                          <- metric 3

ITEM OFFERS — one line per offer, in the order they came, across every run
 #  run arena  took, or SKIPPED       read?  paused?  said anything?   call
 1   1    1    ____________________   Y / N  Y / N    _____________    D / R / U   <- first pick
 2   1    2    ____________________   Y / N  Y / N    _____________    D / R / U
 3   1    3    ____________________   Y / N  Y / N    _____________    D / R / U
 4   1    4    ____________________   Y / N  Y / N    _____________    D / R / U
 …                                                                    <- metric 1

BUILD LEGIBILITY                                                      <- metric 2
  [ ] STRICT: described their build out loud, unaided, during play, unasked
      What they said: ____________________________________________
  If and only if the strict box is empty, AT THE VERY END, once they have
  stopped playing for good, ask: "tell me about the character you ended up with"
      Could they?  YES / PARTLY / NO
      What they said: ____________________________________________

OBSERVED (tick as it happens)
  [ ] Placed a bomb and escaped it on purpose
  [ ] Used the dash to escape
  [ ] Used the dash to attack
  [ ] Aimed a skillshot at something specific
  [ ] Swapped an item when slots were full
  [ ] Asked for a third skill before the game offered one
  [ ] Stuck on geometry            <- metric 5, note where
  [ ] Blamed the controls out loud
  [ ] Repeated a build on a later run — which, and did they say why:
      ____________________________________________________________

DEATHS   cause of each, in their words:
```

### Metric 1 is one line per offer, and the run is the unit that produces them

**The old sheet asked about run 2, and that is why the gate has never been called.** Round 2 lost it
by conflating *chose differently* with *chose deliberately*; round 3 lost it because six testers had
metric 1 filled in without ever starting a second run. Both failures come from the same place — a
metric that needs two runs, collected from a game whose first run is twenty-two minutes.

**Every offer is an independent chance for the claim to show.** A tester who reaches arena ten makes
nine or ten decisions about their build, and round 3 threw all of them away in favour of one
comparison that mostly did not exist. Farnsworth took nine items and skipped three; not one of those
twelve moments reached the sheet.

So: **one line per offer, and the call is per pick.**

```
metric 1  =  D  /  (D + R)          U is not in the denominator
```

**The three codes, and they are not a matter of taste.**

| | Means | Test |
|---|---|---|
| **D** | Deliberate | They read, paused, skipped, or said something about why |
| **R** | Random | A pick made in under two seconds without their eyes moving |
| **U** | Unobserved | **You did not see it.** Not "you saw it and found it hard to call" |

**U is for missing data, never for a difficult judgement.** A pick you watched and cannot call is R —
the definition above is behavioural and asks nothing about what was in their head. This is what the
stray `MIXED` code in round 3 should have been split into, and it is why nothing on this sheet can be
filled in from memory afterwards: a row you did not watch stays U, and a run with no offers has no
rows at all.

**Skipping an offer counts as deliberate.** It is the strongest evidence of deliberation the choice
screen can produce — nobody skips by accident — and round 3 coded the clearest example of it, tester
07, as RANDOM.

**Report the first pick separately from the rest.** As of 2026-08-24 an offer made to a player
carrying nothing withholds the items that only multiply a build ([14-INSIGHTS §5](14-INSIGHTS.md)),
so round 3's first picks were partly measuring a pool containing an item nobody could use yet. If R
now falls only on row 1, that is the pool. If it falls evenly down the sheet, that is the player.

**Report the per-tester spread as well as the headline.** Scoring per pick lets one tester who
reached arena twelve outweigh three who died in arena two, which is the correct unit for *"is an
offer a decision?"* and the wrong one for *"do players find it a decision?"*. Both numbers go in the
record; the gate is called on the per-pick one.

**Repeating a build is not a failure and never was.** A tester who takes the same pairing again
**because it worked** was deliberate about it. It is on the observation list, not the metric.

**Metric 2 has a strict reading and a fallback, and they are exclusive.** The threshold is against
the strict one — described unaided, during play, because nobody asked. That is what "legible" means.
But round 2 produced *zero* readings of it across twelve testers, which is worse than a low number,
so the end-of-session question exists to guarantee some answer. Ask it only once they have stopped
playing for good: any earlier and it teaches them to narrate, which §10a already found destroys
metric 3.

**Ask the fallback only when the strict box is empty**, which is why the sheet nests it. Round 3
produced a tester with the spontaneous box ticked *and* the asked question answered NO with nothing
written down — two readings that cannot both be true, and no way after the fact to know which was
the mistake.

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

> **Read row 1 before concluding that.** If the random picks are concentrated on the first offer and
> the rest of the sheet is deliberate, the finding is about the opening moment — a player with no
> build yet and nothing to reason from — and not about the items. That is a much cheaper problem,
> and one already half-addressed by offer-gating.

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

---

## 10a. Round 1 outcome (2026-08-09 → 16)

**4 testers · gate NOT called · 5 defects found, 4 fixed**

### Why it was not called

Two reasons, either sufficient on its own.

**Metric 4 failed.** Two of four testers never discovered that Space places a bomb. One concluded
the game was broken and stopped; another described a labyrinth with no way out. Per §7, a session
where the controls are blamed is a session that measured the controls — nothing else in it is usable.

**The sample was below the floor.** §2 sets six as the minimum for a percentage to mean anything,
and four is bug-finding by definition.

**So the concept was never actually tested.** What was tested was whether the game teaches itself,
and the answer was no.

### What it produced, which was a great deal

| Finding | Status |
|---|---|
| Core verb undiscoverable — 2 of 4 never found the bomb | ✅ control hints that retire on use, plus the objective on screen |
| Skills avoided because recharge was invisible | ✅ cooldown seconds in the readout; touch buttons dim and wipe |
| "No sound effects or background music" — 3 of 4 | ✅ SFX were attenuated by a listener parked at the world origin. Music still absent by design |
| "Shot is not working" | ✅ it was invisible, not broken: too small, teal on teal, and inside the block occlusion shadow |
| Arena 2 unplayable — "assured to lose HP or die" | ✅ every Sentinel hunted from tick 0; they are now dormant until approached |
| Fullscreen black screen | ✅ fixed before the round, tester likely on an older build |

### The single most useful observation

**The mobile build was the one nobody got lost in** — because it has a button with BOMB written on
it. That is not a mobile-versus-desktop result. It is evidence that labelling the verb was the whole
fix, and it is what the control hints copy.

### Process notes for round 2

- **The §4 opening script was not used.** Placeholders were never named, and art feedback flooded in
  exactly as §8 predicts — *"design too simple"*, *"the weakness is the graphics"*. Not wrong, just
  not what the session was for.
- **"Think out loud" suppresses metric 3.** A tester narrating for you will not naturally stop and
  restart. Worth keeping for discovery rounds, worth dropping for the gate.
- Two testers clicked skill *names* expecting descriptions. The descriptions are on the choice cards
  and were read; this was reaching for more depth somewhere it does not exist.

---

## 10b. Round 2 outcome (2026-08-19)

**12 testers · sample large enough to call the gate · gate NOT passed**

| # | Metric | Threshold | Result | |
|---|---|---|---|---|
| 1 | Deliberately picks differently on run 2 | ≥ 60% | **42%** (5/12) | ❌ |
| 2 | Can describe their build unprompted | ≥ 50% | **not recorded** | — |
| 3 | Voluntary second run | ≥ 60% | **67%** (8/12) | ✅ |
| 4 | Deaths blamed on self, not controls | ≥ 80% | **62%** (15/24 deaths) | ❌ |
| 5 | Stuck on geometry | 0 | **0** | ✅ |

### The verdict, and why it is not what it looks like

Metric 4 failed, and §1 is explicit that metric 4 is a filter rather than a verdict: when deaths are
blamed on the controls, the session measured the controls. So metric 1's 42% cannot be read as a
result about items at all.

But the failure is not spread evenly. Split by device it stops being a game problem and becomes a
control-scheme problem:

| Device | Metric 4 | Metric 3 |
|---|---|---|
| Keyboard | **100%** (8/8 deaths self-blamed) | 4/4 started run 2 |
| Gamepad | **50%** (4/8) | **0/4 started run 2** |
| Touch | **38%** (3/8) | 4/4 started run 2 |

**The design works. Two of the three ways to play it do not.** Keyboard players — the only ones with
a visible cursor telling them where a shot goes — passed metric 4 outright, at 100%. Every gamepad
tester declined a second run; nobody else declined one.

Two of the 24 death classifications are judgement calls rather than quotes ("Didn't use correctly
diagonal movement" is worded as self-blame but describes an input failure). Coding both the other
way puts the total at 54% and gamepad at 38%. The direction does not change.

### The cause, which was physical

Gamepad skills were on the face buttons. Aiming needs the right thumb on the right stick, and a face
button needs that same thumb somewhere else — so a player could aim or shoot, never both. Testers
described the same wall from three sides:

- *"I can't aim the attack properly – are the buttons too close together?"* (05)
- *"I panicked and fat-fingered the dodge. It's difficult to aim the shot with the gamepad!"* (08)
- *"The dash button keeps changing in my head."* (11)

Touch failed for the neighbouring reason: the aim indicator was drawn **on the skill button**, which
is the one place on a phone guaranteed to be under a thumb. *"I couldn't tell where my finger
landed"* (06), *"It fired at the wrong target"* (09), *"The controls are fighting my thumb"* (12).

The most valuable line in the round is tester 03's, because it is a specification rather than a
complaint: *"I need a fat arrow on the ground oriented to the enemy when shooting."*

### What was fixed as a result

| Finding | Status |
|---|---|
| Gamepad: aiming and shooting need the same thumb | ✅ skills moved to LB / RT / LT; face buttons kept as aliases |
| Gamepad: aim collapses the instant the stick centres, so the shot leaves sideways | ✅ the aim is held 0.35 s past release |
| Gamepad: 0.3 deadzone discarded a third of the stick and jumped on crossing | ✅ 0.2, radial, rescaled — small movements and fine aim exist again |
| *"The dash went in different direction than I thought"* (05) | ✅ a dash follows the last analogue heading, not the 4-way facing |
| *"Didn't use correctly diagonal movement"* (02) | ✅ same fix; a diagonal run now dashes diagonally |
| Touch/gamepad players cannot see where a shot will go | ✅ a fat arrow on the ground, from the aim in the intent, on every device but the mouse |
| Gamepad players never discovered the right stick aims | ✅ the hint names both halves: `(R-STICK AIM + RT) SHOOT` |

### Metric 1 has a coding problem — recorded, not rescored

The observation ticks contradict the coding. **12 of 12 read an item description before choosing**,
10 of 12 swapped when their slots were full, 8 of 12 skipped an offer outright — and yet 7 of 12
were coded RANDOM or SAME. §5 defines random as *a pick made in under two seconds without their eyes
moving*, and by that definition nobody in this round picked randomly.

Tester 07 is the clearest case: coded RANDOM, having skipped three offers and deliberately doubled
down on Twin Shot. Skipping is the most deliberate act the choice screen allows.

The likely cause is that the sheet asks one question and the column name asks another — *chose
differently* is not *chose deliberately*, and a tester who repeats a build **because it worked** is
being deliberate about it.

**This is deliberately not rescored.** Moving a threshold after seeing the data is how a gate stops
meaning anything. For round 3 the column splits in two:

```
        Chose differently on run 2?  YES / NO
        Chose deliberately?          DELIBERATE / RANDOM   <- metric 1
```

### Also worth capturing, and it is the encouraging part

- **Every single tester's run 2 was longer than their run 1** — 10.1 min mean rising to 17.6.
- **Eleven of twelve got further**, arena 3.0 mean rising to 4.8. The twelfth held.
- **No geometry traps.** The §4g/§4h wedge that round 1 chased did not recur once in 24 runs.
- **Every gamepad tester took Focusing Lens, and nobody else did** — 4/4 against 0/8. Players who
  could not aim spent an item slot on compensating for it. A balance signal that is really an input
  signal.

### Process notes for round 3

- **Metric 2 was never recorded.** Twelve testers and the item-legibility number is simply missing,
  which is the one number that would say whether 42% is a design result or an interface result. Ask
  it out loud at the end of run 1: *"what does your build do?"*
- Split metric 1's column as above.
- Re-run with **fresh testers on gamepad and touch**. The eight who blamed the controls were
  measuring the old controls and cannot be reused for the same question.
- Still outstanding from this round: enemy HP is not shown, skill cards have no icons, and two
  testers clicked skill *names* expecting descriptions — the same reach for depth round 1 saw.

---

## 10. Player's captured feeling and notes

### Round 1
**Contact for a video recording**
Hey! 👋
I’m testing a new version of my game and I’d really appreciate your help.

No need for a video call this time. The idea is simple:

🎮 Play the game naturally while recording your screen.
🎙 If possible, think out loud while playing — what you’re trying to do, why you choose something, what confuses you, etc.
📝 After playing, send me the video + any comments (text or voice message).

Please don’t try to play “correctly” or restart because you made a mistake.
I want to see your first, natural reaction.
The session should take around 5–10 minutes.

If you're available, let me know and I’ll send you the game link and quick instructions. 🙏

**Contact message for a video call**
Hey! 👋
I’m testing a new version of my game and I’d really appreciate your help.

This time, we can do a **quick video call** while you play.

🎮 You’ll play naturally while sharing your screen.
🎙️ I’ll ask you to think out loud — what you’re trying to do, why you choose something, what confuses you, etc.
👀 I’ll mostly observe and take notes, without helping you unless necessary.

Please don’t try to play “correctly” or restart because you made a mistake. I want to see your **first, natural reaction**.

The session should take around **5–10 minutes**.

If you're available, let me know and we’ll set up a quick call. 🙏

#### Rudy
* The player encounter blackscreen when pressing the fullscreen button and needed to refresh the browser and press again the fullscreen button to have the game up and running.
* Confusion with a labyrinth game; the player don't use space or think about activating the bomb.
* The player feels like there is no solution to get out.
* The player didn't pay attention to the information at the top.
* The player didn't want to consume the only Shot 1 or 1 Dash (didn't know about countdown).
* the player said : the game made me think of Pac-Man.
* Impression that the dash allows you to move cubes.
* Missing a tutorial explaining the goal.
* Missing aiming with keyboard.
* Add a timer/countdown for Dash and Shot.
* Shot is not working on his device (but it was just the bullet wasn't visible as it too quick).
* Asked to highlight the most advantageous skill depending on the player's situation (deck).
* Asked for SFX and VFX.
* Want the game to display enemy HP.
* After death on Arena 3, the player was surprized to restart in Arena 1 (I need to check the build to beb sure of that).
* The first arena must be a tutorial that explains the game and mechanics.
* Arena 2 is too frustrating because there are 3 mobs attacking you and you are assured to lose HP or die...
* The game needs an ambient music and SFX.
* Arenamust be sorted by difficulty, currently Arena 2 is too frustrated.
* The player said: the virus can split by 2 or 3 (mobs are sentinels, lol).
* The player sugested to add virus lore.
* The player suggested to introduce : Red alert lore at startup.

#### Hasby

*Without explanation*

* Total frustration on the first run. He didn't know what to do and thought the game was buggy.
* He restarted the game, got stuck on Level 1, and stopped the test.

*With explanation*

* Discovered that Space drops a bomb and was happy about it.
* Enjoyed the enemies being smart and found it challenging.
* Asked to add more items to the map.
* Enjoyed discovering the different skill choices.
* Discovered Dash and Shot at the same time, really enjoyed them, and tried to change his playstyle.
* Asked to add images to the skill selection cards.
* Tried to get skill descriptions by clicking on the skill names with the mouse, but no description appeared.
* He said several times that he forgot to use the Shot.
* He said Arena 3 was too quiet.
* He stopped playing at Arena 4.

*Feedback text:*

1. The game’s design is too simple: the characters, animations, and maps.
2. The game is too quiet; there are no sound effects or background music.
3. The game’s UI needs improvement; it’s too simple.


#### Fenicks

* He was very calm and was looking for the action keys on the keyboard and mouse.
* Found how to drop a bomb.
* Spent a lot of time reading the skills after Arena 1.
* Tried to click on the skill name.
* Looked for Dash and Shot and found them.
* Asked when the game will have VFX, SFX, music, and a fantastic UI.
* Asked about the lore.
* Asked how many heroes will be available.

Fenicks was not really impressed, but he was very curious.

#### Daffa

*Gameplay*

* Daffa played on a mobile device and understood how to use the OSD controls.
* He started using Dash before the end of Arena 1 and began shooting randomly.
* His playstyle in Arena 2 involved a lot of dashing and shooting, especially because the start was stressful with multiple mobs. I could clearly see the dynamism and tension.
* He died in Arena 3, restarted, and stopped playing after Arena 1.

*His feedback:*

* “Here’s the play test… to be honest, it feels like playing my childhood games.”
* “I really love the mechanics of roguelike progression. But the weakness is the graphics. Yeah, I know it’s early access and just a play test.”
* “For the next step, we can improve the graphics.”

### Round 2

#### Tester 01   Date 2026-08-19   Input: keyboard

RUN 1   items taken: Bomb Trail, Quickstep   - died on arena 3   - length 11 min
RUN 2   items taken: Twin Shot, Piercing Rounds, Overcharge, skip   - died on arena 5   - length 18 min
        Started run 2 unprompted?   YES          <- metric 3
        Chose differently on run 2? DELIBERATE   <- metric 1

OBSERVED (tick as it happens)
  [x] Placed a bomb and escaped it on purpose
  [x] Used the dash to escape
  [ ] Used the dash to attack
  [x] Aimed a skillshot at something specific
  [x] Read an item description before choosing (watch their eyes)
  [x] Swapped an item when slots were full
  [x] Skipped an offer
  [ ] Stuck on geometry            <- metric 5, note where: 
  [ ] Blamed the controls out loud

DEATHS   cause of each, in their words:
  "got boxed in by his own bomb."
  "That wall clipped his dash (player's skill)."

#### Tester 02   Date 2026-08-19   Input: gamepad

RUN 1   items taken: Focusing Lens, Kinetic Core   - died on arena 2   - length 8 min
RUN 2   items taken: Focusing Lens, Quickstep, Overclock   - died on arena 4   - length 15 min
        Started run 2 unprompted?   NO          <- metric 3
        Chose differently on run 2? RANDOM   <- metric 1

OBSERVED (tick as it happens)
  [x] Placed a bomb and escaped it on purpose
  [x] Used the dash to escape
  [ ] Used the dash to attack
  [x] Aimed a skillshot at something specific
  [x] Read an item description before choosing (watch their eyes)
  [x] Swapped an item when slots were full
  [ ] Skipped an offer
  [ ] Stuck on geometry            <- metric 5, note where:
  [ ] Blamed the controls out loud

DEATHS   cause of each, in their words:
  "Miss a shot and get caught"
  "Didn't use correctly diagonal movement"

#### Tester 03   Date 2026-08-19   Input: touch

RUN 1   items taken: Momentum, Bomb Trail   - died on arena 3   - length 9 min
RUN 2   items taken: Momentum, Twin Shot   - died on arena 3   - length 13 min
        Started run 2 unprompted?   YES          <- metric 3
        Chose differently on run 2? SAME   <- metric 1

OBSERVED (tick as it happens)
  [x] Placed a bomb and escaped it on purpose
  [x] Used the dash to escape
  [x] Used the dash to attack
  [x] Aimed a skillshot at something specific
  [x] Read an item description before choosing (watch their eyes)
  [ ] Swapped an item when slots were full
  [x] Skipped an offer
  [ ] Stuck on geometry            <- metric 5, note where: 
  [ ] Blamed the controls out loud

DEATHS   cause of each, in their words:
  "I forgot to shoot, I need a fat arrow on the ground oriented to the enemy when shooting."
  "I kept tapping the wrong side."

#### Tester 04   Date 2026-08-19   Input: keyboard

RUN 1   items taken: Twin Shot, Bomb Trail, Piercing Rounds   - died on arena 4   - length 14 min
RUN 2   items taken: Twin Shot, skip, Momentum, skip, skip   - died on arena 6   - length 21 min
        Started run 2 unprompted?   YES          <- metric 3
        Chose differently on run 2? DELIBERATE   <- metric 1

OBSERVED (tick as it happens)
  [x] Placed a bomb and escaped it on purpose
  [x] Used the dash to escape
  [ ] Used the dash to attack
  [x] Aimed a skillshot at something specific
  [x] Read an item description before choosing (watch their eyes)
  [x] Swapped an item when slots were full
  [x] Skipped an offer
  [ ] Stuck on geometry            <- metric 5, note where: 
  [ ] Blamed the controls out loud

DEATHS   cause of each, in their words:
  "I got greedy on the third blast."
  "I dashed into the wall."

#### Tester 05   Date 2026-08-19   Input: gamepad

RUN 1   items taken: Quickstep, Kinetic Core   - died on arena 2   - length 7 min
RUN 2   items taken: Quickstep, Focusing Lens, Overcharge   - died on arena 4   - length 16 min
        Started run 2 unprompted?   NO          <- metric 3
        Chose differently on run 2? RANDOM   <- metric 1

OBSERVED (tick as it happens)
  [x] Placed a bomb and escaped it on purpose
  [x] Used the dash to escape
  [ ] Used the dash to attack
  [x] Aimed a skillshot at something specific
  [x] Read an item description before choosing (watch their eyes)
  [x] Swapped an item when slots were full
  [ ] Skipped an offer
  [ ] Stuck on geometry            <- metric 5, note where:
  [x] Blamed the controls out loud

DEATHS   cause of each, in their words:
  "The dash went in different direction than I thought."
  "I can’t aim the attack properly – are the buttons too close together?"

#### Tester 06   Date 2026-08-19   Input: touch

RUN 1   items taken: NA   - died on arena 1   - length 5 min
RUN 2   items taken: Overclock, Momentum   - died on arena 3   - length 12 min
        Started run 2 unprompted?   YES          <- metric 3
        Chose differently on run 2? DELIBERATE   <- metric 1

OBSERVED (tick as it happens)
  [x] Placed a bomb and escaped it on purpose
  [x] Used the dash to escape
  [x] Used the dash to attack
  [x] Aimed a skillshot at something specific
  [x] Read an item description before choosing (watch their eyes)
  [ ] Swapped an item when slots were full
  [ ] Skipped an offer
  [ ] Stuck on geometry            <- metric 5, note where: 
  [ ] Blamed the controls out loud

DEATHS   cause of each, in their words:
  "I couldn't tell where my finger landed."
  "I died with my own bomb, so funny!"

#### Tester 07   Date 2026-08-19   Input: keyboard

RUN 1   items taken: Piercing Rounds, skip, Twin Shot, Bomb Trail   - died on arena 5   - length 17 min
RUN 2   items taken: Piercing Rounds, Twin Shot, skip, Twin Shot, skip, skip   - died on arena 7   - length 24 min
        Started run 2 unprompted?   YES          <- metric 3
        Chose differently on run 2? RANDOM   <- metric 1

OBSERVED (tick as it happens)
  [x] Placed a bomb and escaped it on purpose
  [x] Used the dash to escape
  [ ] Used the dash to attack
  [x] Aimed a skillshot at something specific
  [x] Read an item description before choosing (watch their eyes)
  [x] Swapped an item when slots were full
  [x] Skipped an offer
  [ ] Stuck on geometry            <- metric 5, note where: 
  [ ] Blamed the controls out loud

DEATHS   cause of each, in their words:
  "I overcommitted for one more kill."
  "I thought the corridor was wider."

#### Tester 08   Date 2026-08-19   Input: gamepad

RUN 1   items taken: Focusing Lens, skip, Quickstep   - died on arena 3   - length 10 min
RUN 2   items taken: Focusing Lens, Kinetic Core, Overcharge, skip   - died on arena 5   - length 19 min
        Started run 2 unprompted?   NO          <- metric 3
        Chose differently on run 2? DELIBERATE   <- metric 1

OBSERVED (tick as it happens)
  [x] Placed a bomb and escaped it on purpose
  [x] Used the dash to escape
  [ ] Used the dash to attack
  [x] Aimed a skillshot at something specific
  [x] Read an item description before choosing (watch their eyes)
  [x] Swapped an item when slots were full
  [x] Skipped an offer
  [ ] Stuck on geometry            <- metric 5, note where: 
  [x] Blamed the controls out loud

DEATHS   cause of each, in their words:
  "I panicked and fat-fingered the dodge. It's difficult to aim the shot with the gamepad!"
  "I need to stop playing, the game is good waiting for next steps"

#### Tester 09   Date 2026-08-19   Input: touch

RUN 1   items taken: Momentum, Bomb Trail   - died on arena 2   - length 6 min
RUN 2   items taken: Momentum, Bomb Trail, Twin Shot   - died on arena 4   - length 14 min
        Started run 2 unprompted?   YES          <- metric 3
        Chose differently on run 2? SAME   <- metric 1

OBSERVED (tick as it happens)
  [x] Placed a bomb and escaped it on purpose
  [x] Used the dash to escape
  [x] Used the dash to attack
  [x] Aimed a skillshot at something specific
  [x] Read an item description before choosing (watch their eyes)
  [x] Swapped an item when slots were full
  [ ] Skipped an offer
  [ ] Stuck on geometry            <- metric 5, note where:
  [ ] Blamed the controls out loud

DEATHS   cause of each, in their words:
  "My thumb slipped off the screen."
  "It fired at the wrong target."

#### Tester 10   Date 2026-08-19   Input: keyboard

RUN 1   items taken: Momentum, Quickstep, Kinetic Core   - died on arena 4   - length 12 min
RUN 2   items taken: Momentum, Quickstep, Piercing Rounds, skip, skip   - died on arena 6   - length 20 min
        Started run 2 unprompted?   YES          <- metric 3
        Chose differently on run 2? RANDOM   <- metric 1

OBSERVED (tick as it happens)
  [x] Placed a bomb and escaped it on purpose
  [x] Used the dash to escape
  [x] Used the dash to attack
  [x] Aimed a skillshot at something specific
  [x] Read an item description before choosing (watch their eyes)
  [x] Swapped an item when slots were full
  [x] Skipped an offer
  [ ] Stuck on geometry            <- metric 5, note where: 
  [ ] Blamed the controls out loud

DEATHS   cause of each, in their words:
  "I mistimed the blast chain."
  "I dashed straight into the hazard."

#### Tester 11   Date 2026-08-19   Input: gamepad

RUN 1   items taken: Momentum, Focusing Lens, skip   - died on arena 3   - length 9 min
RUN 2   items taken: Momentum, Focusing Lens, Quickstep, skip   - died on arena 5   - length 17 min
        Started run 2 unprompted?   NO          <- metric 3
        Chose differently on run 2? DELIBERATE   <- metric 1

OBSERVED (tick as it happens)
  [x] Placed a bomb and escaped it on purpose
  [x] Used the dash to escape
  [ ] Used the dash to attack
  [x] Aimed a skillshot at something specific
  [x] Read an item description before choosing (watch their eyes)
  [x] Swapped an item when slots were full
  [x] Skipped an offer
  [ ] Stuck on geometry            <- metric 5, note where: 
  [ ] Blamed the controls out loud

DEATHS   cause of each, in their words:
  "I go to the wrong way."
  "The dash button keeps changing in my head."

#### Tester 12   Date 2026-08-19   Input: touch

RUN 1   items taken: Twin Shot, Momentum, Quickstep   - died on arena 4   - length 13 min
RUN 2   items taken: Twin Shot, Momentum, skip, Quickstep, skip   - died on arena 6   - length 22 min
        Started run 2 unprompted?   YES          <- metric 3
        Chose differently on run 2? RANDOM   <- metric 1

OBSERVED (tick as it happens)
  [x] Placed a bomb and escaped it on purpose
  [x] Used the dash to escape
  [x] Used the dash to attack
  [x] Aimed a skillshot at something specific
  [x] Read an item description before choosing (watch their eyes)
  [x] Swapped an item when slots were full
  [x] Skipped an offer
  [ ] Stuck on geometry            <- metric 5, note where: 
  [ ] Blamed the controls out loud

DEATHS   cause of each, in their words:
  "The controls are fighting my thumb."
  "I couldn't get around the rocks fast enough."

## 10c. Round 3 outcome (2026-08-23)

**12 testers · 5 keyboard, 4 gamepad, 3 touch · gate NOT called — but for the first time the
session is trustworthy**

| # | Metric | Threshold | Result | |
|---|---|---|---|---|
| 1 | Deliberately picks differently on run 2 | ≥ 60% | **50%** (3/6) — **n = 6, below the §2 floor** | ⚠️ not callable |
| 2 | Can describe their build unprompted | ≥ 50% | **75%** (9/12) | ✅ |
| 3 | Voluntary second run | ≥ 60% | 83% of the 6 who could · 42% of all 12 | ⚠️ ambiguous |
| 4 | Deaths blamed on self, not controls | ≥ 80% | **100%** (17/17 deaths) | ✅ |
| 5 | Stuck on geometry | 0 | **0** | ✅ |

### The controls are fixed, and that is the headline

**Metric 4 went from 62% to 100%, and nobody blamed the controls out loud on any device** — round 2
had two who did. Seventeen deaths, seventeen self-attributions, several of them cheerful about it:
*"I abused of dash and failed, my bad"* (Mom), *"I go to the wrong direction and get blocked by
blocks, fair"* (Kif).

That last one is worth dwelling on. Round 2's tester 11 said *"I go to the wrong way"* and it was a
complaint about a dash that ignored the diagonal they were running. The same sentence comes back in
round 3 with **"fair"** attached.

§1 makes metric 4 a filter rather than a verdict: it says whether the other four can be believed.
**Round 3 is the first round where they can.**

### And the game got substantially deeper

| | Round 2 | Round 3 |
|---|---|---|
| First run, mean length | 10.1 min | **22.4 min** |
| First run, mean arena at death | 3.0 | **7.2** |

The same content, the same difficulty curve, twice the run. Nothing was added between the rounds
except working controls.

### Metric 2 passes, and it is new information

Never measured before — round 2 produced zero readings across twelve testers. **9 of 12 described
their build unaided, without being asked**, and the end-of-session answers are specific rather than
polite:

- *"First run I had a long-range shot build with cooldown, but second run I went all-in on dash and
  bombs; way more fun."* (Leela)
- *"I built a dash-bomb hybrid with pierce and cooldown; it was strong until I got cornered."* (Mom)
- *"I had the dash-bomb thing, the pierce, and overcharge; it was like a walking artillery."*
  (Bender)

Leela's is the single most valuable line in the round, because it is metric 1 and metric 2 in one
breath: she named two distinct builds, said which she preferred, and said why. **That is the synergy
pillar landing, in a tester's own words.**

### Why metric 1 still cannot be called — and it is a different reason this time

**Half the testers never played a run 2.** Six of twelve, and the six are the long runs: 26, 27, 30,
31 and 34 minutes, dying at arenas 8, 9 and 10. Fry never died at all and simply stopped after 17
minutes at arena 6.

That is not a refusal. **It is a run that has become a full session.** Metric 1 was designed when a
run was ten minutes and a tester would naturally do two; at twenty-two minutes, one run *is* the
sitting. The measurement no longer fits the game it is measuring.

So metric 1 has **n = 6**, against a §2 floor of eight, and 3 of 6 coded DELIBERATE. Not callable.

> **Do not read the sheet's own total.** Taken at face value the metric-1 column says 8 of 12
> DELIBERATE — 67%, a pass. **Five of those eight had no run 2 to be deliberate about.** A column
> asking "chose differently on run 2" cannot be answered by someone who played one run, and filling
> it in anyway would have produced a false pass on the one metric the whole project rests on.

### The one clean failure, and it is one device

Every RANDOM in the round is a touch tester. Every touch tester is a RANDOM. And **no touch tester
described their build spontaneously**, where 9 of 9 keyboard and gamepad testers did.

| Device | n | Described build unaided | Coded RANDOM |
|---|---|---|---|
| Keyboard | 5 | 5/5 | 0 |
| Gamepad | 4 | 4/4 | 0 |
| **Touch** | **3** | **0/3** | **3/3** |

What they said when asked:

- *"I just picked things that sounded cool."* (Amy)
- *"It had a lot of things, I don't know, I just clicked."* (Nibbler)
- *"I just picked whatever, it was mostly dying fast."* (Cubert)

§7 is unambiguous about this combination: **metrics 1 and 2 failing together means they could not
tell what the items do. Interface first, then re-test — do not conclude anything about the design
from it.** Applied to touch alone, because on the other two devices both metrics passed.

It is not the controls this time — no touch tester blamed them, and all three aimed a skillshot at
something specific. **It is the item cards**, which are text-only at phone size. This is exactly the
round-2 leftover that has been sitting in the queue: *icons on the skill cards*, plus the testers in
both previous rounds who clicked item **names** expecting a description. Three rounds have now
pointed at the same screen.

### Also worth capturing

**The bomb may be losing its primacy.** "Placed a bomb and escaped it on purpose" fell from **12/12
in round 2 to 7/12 in round 3**, while "used the dash to attack" held at 7/12 and every build
description in the round leads with dash, pierce or shot. Farnsworth died saying *"I was running too
much and not killing enough."* Open question #3 in
[07-CONCEPT-REVISION §5](07-CONCEPT-REVISION.md) asks whether the bomb stays the primary verb; this
is the first evidence, and it says no. A balance finding rather than a gate failure (§8), but it
goes to the heart of what makes this a Bomberman hybrid rather than a twin-stick shooter.

**Every second run ended at arena 4 or 5** — 4, 4, 4, 4, 4, 5 — and every one was shorter than that
tester's first, which is the exact inverse of round 2 where all twelve second runs were longer. The
likeliest reading is that run 2 has changed activity: after a satisfying twenty-minute run it is a
build experiment rather than a serious attempt, which is what Leela describes. Fatigue after a
thirty-minute first run is an equally good explanation and this data cannot separate them. The
uniformity is worth watching either way.

### Sheet problems to fix before round 4

- ~~**Metric 1 was filled in for six testers who had no run 2.**~~ **Structurally impossible from
  2026-08-24**: the metric is now one line per offer, so a run that produced no offers produces no
  rows, and there is nothing to invent. A missing number is honest; an invented one nearly produced
  a false pass here.
- ~~**A `MIXED` code appeared**~~ **Decided 2026-08-24**: the third state is **U**, and it means the
  pick was not observed. A pick you watched and found hard to call is R — see [§5](#5-what-to-record).
- ~~**Fry's metric 2 contradicts itself**~~ **Fixed 2026-08-24**: the fallback question is nested
  under the strict box on the sheet, so the two readings can no longer both be filled in.

### What round 4 needs

1. ~~**Fix the item cards for touch first.**~~ **Done 2026-08-24**, device-verified on a Galaxy S21
   Ultra, a Solana Seeker 2 and a RedMagic tablet. The interface half of §10d item 1 is closed; the
   design half is still unobserved.
2. ~~**Re-measure metric 1 inside a single run.**~~ **Done 2026-08-24** — [§5](#5-what-to-record) is
   rewritten around one line per offer, with the three codes defined, the first pick reported
   separately, and no question anywhere that needs a second run to answer.
3. **Then re-run.** Metrics 4 and 5 are solved and metric 2 passes on two of three devices; what
   remains unanswered is the one the project exists to answer.

**Both preconditions are met.** Nothing on this list is now waiting on the build.

---

## 10d. Gate decision (2026-08-23) — rounds stopped

**28 testers over three rounds. The gate is called, and deliberately not on metric 1.**

Metric 1 was never successfully measured. Round 1 was four people, below the floor. Round 2 measured
the controls. Round 3 grew the runs past the point where a second one happens. Three attempts, three
different reasons, no number — and running a fourth round would most likely produce a fourth reason,
because the failure is now in the instrument rather than in the game.

**What is being relied on instead**, all from the one round whose metric 4 says it can be trusted:

| Evidence | Round 3 |
|---|---|
| Described their build unaided, in their own playstyle language | 9/12 |
| Read an item description before choosing | 12/12 |
| Swapped an item when slots were full | 11/12 |
| Skipped an offer outright | 11/12 |
| Deaths blamed on self, not controls | 17/17 |
| Stuck on geometry | 0 |
| First run, mean | 22.4 min, arena 7.2 |

A tester who calls their own build *"a walking artillery"* without being asked has answered the
question metric 1 exists to ask, and answered it better than a facilitator's DELIBERATE/RANDOM
judgement could. Leela answered it outright: *"First run I had a long-range shot build with
cooldown, but second run I went all-in on dash and bombs; way more fun."*

**This is weaker than a clean metric 1 and is recorded as such.** The honest claim is not *the gate
passed* — it is *the gate was superseded by better evidence, on two of three devices.*

### What is explicitly not covered by this decision

1. **Touch.** 0 of 3 described a build; 3 of 3 picked at random. §7 calls that an interface failure,
   not a design one, and it now carries forward as build work — the item cards are text-only at
   phone size. **It is not closed, it is relocated.** Verify it with three people on a phone for ten
   minutes, not with another twelve-tester round.

   > **The interface half is closed (2026-08-24).** The cards were resized from the device rather
   > than by eye, and the defect underneath the round-3 failure was found: the `SHOT` button sat on
   > top of the right-hand choice card and swallowed the taps meant for it. Fixed twice over — the
   > controls stand down while a screen is up, and the overlay now raises itself above whatever
   > shares its canvas — and verified on a Galaxy S21 Ultra, a Solana Seeker 2 and a RedMagic
   > (NP05J) tablet. **What is still unobserved is the design half**: whether a player on a phone,
   > now that the cards can be read and tapped, chooses deliberately. That is the ten-minute check
   > with three people, and it no longer has a known defect standing in front of it.
2. **The bomb.** Bomb-and-escape fell from 12/12 to 7/12 while every build description led with
   dash, pierce or shot. The gate never asked this and it matters more than the gate did: it decides
   whether this is a Bomberman hybrid or a twin-stick shooter. Settle it before content is built on
   top of either answer.
3. **M7's exit criterion** — *"≥ 60% of playtesters change their loadout between runs"* — is metric 1
   wearing a different hat and will hit the identical wall. Redefine it to read choices *within* a
   run before it is measured.

### If rounds resume

Nothing here retires the protocol. §1–§9 stand, the sheet in §5 is current, and the one change round
4 would need is metric 1 rewritten for a game whose first run is twenty-two minutes: measure
build-shaping inside a run — swaps made in response to something, skips that hold a slot open,
whether the build has an arc — not a comparison between two runs that testers no longer play.

---

### Round 3

#### Tester Fry   Date 2026-08-23   Input: keyboard

RUN 1   items taken: Bomb Trail, Overcharge,    died on arena 6   length 17 min
RUN 2   items taken: ______________________   died on arena ___   length ___ min
        Started run 2 unprompted?   NA                     <- metric 3
        Chose differently on run 2? NA
        Chose deliberately?         DELIBERATE          <- metric 1

BUILD LEGIBILITY
  [x] Described their build out loud, unaided, at any point      <- metric 2
      AT THE VERY END ONLY, once they have stopped playing, ask:
      "tell me about the character you ended up with"
      Could they?  NO
      What they said: 

OBSERVED (tick as it happens)
  [x] Placed a bomb and escaped it on purpose
  [x] Used the dash to escape
  [ ] Used the dash to attack
  [x] Aimed a skillshot at something specific
  [x] Read an item description before choosing (watch their eyes)
  [x] Swapped an item when slots were full
  [x] Skipped an offer
  [ ] Stuck on geometry            <- metric 5, note where
  [ ] Blamed the controls out loud

DEATHS   cause of each, in their words: stop playing, never died

---

#### Tester Leela   Date 2026-08-23   Input: gamepad

RUN 1   items taken: Piercing Rounds, Quickstep, Focusing Lens, Skip, Twin Shot, Overclock    died on arena 7   length 22 min
RUN 2   items taken: Overcharge, Momentum, Bomb Trail    died on arena 4   length 9 min
        Started run 2 unprompted?   Yes                     <- metric 3
        Chose differently on run 2? Yes
        Chose deliberately?         DELIBERATE          <- metric 1

BUILD LEGIBILITY
  [x] Described their build out loud, unaided, at any point      <- metric 2
      AT THE VERY END ONLY, once they have stopped playing, ask:
      "tell me about the character you ended up with"
      Could they?  YES
      What they said: First run I had a long-range shot build with cooldown, but second run I went all-in on dash and bombs; way more fun.

OBSERVED (tick as it happens)
  [x] Placed a bomb and escaped it on purpose
  [x] Used the dash to escape
  [x] Used the dash to attack
  [x] Aimed a skillshot at something specific
  [x] Read an item description before choosing (watch their eyes)
  [x] Swapped an item when slots were full
  [x] Skipped an offer
  [ ] Stuck on geometry
  [ ] Blamed the controls out loud

DEATHS   cause of each, in their words: Run 1: I was testing buttons. Run 2: dashed into my own bomb by mistake.

---

#### Tester Bender   Date 2026-08-23   Input: keyboard

RUN 1   items taken: Overcharge, Piercing Rounds, Skip, Momentum, Overclock, Skip, Bomb Trail    died on arena 8   length 26 min
RUN 2   items taken: ______________________   died on arena ___   length ___ min
        Started run 2 unprompted?   NA                     <- metric 3
        Chose differently on run 2? NA
        Chose deliberately?         DELIBERATE          <- metric 1

BUILD LEGIBILITY
  [x] Described their build out loud, unaided, at any point      <- metric 2
      AT THE VERY END ONLY, once they have stopped playing, ask:
      "tell me about the character you ended up with"
      Could they?  PARTLY
      What they said: I had the dash-bomb thing, the pierce, and overcharge; it was like a walking artillery.

OBSERVED (tick as it happens)
  [x] Placed a bomb and escaped it on purpose
  [x] Used the dash to escape
  [x] Used the dash to attack
  [x] Aimed a skillshot at something specific
  [x] Read an item description before choosing (watch their eyes)
  [x] Swapped an item when slots were full
  [x] Skipped an offer
  [ ] Stuck on geometry
  [ ] Blamed the controls out loud

DEATHS   cause of each, in their words: I got caught by two mobs while my dash was cooling down.

---

#### Tester Amy   Date 2026-08-23   Input: touch

RUN 1   items taken: Twin Shot, Skip, Overclock, Skip    died on arena 5   length 14 min
RUN 2   items taken: Focusing Lens, Quickstep, Piercing Rounds    died on arena 4   length 10 min
        Started run 2 unprompted?   Yes                     <- metric 3
        Chose differently on run 2? Yes
        Chose deliberately?         RANDOM          <- metric 1

BUILD LEGIBILITY
  [ ] Described their build out loud, unaided, at any point      <- metric 2
      AT THE VERY END ONLY, once they have stopped playing, ask:
      "tell me about the character you ended up with"
      Could they?  PARTLY
      What they said: I just picked things that sounded cool; second one had faster dash and a big shot, I think.

OBSERVED (tick as it happens)
  [ ] Placed a bomb and escaped it on purpose
  [x] Used the dash to escape
  [ ] Used the dash to attack
  [x] Aimed a skillshot at something specific
  [x] Read an item description before choosing (watch their eyes)
  [x] Swapped an item when slots were full
  [x] Skipped an offer
  [ ] Stuck on geometry
  [ ] Blamed the controls out loud

DEATHS   cause of each, in their words: Run 1: I skipped too much and had no way to kill the group. Run 2: I missed a big shot and got overrun.

---

#### Tester Zoidberg   Date 2026-08-23   Input: gamepad

RUN 1   items taken: Overclock, Quickstep, Piercing Rounds, Skip, Momentum, Skip, Overcharge, Skip    died on arena 9   length 31 min
RUN 2   items taken: ______________________   died on arena ___   length ___ min
        Started run 2 unprompted?   NA                     <- metric 3
        Chose differently on run 2? NA
        Chose deliberately?         DELIBERATE          <- metric 1

BUILD LEGIBILITY
  [x] Described their build out loud, unaided, at any point      <- metric 2
      AT THE VERY END ONLY, once they have stopped playing, ask:
      "tell me about the character you ended up with"
      Could they?  YES
      What they said: I built for fast dashes and pierce, then added overcharge at the end for bigger bursts.

OBSERVED (tick as it happens)
  [ ] Placed a bomb and escaped it on purpose
  [x] Used the dash to escape
  [x] Used the dash to attack
  [x] Aimed a skillshot at something specific
  [x] Read an item description before choosing (watch their eyes)
  [x] Swapped an item when slots were full
  [x] Skipped an offer
  [ ] Stuck on geometry
  [ ] Blamed the controls out loud

DEATHS   cause of each, in their words: I dashed into a big crowd and the cooldown was still up; got eaten.

---

#### Tester Hermes   Date 2026-08-23   Input: keyboard

RUN 1   items taken: Focusing Lens, Quickstep, Skip, Overclock, Piercing Rounds    died on arena 6   length 18 min
RUN 2   items taken: Overcharge, Bomb Trail, Skip    died on arena 4   length 11 min
        Started run 2 unprompted?   No                     <- metric 3
        Chose differently on run 2? Yes
        Chose deliberately?         DELIBERATE          <- metric 1

BUILD LEGIBILITY
  [x] Described their build out loud, unaided, at any point      <- metric 2
      AT THE VERY END ONLY, once they have stopped playing, ask:
      "tell me about the character you ended up with"
      Could they?  YES
      What they said: First I tried the big focused shot with fast dash, but second run I went bomb trail and overcharge for clear.

OBSERVED (tick as it happens)
  [x] Placed a bomb and escaped it on purpose
  [x] Used the dash to escape
  [ ] Used the dash to attack
  [x] Aimed a skillshot at something specific
  [x] Read an item description before choosing (watch their eyes)
  [x] Swapped an item when slots were full
  [x] Skipped an offer
  [ ] Stuck on geometry
  [ ] Blamed the controls out loud

DEATHS   cause of each, in their words: Run 1: I got flanked. Run 2: I placed a bomb and dashed the wrong way.

---

#### Tester Nibbler   Date 2026-08-23   Input: touch

RUN 1   items taken: Piercing Rounds, Skip, Overcharge, Bomb Trail, Skip, Twin Shot    died on arena 7   length 21 min
RUN 2   items taken: ______________________   died on arena ___   length ___ min
        Started run 2 unprompted?   NA                     <- metric 3
        Chose differently on run 2? NA
        Chose deliberately?         RANDOM          <- metric 1

BUILD LEGIBILITY
  [ ] Described their build out loud, unaided, at any point      <- metric 2
      AT THE VERY END ONLY, once they have stopped playing, ask:
      "tell me about the character you ended up with"
      Could they?  NO
      What they said: It had a lot of things, I don't know, I just clicked.

OBSERVED (tick as it happens)
  [x] Placed a bomb and escaped it on purpose
  [x] Used the dash to escape
  [ ] Used the dash to attack
  [x] Aimed a skillshot at something specific
  [x] Read an item description before choosing (watch their eyes)
  [x] Swapped an item when slots were full
  [x] Skipped an offer
  [ ] Stuck on geometry
  [ ] Blamed the controls out loud

DEATHS   cause of each, in their words: I dashed in the direction of a bomb, my bad.

---

#### Tester Kif   Date 2026-08-23   Input: gamepad

RUN 1   items taken: Overcharge, Momentum, Skip, Quickstep, Skip, Piercing Rounds, Overclock    died on arena 8   length 27 min
RUN 2   items taken: Bomb Trail, Focusing Lens, Skip    died on arena 4   length 10 min
        Started run 2 unprompted?   Yes                     <- metric 3
        Chose differently on run 2? Yes
        Chose deliberately?         MIXED          <- metric 1

BUILD LEGIBILITY
  [x] Described their build out loud, unaided, at any point      <- metric 2
      AT THE VERY END ONLY, once they have stopped playing, ask:
      "tell me about the character you ended up with"
      Could they?  PARTLY
      What they said: I had a dash-heavy build first, then tried bombs and a big shot second; not sure which was better.

OBSERVED (tick as it happens)
  [x] Placed a bomb and escaped it on purpose
  [x] Used the dash to escape
  [x] Used the dash to attack
  [x] Aimed a skillshot at something specific
  [x] Read an item description before choosing (watch their eyes)
  [x] Swapped an item when slots were full
  [x] Skipped an offer
  [ ] Stuck on geometry
  [ ] Blamed the controls out loud

DEATHS   cause of each, in their words: Run 1: I got eaten. Run 2: I go to the wrong direction and get blocked by blocks, fair.

---

#### Tester Farnsworth   Date 2026-08-23   Input: keyboard

RUN 1   items taken: Overclock, Overcharge, Skip, Focusing Lens, Quickstep, Skip, Piercing Rounds, Momentum, Skip    died on arena 10   length 34 min
RUN 2   items taken: ______________________   died on arena ___   length ___ min
        Started run 2 unprompted?   NA                     <- metric 3
        Chose differently on run 2? NA
        Chose deliberately?         DELIBERATE          <- metric 1

BUILD LEGIBILITY
  [x] Described their build out loud, unaided, at any point      <- metric 2
      AT THE VERY END ONLY, once they have stopped playing, ask:
      "tell me about the character you ended up with"
      Could they?  YES
      What they said: I kept swapping to try everything: cooldown, charge, dash, then pierce and momentum at the end.

OBSERVED (tick as it happens)
  [ ] Placed a bomb and escaped it on purpose
  [x] Used the dash to escape
  [x] Used the dash to attack
  [x] Aimed a skillshot at something specific
  [x] Read an item description before choosing (watch their eyes)
  [x] Swapped an item when slots were full
  [x] Skipped an offer
  [ ] Stuck on geometry
  [ ] Blamed the controls out loud

DEATHS   cause of each, in their words: I was running too much and not killing enough ; 1 vs 3.

---

#### Tester Cubert   Date 2026-08-23   Input: touch

RUN 1   items taken: Quickstep, Skip, Overcharge, Skip    died on arena 5   length 13 min
RUN 2   items taken: Twin Shot, Focusing Lens, Skip    died on arena 4   length 9 min
        Started run 2 unprompted?   Yes                     <- metric 3
        Chose differently on run 2? Yes
        Chose deliberately?         RANDOM          <- metric 1

BUILD LEGIBILITY
  [ ] Described their build out loud, unaided, at any point      <- metric 2
      AT THE VERY END ONLY, once they have stopped playing, ask:
      "tell me about the character you ended up with"
      Could they?  NO
      What they said: I just picked whatever, it was mostly dying fast.

OBSERVED (tick as it happens)
  [ ] Placed a bomb and escaped it on purpose
  [x] Used the dash to escape
  [ ] Used the dash to attack
  [x] Aimed a skillshot at something specific
  [x] Read an item description before choosing (watch their eyes)
  [ ] Swapped an item when slots were full
  [x] Skipped an offer
  [ ] Stuck on geometry
  [ ] Blamed the controls out loud

DEATHS   cause of each, in their words: Run 1: I didn't take enough damage. Run 2: They got me.

---

#### Tester Mom   Date 2026-08-23   Input: gamepad

RUN 1   items taken: Momentum, Overcharge, Piercing Rounds, Skip, Bomb Trail, Skip, Quickstep, Overclock    died on arena 9   length 30 min
RUN 2   items taken: ______________________   died on arena ___   length ___ min
        Started run 2 unprompted?   NA                     <- metric 3
        Chose differently on run 2? NA
        Chose deliberately?         DELIBERATE          <- metric 1

BUILD LEGIBILITY
  [x] Described their build out loud, unaided, at any point      <- metric 2
      AT THE VERY END ONLY, once they have stopped playing, ask:
      "tell me about the character you ended up with"
      Could they?  YES
      What they said: I built a dash-bomb hybrid with pierce and cooldown; it was strong until I got cornered.

OBSERVED (tick as it happens)
  [x] Placed a bomb and escaped it on purpose
  [x] Used the dash to escape
  [x] Used the dash to attack
  [x] Aimed a skillshot at something specific
  [x] Read an item description before choosing (watch their eyes)
  [x] Swapped an item when slots were full
  [x] Skipped an offer
  [ ] Stuck on geometry
  [ ] Blamed the controls out loud

DEATHS   cause of each, in their words: I abused of dash and failed, my bad.

---

#### Tester Zapp   Date 2026-08-23   Input: keyboard

RUN 1   items taken: Overcharge, Skip, Piercing Rounds, Quickstep, Skip    died on arena 6   length 16 min
RUN 2   items taken: Twin Shot, Overclock, Skip, Momentum    died on arena 5   length 12 min
        Started run 2 unprompted?   Yes                     <- metric 3
        Chose differently on run 2? Yes
        Chose deliberately?         DELIBERATE          <- metric 1

BUILD LEGIBILITY
  [x] Described their build out loud, unaided, at any point      <- metric 2
      AT THE VERY END ONLY, once they have stopped playing, ask:
      "tell me about the character you ended up with"
      Could they?  PARTLY
      What they said: First was a shooty dash build, second I tried twin shot and momentum to dash through enemies.

OBSERVED (tick as it happens)
  [ ] Placed a bomb and escaped it on purpose
  [x] Used the dash to escape
  [x] Used the dash to attack
  [x] Aimed a skillshot at something specific
  [x] Read an item description before choosing (watch their eyes)
  [x] Swapped an item when slots were full
  [x] Skipped an offer
  [ ] Stuck on geometry
  [ ] Blamed the controls out loud

DEATHS   cause of each, in their words: Run 1: I got swarmed after a bad skip. Run 2: Momentum made me too aggressive and I died in a group.
