# M10 Final — VR2 — R1 Implementation Planning 1

## Status

**PLANNING ONLY — selected C4 production staging contract.**

Returned `RP1C-ENGINEERING-REPAIR-SELECTION1` records the authored engineering decision `SELECT-C4`. This planning milestone does not implement or activate the repair. It freezes the bounded R1 production-integration design and the evidence required before R2 may open.

## 1. Selected repair and immutable history

Selected candidate:

```text
C4-ALLOCATION-NEUTRAL-VAPOR-SEAM-COMPLETE-SURROGATE
runtime qualification profile = AMBIENT-UNSET
```

The selected C4 shadow implementation remains frozen evidence. It is not moved, edited or reinterpreted by R1 planning.

Historical/current production semantics remain immutable:

```text
WaterSteamThermodynamicClosureMode.HistoricalCorrelationTopology = 0
WaterSteamThermodynamicClosureMode.CorrelationConsistentInverseDomain = 1
```

R1 plans a new explicit opt-in mode only:

```text
WaterSteamThermodynamicClosureMode.ReferenceConsistentTabulatedInverseDomain = 2
```

The default `SimplifiedWaterSteamThermodynamicModel()` constructor remains unchanged. Existing exact-v9 factories continue selecting `CorrelationConsistentInverseDomain`; no exact-v9 identity, save, recording or historical replay is reinterpreted.

## 2. Production boundary

R1 implementation may change only the bounded water/steam production area needed to introduce mode 2. The expected existing production files are:

- `src/NuclearReactorSimulator.Simulation/NuclearReactorSimulator.Simulation.csproj`;
- `WaterSteamThermodynamicClosureMode.cs`;
- `SimplifiedWaterSteamThermodynamicModel.cs`.

The project-file change is required only to declare the checked-in binary payload as an explicit `EmbeddedResource`; SDK default resource globs do not implicitly embed arbitrary binary files. New production-local components may be added only under `src/NuclearReactorSimulator.Simulation/Physics/Fluids`, and the payload lives under `Physics/Fluids/ReferenceData`. No Application scenario/factory call site may select the new mode during R1. No Domain, Infrastructure, solution-file or unrelated project-file behavior is changed.

## 3. Reference-data provenance contract

C4 was qualified as a bounded IF97-derived table surrogate, while the IF97 helper itself remains test/reference-only. R1 therefore must **not** copy the test-only IF97 implementation into the production runtime and must not regenerate IF97 tables during normal application startup.

The production mode uses a checked-in, versioned, bit-exact embedded data payload generated **offline in C#** from the existing independent test-only IF97 reference/generation path. The generator is owned by the Simulation test/reference area; PowerShell may only invoke the C# generator, and Python is not part of the generation contract. The generation step must emit a SHA-256 manifest and be reproducible in the dedicated test/reference path. Normal build and normal runtime do not implicitly regenerate the payload.

The binary contract is explicit rather than implementation-defined:

```text
magic             = NRSVR2C4
schema-version    = 1
byte-order        = little-endian
floating values   = IEEE-754 binary64 raw bits
integrity         = SHA-256 of the complete payload
resource path      = src/NuclearReactorSimulator.Simulation/Physics/Fluids/ReferenceData/NRSVR2C4.v1.bin
logical name       = NuclearReactorSimulator.Simulation.Physics.Fluids.ReferenceData.NRSVR2C4.v1.bin
hash anchor        = compiled C# constant in production source
load policy       = once per process / immutable thereafter
```

The runtime verifies schema and hash while constructing the first mode-2 resolver, before any `TryResolve` call. Lazy first-resolve loading is forbidden. The decoded payload is retained in a **static process-wide immutable cache**; every later mode-2 resolver reuses the same validated structures, so `LOAD-ONCE-PER-PROCESS` is literal rather than per-model-instance. The manifest-resource logical name is frozen as `NuclearReactorSimulator.Simulation.Physics.Fluids.ReferenceData.NRSVR2C4.v1.bin`; the expected SHA-256 is anchored in compiled production C# rather than trusted from a second mutable payload file. Any SHA-256 manifest emitted by the generator is an **R1 evidence/provenance output only, not a runtime authority file**. Its canonical returned representation is `03-reference-data-provenance.txt`. A missing, malformed or hash-invalid payload fails closed with `InvalidDataException`. Resolve-time file/resource I/O, payload decoding and payload-driven allocation are forbidden; the already-loaded immutable structures are used directly and are never mutated after resolver construction. No opaque hand-tuned constant is accepted without generation provenance.

## 4. Resolution precedence that must be preserved

The future implementation file set is frozen before coding. Existing production files that may change are exactly:

- `src/NuclearReactorSimulator.Simulation/NuclearReactorSimulator.Simulation.csproj`;
- `src/NuclearReactorSimulator.Simulation/Physics/Fluids/WaterSteamThermodynamicClosureMode.cs`;
- `src/NuclearReactorSimulator.Simulation/Physics/Fluids/SimplifiedWaterSteamThermodynamicModel.cs`.

The only new production files are:

- `src/NuclearReactorSimulator.Simulation/Physics/Fluids/ReferenceConsistentTabulatedInverseResolver.cs`;
- `src/NuclearReactorSimulator.Simulation/Physics/Fluids/ReferenceData/NRSVR2C4.v1.bin`.

The only new R1 test/reference files are:

- `tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/Reference/NrsVr2C4ReferencePayloadGenerator.cs`;
- `tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/Reference/R1SelectedC4OptInClosureImplementationTests.cs`.

That is an **exact modification allowlist**, not merely a directory suggestion. Existing files outside the three-item list may not change; new production/test files outside the named list require replanning.

The production R1 resolver preserves the selected C4 precedence exactly:

1. `C3-SUPERHEATED-VAPOR-SEAM`;
2. `C2-MIXTURE-PREFIX`;
3. `C2-LIQUID-TABLE-PREFIX`;
4. `C2-NEAR-BOUNDARY-LIQUID-PREFIX`;
5. immutable-C2 fallback-equivalent path for remaining supported states;
6. `C3-SATURATED-VAPOR-SEAM-FALLBACK`.

No node-id special case, inventory clamp, hidden latent-energy injection or fallback pressure is introduced.

Mode 2 must dispatch to its dedicated resolver **before** entering the existing mode-0/mode-1 resolution pipeline. It must not fall through to the current equality-based `CorrelationConsistentInverseDomain` branches or inherit a mixture of historical and mode-1 behavior. The old pipeline remains byte-for-byte attributable to modes 0 and 1 only.

## 5. R1 implementation acceptance boundary

The future implementation gate is `R1-SELECTED-C4-OPT-IN-CLOSURE-IMPLEMENTATION1`. It is an implementation/staging gate, not an activation gate.

It must prove at least:

- new mode numeric value is 2 and existing mode values/semantics remain unchanged;
- default constructor behavior is unchanged;
- exact-v9 composition/factories are unchanged;
- production code has no test-assembly or direct IF97 runtime dependency;
- checked-in reference payload provenance and regeneration are reproducible by generating twice into temporary locations, requiring byte-identical outputs, then comparing those bytes/hash to both the checked-in embedded payload and the compiled SHA-256 constant;
- production mode matches frozen C4 on the **exact frozen semantic corpus**, not merely a count of 1,967 comparisons: 1,679 state rows from `02-state-semantic-equivalence.csv` plus 288 hydraulic rows from `03-hydraulic-semantic-equivalence.csv`, with their frozen SHA-256 identities and unique row identities preserved;
- all 1,967 comparisons have zero state-bit, hydraulic-bit, repeat or resolve mismatches;
- mode-2 resolve execution performs no payload/resource I/O, no payload decoding and no resolve-time allocation;
- historical mode regression explicitly covers both mode `0` and mode `1`, and the ordinary Release suite remains green;
- no production default/runtime override or new exact identity is activated.

R1 does not need to repeat the large FDPC2 campaign. R2/R3 own focused thermodynamic/reference/topology and exact-v9-equivalent requalification after the production path exists.

## 6. Authority after R1 planning

A returned planning PASS may authorize implementation of the bounded R1 gate only. It does not authorize default activation, an exact-v9 change, a new exact version, VR3, P3-R1 or a second replacement-long baseline.

The successful implementation successor remains:

```text
R2-FOCUSED-THERMODYNAMIC-REFERENCE-TOPOLOGY-QUALIFICATION-PLANNING-ONLY
```

and only after returned R1 implementation adjudication.
