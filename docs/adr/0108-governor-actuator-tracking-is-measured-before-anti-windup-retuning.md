# ADR 0108 — Governor/actuator tracking is measured before anti-windup retuning

## Status

Proposed / M10.9.4.1-D.3 audit candidate.

## Context

The current-v2 turbine speed controller has output-saturation anti-windup, while the physical control-valve actuator is rate limited to 0.5 fraction/s (2 seconds for full travel). The controller does not currently receive the committed actuator position as an anti-windup tracking signal.

This creates a structural possibility of integral accumulation while controller output and valve position diverge. The existence of that structural possibility does not prove that it is material at the validated operating point.

## Decision

D.3 is evidence-first and changes no production controller or actuator law.

The dedicated audit applies the canonical operator stimulus of five `SPEED RAISE` commands (+50 rpm reference), observes the rate-limited response, restores the reference with five `SPEED LOWER` commands and records:

- controller output;
- committed control-valve position;
- command/position gap;
- integral term;
- speed error;
- post-restoration residual integral offset.

A command/position gap of at least 5 percentage points is required so the audit genuinely exercises actuator rate limiting. Tracking anti-windup is considered justified only if the integral moves by at least 2 controller-output percentage points while that material lag exists.

## Consequences

- If the gate remains below 2 percentage points, D.3 closes without a production control-law change and tracking anti-windup is deferred.
- If the gate reaches or exceeds 2 percentage points, a separate D.3.1 correction may add actuator-position tracking/back-calculation with dedicated bumpless-transfer and replay/regression tests.
- D.3 does not retune PID gains, droop, valve travel rate, stage resistance or admission hydraulics.
