# ADR 0011 — Valves modulate the existing passive-flow model

- Status: Accepted for M1.4
- Date: 2026-07-20

## Context

M1.3 established one deterministic passive hydraulic law for bidirectional pipe flow. M1.4 needs controllable restrictions, characteristic curves and fail-safe behaviour without duplicating the hydraulic solver or encoding closed valves using infinite/magic resistances.

## Decision

A valve composes an existing `PipeDefinition` whose resistance represents the fully-open path.

Mechanical `ValvePosition` is mapped through a `ValveCharacteristic` to a normalized flow-capacity coefficient `g`.

For `g > 0`, effective quadratic resistance is:

`R_effective = R_fully_open / g²`

The resulting temporary effective pipe is solved by the existing `PipeFlowSolver`.

For `g = 0`, flow is exactly zero and no infinite resistance is constructed.

Fail-safe action is part of immutable `ValveDefinition`; fail-safe activation is part of immutable `ValveState`. Effective position is resolved before the characteristic is evaluated.

## Consequences

- M1.3 remains the single passive pressure-driven flow law.
- Fully-open valve behaviour is exactly equivalent to its wrapped pipe.
- Closed behaviour is exact and numerically safe.
- Characteristic curves can evolve independently from hydraulic conservation.
- Future actuator/control models can change `ValveState` without changing the flow solver.
- Fail-safe semantics are deterministic and explicit.

## Deferred

Actuator dynamics, control commands, stiction, leakage, detailed valve sizing, cavitation/choking and specialized valve families are outside M1.4.
