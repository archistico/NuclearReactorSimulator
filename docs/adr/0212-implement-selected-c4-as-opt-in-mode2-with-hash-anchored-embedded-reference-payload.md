# ADR-0212 — Implement selected C4 as opt-in mode 2 with a hash-anchored embedded reference payload

## Status

Accepted for R1 implementation evidence only.

## Context

RP1C selected C4 after green FDPC2. R1 Implementation Planning 1 returned `PASS-AS-AUTHORED` and freezes an implementation boundary that must preserve existing closure modes, default construction and exact-v9 while removing any production runtime dependency on the independent IF97 reference harness.

## Decision

Implement `WaterSteamThermodynamicClosureMode.ReferenceConsistentTabulatedInverseDomain = 2` as explicit opt-in only. Dispatch mode 2 to a dedicated `ReferenceConsistentTabulatedInverseResolver` before the existing mode-0/mode-1 pipeline. Do not permit fallthrough into equality-based historical/current branches.

Generate the selected C4 reference tables offline in C# and embed them as `ReferenceData/NRSVR2C4.v1.bin` with logical name `NuclearReactorSimulator.Simulation.Physics.Fluids.ReferenceData.NRSVR2C4.v1.bin`. Production source owns the expected SHA-256 as a compiled constant. The resource is validated and decoded once into a static process-wide immutable cache when the first mode-2 resolver is constructed. Resolve-time resource I/O, payload decoding and payload-driven allocation are forbidden.

Qualification is bound to the frozen 1,679-state + 288-hydraulic C4 corpus and requires bit-equivalent production results, repeatability, path equivalence, model integration equivalence, historical mode 0/1 regression and ordinary Release PASS.

## Consequences

- mode 2 exists in production code but remains opt-in and unused by current exact-v9/default call sites;
- the independent IF97 harness stays test/reference-only;
- historical closure semantics remain interpretable;
- returned R1 evidence must be adjudicated before any R2 planning;
- no default activation, exact-version change, production runtime override, VR3, P3-R1 or replacement-long authority follows from this ADR.
