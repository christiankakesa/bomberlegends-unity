# **GAME DESIGN DOCUMENT (GDD)**

# **Project Title: Bomber Legends**

**Version:** 2.0 — concept revision

**Date:** 23 March 2026 (v1.0) — translated 5 August 2026 · perspective revised 6 August 2026 · **concept revised 6 August 2026 (v2.0)**

**Author:** Christian Kakesa

**Original:** French, archived unmodified at [`GDD.fr.md`](GDD.fr.md). English is the working language of the project; the French copy is a historical record and is not maintained.

**Genre:** Action / Puzzle / Tactical Strategy (modernised "Bomberman")

**Target Platform:** Mobile-first.

1. Android — primary target from Milestone 0
2. WebGL — remote playtests and itch.io
3. iOS — soft launch
4. Desktop Windows / Linux / Mac — later port, Steam after that

Consoles (PS5, Xbox, Switch 2) are out of scope until the prototype is validated.

**Business Model:** Free-to-Play (F2P)

**Engine:** Unity 6.3 LTS or newer. Decided, not revisited.

**Technical Stack:**

* **Client:** Unity 6.3 LTS, C#
* **Backend:** Nakama, PostgreSQL/CockroachDB, Go — **deferred to Milestone 9**; no backend development before the prototype is validated

**Technical decisions:** see `01-ANALYSIS.md` §13 and `README.md`.

---

## **1. EXECUTIVE SUMMARY (PITCH)**

An action hybrid that merges the explosive grid destruction of Bomberman, the precision skillshot
controls of a MOBA, and the item-synergy buildcrafting of a roguelite.

> **"Unleash explosive item synergies and craft devastating builds in an action arena powered by
> MOBA-precision controls and Bomberman-style arena destruction."**

**Marketing hooks**

* *"Aim. Bomb. Adapt. Master the precision of a MOBA inside an explosive roguelite arena."*
* *"3 Skills. 4 Passives. Endless Synergies. Forge your ultimate bomb kit and break the arena."*
* *"Bomberman evolved — now with skillshots, item synergies, and zero limits."*

### The differentiator

Items **change how skills behave**, they do not add percentages. A passive that freezes on impact
turns a bomb into crowd control; one that chains detonations turns a single charge into a cascade.
This is the Isaac / Risk of Rain engine, and it is the only one of the three pillars that generates
replay value by itself.

### Structure

Single-player roguelite runs: clear a stage, choose an item, go again; a run ends on death.
Multiplayer remains architecturally possible but is not the product.

---

## **2. GAMEPLAY PILLARS**

1. **Reinvented Nostalgia (Bomberman base).** Accessible grid destruction, maze navigation and chain
   reactions, modernised with real-time combat.
2. **Skill Cap and Precision (MOBA controls).** 360° continuous movement, directional skillshots,
   dashes and real-time dodging.
3. **Synergy Buildcrafting (roguelite loop).** High-impact items that alter how active skills behave
   rather than nudging numbers.
4. **Neon Afro-Futurism.** The visual identity carried over from v1.0 and unchanged — the project's
   strongest marketing asset.

> *Superseded from v1.0:* "Race Against the Clock" is demoted from a pillar. Time pressure works as
> an anti-camping measure, not as the source of difficulty, and it conflicts with the deliberate pace
> that buildcrafting needs.

---

## **2b. LOADOUT STRUCTURE**

Capped deliberately, to protect combat readability and the mobile HUD:

* **3 active skills maximum**, unlocked during a run — typically one mobility, one skillshot, one
  heavy area effect.
* **4 passive items maximum**, upgradable during a run, which modify active skills and bomb
  behaviour rather than granting flat stats.

---

## **2c. HYBRID SPATIAL MODEL** *(the core technical idea)*

Two layers occupy the same arena:

| Layer | Governs | Space |
|---|---|---|
| **MOBA layer** | Character movement, skill aiming, projectiles, dodging | **Continuous 360°** |
| **Bomberman layer** | Bomb placement, destructible blocks, blast propagation | **Anchored grid** |

Blasts propagate strictly along orthogonal grid axes, so danger zones stay tile-shaped and readable
however freely the player moves.

**Destructible blocks also block skillshots.** Without that, the grid would become decorative the
moment movement went continuous — it would only govern where bombs sit. Blocking line of fire makes
the maze matter to the MOBA layer too, so the two layers reinforce each other instead of coexisting.

---

## **3. UNIVERSE AND AESTHETIC**

### **3.1. Setting (Lore)**

In the megacity of **Ébène-Prime**, the **Sombra-Corps** corporation controls all data flow. The player is a member of the **Génération Néon** ("Neon Generation"), rebels who physically hack the city's servers to free information. Each level is a *sector* (a district) that the player must cross to reach a main data terminal. The obstacles are physical data blocks and **Sentinelles** (Sentinels — the enemies).

### **3.2. Art Direction (based on the reference image)**

* **Style:** **Low-poly 3D**, viewed three-quarter top-down over a square grid (revised 2026-08-06 to
  v2.0; superseding both the isometric pixel art of v1.0 and the 2D sprite direction of v1.2).
  360° movement makes 3D the cheaper pipeline: a model rotates, whereas a sprite character needs a
  frame authored per facing.
* **Colour Palette:** Deep night (blue/purple) contrasted with vibrant neon (cyan, magenta, golden orange).
* **Architecture:** A mix of afro-futurism and cyberpunk. Futuristic buildings with geometric tribal motifs, technological hieroglyphs, and cybernetic tropical vegetation.
* **Protagonist:** Silhouette of a man with dreadlocks, wearing a luminous tech suit.
* **Objects:** Bombs are glowing plasma orbs. Destructible blocks are complex technological cubes.

---

## **4. CORE GAMEPLAY LOOP**

1. **Level Start:** The player spawns on the isometric grid. Objectives and the timer begin.
2. **Exploration & Destruction:** The player moves their character across the grid, placing bombs to destroy obstacles (orange/purple cubes) and reveal passages or rewards.
3. **Threat Management:** Avoid your own explosions, enemies, and traps. Use skills.
4. **Reach the Exit:** Once the path is clear and the data objectives are met, the player must reach the **Porte de Données** (Data Gate) before time runs out.
5. **Meta-Game:** Spend collected resources to upgrade the character and skills in the central hub.

---

## **5. GAME MECHANICS**

### **5.1. Movement and Grid**

* **Perspective:** Three-quarter top-down over a **square** grid. Columns run across the screen, rows recede up it with a slight foreshortening, and blocks stand up off the floor.
* **Movement Grid:** Movement is visually smooth, but the character automatically aligns to an invisible tile grid for precise bomb placement and explosion collision ("soft-grid" style).

### **5.2. Bomb System (Core Action)**

* **Placement:** The player presses the "BOMB" button to place a bomb on their current tile.
* **Fuse Delay:** A fixed delay (e.g. 3 seconds) before detonation.
* **Range:** By default, the explosion propagates in a cross shape (N, S, E, W) across 2 tiles. Can be upgraded.
* **Physics:** Bombs cannot be walked through. They block both enemies and the player.

### **5.3. In-Match HUD and Economy (Top of Screen)**

* **Score:** Points earned by destroying blocks and enemies, and by finishing quickly.
* **Time:** Level countdown. If the timer runs out (e.g. 02:34 → 00:00), the player loses a life.
* **Lives:** Number of remaining attempts (here, 3 hearts). Losing a life (explosion, enemy, or timeout) resets the character to the starting point of the current level, with the timer reset.

---

## **6. SKILL SYSTEM (BOTTOM HUD)**

This is the main strategic element of the HUD in the reference image.

### **6.1. PASSIVE Skills (Left Slots)**

These skills activate situationally or have a constant effect, with no direct usage cost, but may have activation gauges (the coloured bars next to the icon).

* **1. SPEED BOOST (Winged Boot):**
  * **Effect:** Increases the character's base movement speed by 25%.
  * **Token Gauge:** The adjacent gauge fills as the player walks. Once full, the player can activate a temporary 3-second *Sprint* (for example, by double-tapping the joystick).
  * **Upgrade:** Increases speed, sprint duration, or charge rate.
* **2. SHIELD:**
  * **Effect:** Provides automatic protection against ONE explosion (own bomb or enemy) or ONE enemy impact.
  * **Token Gauge:** Recharges slowly after use. The gauge shows recharge progress.
  * **Upgrade:** Reduces recharge time, adds a second shield, or adds an area-of-effect (AoE) explosion when the shield breaks.

### **6.2. ACTIVE Skills (Right Slots)**

These skills have a powerful instant effect and are governed by a visible *cooldown*.

* **3. BOMB (Red Orb, 5.0 s Cooldown):**
  * **Effect:** This is the main bomb-placement button. Pressing it places a standard bomb.
  * **Cooldown:** 5 seconds between each bomb placement.
  * **Upgrade Handling:** The player starts with 1 simultaneous bomb. Upgrades unlock more simultaneous bomb slots, but the base 5-second cooldown applies to *each* slot. (E.g. with 2 bomb slots, the player can place one bomb, starting a 5 s cooldown on that slot, then place another, starting a 5 s cooldown on the second slot.)
* **4. SPECIAL (Purple Orb, 5.0 s Cooldown):**
  * **Effect:** A powerful action unrelated to the standard bomb.
  * **Skill Type:** The player chooses *one* Special skill from several before the level (e.g. *TELEPORT* a short distance through a block, *LIGHTNING CHAIN* to instantly destroy 3 blocks in a straight line, *BOMB CONTROLLER* to manually detonate the last bomb placed). The reference image shows a central energy symbol suggesting a burst of pure energy around the character.
  * **Cooldown:** Fixed 5-second recharge.

---

## **7. CONTROLS (MOBILE)**

* **Orientation:** Landscape.
* **Movement (Left):** A transparent virtual joystick in the bottom-left corner for precise analogue/angular control (grid-compatible).
* **Actions (Right):** The 4 skill buttons (2 clearly active, 2 informational passives) arranged ergonomically for the right thumb.

---

## **8. GAME EVOLUTION (TACTICAL LEVEL DESIGN)**

The reference image shows a basic level. Evolution comes through:

### **8.1. Obstacle Types (Blocks)**

* **Basic Blocks (Orange Cubes):** Destructible by one bomb. Reveal data fragments (Data Coins).
* **Reinforced Blocks (Purple Cubes):** Require two explosions to destroy.
* **Indestructible Walls (Buildings and Neon Platforms):** The base structure of the maze.
* **Moving Blocks:** Platforms that travel along grid rails.

### **8.2. Enemies (Sombra-Corps Sentinels)**

* **"Basic Patroller"** *(Patrouilleur Basic)*: Follows a fixed path, lethal on contact, not intelligent.
* **"Bomber-Drone"** *(Bombardier-Drone)*: Fires energy orbs at range.
* **"Neon Hunter"** *(Chasseur-Néon)*: Faster; targets the player when in line of sight.

### **8.3. Level Objectives**

* **"Collect 10 Nodes":** Destroy blocks to find hidden data nodes.
* **"Eliminate all Sentinels":** Clear the level.
* **"Survival":** Survive 3 minutes against infinite waves of enemies.

---

## **9. META-GAME AND PROGRESSION (THE HUB)**

After each level, the player returns to the **Ébène-Prime Hub**.

### **9.1. Economy**

* **Data Coins:** Collected in missions (by destroying blocks and enemies). Base currency.
* **Cœurs Néon** *(Neon Hearts — premium)*: Purchased with real money or earned during events.

### **9.2. Upgrades (The Tech Tree)**

The player spends Data Coins to upgrade their skills:

* **Bomb Tech:** Explosion range (+1 tile), damage (+), bomb slots (+).
* **Passive Tech:** Reduce Shield cooldown, increase max speed, etc.
* **Active Tech:** Unlock new "Special" skills.

### **9.3. Customisation (Skins)**

The player can buy skins for their Speed-Runner with Cœurs Néon:

* Different cyberpunk outfits.
* Neon trail colours.
* Bomb visual effects.

---

## **10. TECHNICAL RISKS AND CHALLENGES**

* **Isometric Collision:** Ensuring the player never gets stuck in isometric corners, and that bomb placement on tiles is perfectly aligned visually.
* **Cooldown Readability:** Keeping the 5.0 s timers and passive gauges clear and readable even during intense action.
* **Mobile Performance:** Detailed pixel art with many neon lighting effects can be resource-hungry on low-end mobile devices.

---

> ### Concept revision, 6 August 2026 (v2.0)
> Three decisions taken with the concept revision, each recorded because each is expensive to undo:
> 1. **Low-poly 3D**, not 2D sprites. Driven by 360° movement and the reference art.
> 2. **PC-first**, mobile as a later port. Mouse aiming suits skillshots, and a seven-slot loadout is
>    comfortable with a mouse and cramped under a thumb. Supersedes the Android-first decision of
>    2026-08-05.
> 3. **Own bombs hit hard, enemies chip.** Combat is HP-based, but the player's own blast removes a
>    large share of maximum health, so self-trapping stays frightening. A dash plus even HP damage
>    would have quietly deleted the tension the Bomberman layer exists to create.
>
> ### Perspective change, 6 August 2026
> v1.0 specified a **true isometric** view. That is superseded by a **three-quarter top-down view of a
> square grid**, for three reasons:
> 1. **Controls.** In an isometric view the four grid directions land on screen diagonals, so a thumb
>    pushed "up" is genuinely ambiguous. This was the highest-rated feel risk in the project
>    (`01-ANALYSIS.md` G6). On a square grid, up is north and right is east — the ambiguity stops
>    existing rather than being mitigated.
> 2. **Screen usage.** A diamond board wastes all four corners of a landscape phone; a square grid
>    fills the frame, so tiles read larger on the same device.
> 3. **Art cost.** Square tiles at three-quarter view are materially cheaper to author than isometric
>    ones, which were the dominant cost in the project (`01-ANALYSIS.md` S3).
>
> **The afro-futurist neon art direction is unchanged** and remains the project's primary
> differentiator. Only the camera angle and the tile shape changed. The simulation was unaffected:
> the grid never knew how it was drawn, so this cost one view class and its tests.

> **Engineering note.** This document records design *intent*. Several mechanics described here are
> challenged on gameplay grounds in `01-ANALYSIS.md` §9 and §12 — in particular the 5 s bomb cooldown (§6.2.3),
> the Shield passive (§6.1.2), the timer reset on death (§5.3), and the absence of any chain-detonation rule.
> The scope actually being built is defined in `02-PROTOTYPE-SCOPE.md`, not here.
