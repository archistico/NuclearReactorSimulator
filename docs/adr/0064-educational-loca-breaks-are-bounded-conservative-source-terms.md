# ADR 0064 — Educational LOCA-class breaks are bounded conservative source terms

- **Status:** Accepted / M8.5 hotfix 2 validated
- **Date:** 2026-07-21

## Context

M8.2 introduced selected fixed-rate node leaks as signed mass/energy source terms. M8.5 needs larger educational leak/LOCA-class scenarios whose discharge responds to plant pressure, while preserving the validated rule that `PlantNetworkOrchestrator` integrates every conserved fluid/thermal inventory exactly once.

A realistic LOCA model would require critical-flow/two-phase discharge correlations, detailed break geometry, flashing, containment backpressure, ECCS and much higher thermal-hydraulic fidelity than the current lumped educational model supports.

## Decision

Introduce one typed M8.5 fault effect, `loca.pressure-driven-break`.

For each committed fixed step:

1. resolve the exact canonical source fluid node;
2. compute positive driving pressure relative to an explicit ambient pressure;
3. scale a declared reference mass flow with a bounded square-root pressure relation;
4. cap removal by an explicit maximum fraction of the committed node inventory for that fixed step;
5. deterministically probe the committed source-node inventory after the proposed mass/carried-energy removal and, when required, further reduce only that break removal so the candidate remains inside the existing simplified water/steam thermodynamic envelope;
6. emit negative mass flow plus carried source-node internal-energy flow as `PlantNetworkSourceTerms`;
7. let the existing single plant-network integration and thermodynamic closure determine resulting inventory, pressure, temperature and phase.

No node pressure, temperature, void, level or predetermined accident outcome is written by M8.5.

## Consequences

- Mass and energy loss remain visible in existing conservation audits.
- Depressurization is a consequence of conserved inventory evolution, not a scripted state variable.
- Replay remains deterministic because the break depends only on committed state, fixed timestep and immutable scenario parameters.
- The per-step inventory bound is an explicit maximum request. A deterministic thermodynamic-admissibility guard may reduce it further but never increase it, add inventory, relax closure, or alter committed state; both guards are numerical/fidelity limits for the lumped model, not physical ECCS or licensing-grade break-flow correlations.
- Fixed-rate M8.2 leaks remain available for small prescribed leaks; M8.5 pressure-driven breaks cannot silently stack with them on the same node.

## Rejected alternatives

- Directly setting pressure/temperature during a LOCA scenario: violates physical ownership and conservation.
- Adding a second accident-specific hydraulic integrator: duplicates M3 ownership.
- Implementing licensing-grade critical-flow/containment/ECCS physics inside M8.5: exceeds the validated model fidelity and would create false precision.
