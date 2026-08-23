# ADR 0188 — Separate grid-droop proportional and integral speed references

## Status

Accepted / Diagnostic 8 confirmed the versioned breaker-closed integral-reference repair substantially removes droop-driven windup; exact-v7 operating-point qualification remained negative and exact-v4 remains production.

## Context

The validated breaker-aware governor derives an effective speed-controller reference from synchronous grid speed plus a requested-load droop offset. This worked over the historical short qualification windows but Diagnostic 7 exposed a long-horizon incompatibility with non-zero integral gain: while paralleled to a stiff grid, rotor speed remains near synchronous speed, so the intentional droop offset appears as a permanent positive PI/PID error. The integral term therefore grows at approximately `Ki × droop error`, steadily moving the control valve and steam-path inventories.

Changing the initial controller integral cannot eliminate a persistent integration error. Removing integral gain globally would alter pre-synchronization speed control and historical exact-version semantics.

## Decision

Add an optional integral reference to the generic controller input. Null preserves the historical single-reference PID exactly.

Add a versioned governor integral-reference mode. Historical definitions default to `EffectiveDroopSetpoint`. The new candidate mode `SynchronousSpeedWhenParalleled` applies only when the generator breaker is closed and the speed controller is automatic:

- proportional/derivative error uses the existing droop-shifted effective reference;
- integral error uses synchronous mechanical grid speed.

Breaker-open behavior is unchanged. Controller gains, droop magnitude and actuator ownership are unchanged.

Exact-v7 is the first scenario identity opting into the new mode. Exact-v4 remains production; exact-v5 and exact-v6 retain historical mode semantics.

## Consequences

The intentional droop offset can command loaded governor response without being integrated away. Integral action remains available to remove true synchronous-speed bias. Historical exact-version behavior is preserved through the default mode rather than silently reinterpreted.

Production activation is not implied. Diagnostic 8 must first demonstrate bounded 600 s whole-cycle behavior and conservative closure.
