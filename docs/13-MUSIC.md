# Music and sound direction

**Status 2026-08-16.** Sound effects are generated placeholders, audible in build. There is no music.
This is the brief for both — written for a composer, and usable directly as prompt material.

Related: [09-GAME-FEEL.md](09-GAME-FEEL.md) for how audio is wired to gameplay events,
[11-ART-DIRECTION.md](11-ART-DIRECTION.md) for the visual half of the same problem.

---

## 1. The premise: the Grid stole the music

The lore hands the score its entire architecture in one line.

> *"The people wander in silence, their inner flames extinguished."*

**Silence is not the absence of a soundtrack. Silence is what the enemy did.** Sombra-Corps locked
away every memory, dream and heartbeat — and a heartbeat is a rhythm, a memory is a melody. The
Shadow Grid did not merely drain Ébène-Prime of colour. It took its music, and left a hum.

Which means the score is not accompaniment. **It is the thing the player is taking back.** Every
sector liberated returns a little more of the city's song, and the soundtrack is the scoreboard.

Three consequences follow, and everything in this document descends from them:

1. **A locked sector should sound wrong** — thin, monophonic, colourless. Not quiet in a
   restful way; quiet in the way a room is quiet after something has been removed from it.
2. **Music arrives as a reward, not as a backdrop.** It layers in as the player earns it.
3. **The enemy is made of stolen music.** Sentinels are *"corrupted echoes of stolen hopes"*. Take
   that literally: their sound is the game's own melody, degraded.

That last one is the strongest idea available and §4 is built on it.

---

## 2. The Two Voices

| | **The Grid Voice** — Sombra-Corps | **The Soul Voice** — Génération Néon |
|---|---|---|
| Sound | Synthetic, quantised, cold | Acoustic, human, breathing |
| Timing | Locked to the grid, machine-exact | Loose — ahead or behind the beat |
| Register | Sub bass and brittle highs, **hollow mids** | Warm mids, the human band |
| Harmony | None. Unison and octaves only | Thirds, fifths, harmony returning |
| Instruments | Sub, FM bells, granular noise, glitch | The sector's culture, and live voice |
| Meaning | What was taken | What is being reclaimed |

**The Grid Voice never changes.** It is the same enemy in every sector on Earth.
**The Soul Voice is always local.** It is whoever lives there.

That single split does the whole job. Every track shares a spine; no two share a skin. The
Afrofuturist launch palette is not a special case — it is simply the first Soul Voice.

### Colour, stolen and returned

The art direction moves a sector from desaturated grey to hyper-saturated neon. Audio has an exact
equivalent, and it is not a metaphor — it is a mix instruction:

| | Locked | Liberated |
|---|---|---|
| Harmony | Bare octaves and unison | Thirds and fifths return |
| Spectrum | Band-passed 300 Hz – 4 kHz | Full range opens |
| Timing | Rigidly quantised | Human, slightly loose |
| Reverb | Dry, close, airless | Wide, alive, a real room |

**Desaturated colour is a filtered spectrum. Restored colour is harmony.** Run both transitions off
the same game state and the sector visibly and audibly wakes at the same moment.

---

## 3. The Ember

*"Their inner flames extinguished."* An ember is what survives when a fire is put out — and the last
thing the Grid failed to take is the tune everyone still half-remembers.

**The Ember is a contour, not a scale.** That distinction is the whole of it, and getting it wrong
is the most likely way this soundtrack fails.

**Shape:** rising fourth, step down, rising fifth, fall to the tonic. In the launch palette that is
roughly `D – G – F – C – D`, minor pentatonic on D. Simple enough for a child's kalimba,
ornamentable enough for a ney.

### What must not change, and what must

| Invariant | Free to change |
|---|---|
| **The contour** — up a fourth, down a step, up a fifth, fall home | The scale it is drawn from |
| The rhythm of the five notes | The tuning system |
| Its role: appears in every cue, in some state | Ornament, register, tempo, key |

A motif survives being re-moded. Listeners recognise **shape** long before they recognise pitch
content — which is why a theme still reads when it is transposed, re-harmonised or played in a
different mode entirely.

### An honest note on pentatonic

An earlier draft of this document claimed anhemitonic pentatonic was "the genuine intersection" of
every tradition here. **That was too neat.**

It is true of Japanese *in-sen* and *hirajoshi*, Chinese *gong* and *yu*, West African melodic
practice and Nordic modal song. Those four can carry the exact five pitches and sound native doing
it.

It is **not** true of Persian *dastgah* or Gregorian chant. Both are heptatonic; Persian additionally
uses intervals that do not exist in equal temperament at all. Forcing either into five equal-tempered
notes removes precisely what makes it itself, and every listener from those traditions hears it.

**So those palettes keep their own scales and quote the Ember by contour.** A ney plays up a fourth,
down a step, up a fifth and home — in Shur, with its own *koron* — and it is unmistakably the same
melody without being the same notes. A choir does it in Dorian across seven degrees.

That is not a compromise. It is how a theme travels.

**Rules**

- Five notes. Never six
- Nothing shorter than an eighth at 100 BPM — it must survive a plucked instrument's decay
- Singable by an untrained voice. If it needs an instrument to work, it is too complex
- **The contour is sacred. The pitches are a suggestion**
- **It appears in every cue in the game**, at some tempo, in some state

---

## 4. Corruption: the enemy is the Ember, broken

Sentinels are *corrupted echoes of stolen hopes*. So they are not given their own theme. **They are
given the Ember, damaged** — and the damage is the design.

| Enemy | What the Ember becomes |
|---|---|
| Sentinel (basic) | Two notes only, looped, quantised hard, pitched down a fourth |
| Alerted Sentinel | Three notes, tempo doubled, bit-crushed |
| Sentinel Lord | The full five notes — **reversed**, stretched, in the Grid Voice |
| The mastermind | The Ember at quarter speed, one note per bar, almost unrecognisable |

A player who has heard the Ember for six hours will recognise it inside the final boss without ever
being told. That is the payoff, and it costs nothing but discipline: **write no new melodies for
enemies.**

### Purification returns a fragment

The lore says *purify*, not kill. Take it at its word.

**When a Sentinel dies, its stolen note is released** — one clean pitch from the Ember, in the Soul
Voice, unquantised. Not a splat: a release. A held dissonance resolving.

Clearing an arena therefore assembles the Ember note by note, in the order the player kills. **The
last Sentinel completes the phrase**, and `MUS_Cleared` catches it.

### It must be a queue, not a trigger

The naive build — one tone per `EnemyKilled`, played immediately — works only when kills are spaced
out. **In this game they usually are not.** A chain detonation kills three or four Sentinels on a
single tick, and four Ember pitches fired simultaneously is not a phrase. It is a chord nobody asked
for, landing at exactly the moment the player did something impressive.

**So notes go into a queue and drain at a fixed interval — roughly 130 ms.**

| Kills | What is heard |
|---|---|
| One | A single clean note. A release |
| Two, spaced | Two notes of the phrase, in order |
| **Four in one chain** | **A fast ascending run — a flourish** |
| Whole arena over a minute | The Ember assembling, note by note |

The queue does not merely avoid a problem; it turns the common case into the best-sounding one. **A
big chain becomes a musical flourish**, which is precisely the feedback a big chain deserves — and it
is the same instinct as the chain pitch escalation in §9.

**Implementation:** a small FIFO in the feedback layer, drained on a timer, holding the next pitch
index. Notes are dropped rather than queued indefinitely if more than about eight are pending — a
run longer than that stops reading as a phrase. Nothing about this touches the simulation; it reacts
to `EnemyKilled` like any other view-layer effect.

---

## 5. Pressure: how the run escalates

### The mistake this section exists to avoid

The obvious design is to start thin and add stems as the run goes on, so the music grows with the
player. **Do not do that.**

Playtesting showed testers dying on arenas two and three. Under an additive scheme, the arrangement
this document spends its length describing is one almost nobody would hear — and the version
*everyone* hears, the one that decides whether they play again, would be the deliberately hollow one.
**First impressions cannot be the unfinished draft.**

### Arena one is already the full song

Every arena carries a complete, satisfying arrangement. Escalation is **transformation, not
accumulation**: the same six stems, re-balanced, as the Grid tightens its grip the deeper the player
goes.

| Stem | Voice | Contains |
|---|---|---|
| **A · Hum** | Grid | Pedal tone. The sound of the prison |
| **B · Pulse** | Grid | Machine heartbeat, quantised |
| **C · Groove** | Soul | The sector's percussion |
| **D · Ember** | Soul | Lead instrument, the theme |
| **E · Answer** | Soul | Second voice, call-and-response |
| **F · Chorus** | Soul | Human voice |

| Depth | Grid | Soul | What it sounds like |
|---|---|---|---|
| **Arena 1–2** | Present, low | **Full and warm** | Confident. The city still remembers itself |
| **Arena 3–4** | Rising, pulse hardens | Filtered, narrowing | Something is closing in |
| **Arena 5–6** | Dominant, mids hollowed | Pushed to the edges, reverb lost | Outnumbered but stubborn |
| **Arena 7+** | Almost total | **The Ember alone, refusing to stop** | One voice against a machine |

**The story inverts, and it is better for it.** The player does not gradually accumulate music. They
descend into a place that is trying to take it away, and the Ember will not shut up.

> Reclamation is **punctuation, not gradient**. It happens at `MUS_Cleared` — a full-spectrum,
> consonant, four-second payoff (§6.6) — and then the next sector closes in again. That rhythm of
> pressure and release is a run; a slow accumulation is a progress bar.

**Two overrides:**

- **Resolve failing (≤ 25 HP):** duck the Soul stems, push the Grid Pulse, add a high sustained
  tone. The music gets *quiet and airless*, not loud. Restores on heal.
- **Last Sentinel alive:** thin to Hum, Pulse and Ember. The arena is held open, waiting for the
  final note.

---

## 6. Cue list

Every cue states the Ember somewhere.

### 6.1 Boot — `MUS_Boot`
**3–5 s.** A single Grid hum, then one acoustic note answering it — the first human sound in the
game, and it does not resolve. The Grid speaks first; something small answers.

### 6.2 Hub, Ébène-Prime — `MUS_Hub`
**2:00 loop, 72 BPM.** The only safe place. The Ember complete, warm, on the sector's lead
instrument over a drone. Minimal percussion.

**No rhythmic drive.** If the hub grooves, the player rushes, and this is where builds get chosen.
It must survive a twentieth listen.

### 6.3 Choice screen — `MUS_Choice`
**0:30 loop, no tempo.** Suspended. Drone, Ember fragments, no downbeat. The player is reading three
cards and committing to a build; the music must not push. Ducks under UI.

### 6.4 Arena — `MUS_Arena_<Sector>`
**1:30 loop, 96–104 BPM, six stems re-balanced per §5.** The main body of work — one per Soul Voice.

**Write it as the arena-one mix first**: complete, warm, confident. That is the version most players
will ever hear, and it is the one that decides whether they play again. The deeper mixes are
re-balances of the same recording, not additions to it.

Driving but **not busy in the low-mids** (§8). A player pinned in a corner must not feel the music
running away from them.

### 6.5 Danger — *mix state, not a cue*
See §5. Never a separate track.

### 6.6 Sector cleared — `MUS_Cleared`
**4 s.** Catches the final purified note and **resolves the Ember upward** — the only consonant
landing in the entire game. Colour returning, made audible. Everywhere else the theme is withheld;
here it is given.

### 6.7 Awakening — `MUS_Awakening`
**6–8 s, ducks everything.** *"When your resolve peaks."* The arena bed stops dead. One held breath.
Then **the Grid Voice drops out entirely** and the Ember returns at full weight — all acoustic, full
percussion, chorus. For six seconds the machine is simply gone.

Music-led, not effect-led. This is the loudest the Soul Voice ever gets.

*(Awakening is not built — see the open questions in the concept revision. Specified so audio is not
an afterthought if it is.)*

### 6.8 Death — `MUS_Death`
**5 s.** Every Soul stem drops. The Grid Hum remains, alone, and holds. The Ember plays once, slow,
unaccompanied — then stops mid-phrase. The city goes quiet again.

**It must resolve fast.** The player wants to restart, and a ten-second lament is a wall between
them and the next run. Five seconds, silence, hub.

### 6.9 Results — `MUS_Results`
**0:45 loop, 80 BPM.** Reflective. The Ember harmonised for the first time — two voices, not one.
The run is over; there is something to look back on.

### 6.10 Credits — `MUS_Credits`
**2:30–3:00.** The thesis, stated plainly: **the same five notes passed between koto, guzheng,
mbira, tagelharpa, ney and choir**, eight bars each, building until all of them play together over
one drone.

The city's music restored, in everyone's voice at once. It is also the best marketing asset the
audio will produce, and it should be cut as a standalone track.

### 6.11 Ambience — `AMB_<Sector>`
**Looping, no tempo.** Grid hum, data-wind, corrupted static. Thins audibly as a sector liberates.
Below −30 LUFS; never consciously heard, always missed when gone.

---

## 7. The Soul Voices

Each palette supplies the acoustic layer only. Grid Voice, tempo, form and Ember do not change.

**Standing rule: research, do not imitate.** Real modes, real instruments, played as they are
actually played. The failure mode is a Western melody with an exotic instrument pasted on top, and
every listener from that culture hears it instantly.

### Afrofuturist — *launch*
- **Instruments:** mbira, balafon, talking drum, djembe, shekere, against modular synth
- **Rhythm:** polyrhythm, 6/8 against 4/4 — cross-rhythm, not syncopation
- **Voice:** call-and-response, group answer
- **Character:** the machine is being *played*, not obeyed

### Japanese
- **Instruments:** koto, shakuhachi, shamisen, taiko, kane
- **Modes:** *in-sen*, *hirajoshi* — both sit inside the pentatonic frame directly
- **Ornament:** shakuhachi breath noise, koto pitch-bend, and *ma* — deliberate silence used as
  rhythm, which fits a game about stolen silence better than any other palette
- **Character:** restraint, then sudden violence

### Chinese
- **Instruments:** guzheng, erhu, dizi, pipa, bianzhong
- **Modes:** *gong* and *yu*
- **Ornament:** heavy erhu portamento, guzheng tremolo and glissando
- **Character:** flowing and lyrical over a rigid machine pulse — the sharpest contrast of any palette

### Persian
- **Instruments:** santur, tar, setar, ney, tombak, daf
- **Modes:** *dastgah* — Shur or Nava. **Not pentatonic.** This palette keeps its own scale and
  quotes the Ember by contour (§3): up a fourth, down a step, up a fifth, home — in Shur, with its
  own *koron*. Same melody, different notes.
- **⚠ Decide before recording:** the quarter-flat will clash with the fixed-pitch Grid Voice. Either
  detune the Grid drone to the dastgah for this palette, or confine microtones to ornament. Retuning
  afterwards means re-recording everything.
- **Character:** ornate, cyclical, hypnotic

### Norse
- **Instruments:** tagelharpa, lyre, frame drum, bukkehorn, kulning
- **Modes:** Dorian and Aeolian, drone-heavy
- **Ornament:** overtone singing, bowed drone, deliberate roughness
- **Character:** cold, wide, ancient — the least electronic palette, and the largest contrast

### Gregorian
- **Instruments:** male voice, near-unaccompanied; optional organ pedal
- **Modes:** Dorian, Phrygian; organum at the fourth and fifth. **Heptatonic, not pentatonic** — the
  Ember is quoted by contour across seven degrees (§3), not squeezed into five.
- **Character:** the strangest against a machine pulse and possibly the most striking.
- **Handle tempo deliberately:** chant has none and the arena needs one. Let the chant float free
  over a strict percussion bed rather than forcing it onto the grid — the tension between them is
  the point, and it is the Two Voices made literal.

---

## 8. Mix rules

Engineering constraints, not taste. The game is worse if these are broken.

### The damage cue must always cut through

`DamageTaken` is the most important sound in the game — two of the five validation-gate metrics
depend on a player knowing what hurt them.

- **Carve 1–4 kHz in every music stem.** A permanent 2–3 dB dip, not a duck.
- Music ducks 6 dB for 400 ms on `DamageTaken` and `PlayerDied`.
- Nothing in the music may share that cue's band or transient shape.

### Explosions own the low-mid transient

A chain detonation is the loudest, least predictable event in the game.

- **No dense kick in 60–200 Hz.** Use sub below 60 or mid percussion above 200.
- Percussion should be *timbral* rather than *impactful* — shakers, frame drums, hands, not
  compressed kicks.
- **The arena bed should sound slightly thin alone.** That is correct. The bombs fill it.

### Phone speakers are the target

The first audience is Asia, largely mobile. A phone speaker reproduces almost nothing below 300 Hz.

- **Mix mid-forward.** Weight that lives in sub bass is silent to most of the audience.
- **Check on a phone speaker as the primary reference, not the final one.**
- Sub content is a bonus for headphone users, never load-bearing.

### The game must be playable muted

Many will play in public with sound off.

**No information may be audio-only.** Every cue needs a visual counterpart. Music's job is to make a
player *want* the sound on — never to punish them for not having it.

### Loudness

| | Target |
|---|---|
| Master | −16 LUFS integrated, −1 dBTP |
| Music bed | −20 LUFS |
| SFX peaks | −6 dBFS |
| Ambience | −30 LUFS |

---

## 9. Sound effects

Currently generated placeholders. They already implement the three things that matter most: random
pitch per instance, voice limiting, and blast audio bound to the **detonation** rather than to each
blast tile.

> *"Each blast is a promise shouted into the dark."*
>
> Take it literally. **Every explosion carries a human component** — a vocal transient buried in the
> noise, felt more than heard. Not a shout, not a grunt. Breath and body inside the blast, so the
> Blaze Code never sounds like ordnance. It sounds like someone.

| Cue | Character | Note |
|---|---|---|
| Bomb placed | Soft, low, mechanical-organic | Placed hundreds of times a run — must never fatigue |
| Fuse | Rising tick, accelerating | The only sound carrying a countdown |
| Detonation | Body-heavy, short tail, **human transient inside** | Sub plus mid crack; 1–4 kHz left clear |
| Blast tile | *(silent)* | Bound to the detonation. A hundred tiles is a hundred voices |
| Block destroyed | Crisp granular shatter | Freed colour — it should sound *good*, it is a reward |
| Sentinel purified | **One clean Ember note**, unquantised | §4. A release, not a splat — queued at ~130 ms so a chain becomes a run, not a chord |
| **Damage taken** | **Unmistakable, unique, unpleasant** | Loudest cue in the game. See §8 |
| Death | Long, falling | Hands off to `MUS_Death` |
| Dash | Air, cloth, doppler | Should feel like effort |
| Shot | Tight, bright, short | Carries a trail visually; the sound is a point |
| Item taken | Rising, warm | The only unambiguously happy sound |

**Chain pitch escalation** — each successive detonation in a chain a semitone higher, resetting when
the chain ends. The cheapest way to make a big chain feel earned, still unimplemented, and the
highest-value SFX work outstanding.

---

## 10. Technical constraints

**The WebGL build is 10 MB today.** Audio can double it trivially, and load time is the single
biggest lever on whether a browser tester ever reaches the game.

| | Budget |
|---|---|
| Music, per cue | ≤ 900 KB compressed |
| Total music, launch | ≤ 6 MB |
| SFX, each | ≤ 40 KB |
| Total SFX | ≤ 1.5 MB |

- **Vorbis** for music at quality 0.4–0.5; **ADPCM or PCM** for short SFX, where decode cost beats size
- **Mono SFX.** Playback is flat by design; stereo doubles size for nothing
- **Stems must loop seamlessly and share a sample count** — vertical layering collapses if they drift
- Music streams; SFX load into memory
- Consider shipping WebGL with fewer stems than native. Load time is a retention metric

---

## 11. Generative pipeline

SUNO plus editing works, with two disciplines.

**Generate stems, not tracks.** A finished song cannot be layered by arena number. Ask for the drone
alone, the percussion alone, the lead alone — same tempo, same key — then assemble.

**Fix tempo and key in every prompt of a palette.** 100 BPM, D minor pentatonic, stated every time.
Otherwise the stems will not sit together and no editing will rescue them.

### Prompt seeds

Swap the palette; keep everything after it identical.

> **Arena, Japanese** — *"Instrumental game music, 100 BPM, D minor pentatonic, in-sen mode. Koto and
> shakuhachi over a cold analogue synth drone and sparse quantised frame-drum pulse. Modal, no chord
> resolution, hypnotic and driving. Warm acoustic instruments against cold machine electronics.
> Leave the midrange open. Loopable. No vocals."*
>
> **Arena, Persian** *(contour palette — do not ask for pentatonic)* — *"Instrumental game music,
> 100 BPM, Persian dastgah Shur. Santur and ney with tombak and daf, over a cold synth drone tuned to
> the mode. Ornate, cyclical, hypnotic. Modal, no chord resolution. Warm acoustic against cold
> electronics. Loopable. No vocals."*

> **Arena, Afrofuturist** — *"Instrumental game music, 100 BPM, D minor pentatonic. Mbira and balafon
> with polyrhythmic hand percussion, 6/8 against 4/4, over a cold analogue synth drone. Modal, no
> chord resolution. Warm acoustic against cold electronics. Loopable. No vocals."*

> **Grid stem (all sectors)** — *"Cold analogue synth drone in D, 100 BPM. Sub bass pedal tone,
> brittle FM bell fragments, quantised glitch percussion, hollow midrange. Oppressive, machine-exact,
> no melody, no warmth. Loopable."*
>
> **Grid stem, deep sectors** — same, plus *"denser, harder quantised pulse, more aggressive glitch,
> pressing and claustrophobic."* Used for the arena 5+ re-balance in §5; the Soul stems are filtered
> rather than re-recorded.

> **Hub** — *"Slow instrumental, 72 BPM, D minor pentatonic. Solo [instrument] over a warm drone.
> Almost no percussion. Contemplative, safe, unhurried. Loopable. No vocals."*

> **Death** — *"Five seconds. Solo [instrument], D minor pentatonic, unaccompanied, slow and falling,
> over a single cold synth drone. Stops mid-phrase. No percussion. Unresolved."*

> **Credits** — *"Instrumental, 96 BPM, D minor pentatonic over a sustained drone. The same five-note
> melody passed between koto, guzheng, mbira, tagelharpa, ney and male choir in turn, eight bars
> each, building until all play together. Triumphant but restrained."*

**Always edit afterwards.** Generated audio drifts in tempo and rarely loops cleanly. The loop point
and stem alignment are hand work, and they are the difference between a game score and a song playing
over a game.

---

## 12. Order of work

1. **The Ember.** Five notes. Nothing else can begin.
2. **The Grid Voice.** One stem set, used by every sector forever. Get it right once.
3. **One arena, one Soul Voice, all six stems — written as the arena-one mix.** Prove the
   re-balance in engine before writing more; the deep mixes are the same recording, not new work.
4. **Purification notes, the queue, and `MUS_Cleared`.** They close the loop and make §4 real. Build
   the queue with the notes — a chain kill is the common case, not the edge case.
5. **Death and Awakening.** Short, high-impact.
6. **Hub and Choice.** Long-listen cues; grating ones do real damage.
7. **Remaining Soul Voices**, one per sector as sectors ship.
8. **Credits**, last — it needs every palette to exist.

**Do not commission music before the validation gate reports.** If the loop changes shape, arena
length and escalation change with it, and adaptive music written against the old shape is wasted.
