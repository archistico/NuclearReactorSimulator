# ADR 0043 — Protection overrides normal control through canonical command seams

## Status

Accepted; M5.5 validated.

## Context

M5.3 and M5.4 introduced normal automatic control over canonical rods, pumps and turbine admission valves. M5.5 must add SCRAM, turbine/generator trips, permissives and interlocks without hiding protection inside PID logic, duplicating physical state or bypassing validated M2/M4 ownership.

## Decision

1. Protection consumes the same M5.1 `MeasuredSignalFrame` as normal control and never reads `FullPlantSnapshot` true state directly.
2. Trip functions are deterministic and latched; reset requires explicit operator input, safe reset thresholds and configured measured permissives.
3. Interlocks are deterministic non-latching command inhibits and remain distinct from trip latches.
4. Protection has explicit priority over normal process-control commands.
5. Reactor SCRAM overrides the existing M2 rod-command seam with insert commands; it does not write reactivity or MW directly.
6. Turbine trip closes canonical M4.1 stop valves and asserts the existing M4.2 turbine `TripCommand` seam.
7. Generator trip asserts the existing M4.5 breaker-open seam and suppresses breaker close.
8. Protection state contains logical latches only; authoritative rod/valve/rotor/electrical states remain in their validated domains.
9. Alarm presentation, acknowledgement and annunciator latching remain M5.6 responsibilities.

## Consequences

- Normal control and protection can be tested independently and then composed deterministically.
- Protection behavior remains affected by the same instrumentation quality/fault semantics seen by controllers.
- There is no second rod, steam, turbine or electrical state model.
- Later alarm logic can observe protection snapshots without owning or triggering physical protection actions implicitly.
