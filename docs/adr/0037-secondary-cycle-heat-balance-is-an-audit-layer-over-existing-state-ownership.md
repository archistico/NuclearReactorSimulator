# ADR 0037 — Secondary-cycle heat balance is an audit layer over existing state ownership

## Status

Accepted for M4.6 baseline candidate.

## Context

By M4.5, the simulator already owns the complete physical path from reactor heat generation through steam transport, turbine expansion, condensation, feedwater return, rotor dynamics and generator/grid conversion.

Adding an integrated heat balance creates a risk of duplicating inventories, re-integrating component energy, or treating turbine shaft work as both an external thermofluid loss and a separate plant loss.

## Decision

M4.6 introduces a thin `IntegratedSecondaryCycleDefinition` / `IntegratedSecondaryCycleSolver` composition boundary over the validated M4.5 stack.

The solver:

1. delegates all physical state evolution to `GeneratorGridSolver`;
2. introduces no new mutable plant, rotor or electrical state;
3. reads the existing `PlantNetworkAudit`, `TurbineMechanicalAudit` and `GeneratorElectricalAudit`;
4. cancels turbine shaft work exactly once as an internal thermofluid-to-mechanical transfer;
5. reconciles generator mechanical load against electrical export and explicit conversion loss;
6. exposes raw signed closure and classification residuals without correction.

## Consequences

- The single thermofluid integration boundary remains unchanged.
- Rotor state remains mechanically owned by M4.2.
- Electrical state remains owned by M4.5.
- M4.6 can verify the complete first-law path without adding new physics.
- M4.7 can build reference operating-point and long-run steady-state verification on a stable top-level composition boundary.
