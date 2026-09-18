# ADR-0211 — Stage selected C4 behind a new opt-in production closure mode before requalification

## Status

Accepted for R1 implementation planning only.

## Context

RP1C Engineering Repair Selection 1 returned `SELECT-C4`. C4 is a test/reference/shadow implementation whose qualified behavior depends on IF97-derived tables, while ADR-0194 requires historical exact-v9 semantics and the existing `CorrelationConsistentInverseDomain` production behavior to remain immutable.

## Decision

Plan R1 as a bounded production staging change behind a new explicit `WaterSteamThermodynamicClosureMode.ReferenceConsistentTabulatedInverseDomain = 2` value. Preserve values 0 and 1, the default constructor and all exact-v9 call sites. Do not activate the new mode by default.

The selected IF97-derived table data is generated offline in C# from the independent test/reference path, checked in as a versioned bit-exact embedded payload and accompanied by reproducible SHA-256 provenance. The binary contract is `NRSVR2C4` schema 1, little-endian IEEE-754 binary64 raw bits, loaded once and SHA-256 validated before use. `NuclearReactorSimulator.Simulation.csproj` is therefore an explicit R1 modification boundary for the `EmbeddedResource` declaration. Production runtime must not depend on the test assembly or execute the independent IF97 reference to construct tables.

Mode 2 owns a dedicated resolver dispatch before the existing mode-0/mode-1 branch pipeline. It may not fall through into the equality-based historical/current branches. Resolve-time resource I/O, payload decoding and allocation are forbidden.

R1 production-vs-shadow equivalence is bound to the exact frozen 1,679 state + 288 hydraulic semantic corpus and its hashes; a replacement corpus with the same total row count is not sufficient.

## Consequences

- R1 may stage production code only after returned planning adjudication;
- C4 shadow evidence remains immutable and becomes the equivalence oracle for the production mode;
- exact-v9 and historical saves/recordings remain interpretable under their original closure semantics;
- R2/R3 requalification is mandatory before any versioned activation decision;
- no default switch, new exact identity, VR3, P3-R1 or replacement-long authority follows from this ADR.

### Review-hardening clarification

The future binary resource path and manifest logical name are frozen as `src/NuclearReactorSimulator.Simulation/Physics/Fluids/ReferenceData/NRSVR2C4.v1.bin` and `NuclearReactorSimulator.Simulation.Physics.Fluids.ReferenceData.NRSVR2C4.v1.bin`. The expected payload SHA-256 must be anchored in compiled production C#; a sibling manifest file is not a trusted runtime hash authority.

The payload is loaded, schema/hash-validated and decoded exactly once per process when the first mode-2 resolver is constructed; the resulting structures live in a static process-wide immutable cache reused by every later mode-2 resolver. Lazy first-resolve payload loading is forbidden. Generator SHA-256 manifest output is evidence/provenance only and is never a runtime authority. The contract treats the three listed existing production files as an exact modification allowlist and freezes the only permitted new files: `ReferenceConsistentTabulatedInverseResolver.cs`, `ReferenceData/NRSVR2C4.v1.bin`, `NrsVr2C4ReferencePayloadGenerator.cs` and `R1SelectedC4OptInClosureImplementationTests.cs`. Offline generation uses invariant culture and canonical ordering and must regenerate twice into temporary locations with byte-identical output before comparing against the checked-in payload and compiled SHA-256 constant.
