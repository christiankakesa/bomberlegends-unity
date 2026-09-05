# Author play log — not evidence

**Every entry here is the developer playing his own build.** n = 1 across the whole file, and the one
is excluded from any sample by [10-PLAYTEST-PROTOCOL §8](10-PLAYTEST-PROTOCOL.md). Nothing in this
file is a round, a percentage, a threshold or a gate reading. It exists because outside testers are
not available (decided 2026-09-05) and momentum should not wait for them — and because the desk lies
about device feel and device hardware, which are the two risks CLAUDE.md names.

**What the author cannot measure**, so none of these columns exist here: whether a pick was
deliberate (he wrote the cards), whether a build can be described (he wrote the descriptions),
whether a second run is voluntary (he is the facilitator), whether a death is blamed on the controls
(he knows which mechanisms exist). Whether the bomb is still the verb, what a bomb is worth, whether
the tail is too hard for a 7-arena player, whether the first pick reads as random, whether anyone
asks for a third skill — all of these are questions about strangers and stay with
[10 §10d](10-PLAYTEST-PROTOCOL.md). 14-INSIGHTS §2 already recorded the standard, verbatim: *"the
author playing his own build, which is worth recording and is not evidence."*

**What an entry does feed:** bug tickets with a seed and an arena; device numbers (frame time,
temperature, tap misfire counts, dp) that are the only figures ever compared between entries;
`08-IMPLEMENTED` rows flipped from 🧪 *built* to ✅ *built and played* — a status, never
*validated*; and hypotheses phrased as something a stranger could be observed doing on the §5 sheet.
**What it never feeds:** [07 §3](07-CONCEPT-REVISION.md), [10 §10a–d](10-PLAYTEST-PROTOCOL.md), or
any table beside a round's numbers. Run length and arena reached are recorded as facts of the
recording and are never compared across entries — a ceiling player's time bounds nothing the
project asks about.

**Standing biases, stated once so entries need not repeat them:** knows every item; knows the wake-up
and where enemies come from; materially better than round 3's testers; has replayed seed 1's boards
for a month. One author session predates this log (2026-08-24, eight arenas, no recording, no seed
noted) — see 14 §2; it is not an entry.

**Discipline, from §5 of the protocol:** the Purpose and Label are written before the phone is
touched; the session is screen-recorded and counts are taken from the recording, never from memory;
anything not checked is NOT SEEN. If it reproduces from a seed and an arena number, it is a bug and
may be fixed. If it needs an adjective, it waits for a round.

---

## The sheet

```
Session   S-YYYY-MM-DD-a
Build     <short commit> clean|dirty · Development|Release · APK|WebGL|Editor
Device    S21 Ultra | Seeker 2 | RedMagic NP05J | desktop      Input: touch | pad | kb+m
Config    seed __ (pause menu) · startingItems EMPTY | <list> · startingArena __ · overrides or "defaults"
Label     BUG | DEVICE | MECHANISM | FEEL          (one per session, chosen before it starts)
Purpose   one line, written before playing

RUNS (from the recording)
  arena at stop · length · offered / took at each choice screen incl. SKIP · cause, in factual words

BUGS
  seed · arena · device · steps · recording timestamp · ticket
  stuck on geometry always goes here, with the seed

DEVICE NUMBERS (method stated once, the first time a number appears)
  frame time p50/p99 at 0/5/10 min · battery °C and % · overlay error lines
  GC B/frame and draw calls on the profiler-attached entry only
  tap misfires /30 · short-drag misfires /30 · aimed shots at a block hit /20 · dp per device

MECHANISM (YES | NO | NOT SEEN, with arena) — 08-IMPLEMENTED's 🧪 rows only
  first offer with nothing held had no Kinetic Core / Overclock (≥ 5 fresh seeds)
  a declined first offer left the second still gated
  an alerted Sentinel left the blast footprint and held at its edge
  block clustering visible in a generated arena
  lane assist at 0.5 kept the stick off pillar corners
  background the app mid-arena, resume: same arena, same build, health as on entering the arena
  bait into a pocket and kill: NOT POSSIBLE | not tried        (a success is no information)

FEEL (author opinion, one line each)

HYPOTHESES FOR A ROUND
  one stranger-observable behaviour each, phrased as a §5 sheet observation — no dial, no direction

FOLLOW-UPS
  08 rows flipped 🧪 → ✅ · tickets · doc corrections
```

---

## The programme — about five hours over five evenings

Written so a skipped evening is visible as a gap rather than silently absorbed.

| | Where | ~ | What |
|---|---|---|---|
| 1 | desk | 30 min | Build a Development APK and note its commit. Create the first entry. Five-minute browser check: a WebGL run resumes across a page refresh (07 §4l). |
| 2 | S21 Ultra | 45 min | **DEVICE.** Start at arena 9 with a starting build. Phone idle and unplugged 15 min first. Ten-minute heavy window with the recorder *off*: the tail wake-up with twelve alerted Sentinels on a 31 × 21 board; a Bomb Trail + Overcharge chain; the arena-clear rebuild into arena 10. Numbers at 0/5/10 from the overlay and `adb shell dumpsys battery` over wireless adb. Profiler attached on this device only; that capture is the T-036 baseline. |
| 3 | RedMagic + Seeker 2 | 1.5 h | **DEVICE.** Same window on each, cooling one while measuring the other. Tablet: HUD line and touch cluster against the 25-unit camera, reach of SHOT / DASH / cancel from the resting thumb. Seeker 2: bomb drop audible and dash quieter, at default media volume at arm's length — into FEEL, not DEVICE NUMBERS. Record each SoC (`adb shell getprop ro.board.platform`); the slowest is the working floor device, with the note that none of the three is the mid-tier 02 §6 names. |
| 4 | S21 Ultra | 75 min | **BUG.** Random seed shown on screen, nothing held, one climb to arena 9+. Touch counts in arena 1 before anything wakes: 30 taps, 30 short drags, 20 aimed shots at one destructible block from a fixed distance. Mechanism ticks on the way up. Background and resume once at depth. One bait attempt. Quit to hub before dying. |
| 5 | desk | 1 h | Harness numbers in 07 §4q's table style with a control row: blocks destroyed per bomb at cluster size 1 vs 3; bomb kill share against alerted vs dormant Sentinels; enemies alerted at once and time in the tail at `ArenaTailShare` 50 vs 100. These are the rule-side halves of round 4's questions and need no human. |

**What is deliberately not on the programme.** No A/B of `ArenaTailShare` or any other simulation
value on the author: a busier tail is harder by construction, the author's result at 50 vs 100
answers nothing about a 7-arena player, and 14 §2 already names the dial as one to reach for *after*
round 4 is read. Every value round 4 will be measured against is frozen and listed in
[10 §10d](10-PLAYTEST-PROTOCOL.md). No kill-source shares or per-arena HP from frame-stepping an
hour of video — a run's facts that take an hour to extract get filled in from memory or not at all.

**The tooling, built 2026-09-05.** On `MatchInstaller` in the Match scene: **Seed** 0 draws a fresh
seed for every attempt (the shipped value; any other number fixes the run for replaying one board),
and **Starting arena** above 1 begins every attempt there with the starting items and full health —
a run that starts deep neither resumes the saved run nor writes over it. The **pause menu** shows
`SEED n · commit · DEV|REL`, which is the line an entry's Config and Build fields are copied from;
the commit is stamped into the player version by every build and carries a `*` when the tree was
dirty. The development overlay's first line is `FRAME p50 · p99` over the last ten seconds, which is
where a frame-time number comes from — `dumpsys gfxinfo` reads Android's own view frames and not
Unity's surface. The development APK connects to the Editor's profiler on launch when one is
listening over adb. So evening 1 is the first entry and the WebGL refresh check, and nothing else.

---

## Entries

*(none yet — the first is the first recorded session)*
