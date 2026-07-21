# ADR 0042 — Secondary-cycle controls use canonical valve/pump owners and the existing turbine-flow seam

## Status

Accepted for M5.4 baseline candidate.

## Context

M5.4 must add turbine governing, steam-pressure control and condensate/feedwater inventory loops without introducing a parallel hydraulic graph, duplicate pump/valve state, or a direct controller-output-to-shaft-power shortcut. M4.2 still exposes stage-group mass flow as the replaceable manual turbine-demand seam.

## Decision

1. M5.4 controllers continue to consume only `MeasuredSignalFrame`.
2. Normal turbine governing may target only canonical M4.1 control/admission valves; stop valves remain reserved for M5.5 isolation/trip logic.
3. Feedwater and condensate loops replace only canonical M4.4 `PumpState` operating commands.
4. M5.4 derives the M4.2 stage-group mass-flow seam from the non-negative limiting projected flow through the canonical stop/control/admission valve path.
5. `ValveFlowSolver` is used only as a committed-state/stateless projection; `PlantNetworkOrchestrator` remains the sole hydraulic and conserved-inventory integrator.
6. M5.3 and M5.4 must consume the same canonical measured-signal frame and may not own the same physical actuator target.
7. Protection overrides, trip arbitration, SCRAM and interlocks remain outside M5.4 and belong to M5.5.

## Consequences

- turbine valve commands affect actual turbine steam demand through an explicit validated seam;
- no second valve/pump physics model is introduced;
- feedwater and condensate loops preserve M4.4 ownership;
- the control architecture is ready for deterministic protection override arbitration in M5.5.
