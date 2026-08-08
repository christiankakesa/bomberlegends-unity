# Game feel — the juice plan

**Status 2026-08-08 · nothing here is built.** `IAudioService` is designed and only
`SilentAudioService` implements it; `PlaySfx` has no callers anywhere in the project. The game is
silent, and its only visual feedback is the greybox effects listed in
[08-IMPLEMENTED.md](08-IMPLEMENTED.md).

This document exists so that when it *is* built, it is built once and by a designer rather than
piecemeal and by an engineer.

---

## 1. The rule everything else follows

> **Feel is a consumer of the simulation, never a part of it.**

Every moment worth reacting to is already announced as a `SimEvent`. The simulation does not know a
sound or an effect exists, and must not: it has to stay deterministic, engine-free, and runnable on a
server with no view at all.

Two consequences that are easy to get wrong:

- **Hit-stop cannot be a visual trick here.** Freezing a fixed-tick authoritative simulation on frame
  time breaks determinism and replay validation. If hit-stop is wanted it must be a *simulation rule*
  — a real pause counted in ticks — decided as a gameplay change, not added by the effects layer.
- **Screen shake, squash and stretch, and camera kick never touch simulation state.** They are read
  from events and applied to the view. A shake that moved the player would be a physics bug that only
  appears on impact.

## 2. The moments

Every one of these already fires a `SimEvent`, so none of them needs new simulation work.

| Moment | Event | Sound | Visual |
|---|---|---|---|
| Bomb placed | `BombPlaced` | soft thunk | brief scale pop, fuse spark |
| Fuse burning | *(state)* | ticking, accelerating near the end | pulse rate rising with the fuse |
| Bomb detonated | `BombDetonated` | body-heavy boom | flash, camera kick |
| Blast tile appears | `BlastSpawned` | **limited** — one per detonation, not per tile | fire bloom, brief light |
| Block destroyed | `BlockDestroyed` | crunchy pop | debris burst, dust puff |
| Enemy killed | `EnemyKilled` | distinct from block destruction | dissolve, brief slow ring |
| **Player damaged** | `DamageTaken` | unmissable, unique | screen edge flash, strong shake |
| Player died | `PlayerDied` | one decisive hit | desaturate, slow-motion camera pull |
| Dash | `DashStarted` | whoosh | trail, squash on launch |
| Shot fired | `ProjectileFired` | crisp, light | muzzle flare |
| Shot connects | `ProjectileEnded` | pitched by damage dealt | impact spark |
| Item taken | `ItemAcquired` | rising, rewarding | card flourish, brief glow on the player |
| Arena cleared | `ArenaCleared` | resolving chord | slow zoom, calm |
| Arena border hit | *(view only)* | none | **squash and stretch, speed preserved** |
| UI focus moved | *(view only)* | soft tick | highlight snap |

`DamageTaken` and `PlayerDied` are the two that must never be missed. Everything else can be
subtle; being hurt cannot be.

## 3. The tricks that actually matter

Ordered by how much they buy for the effort.

1. **Random pitch offset per instance.** Already a field on `SfxDefinition`. A run places hundreds of
   bombs; identical playback becomes fatiguing within one session, and this removes it for free.
2. **Several clip variants per effect.** Also already a field. Two or three variants plus pitch
   variation is effectively unlimited perceived variety.
3. **Ascending pitch through a chain.** Each successive detonation in one chain plays a semitone
   higher than the last, resetting when the chain ends. This is the single cheapest way to make a
   big chain feel *earned* rather than noisy — the ear hears an escalation the eye already saw.
4. **Voice limiting.** `MaxConcurrent` and `MinRetriggerInterval` exist for exactly this: a chain can
   set a hundred tiles alight in one tick, and one sound per *tile* would clip, distort and spike the
   CPU. Blast audio must be per detonation, not per tile.
5. **Camera kick scaled by blast size**, not a fixed shake. A one-bomb pop and a nine-bomb chain
   should not feel the same.
6. **Squash and stretch on the player.** Launch into a dash, land on a wall, hit the arena border.
   Cheap, entirely view-layer, and it does more for perceived responsiveness than any particle.
7. **Debris that inherits blast direction.** Already have a debris flash; making it fly outward from
   the detonation costs nothing and reads far better.

**Readability outranks all of it.** A blast tile is lethal, and no effect may obscure whether a tile
is on fire. Every effect needs an intensity cap and an off switch, both for accessibility and because
the first thing a playtest will show is which effect is hiding the thing that killed them.

## 4. Designer ownership — the architectural requirement

> **A designer must be able to add, retune, or remove feedback without an engineer and without a
> recompile.** Requested explicitly on 2026-08-08.

That is not a naming convention, it is a structural constraint, and meeting it means one thing:

### A feedback table, authored as an asset

A `FeedbackTable` ScriptableObject mapping `SimEventType` → an `SfxDefinition`, a VFX prefab or
pooled effect id, a camera-shake profile, and an intensity scale. The view layer walks the event
stream and looks each event up. Nothing in code names a particular sound or effect.

What this buys:

- Binding a sound to `ItemAcquired` becomes a row in an asset, not an edit to
  `MatchViewSynchroniser`.
- A new event type gets feedback without touching the view at all.
- Effects can be retuned while the game is running, which is the only practical way to tune feel.
- The whole feedback set is visible in one place rather than scattered through a switch statement.

The alternative — a `case` per event inside the synchroniser, which is how it works today for the
three effects that exist — does not scale past about a dozen events and quietly makes every feel
change an engineering ticket.

### The same applies to UI

Colours, fonts, spacing and copy currently live in code, because the interface is greybox and built
at run time. That is the correct trade *now* and the wrong one the moment the interface is real:
those belong in a `UiTheme` asset and a string table before any art or copy pass begins.

## 5. Suggested order

Audio first, and not for its own sake: **it is the fastest way to find out whether the moments are
even legible.** A player who cannot hear the difference between their own bomb and an enemy dying is
being told something about the design, not about the mixer.

1. `AudioService` implementing the existing contract — pooled sources, voice limiting, bus volumes.
2. `FeedbackTable` and the lookup in the view layer. Do this *before* wiring individual effects, or
   the switch statement gets written and then has to be undone.
3. Bomb, blast, block, damage, death. The five moments that carry the loop.
4. Camera kick and squash-and-stretch.
5. Skills, items, arena transitions.
6. Chain pitch escalation, debris direction, and the rest of §3.

Deferred backlog items `T-020` (audio) and `T-021` (screen shake) are absorbed by this plan.
