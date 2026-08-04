# Claude Project Instructions

## Project Philosophy

This project is built around one principle:

Gameplay first.

Every implementation should maximize iteration speed while keeping production-quality architecture.

Never sacrifice long-term maintainability for a short-term shortcut.

---

# Engine

Unity 6.3 LTS or newer.

Decided. Not revisited.

# Target Platforms

Mobile-first. Ladder decided 2026-08-05 (see docs/01-ANALYSIS.md, Q2).

1 Android — primary target from Milestone 0

2 WebGL — remote playtest and itch.io channel

3 iOS — soft launch

4 Desktop Windows / Mac / Linux — near-free port, Steam later

Out of scope until the game is validated:

- PS5
- Xbox
- Switch 2

Android is the primary target from the first build, not a later port.

Device performance and touch feel are the two largest risks in this project.

Both must be measured on real hardware every milestone.

Consoles require devkits, certification and a publisher relationship.

None of that starts before the validation gate passes.

---

# Primary Goal

Build a fun prototype first.

Only after gameplay validation should additional systems be added.

Always ask:

"Does this improve the player's experience?"

---

# Development Priorities

Priority order:

1 Gameplay

2 Feel

3 Performance

4 UX

5 Graphics

6 Polish

---

# Preferred Packages

Use whenever appropriate:

- Unity Input System
- Addressables
- TextMeshPro
- Cinemachine
- Universal Render Pipeline
- Unity Localization
- Unity Test Framework

If async is needed:

Use UniTask.

Avoid standard Task unless necessary.

---

# Architecture

Favor feature-based organization.

Example

Gameplay/

    Combat/

    Enemy/

    Weapons/

    Progression/

Each feature owns:

- Controllers
- Models
- ScriptableObjects
- UI
- Events

Avoid giant Managers.

---

# Gameplay Systems

Prefer systems over inheritance.

Example

Player

↓

MovementSystem

↓

CombatSystem

↓

DamageSystem

↓

UpgradeSystem

Each system has a single responsibility.

---

# Coding Rules

Always:

- explicit modifiers
- private serialized fields
- XML docs for reusable APIs
- clean Inspector

Never:

- use regions
- comment obvious code
- abbreviate names unnecessarily
- expose fields publicly

---

# Inspector Rules

Designers should be able to configure systems without modifying code.

Use:

Header

Tooltip

Space

Range

Min

where appropriate.

Inspector usability matters.

---

# Asset Loading

Production assets

↓

Addressables

Editor-only assets

↓

AssetDatabase

Resources folder only if justified.

---

# Audio

Use AudioMixer.

Bus hierarchy

Master

Music

SFX

UI

Voice

Ambience

No AudioSource should directly control volume.

---

# Save System

Gameplay code must never know storage implementation.

Saving should be abstracted.

---

# Performance

Avoid:

- allocations
- LINQ
- reflection
- Find()
- string concatenation every frame

Pool:

- bullets
- enemies
- particles
- damage numbers

---

# Code Reviews

Before finishing a task verify:

✓ Code compiles

✓ No obvious GC allocations

✓ Inspector clean

✓ Names clear

✓ No duplicated logic

✓ Single responsibility

✓ Extensible

---

# AI Workflow

For every request:

1 Understand

2 Plan

3 Explain

4 Implement

5 Review

Never immediately write hundreds of lines of code.

---

# Feature Workflow

New gameplay features follow this order.

Movement

↓

Combat

↓

Enemy

↓

Damage

↓

UI

↓

Audio

↓

Visual effects

↓

Optimization

↓

Polish

---

# Prototype Rules

Always implement the smallest playable version first.

For example:

Instead of

Inventory

Crafting

Economy

Achievements

Build

One weapon

One enemy

One level

One reward

Validate gameplay before scaling.

---

# If Requirements Are Unclear

Do not guess.

Ask questions.

Explain assumptions.

Offer alternatives.

---

# Definition of Done

A feature is complete only if:

- Compiles
- Runs
- Can be tested immediately
- Has no obvious architectural issues
- Is easy to extend
- Is production ready
- Matches Unity 6.3 LTS best practices

---

# Final Rule

Never optimize for writing code.

Optimize for shipping an excellent game.