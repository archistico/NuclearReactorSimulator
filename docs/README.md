# Documentation Map

This directory is the architectural and continuity record for Nuclear Reactor Simulator.

## Start here

- `PROJECT_HANDOFF.md` — **authoritative current checkpoint**, ownership rules and exact continuation point.
- `NEW_CHAT_START.md` — ready-to-paste bootstrap for restarting work in a new conversation.
- `PROJECT_STATUS.md` — current capability map, current candidate and deliberate omissions.
- `ROADMAP.md` — milestone sequence, phase gates and future scope.
- `ARCHITECTURE.md` — system composition, state ownership and cross-domain boundaries.

## Decision records

`adr/` contains Architecture Decision Records. Later work must preserve accepted decisions unless an explicit superseding ADR is created. The newest control-room/runtime/scenario/fault decisions are ADR 0046–0062.

## Milestone records

`milestones/` records delivered scope and validation state per milestone. A milestone file describing delivered code does **not** by itself mean the milestone is validated; validation requires explicit local build/test confirmation recorded in the handoff/status/roadmap.

## Domain documents

The remaining top-level Markdown files document subsystem contracts and their ownership boundaries: reactor physics, primary circuit, turbine island, electrical system, control/protection, instrumentation, alarms, control-room presentation, the M7 operating/training framework, M8.1 deterministic fault injection, M8.2 hydraulic component faults and M8.3 instrumentation/control faults.

When modifying a subsystem, update its domain document together with the milestone/ADR/handoff documents rather than leaving architecture knowledge only in source comments or chat history.

## Current restart checkpoint

At this documentation refresh:

- M7.7 Training Objectives, Procedure Guidance & Evaluation is validated and the M7 gate is complete.
- M8.1 Deterministic Fault-Injection Framework is validated.
- M8.2 Hydraulic Component Faults hotfix 2 is validated.
- M8.3 Instrumentation & Control Faults is the current baseline candidate.
- After explicit M8.3 validation, continue with M8.4 Turbine/Generator/Feedwater/Condenser Transients.

See `PROJECT_HANDOFF.md` for the full authoritative statement.
