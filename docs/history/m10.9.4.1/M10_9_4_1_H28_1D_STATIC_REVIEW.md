# M10.9.4.1-H.28.1-D static review

H.28.1-D is a CPU implementation optimization over validated H.28.1-B.

Static invariants checked at package time:

- no change to H.9 public result/telemetry records;
- no change to H.20 or H.22 authority/commit types;
- no change to finite-difference probe count or numerical options;
- exact-equality-only fluid-node reuse;
- coarse saturation cache contains the same 513 fixed scan temperatures and uses the unchanged saturation correlation to construct values;
- boundary-aware and bisection thermodynamic code remains dynamic;
- no direct wall-clock/timer API introduced into Simulation;
- frozen H.28.1-B evidence is included for provenance;
- H.28 remains failed and H.29 blocked.

This review is not a substitute for compilation, the ordinary suite or the focused H.28.1-D gate.

Package-time static verification additionally recorded:

- delta from validated H.28.1-B: 26 paths, 6 C# files, 4 paths under `src/`;
- the H.9 result record, H.21 sidecar step-result record and H.21/H.22 integration telemetry are byte-identical to H.28.1-B;
- `Simulation` contains zero forbidden wall-clock/timer tokens covered by the architecture rule;
- the H.28.1-D attribution-row constructor has the same 55 fields as its record declaration;
- frozen H.28.1-B summary/steps/cost-centers/metrics fingerprints match the user-validated artifacts;
- no `bin`, `obj` or generated audit artifact is part of the intended package.

The package environment has no .NET SDK, so these checks do not promote H.28.1-D; local build, ordinary tests and focused audit remain mandatory.
## Preflight Hotfix 1 addendum

A second compile-risk pass found and removed the unused `mappedProbeReuseFraction` local from the focused audit. The mapped reuse ratio remains emitted by the report code directly from the counters. No file under `src/` changed in this preflight hotfix.

