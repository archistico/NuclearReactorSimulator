# ADR 0061 — Hydraulic faults constrain canonical components and use the single network source-term boundary

**Status:** Accepted — M8.2 validated

## Context

M8.1 validates deterministic fault declaration, scheduling and lifecycle but intentionally owns no equipment failure physics. M8.2 must make pump, valve, restriction and selected leak faults physically observable without bypassing M3's single inventory integrator, M5 control/protection ownership or M7 scenario boundaries.

## Decision

1. Concrete hydraulic fault applicators bind through a runtime-side `IHydraulicComponentFaultTarget`; scenario code never receives or mutates `PlantState` directly.
2. The runtime converts active fault effects into immutable `HydraulicComponentFaultInputs` consumed once per deterministic step.
3. Normal control and protection still compute their canonical commands first. Hydraulic component failures then constrain the physical component state before the existing M4.7/full-plant step. This models a component that may fail to obey a valid command without transferring protection ownership to the fault layer.
4. Pump trip/degradation acts only on canonical `PumpState` run/speed capability before `PumpFlowSolver`; no flow or pressure is written directly.
5. Valve fail-open/fail-closed/stuck and path restriction/blockage act only on canonical `ValveState` position before `ValveFlowSolver`. Stuck position is captured from the committed state at activation.
6. M8.2 path restriction/blockage targets canonical valve-controlled paths. Arbitrary pipe-definition mutation is excluded rather than creating a second hydraulic topology; later M8.5 break behavior composes through conservative source-term boundaries instead.
7. Selected leaks are signed external `PlantNetworkSourceTerms`: negative mass flow plus the corresponding source-node specific-internal-energy export. They pass through the same one `PlantNetworkOrchestrator` integration and audit boundary as all other staged source terms.
8. Fault clearance removes the forcing constraint; it does not teleport a physical component back to a prior position/speed. Normal control or later operator/protection commands determine subsequent recovery.
9. Target IDs and numeric parameters are validated fail-closed and use invariant deterministic parsing.

## Consequences

Hydraulic faults produce real plant consequences while retaining canonical pump/valve/thermofluid ownership and conservation accounting. Protection remains the authoritative command/arbitration owner, but a physically failed component may be unable to execute that command. M8.3+ can compose additional fault families through the same M8.1 lifecycle without enlarging the generic scheduler into a physics owner.
