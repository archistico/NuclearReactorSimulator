# M10 Final — VR2 — R1 Selected C4 Opt-In Closure Implementation 1

## Status

**CANDIDATE — IMPLEMENTATION EVIDENCE ONLY**

Prerequisite: `R1-IMPLEMENTATION-PLANNING1` returned `PASS-AS-AUTHORED` and independently adjudicated PASS.

## Purpose

Stage the RP1C-selected C4 thermodynamic behavior in production behind the new explicit opt-in closure mode `WaterSteamThermodynamicClosureMode.ReferenceConsistentTabulatedInverseDomain = 2`. Existing closure modes 0/1, default construction, exact-v9 call sites, runtime defaults and historical exact identities remain unchanged.

## Production boundary

Exactly three existing production files may change: the Simulation project file, `WaterSteamThermodynamicClosureMode.cs`, and `SimplifiedWaterSteamThermodynamicModel.cs`. Exactly two production files may be added: `ReferenceConsistentTabulatedInverseResolver.cs` and `ReferenceData/NRSVR2C4.v1.bin`. No Application, Domain, Infrastructure, solution, scenario-factory or exact-v9 change is authorized.

## Reference-data contract

The production runtime does not execute the independent IF97 reference. IF97-derived data is generated offline by the C# test/reference generator and checked in as the embedded resource `NuclearReactorSimulator.Simulation.Physics.Fluids.ReferenceData.NRSVR2C4.v1.bin`. The payload uses `NRSVR2C4` schema 1, little-endian IEEE-754 binary64 raw values, and is anchored by the compiled SHA-256 constant `EF49B1D097FC63F1F1254E425F46C6ACA837B82C58EFBA9C7774727C51C82267`.

Hotfix 4 aligns that checked-in payload with the exact two-run, byte-identical C# regeneration returned from the qualified local Windows/.NET execution environment after Hotfix 3 quantified cross-environment last-bit drift in 429 floating-point fields. No IF97 equation, C4 source or runtime resolver algorithm is altered by this provenance alignment.

The first mode-2 resolver construction validates and decodes the payload into a static process-wide immutable cache. Resolve-time resource I/O, payload decoding and payload-driven allocation are forbidden.

## Equivalence gate

The focused R1 test must prove:

- payload regeneration is deterministic across two independent C# generations and byte-identical to the checked-in resource;
- the compiled SHA-256 anchor matches the payload;
- 1,679 frozen state comparisons are bit-equivalent to C4;
- 288 frozen hydraulic comparisons are bit-equivalent;
- state, hydraulic, repeat, resolve, resolution-path and model-integration mismatches are all zero;
- raw C4/resolver state equivalence remains bitwise in the frozen Celsius/MPa representation, while the model-integration check compares the Domain quantities in their canonical stored units (kelvins and pascals) so unit conversion itself is validated without requiring an impossible MPa round-trip bit identity;
- measured resolve-time allocation is zero;
- payload runtime I/O/decode counters remain zero;
- historical mode 0 remains identical to default construction;
- historical mode 1 remains source-identical to the Planning 1 baseline after removing only the three authorized mode-2 integration fragments; the reconstructed legacy source must match SHA-256 `93C5212C09D5D7362531398DE1CED105589D93DF9A892A3E6BE6BF401446D55E` exactly. The older density-derived 360-row comparison is retained only as diagnostic evidence because the frozen corpus stores density rather than the original specific-volume input, so reciprocal reconstruction is not bitwise authoritative;
- the ordinary Release suite passes with the R1 opt-in unset.

The final evidence tree contains exactly seven files.

## Authority boundary

This gate stages mode 2 only. It does not activate mode 2 by default, change exact-v9, create a new exact version, change production runtime configuration, alter thresholds, authorize VR3/P3-R1, or authorize a second replacement-long baseline. Returned R1 evidence must be independently adjudicated before opening R2 planning.
