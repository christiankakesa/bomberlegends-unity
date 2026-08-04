# Bomber Legends — Engineering Process & Technical Leadership (Phases 6–7)

**Applies to:** every task from T-001 onward, for the life of the project.

---

## Part A — Pre-Implementation Protocol (Phase 6)

**No code is written until the five points below have been stated and accepted.** This is not ceremony —
each point catches a specific class of expensive mistake, and each has already caught one in this project
(the 5 s bomb cooldown, the missing chain detonation, the neon lighting budget).

For every feature, before implementation:

### 1. Approach
What is being built, which systems it touches, and which of the *simpler* alternatives were rejected and why.
If a simpler option was not considered, the approach is not ready.

### 2. Architectural impact
- Which assemblies change, and does the dependency graph of `03-ARCHITECTURE.md` §2 still hold?
- Does it belong in `Simulation` (rules) or `Gameplay` (presentation)? **Getting this wrong is the most
  expensive mistake available in this codebase**, because rules that leak into the view layer cannot be
  tested, cannot be replayed, and cannot be moved to a server.
- Does it introduce new state? Where does that state live, and who owns it?
- Does it add a dependency? Third-party packages need explicit justification.

### 3. Performance considerations
- Allocations per frame and per tick — the answer must be **0** in gameplay paths.
- Draw-call and overdraw impact.
- Does it run per-frame, per-tick, or on an event? Event-driven wins unless proven otherwise.
- Does it need pooling? If it spawns during a match, yes.

### 4. Mobile implications
- Frame-time cost on the **low-tier** device, not the development machine.
- Touch input, thumb occlusion, and safe-area impact.
- Battery and thermal cost of any sustained effect.
- Memory and download-size delta.
- Behaviour when the app is backgrounded mid-feature.

### 5. Future extensions
- Does it survive multiplayer (Q1)? Anything writing gameplay state outside `Simulation.Tick` does not.
- Is it data-driven enough for a designer to tune without an engineer?
- What is the obvious next request, and does this design accommodate it or block it?

### Then, and only then

Implement **incrementally**. Never generate a whole system in one pass. Ship the smallest version that runs,
verify it on a device, then extend.

### After implementation — self-review against `CLAUDE.md`

✓ Compiles ✓ No GC allocations ✓ Inspector clean ✓ Names clear ✓ No duplicated logic
✓ Single responsibility ✓ Extensible ✓ Tests green ✓ Verified on device

---

## Part B — Standing Technical Direction (Phase 7)

How this project is led, stated as commitments rather than aspirations.

### 1. Poor ideas get challenged — including the ones already written down

`01-ANALYSIS.md` challenges the 5 s bomb cooldown, the shield passive, the vertical progression tree, the
console platform list, and the backend stack — all of which are in the GDD. Design documents are inputs, not
instructions. **A design that survives scrutiny is stronger; one that cannot survive it should not be built.**

The corollary: a challenge is raised once, with reasoning and an alternative. If the decision is reaffirmed,
it is implemented properly and without further argument, with the reasoning recorded so the outcome is
attributable to the decision rather than the implementation.

### 2. The simpler solution is the default

Concretely, in this project:
- Manual composition root instead of a DI framework, until the graph outgrows it (~30 services).
- `JsonUtility` instead of a serialisation library, until the model needs polymorphism.
- uGUI everywhere in the slice instead of two UI systems.
- Grid maths instead of Unity physics.
- Local saves instead of a backend, until there is a reason for a backend.
- Every one of these can be upgraded later behind an interface that already exists.

### 3. Technical debt is taken deliberately or not at all

Acceptable: a tunable defaulting to a value we intend to change (the bomb cooldown), placeholder art,
a no-op analytics service.
Not acceptable: skipping the save schema version, skipping pooling, putting rules in `MonoBehaviour`s,
skipping tests on `Simulation`. These are not shortcuts — they are decisions that cost a rewrite.

Every deliberate shortcut is recorded with the condition that retires it.

### 4. Composition over inheritance, always

There is no `EnemyBase`. There are enemy *definitions* (data) processed by *systems* (behaviour). A new enemy
is a ScriptableObject plus, at most, one behaviour strategy — not a subclass. The same rule holds for
abilities, pickups, and blocks. This is why M6 can add two enemies in five days.

### 5. Event-driven, with the right tier for the job

Three tiers, three rules (`03-ARCHITECTURE.md` §7). The mistake to avoid is using ScriptableObject event
channels for per-tick gameplay because they are convenient in the Inspector — that is how a clean
architecture becomes a 4 ms/frame indirection tax.

### 6. Iteration speed is a feature, and it is defended

Split assemblies for fast compiles, play-from-any-scene, levels as data rather than scenes, feel parameters
tunable in play mode, tests that run in milliseconds. The moment iteration slows, fixing it takes priority
over features — a slow loop compounds against every remaining task in the project.

### 7. Gameplay is validated on hardware, by strangers

Editor testing proves the code runs. Device testing by someone who has never seen the game proves it works.
Gate A in `02-PROTOTYPE-SCOPE.md` is written to be **falsifiable and recorded before testing begins**, so a
disappointing result cannot be rationalised into a passing one after the fact.

### 8. Scope is defended with the pitch

Every new feature request is answered with: *"Does this improve the player's experience, and does it beat the
thing it displaces?"* This is why GDD §1 being empty (Risk S8) matters — without a one-paragraph pitch, scope
has nothing to be measured against, and every feature sounds reasonable in isolation.

### 9. The build is always playable

No milestone ends in a broken state. `main` always produces an installable build. A feature that cannot be
finished is reverted, not left half-integrated behind a flag nobody removes.

---

## Part C — Working Agreement

**What you get from me on every task, without being asked:**
1. The approach and the rejected alternatives, before any code.
2. An honest complexity estimate, including when a task should be split.
3. A direct statement when a request will cause problems — once, with an alternative.
4. Code that satisfies the universal DoD in `05-BACKLOG.md`, or an explicit statement of what is missing.
5. Test criteria written before the implementation, not retro-fitted to pass it.

**What I need from you:**
1. Answers to Q1–Q5 in `01-ANALYSIS.md` §13. Q1 (single-player vs. multiplayer) and Q3 (single-screen vs.
   scrolling levels) shape the architecture; the others shape the design.
2. A decision when I flag a fork — I will recommend, but the call is yours.
3. Honest playtest data at the gate, including when it says stop.
4. An answer on art capacity (Q8) before M8 is scheduled.

---

## Current State

| Item | Status |
|---|---|
| Documentation | ✅ Phases 1–7 complete |
| Unity project | ❌ Not created — T-001 is the first task |
| Open blocking questions | ⚠️ Q1, Q2, Q3 (see `01-ANALYSIS.md` §13) |
| Code written | **None, by design** |

**Ready to begin T-001 on request.** Milestone 0 (T-001 → T-009) does not depend on any open question and
can start immediately.
