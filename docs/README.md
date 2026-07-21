# Documentation Map

This directory is the architectural and continuity record for Nuclear Reactor Simulator.

## Start here

- `PROJECT_HANDOFF.md` — **authoritative current checkpoint**, ownership rules and exact continuation point.
- `NEW_CHAT_START.md` — ready-to-paste bootstrap for restarting work in a new conversation.
- `PROJECT_STATUS.md` — current capability map, current candidate and deliberate omissions.
- `ROADMAP.md` — milestone sequence, phase gates and future scope.
- `ARCHITECTURE.md` — system composition, state ownership and cross-domain boundaries.

## Decision records

`adr/` contains Architecture Decision Records. Later work must preserve accepted decisions unless an explicit superseding ADR is created. The newest control-room/runtime/scenario decisions are ADR 0046–0055.

## Milestone records

`milestones/` records delivered scope and validation state per milestone. A milestone file describing delivered code does **not** by itself mean the milestone is validated; validation requires explicit local build/test confirmation recorded in the handoff/status/roadmap.

## Domain documents

The remaining top-level Markdown files document subsystem contracts and their ownership boundaries: reactor physics, primary circuit, turbine island, electrical system, control/protection, instrumentation, alarms, control-room presentation and the M7.1 versioned initial-condition/scenario framework, M7.2 cold-shutdown/pre-start flow and M7.3 first-criticality/low-power progression.

When modifying a subsystem, update its domain document together with the milestone/ADR/handoff documents rather than leaving architecture knowledge only in source comments or chat history.

## Current restart checkpoint

At this documentation refresh:

- M7.2 Cold Shutdown & Pre-Startup (hotfix 1) is the last explicitly locally validated baseline; the M6 gate remains complete.
- M7.3 First Criticality & Low-Power Operation is the current baseline candidate.
- After explicit M7.3 validation, the next milestone is M7.4 Heat-Up, Steam Raising & Turbine Startup.

See `PROJECT_HANDOFF.md` for the full authoritative statement.
