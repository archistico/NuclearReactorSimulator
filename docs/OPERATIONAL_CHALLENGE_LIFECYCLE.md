# Operational Challenge Lifecycle

M10.9.6 introduces deterministic operator-training challenges without making challenge state a physical plant owner. This document defines the M10.9.6.1 lifecycle and logical-time contract.

## Ownership

Challenge lifecycle is Application-layer observational state. It may consume:

- immutable `ControlRoomSnapshot` values;
- accepted `ScenarioOperatorActionRecord` history;
- versioned scenario/objective metadata;
- authored challenge condition definitions.

It must not consume wall-clock time and must not receive a plant command dispatcher, supervisory-control dispatcher, mutable Simulation state or protection ownership.

## Lifecycle

The authoritative lifecycle is:

```text
NOT STARTED
    ↓ logical readiness boundary
READY
    ↓ authored activation condition
ACTIVE
    ├─→ COMPLETED
    ├─→ FAILED
    └─→ CANCELLED
```

`CANCELLED` is reached only through an explicit session/challenge lifecycle call. Presentation navigation cannot cancel or reset a challenge.

An explicit reset returns the challenge to `NOT STARTED` and immediately reconciles the current logical evidence. Therefore a reset performed after the authored readiness boundary may deterministically advance again to `READY` or `ACTIVE` at the same logical step.

## Logical time

All challenge timing is expressed in canonical logical simulation steps:

- `ReadyAtLogicalStep` — earliest absolute scenario step at which the challenge may become `READY`;
- optional target completion window offsets — presentation/evaluation metadata relative to the actual activation step;
- optional hard failure deadline offset — the only built-in timing rule that can fail a challenge.

Target windows are not automatic failure rules. If a challenge needs failure for missing a deadline, the definition must declare a hard deadline explicitly.

No `DateTime`, `DateTimeOffset`, wall-clock timer or UI refresh cadence participates in lifecycle transitions.

## Conditions

A versioned `ChallengeDefinition` owns separate authored references for:

- activation condition;
- required observations;
- completion conditions;
- optional failure conditions.

The condition evaluator receives only immutable snapshots and accepted operator-action history. The evaluator returns an evidence string and satisfaction state for the current logical step. Required observations are true completion prerequisites, not decorative telemetry: all required observations and all completion conditions must be satisfied before `COMPLETED`.

If a declared failure condition and all completion conditions become satisfied in the same logical step, failure takes precedence. This is a fail-closed lifecycle rule; it does **not** globally classify any plant event as challenge failure. Which observations are failure conditions remains challenge-definition-owned.

## Objective and assistance policy

Every challenge references one objective already declared by its owning `ScenarioDefinition`. It also declares:

- permitted `TrainingGuidanceMode` values;
- a scoring-policy identity string.

M10.9.6.1 performs no score arithmetic. The scoring-policy identity is only a stable seam for M10.9.6.3.

## Replay and checkpoint reconstruction

Challenge state is derivable rather than an authoritative physical-state dump. Given the same:

- exact scenario/initial-condition identity;
- challenge exact identity;
- deterministic logical-step sequence;
- accepted operator-action trace;

challenge transitions must reconstruct identically regardless of presentation publication stride.

M10.9.6.5 will close the full recorder/checkpoint/replay integration around this contract; M10.9.6.1 freezes the deterministic lifecycle semantics that integration must preserve.

## Non-scope

M10.9.6.1 does not introduce:

- external energy-demand profiles;
- score weights, grades or reward/penalty arithmetic;
- challenge UI;
- automatic plant commands;
- supervisory objectives;
- new faults, protection rules or physical models.
