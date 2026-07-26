# ADR 0089 — Current-v2 governor switches from speed reference to grid-load droop

## Status
Accepted and validated in M10.9.4 Hotfix 22. The 150 rpm calibration is superseded for current-v2 by ADR 0110 / E.2; the breaker-aware ownership decision remains active.

## Decision
The current-v2 turbine governor keeps one canonical speed PID and one canonical control-valve actuator.
With the generator breaker open, the operator turbine-speed setpoint is authoritative. With the breaker closed and the controller automatic, the effective governor reference is derived from grid synchronous speed plus a load-droop offset proportional to canonical requested electrical power.

At Hotfix 22, current-v2 used a 150 rpm full-load reference rise on the 3000 rpm machine. E.2 supersedes only that scale calibration with 1.5 rpm on the 10 MWe current-v2 generator, preserving the same 0.75 rpm displacement at the 5 MWe point. Legacy/versioned definitions keep `GovernorDroop = null` and preserve historical speed-reference-only behavior.

Manual controller mode bypasses the automatic droop rewrite. Breaker closure takes effect from the next committed control step, preserving deterministic step-boundary semantics.

## Consequences
- Pre-synchronization run-up remains speed controlled.
- After paralleling, `GENERATOR LOAD RAISE/LOWER` changes the governor reference instead of leaving the isochronous speed target as the only turbine command.
- No second governor PID, hidden actuator state or duplicate control-valve owner is introduced.
- Protection, actuator travel rates, generator electromagnetic loading and UI authority remain unchanged.
