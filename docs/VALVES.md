# Valves

## Purpose

M1.4 introduces valves as controllable restrictions layered on top of the validated M1.3 passive pipe model.

A valve does **not** define a second hydraulic law. It modifies the effective resistance of an existing `PipeDefinition`, and `PipeFlowSolver` remains the single passive pressure-driven flow solver.

## Domain model

A valve is represented by:

- `ValveDefinition` — immutable identity, wrapped fully-open pipe, characteristic and fail-safe action;
- `ValveState` — immutable last mechanical position and whether fail-safe behaviour is active;
- `ValvePosition` — normalized closed-to-open mechanical position in `[0, 1]`;
- `ValveCharacteristic` — maps mechanical position to normalized flow capacity;
- `ValveFailSafeAction` — fail closed, fail open or hold last position.

The wrapped pipe resistance is always the **fully-open** hydraulic resistance.

## Characteristic curves

The characteristic solver returns a normalized flow-capacity coefficient `g` in `[0, 1]`.

Supported M1.4 characteristics:

- **Linear:** `g = x`;
- **Quick-opening:** `g = sqrt(x)`;
- **Equal-percentage normalized:** `g = (r^x - 1) / (r - 1)`, with rangeability `r > 1`.

Here `x` is normalized mechanical position.

The endpoint contract is exact for every characteristic:

- `x = 0` -> `g = 0`;
- `x = 1` -> `g = 1`.

## Effective hydraulic resistance

M1.3 uses:

`Δp = R · m_dot · |m_dot|`

To make normalized capacity `g` scale mass-flow capacity directly, M1.4 uses:

`R_effective = R_fully_open / g²`

Therefore, under the same pressure difference:

- fully open (`g = 1`) matches the underlying pipe exactly;
- linear 50% (`g = 0.5`) produces 50% of the fully-open mass-flow magnitude;
- a closed valve (`g = 0`) produces exactly zero flow.

Closed valves are handled explicitly. The model does not encode closure using infinity or an arbitrary giant resistance.

## Fail-safe semantics

When fail-safe is inactive, effective position equals `ValveState.Position`.

When active:

- `FailClosed` -> effective position is exactly closed;
- `FailOpen` -> effective position is exactly fully open;
- `HoldLastPosition` -> effective position remains the last mechanical position.

M1.4 does not yet model actuator travel time, torque, pneumatic pressure, electrical supply or mechanical sticking. Those are later control/component-dynamics concerns.

## Conservation and direction

After effective resistance is established, the existing `PipeFlowSolver` computes pressure-driven bidirectional flow and conservative upstream internal-energy advection.

Therefore valve-controlled flow inherits the M1.3 guarantees:

- pressure reversal reverses mass flow naturally;
- energy advection follows the true upstream node;
- endpoint mass balances are equal and opposite;
- endpoint energy balances are equal and opposite;
- no ordering dependency is introduced into future network evaluation.

## Explicit non-goals for M1.4

M1.4 does not yet model:

- actuator movement rate or stroke time;
- position commands or control loops;
- valve stiction, hysteresis or leakage;
- cavitation/flashing/choked two-phase flow;
- detailed `Cv`/`Kv` sizing correlations;
- check valves or relief valves;
- pump-generated head.

These are intentionally deferred so the valve abstraction remains small, deterministic and composable.
