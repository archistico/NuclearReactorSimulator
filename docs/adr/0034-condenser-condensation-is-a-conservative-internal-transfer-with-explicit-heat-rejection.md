# ADR 0034 — Condenser condensation is a conservative internal transfer with explicit heat rejection

## Status

Accepted for M4.3 baseline candidate.

## Context

M4.2 terminates each turbine stage group at a canonical exhaust fluid node and explicitly extracts shaft work. M4.3 must add condensation, condenser pressure/vacuum behavior and hotwell inventory without introducing a second fluid integrator or a synthetic vacuum state disconnected from mass/energy conservation.

## Decision

1. Every M4.2 turbine stage group is bound to exactly one M4.3 condenser.
2. The condenser steam space is the existing canonical M4.2 exhaust fluid node.
3. Each condenser transfers condensed mass internally from its steam-space node to a canonical hotwell fluid node.
4. Steam-space energy removal uses committed steam-space specific internal energy; hotwell energy addition uses committed hotwell specific internal energy.
5. The energy difference is declared as signed external heat-rejection power through a replaceable cooling-water/environment boundary.
6. Condensation rate is limited by configured condenser capacity, committed vapor inventory and available cooling heat-rejection power.
7. Exhaust pressure/vacuum is derived from the canonical fluid inventory and thermodynamic closure after the single network integration; no independent vacuum integrator is introduced.
8. `TurbineExpansionSolver` receives a backward-compatible supplemental-source-term overload so M4.3 composes before the same single plant-network integration boundary.
9. Rotor mechanical state remains owned and integrated only by M4.2.
10. No hidden control, air-ejector logic, circulating-water hydraulics or feedwater-train behavior is introduced in M4.3.

## Consequences

- Condenser mass conservation is explicit and internal.
- Heat rejection is visible in the global plant energy audit.
- Hotwell inventory is immediately available as a canonical upstream seam for M4.4.
- Vacuum dynamics remain coupled to conserved exhaust inventory instead of drifting in a parallel state model.
- Higher-fidelity cooling-water/environment models can replace the M4.3 boundary without rewriting condenser or turbine topology.
