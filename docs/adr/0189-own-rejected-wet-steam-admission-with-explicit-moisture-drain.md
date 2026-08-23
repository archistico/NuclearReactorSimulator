# ADR 0189 — Own rejected wet-steam admission with an explicit moisture drain

## Status

Proposed / M10 Final Long Failure Diagnostic 10 candidate.

## Context

ADR 0099 introduced `VaporMassFractionLimited` so liquid at turbine admission could not cross the work-producing stage as an unmodelled zero-work bypass. Diagnostic 9 now proves a complementary ownership defect in that abstraction: for wet steam, `commanded - effective = commanded * (1 - vapor fraction)` remains in `turbine-inlet` and matches the measured inlet inventory growth to within about `4.6e-5 kg/s` over the late census window.

A seed change cannot remove this residual. Restoring unrestricted total-mixture transport would also undo the safety intent of ADR 0099.

## Decision

Add a new opt-in `TurbineAdmissionPhasePolicy.VaporMassFractionLimitedWithMoistureDrain`.

Under this policy:

- commanded hydraulic admission remains unchanged;
- vapor mass fraction alone enters the work-producing turbine stage and exhaust path;
- rejected non-vapor mass is routed to an explicit canonical `MoistureDrainNodeId`;
- the drain node is mandatory and must differ from admission and exhaust nodes;
- saturated-mixture vapor/liquid transport energies are resolved from water/steam saturation properties at committed inlet pressure;
- only vapor flow produces shaft work;
- the inlet source term removes the sum of vapor and drained liquid mass/energy exactly once;
- stage energy ownership closes as inlet = exhaust + drain + shaft;
- trip blocks both vapor-stage and moisture-drain transfer;
- historical `LegacyUnrestricted` and `VaporMassFractionLimited` semantics remain unchanged.

Exact-v8 binds the moisture drain to the existing `hotwell` node. This is an educational lumped separator/drain owner, not a detailed moisture-separator/reheater model.

## Consequences

The repair preserves the D.1 prohibition on liquid traversing the work-producing stage while eliminating the unowned wet-steam mass reservoir. It may expose a new hotwell/feedwater or energy operating-point mismatch; therefore exact-v8 remains candidate-only until the 600 s Diagnostic 10 evidence shows bounded whole-cycle behavior. No production activation or replacement-long authorization follows from this ADR alone.
