# Bomber Legends — Documentation Index

| Doc | Phase | Purpose |
|---|---|---|
| [GDD.md](GDD.md) | — | Game Design Document (v1.1, EN). Source of truth for *intent*, not for scope. |
| [GDD.fr.md](GDD.fr.md) | — | French original, archived. Historical record — **not maintained**. |
| [01-ANALYSIS.md](01-ANALYSIS.md) | 1 | Vision, fantasy, loops, risks, contradictions, and the 10 open questions. |
| [02-PROTOTYPE-SCOPE.md](02-PROTOTYPE-SCOPE.md) | 2 | The vertical slice: what ships, what is postponed, and the pass/fail gates. |
| [03-ARCHITECTURE.md](03-ARCHITECTURE.md) | 3 | Unity 6.3 technical architecture. Assemblies, simulation, services, data flow. |
| [04-ROADMAP.md](04-ROADMAP.md) | 4 | Milestones M0–M10, each producing a playable build. |
| [05-BACKLOG.md](05-BACKLOG.md) | 5 | Task backlog T-001 → T-063 with dependencies, complexity, tests, DoD. |
| [06-ENGINEERING-PROCESS.md](06-ENGINEERING-PROCESS.md) | 6–7 | Pre-implementation protocol and standing technical direction. |
| [CLAUDE.md](../CLAUDE.md) | — | Project conventions (binding). |
| `.claude/skills/unity-6.3/SKILL.md` | — | Unity engineering standards (binding). |

## Reading order for a new engineer

`01` → `02` → `03` → `05`. The roadmap and process docs are reference.

## Decisions locked (2026-08-05)

- **Q1 — Single-player now, multiplayer-ready architecture.** Deterministic engine-free `Simulation`
  assembly from commit #1; Nakama postponed to M9+.
- **Q2 — Android → WebGL → iOS → Desktop.** Consoles out of scope. Supersedes the platform list in
  `CLAUDE.md` and in the GDD header.
- **Q3 — Single-screen levels, ~13×11 tiles.** No camera scrolling.

## Still open

Q4–Q10 in [01-ANALYSIS.md §13](01-ANALYSIS.md) — bomb economy, meta shape, retention loop, lives fail
state, art capacity, business model, and the empty GDD §1 pitch. None of them block Milestone 0.

**Next action:** T-001 in [05-BACKLOG.md](05-BACKLOG.md).
