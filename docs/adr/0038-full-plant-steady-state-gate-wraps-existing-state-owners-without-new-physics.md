# ADR 0038 — Full-plant steady-state gate wraps existing state owners without new physics

## Status

Accepted for M4.7 baseline candidate.

## Context

By M4.6 the reactor-to-grid physical path already spans the canonical plant network, turbine rotor mechanics and generator/grid electrical state. The final M4 gate needs one headless state/snapshot boundary, a configurable reference operating point, long-run drift verification and plant-performance diagnostics before M5 automatic control is introduced.

Creating a fourth independent state model or a new integration boundary would duplicate ownership and risk breaking the conservation architecture established in M3/M4.

## Decision

- Introduce `FullPlantState` only as an immutable envelope over `PlantState`, `TurbineExpansionState` and `GeneratorGridState`.
- Introduce `FullPlantSolver` as a thin delegate over the M4.6 `IntegratedSecondaryCycleSolver`; it performs no independent physical integration.
- Introduce `FullPlantSnapshot` as the canonical true-state observation boundary for later instrumentation.
- Define fixed-input `FullPlantReferenceOperatingPoint` and explicit `FullPlantSteadyStateCriteria`.
- Measure long-run drift with `FullPlantLongRunRunner` without state resets, trims or corrective bookkeeping.
- Derive performance ratios only from already audited powers; zero-denominator ratios remain undefined.
- Keep all automatic control, synchronization, AVR/governor and protection logic outside M4.7.

## Consequences

- M4 closes with one deterministic reactor-to-grid state/snapshot seam suitable for M5.
- Drift failures remain observable instead of being hidden by a steady-state solver that edits state.
- Existing subsystem ownership and the single thermofluid integration boundary remain intact.
- Future sensors/controllers can consume a stable top-level snapshot without coupling to internal solver order.
