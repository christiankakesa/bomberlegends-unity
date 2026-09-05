# Bomber Legends TODO

- [x] ~~**Movement joystick: the touch area must be the whole bottom-left quarter.**~~
  **Done 2026-09-05**, device-verified on the S21 Ultra. Found in play: sometimes the player did
  not move at all, and it was never the input feel — the thumb had landed beside the stick rather
  than on it. The stick was one 300-unit object that both listened for presses and was the circle
  being drawn, so recentring under the thumb — which it already did, and which its own doc comment
  called the fix for "a thumb reaching for a fixed circle it cannot see" — could only ever run for
  a press that had already hit the circle. Those are now two objects: an invisible listening area
  covering the quarter, and the circle inside it that moves to meet the thumb.

- [ ] Sound with Minimax Music 3 (local AI)?
- [ ] Theme & Juicy
  * [ ] Colors...
  * [ ] Haptic...
    - Responsiveness
      * [ ] Input response time: fidelity of effect, prioritize snappiness.
      * [ ] Input latency: prioritize player feeling
      * [ ] Animation blending: high fidelity animation, more responsive especially with attacks, make sure the attack feel unbelievable
      * [ ] Locomotion response
    - Animation
      * [ ] Use SpriteLoop for 2D animation? Does it have MCP connector? Or SKILL.md for SpriteLoop?
      * [ ] Slow in / Slow out
      * [ ] Exaggeration & strong posing
      * [ ] Ennemy animation
        - [ ] Make the animation as readable as possible against player animation that is as satisfying as possible
        - Make the player the ability to read and learn ennemy atack patterns
        - Telegraphing
          * [ ] Clear poses
          * [ ] Strong anticipation
          * [ ] Sound design
          * [ ] Visual effects
          * [ ] Proper hierarchy: weak, normal strong
          * [ ] Make the combat fair: the player can realize that he has the information needed to avoid the mistake/damage/lose, ...
          * [ ] Recovery time in the animation
        - Hit reactions
          * [ ] Instant reaction
          * [ ] Exaggerated poses
          * [ ] Hit reactions sell strength
    - Because the camera is relatively static, players may expect more speed and snappiness.
- [ ] Maturity path
  * [ ] Proven mechanics
  * [ ] Level design
  * [ ] Longevity
- [ ] Skills
  * [ ] Skill duration must be fair regarding the type of skill ( slow but more damage, quick but less damage, ...)
  * [ ] Cheat for player
    - [ ] Aim assist (player must need to configure it to enjoy the game)
    - [ ] Magnetism
    - [ ] Hit impulse
    - [ ] Input buffering
    - [ ] Generous hitboxes, multiple hitboxes
  * [ ] Juice it up
    - [ ] Hit flash
    - [ ] Hit reaction
    - [ ] Hit impulse
    - [ ] Hit stop / Time dilation: cyrve to ease in / ease out (transition)
    - [ ] Hit VFX
    - [ ] Screen shake
    - [ ] Controller feedback
    - [ ] Beefy audio
    - [ ] Procedural enemy shake: can reinforce the impact of an attack, make it with some degree of personalization
    - [ ] 

## Idea animation and characters

```txt
Ce qui a réellement shippé — découvert en auditant les commits — c'est la Voie B, celle qu'on avait qualifiée de secours :
1. `make_poses_batch.py` génère un dict de poses par personnage
2. Z-Image Turbo (6B, FP8) render chaque frame en local via ComfyUI
3. Les spritesheets s'assemblent et atterrissent dans `SpriteAnimator`
4. Le contrat gameplay→rendu (`anim_name` / `anim_frame` / `anim_len`) reste déterministe — aucune frame ne dérive

Aucun humain dans la boucle de rendu. Le moteur Godot consomme des spritesheets comme si elles venaient d'un graphiste.

Le vrai problème non résolu : ComfyUI se duplique. Quatre fois en deux semaines — un process enfant du premier, RAM saturée, queue bloquée. Fix en dur : killer les deux PID `main.py` et relancer proprement. Pas élégant. Ça tient.

La prochaine contrainte est ailleurs : un jeu de combat, est-ce pour un enfant de 8 ans ou pour un amateur de grimdark ? L'arbitrage est câblé jusque dans le code — palette, portraits, intensité visuelle. On ne peut pas faire les deux.
```
