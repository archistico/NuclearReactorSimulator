# ADR 0089 — Current-v2 governor switches from speed reference to grid-load droop

## Status
Accepted for M10.9.4 Hotfix 22 candidate.

## Decision
The current-v2 turbine governor keeps one canonical speed PID and one canonical control-valve actuator.
With the generator breaker open, the operator turbine-speed setpoint is authoritative. With the breaker closed and the controller automatic, the effective governor reference is derived from grid synchronous speed plus a load-droop offset proportional to canonical requested electrical power.

Current-v2 uses a 150 rpm full-load reference rise on the 3000 rpm machine (5% droop). Legacy/versioned definitions keep `GovernorDroop = null` and therefore preserve the historical speed-reference-only behavior.

Manual controller mode bypasses the automatic droop rewrite. Breaker closure takes effect from the next committed control step, preserving deterministic step-boundary semantics.

## Consequences
- Pre-synchronization run-up remains speed controlled.
- After paralleling, `GENERATOR LOAD RAISE/LOWER` changes the governor reference instead of leaving the isochronous speed target as the only turbine command.
- No second governor PID, hidden actuator state or duplicate control-valve owner is introduced.
- Protection, actuator travel rates, generator electromagnetic loading and UI authority remain unchanged.
