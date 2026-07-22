# ADR 0066 — Safety-response evaluation is observational and reuses existing faults

## Status

Accepted — M8.7 hotfix 2 validated / M8 gate complete.

## Context

M8.1–M8.6 establish deterministic fault lifecycle, concrete component/instrumentation/transient/break/electrical-loss effects and scenario definitions. The final M8 milestone needs safety-response exercises with acceptance criteria and operator timelines. Creating scenario-specific physics or directly writing protection outcomes would duplicate validated owners and make training results scripted rather than emergent.

## Decision

M8.7 shall:

- reuse exact prior M8 fault declarations and their registered applicators;
- evaluate acceptance criteria only from committed `ControlRoomSnapshot` presentation state and accepted operator actions;
- use the existing M7.7 deterministic checkpoint/scoring framework;
- expose the existing logical `ScenarioOperatorActionJournal` for debrief timeline capture;
- never inject protection latches, controller outputs, breaker states or thermohydraulic outcomes to satisfy an acceptance criterion.

Automatic events remain represented by committed fault/protection/alarm snapshots. Operator timelines contain only accepted typed operator commands.

## Consequences

Safety-response scoring is deterministic, replay-compatible and independent of UI publication cadence. Scenario fidelity cannot exceed the source fault/model fidelity, and M8.7 cannot be validated independently while its M8.5/M8.6 source chain remains unvalidated.
