# ADR 0101 — Governor effective setpoint and actuator tracking are audited before new anti-windup

## Status

Accepted as evidence gate; outcome recorded by M10.9.4.1-D.3.1.

## Context

The canonical controller solver already provides bounded outputs, bumpless transfer and conditional-integration anti-windup when the controller output saturates. Current-v2 secondary valves also have finite travel rates, so controller command and physical valve position can temporarily differ.

A new actuator-position tracking anti-windup law would add state/coupling to a validated generic controller boundary. It must not be introduced merely because finite actuator travel exists.

The first D.2 runtime perturbation also used direct speed-reference commands from a breaker-closed seed. While paralleled, the governor droop adapter intentionally derives the effective speed-controller setpoint from requested electrical load, so that journey did not guarantee a real effective-setpoint step.

## Decision

D.3 remains evidence-only.

The breaker-open audit uses direct `SPEED RAISE/LOWER` commands and verifies the effective setpoint changes by ±10 rpm. The breaker-closed audit uses `LOAD RAISE/LOWER` and verifies the droop-derived setpoint changes by 0.75 rpm per 5 MWe. That displacement was originally produced by 1,000 MWe/150 rpm and is deliberately preserved by E.2 through 10 MWe/1.5 rpm.

Both journeys record P/I/D terms, saturation, existing anti-windup state, bounded controller output, physical control-valve position, command/position gap, rotor speed, turbine flow and shaft power.

No tracking anti-windup, controller gain, actuator travel rate, droop, resistance or turbine-flow law is changed until this evidence demonstrates material actuator-induced windup.

## Consequences

The corrected evidence distinguishes three different limitations:

1. hydraulic authority compression at large valve openings — D.2;
2. controller-output versus finite-rate actuator tracking and possible windup — D.3;
3. reference-scale normalization of governor load fraction — resolved structurally by the coordinated E.2 scale migration while preserving the measured 0.75 rpm step.

If existing conditional integration and finite valve travel recover cleanly, Phase D closes without additional controller physics. If material tracking windup is proven, a narrowly scoped D.3.x checkpoint may add a versioned tracking law. The 0.75 rpm load-step displacement is retained as scale evidence and is not corrected by isolated governor retuning.

## Evidence outcome

The corrected breaker-open audit did not demonstrate a need for immediate tracking anti-windup. It demonstrated a more fundamental missing plant path: with the breaker open, generator torque and passive mechanical losses were both zero, so a rotor at 3301 rpm could not decelerate after steam admission closed. D.3.1 therefore adds versioned passive rotor loss first. Anti-windup remains unchanged until the corrected post-loss evidence is reviewed.
