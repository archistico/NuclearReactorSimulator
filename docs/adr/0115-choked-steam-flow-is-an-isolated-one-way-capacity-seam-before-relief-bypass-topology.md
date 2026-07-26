# ADR 0115 — Choked steam flow is an isolated one-way capacity seam before relief/bypass topology

**Status:** Accepted for M10.9.4.1-F.1 candidate

**Date:** 2026-07-26

## Context

The current main-steam and admission paths use the generic quadratic hydraulic law. That law is suitable for the existing reduced-order connected network, but it cannot represent the physical capacity plateau that occurs when a sufficiently large pressure ratio drives a compressible vapor path to sonic conditions.

Phase F must eventually add conservative relief and bypass paths. Adding those paths before defining and testing the capacity law would mix geometry, topology, valve authority, source-term ownership and choked-flow calibration in one change. It would also make it difficult to distinguish an equation defect from an inventory-integration defect.

## Decision

F.1 introduces a separate ideal-vapor, one-way compressible capacity seam:

- `SpecificGasConstant` is a typed semantic quantity in J/(kg K);
- `CompressibleSteamFlowDefinition` owns full-open throat area, discharge coefficient, specific gas constant and heat-capacity ratio;
- `CompressibleSteamFlowSolver` accepts upstream absolute pressure/temperature, downstream absolute pressure and an effective-area fraction;
- subcritical flow follows the ideal isentropic pressure-ratio relation;
- flow is capped at the analytic sonic/choked capacity when the downstream/upstream pressure ratio reaches the critical value;
- zero opening or non-positive forward pressure head produces zero flow;
- the seam does not mutate `PlantState` and does not yet own any valve or topology.

The representative current-v2 audit uses 6.2725 MPa, 278.5 °C, 100 mm², discharge coefficient 0.95, water-vapor gas constant 461.526 J/(kg K) and heat-capacity ratio 1.3 only as deterministic numerical evidence. Those values are not yet a relief-valve or bypass sizing decision.

## Consequences

- The mathematical transition from subcritical to choked capacity can be validated independently.
- F.2 can compose conservative relief topology over a known capacity seam rather than inventing a second flow equation.
- Existing pipes, valves, turbine admission, plant inventories, protection, HMI and replay behavior remain unchanged in F.1.
- The model is explicitly ideal-vapor and reduced-order; it is not a homogeneous-equilibrium or non-equilibrium two-phase critical-flow model.
- Flow-work/enthalpy transport remains Phase G. F.1 therefore reports mass-flow capacity only and does not reinterpret the current internal-energy transport contract.
