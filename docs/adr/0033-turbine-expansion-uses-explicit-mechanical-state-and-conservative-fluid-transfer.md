# ADR 0033 — Turbine expansion uses explicit mechanical state and conservative fluid transfer

## Status

Accepted and validated with the M4.2 baseline.

## Context

M4.1 terminates the canonical main-steam network at a replaceable external turbine-admission sink. M4.2 must introduce shaft work and rotor dynamics without creating a second fluid integrator, hiding energy extraction in diagnostics, or misrepresenting mechanical kinetic energy as thermal inventory.

## Decision

1. M4.2 disables the temporary M4.1 terminal sink by requiring its commanded mass flow to be zero.
2. Each M4.1 turbine-admission seam feeds exactly one canonical lumped turbine stage group.
3. A stage group transfers steam mass internally from the committed turbine-inlet node to an existing canonical exhaust node.
4. Inlet mass and full inlet internal-energy flow are removed; equal mass and residual exhaust energy are added at the exhaust node.
5. The difference is explicit shaft power. The thermofluid plant-network audit records this as signed energy leaving the thermofluid inventory domain.
6. Rotor kinetic energy is not stored in `PlantState` or a fake `ThermalBodyState`. M4.2 introduces a separate immutable `TurbineExpansionState` containing explicit rotor angular speed.
7. Rotor dynamics integrate torque over one deterministic fixed interval from committed state only. Turbine torque, external load torque, net torque, speed and kinetic-energy change remain observable.
8. A mechanical audit independently closes shaft work against rotor kinetic-energy change plus external mechanical load.
9. Overspeed detection is diagnostic. Automatic trip logic/latching remains a future protection-system responsibility.
10. An explicit trip command seam may block turbine expansion immediately, but it does not mutate upstream valve states or implement automatic protection sequencing.
11. M4.1 receives a backward-compatible supplemental-source-term overload so M4.2 still reaches exactly one `PlantNetworkOrchestrator` integration.

## Consequences

- mass remains conservative through turbine expansion;
- steam-energy extraction is explicit and auditable;
- mechanical state has correct semantic ownership;
- no same-step sequential mutation is introduced;
- M4.3 can attach condenser/vacuum physics to canonical exhaust nodes;
- M4.5 can replace the manual external load-torque seam with generator electromagnetic torque;
- M5 can connect overspeed indication to deterministic protection/trip logic without rewriting turbine physics.
