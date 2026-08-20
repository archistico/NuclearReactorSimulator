# Roadmap

This file contains **future work only**. The authoritative current checkpoint and active validation gate remain in `PROJECT.md`.

## Execution discipline after Phase I

The post-Phase-I sequence is fixed unless a gate produces direct contrary evidence:

```text
M10.9.4.1 / Phase I final closure
        ↓
M10.9.5 Contextual Command Consequence Model
        ↓
M10.9.6 Operational Challenge & Energy-Demand Framework
        ↓
M10.9.7 Mission & Performance Workstation
        ↓
M10.9.8 Integrated Human-Automation-HMI Validation Gate
        ↓
M11 Release Hardening
        ↓
post-release engineering backlog
```

Rules:

1. work on one milestone at a time;
2. each milestone starts from the latest explicitly validated baseline only;
3. implementation begins only after the milestone contract, non-scope and gate are documented;
4. promotion requires build + complete ordinary suite + focused gate + any stated manual HMI check;
5. a failing gate authorizes work only on the demonstrated failing contract;
6. M10.9.5–M10.9.8 may expose or present existing physics/control/protection truth but may not create new reactor physics, component laws, protection thresholds or control ownership;
7. if operator-experience work reveals missing physics, record it for the post-M11 engineering backlog instead of expanding M10 scope;
8. no speculative audit, retuning or numerical requalification is added after a green gate merely “for safety”.

## Expected validation burden

This is a planning classification, not a duration promise:

| Milestone | Expected validation profile | Relative cost |
| --- | --- | --- |
| M10.9.5 | ordinary + focused semantic/replay tests + manual HMI | low/medium |
| M10.9.6 | ordinary + deterministic challenge/demand/replay tests + manual exercise checks | medium |
| M10.9.7 | ordinary + focused presentation/replay tests + manual HMI | low/medium |
| M10.9.8 | integrated 3×3 assistance/authority matrix + degraded/protection/replay + full manual HMI | high |
| M11 | compatibility, long/performance/memory, packaging/clean-machine and release-manual gates | high |

M10.9.5–M10.9.7 should not normally require multi-hour numerical requalification. The two intentionally expensive future checkpoints are M10.9.8 integration closure and M11 release closure.

## Current transition — Phase I closed / M10.9.5 active

M10.9.4.1 / Phase I is validated and closed. Authoritative desktop exact `@4` and synchronization exact `@3` remain the frozen production baselines for post-Phase-I operator-experience work.

M10.9.5.1 is validated. The active milestone is **M10.9.5.2 — explicit dependency-chain projection**. It may add Application/presentation metadata and tests only; it must not alter plant physics, numerical authority, protection thresholds or command dispatch ownership.

## M10.9.5 — Contextual Command Consequence Model

**Purpose:** explain what a selected command directly requests, what it is expected to influence, what currently blocks it, what the operator should monitor, and what the simulator actually did after dispatch.

Detailed plan: [`milestones/M10.9.5.md`](milestones/M10.9.5.md).

Planned sequence:

1. M10.9.5.1 — consequence semantics and command catalog;
2. M10.9.5.2 — explicit dependency-chain projection;
3. M10.9.5.3 — COMMANDS/context-inspector/schematic integration;
4. M10.9.5.4 — observed-response presentation separated from expected influence;
5. M10.9.5.5 — automated/manual closure gate.

No predictive UI physics, automatic command execution, invented causality or new permissive owner is allowed.

## M10.9.6 — Operational Challenge & Energy-Demand Framework

**Purpose:** add deterministic training objectives and external electrical-demand references without making scoring or challenge state a physical plant owner.

Detailed plan: [`milestones/M10.9.6.md`](milestones/M10.9.6.md).

Planned sequence:

1. M10.9.6.1 — challenge lifecycle and logical-time contract;
2. M10.9.6.2 — deterministic external energy-demand profiles;
3. M10.9.6.3 — multidimensional evaluation/scoring contract;
4. M10.9.6.4 — bounded challenge packs using existing plant/fault owners;
5. M10.9.6.5 — replay/checkpoint/determinism and closure gate.

`GRID DEMAND`, generator requested load and actual electrical output remain three separate semantics.

## M10.9.7 — Mission & Performance Workstation

**Purpose:** present objectives, demand, progress, score decomposition and deterministic performance history without changing the M10.9.6 evaluation owner.

Detailed plan: [`milestones/M10.9.7.md`](milestones/M10.9.7.md).

Planned sequence:

1. M10.9.7.1 — immutable mission/performance presentation contract;
2. M10.9.7.2 — workstation placement/navigation decision;
3. M10.9.7.3 — objective/demand/progress/score UI;
4. M10.9.7.4 — deterministic timeline and drill-down;
5. M10.9.7.5 — keyboard/minimum-window/manual closure gate.

**Open design decision before implementation:** the existing Operator Computer has a validated fixed F1–F8 contract. The current recommendation is a dedicated main-HMI Mission/Performance workspace linked from COMPUTER, with only a compact summary/link inside the existing computer pages; do not invent F9 without an explicit architecture decision.

## M10.9.8 — Integrated Human-Automation-HMI Validation Gate

**Purpose:** validate M10 as one coherent operator system across assistance, control authority, faults, protection, challenge/scoring, replay and manual operation.

Detailed plan: [`milestones/M10.9.8.md`](milestones/M10.9.8.md).

Planned sequence:

1. M10.9.8.1 — freeze the validation matrix and invariants;
2. M10.9.8.2 — automated assistance × authority matrix;
3. M10.9.8.3 — degraded measurement/fault/protection/takeover cases;
4. M10.9.8.4 — replay/checkpoint/same-seed and scoring integrity;
5. M10.9.8.5 — manual HMI/keyboard acceptance and M10 closure.

M10 closes only after M10.9.8 is explicitly validated.

## M11 — Release Hardening

**Purpose:** turn the validated M10 simulator into a release candidate without adding new gameplay or physics features.

Detailed plan: [`milestones/M11.md`](milestones/M11.md).

Planned sequence:

1. M11.1 — release/support contract and version freeze;
2. M11.2 — save/scenario/session compatibility and migration hardening;
3. M11.3 — performance, memory and long-run release budgets;
4. M11.4 — packaging/publish/deployment verification;
5. M11.5 — documentation/manual/known-limitations release alignment;
6. M11.6 — release-candidate clean-machine and final acceptance gate.

No feature work is accepted inside M11 unless it fixes a release-blocking defect demonstrated by an M11 gate.

## Post-M11 engineering horizon — not on the M10/M11 critical path

The approved extreme-operation/damage/accident direction remains in `FUTURE_GAMEPLAY_CONTROL_ROOM_AND_ACCIDENT_DIRECTION.md`.

The intended order after release hardening is:

1. extreme-envelope component directionality/support audit;
2. persistent equipment integrity/stress primitives;
3. pressure-boundary, rotating-equipment and electrical-damage models, one causal family at a time;
4. post-trip decay-heat/core-damage prerequisites before severe core-damage claims;
5. incident severity and post-incident persistence/replay integration;
6. later spatial-core and control-room visual extensions.

These items must not become prerequisites for M10.9.5–M11 merely because they are valuable future work.

## Deferred maintenance

Also non-blocking for M10.9.5 unless new evidence changes the risk:

- physical removal of H.5 `DeterministicHybridSemiImplicit` historical source seams;
- physical removal of H.21 `FourNodeBranchContinuityShadowIntegrated` historical source seams;
- investigation of whether validated long-horizon inventory/energy drifts can be reduced by a separately qualified physical/initialization improvement;
- simplification/removal of branch-continuity machinery only after a separate post-Phase-I proof that current repaired trajectories no longer require it;
- performance optimization of the authoritative corrected path beyond the validated repaired Stage-4 bounds.
