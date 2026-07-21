# Documentation Map

This directory is the architectural and continuity record for Nuclear Reactor Simulator.

## Start here

- `PROJECT_HANDOFF.md` — **authoritative current checkpoint**, ownership rules and exact continuation point.
- `NEW_CHAT_START.md` — ready-to-paste bootstrap for restarting work in a new conversation.
- `PROJECT_STATUS.md` — current capability map, current candidate and deliberate omissions.
- `ROADMAP.md` — milestone sequence, phase gates and future scope.
- `ARCHITECTURE.md` — system composition, state ownership and cross-domain boundaries.

## Decision records

`adr/` contains Architecture Decision Records. Later work must preserve accepted decisions unless an explicit superseding ADR is created. The newest control-room/runtime/scenario/fault decisions are ADR 0046–0061.

## Milestone records

`milestones/` records delivered scope and validation state per milestone. A milestone file describing delivered code does **not** by itself mean the milestone is validated; validation requires explicit local build/test confirmation recorded in the handoff/status/roadmap.

## Domain documents

The remaining top-level Markdown files document subsystem contracts and their ownership boundaries: reactor physics, primary circuit, turbine island, electrical system, control/protection, instrumentation, alarms, control-room presentation and the M7.1 versioned initial-condition/scenario framework, M7.2 cold-shutdown/pre-start flow, M7.3 first-criticality/low-power progression, M7.4 heat-up/steam-raising/turbine-startup flow, M7.5 grid-synchronization/load-increase flow, M7.6 power-manoeuvring/normal-shutdown flow, M7.7 training objectives/guidance/evaluation framework and M8.1 deterministic fault-injection framework and M8.2 hydraulic component faults.

When modifying a subsystem, update its domain document together with the milestone/ADR/handoff documents rather than leaving architecture knowledge only in source comments or chat history.

## Current restart checkpoint

At this documentation refresh:

- M7.7 Training Objectives, Procedure Guidance & Evaluation is the last explicitly locally validated baseline; the M7 gate is complete.
- M8.1 Deterministic Fault-Injection Framework is validated.
- M8.2 Hydraulic Component Faults is the current baseline candidate.
- After explicit M8.2 validation, the next milestone is M8.3 Instrumentation & Control Faults.

See `PROJECT_HANDOFF.md` for the full authoritative statement.
