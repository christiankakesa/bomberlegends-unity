# Art direction — what it means for the build

**Source: [GDD.md](GDD.md) §3, updated 2026-08-09.** That section is the authority on how the game
should look and what its world is. This one is the engineering reading of it: what the words commit
us to, what they cost, and what they leave undefined.

Nothing here is built. The game is greybox: primitives, flat URP/Lit materials, generated sounds.

---

## 1. Naming

The lore gives canonical names to things the code calls generically. Worth adopting before there is
content, because renaming a concept after assets, save fields and analytics events carry the old name
is the kind of change nobody ever finishes.

| In the build today | Lore name | Note |
|---|---|---|
| Bomb | **Soul Orb** | Personalised per hero; leaves afterimages in their signature colour |
| Destructible block | **Corrupted Data Cube** | Shatters into "freed colour" |
| Solid block | Shadow Grid growth | Black crystalline in locked sectors |
| Enemy | **Sombra-Corps Sentinel** | Already used in GDD §8.2 |
| Boss | **Sentinel Lord** | Matches the proposal in [08-IMPLEMENTED.md](08-IMPLEMENTED.md) |
| Arena | **Sector** | Matches the "sector towers" proposal |
| Active skill | **Bomb Art** | See §4 — the definition is not settled |
| Player | **Neon Soul**, of the Génération Néon | |
| Hub | Ébène-Prime Hub | |

### Where a lore name helps and where it costs

Lore wording is adaptable, and it should be. The rule that decides it:

> **Anything the player must parse under pressure uses the plain word. Lore names live where there is
> time to read them** — menus, item descriptions, the hub, the narrative.

A bomb is called a bomb in the HUD. Everyone already knows what a bomb does in a game built on a
grid, and "Soul Orb" costs a beat of comprehension to buy a beat of flavour — a poor trade at the
exact moment someone is deciding whether to place one. The same goes for **Corrupted Data Cube**
against "block" in a tutorial line.

Names that cost nothing, because they name things the player has no prior word for: **Sector**,
**Sentinel**, **Sentinel Lord**, **Blaze Code**, **Fractured Heart**. These are worth keeping
everywhere.

This is not an argument for less lore. It is an argument for putting it where it is read rather than
where it is skimmed.

**The simulation should keep its generic names.** `BombState`, `EnemyState`, `TileType.Destructible`
describe rules, not fiction, and the layer is deliberately free of anything a designer might rename.
Lore names belong in the view, the interface and the content assets.

---

## 2. What the art direction commits us to

### It agrees with what is built, which is the good news

*"Low-poly 3D … three-quarter top-down view over a square grid."* That is exactly what M2b produced
and what the follower camera does now. **Risk G6 (isometric controls) is retired for good** — the
art direction and the implementation describe the same camera.

Low-poly also suits the mobile target, and swapping primitives for authored meshes touches only
`PlaceholderMeshes` and the view. The simulation cannot tell the difference, which is decision D1
paying off for the fourth time.

### Cel-shading walks straight into a trap we have already fallen into

The greybox draws with `Universal Render Pipeline/Lit`. Cel-shaded edges need a custom shader —
Shader Graph or hand-written — and **every renderer in this game is created at run time.**

> Nothing in any scene references those materials, so the build strips their shaders. That is not a
> hypothesis: it happened at M1 and produced a device build where the interface drew perfectly and
> the world was invisible. `ShaderInclusionTool` exists because of it.
>
> **Any new shader must be added to the always-included list, and the cost measured.** Adding the 2D
> sprite shaders once took the APK from 84 MB to 143 MB, because every variant compiles.

### Neon and bloom are a mobile performance decision, not a look

The palette rests on emissive glow — *"hyper-saturated neon"*, *"luminous trails"*, *"the entire body
erupts into a brilliant energy aura"*. Bloom is among the most expensive post-process effects on
mobile GPUs, and it is being asked to carry the identity of the game.

That needs a decision rather than a default: a quality tier where bloom is reduced or replaced on
mobile, and a measurement on the Galaxy S21 before any of it is authored. **An art direction that
only runs at 30 fps on the primary platform is a scope problem discovered late.**

### The palette shift is a system, not a painting

*"Locked sectors are washed in desaturated grays … liberated areas erupt with hyper-saturated neon."*

If a sector changes appearance **as it is cleared**, that is a runtime, state-driven global colour
change: a colour-grading Volume whose weight is driven by run state. That is engineering with a data
seam, and it belongs in the same family as the `FeedbackTable` — a designer should be able to author
the two grades and the transition without an engineer.

If instead locked and liberated are simply *different sectors*, it is authoring and costs nothing.
**Those are very different jobs and the wording does not settle which is meant.**

### Signature colour per hero is cheap now and expensive later

*"Each hero's Blaze Code emits a signature inner glow … that tints their explosions and leaves
luminous trails."* Per-instance tinting is already the pattern here — `MaterialPropertyBlock`, used
by every pooled view — so this costs a colour field on the character and threading it through the
effects. Retrofitting it after effects are authored against fixed colours is far worse.

### Impact frames are a simulation change, not a visual effect

*"Stylized impact frames … accompany Awakening detonations."* Impact frames are hit-stop, and this
has been recorded once already: **freezing a fixed-tick authoritative simulation on frame time breaks
determinism and replay validation.** If it is wanted — and for this art direction it probably is — it
must be a simulation rule counted in ticks, decided as a gameplay change.

---

## 3. Contradictions the update exposes

The lore and art are v2.0. Several mechanics sections around them are still v1.0.

| Where | Says | Reality |
|---|---|---|
| §4.1 | "spawns on the **isometric** grid" | Contradicts §3.2 and §5.1 **in the same document**. Straight error; corrected. |
| §4.4 | Reach the Data Gate before the timer | No exit and no timer exist. A sector is cleared by killing every Sentinel. |
| §5.3 | Score, countdown, **3 lives** | Health pool of 100, no timer, no lives — the v2.0 lethality decision. |
| §6.1 | Passives are Speed Boost and Shield | Passives are items; nine exist, two slots. |
| §6.2 | Actives are Bomb and Special, 5 s cooldown | Three active slots: Dash and Skillshot built. Bombs use the capacity model; `BombCooldownTicks` is 0. |

These were superseded by [07-CONCEPT-REVISION.md](07-CONCEPT-REVISION.md), which remains the
authority. They are listed because the lore update makes the GDD **internally** inconsistent, which
is worse than being out of date — a reader has no way to tell which half is current.

---

## 4. What the lore introduces and nothing defines

New fiction, no mechanics. Each of these is a design decision wearing a name.

**Awakening.** *"When your resolve peaks, unleash your Awakening — a catastrophic neon detonation
that rewrites the battlefield."* There is no resolve, no meter and no ultimate anywhere in §6. This
is a new mechanic, and an interesting one — but *what fills resolve* is the whole design. Damage
dealt rewards aggression; damage taken rewards recklessness; chain size rewards the Bomberman layer.
They produce three different games.

**Bomb Arts.** *"Your signature Bomb Art (a flashy, unique fighting style)"*, and *"every freed
sector unlocks new Bomb Arts"*. Is a Bomb Art a character, a loadout, a single skill, or a skin? It
is used as identity in one line and as a collectible in another.

**Fractured Hearts.** Harvested by purifying Sentinels. §9.1 lists Data Coins and Cœurs Néon. A third
currency, a rename of the first, or the Awakening resource?

**"Purify" rather than kill.** Cosmetic framing, or does a purified Sentinel behave differently from
a destroyed one?

> None of these blocks the validation gate, and none should be built before it. They are recorded so
> the answers are chosen rather than inherited from whichever line of fiction gets read first.

---

## 5. Suggested order, when art starts

Character work has a skill with this project's budgets already worked out:
`.claude/skills/character-design/`. It overrides a generic production spec whose defaults — 15k
triangles, 2048² textures, PBR maps, subsurface scattering, one unit per metre — were written for a
different kind of game and would produce assets several times over budget and in the wrong style.


Art should not start before the gate. When it does:

1. **Measure bloom on device** with the greybox before authoring anything that depends on it.
2. **Cel-shader, plus its entry in `ShaderInclusionTool`, plus an APK size measurement.** Prove the
   pipeline on one cube before there are a hundred assets riding on it.
3. **Signature colour threaded through the effects**, while there are few of them.
4. Meshes replacing primitives, one at a time, through `PlaceholderMeshes`.
5. The palette-shift system, once §2 settles whether it is one.

Impact frames and Awakening are gameplay, and belong with a milestone rather than an art pass.
