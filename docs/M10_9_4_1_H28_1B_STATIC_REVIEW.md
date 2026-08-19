# M10.9.4.1-H.28.1-B — Static Review

H.28.1-B is an implementation-only duplicate-work reduction over validated H.28.1-C Hotfix 2.

Static invariants:

- historical explicit solve remains first and is still the immediate fallback;
- committed hydraulic balances/flow maps are retained from already-executed pipe/valve/pump solves;
- historical explicit fluid-node states are materialized once before the sidecar;
- the sidecar reconstructs the canonical H.4 total balance exactly as before;
- a historical node is reused only when its actually-applied total balance exactly equals the canonical H.4 balance;
- any mismatched node is reintegrated through the unchanged H.4 predictor path;
- the legacy public predictor path remains available and unchanged for existing H.4/H.5/H.21 contracts;
- the optimized reuse path is internal;
- the end-of-predictor evaluation remains mandatory and unchanged;
- H.9/H.20/H.22 deterministic records are not extended with performance fields;
- reuse counters live only in diagnostic attribution;
- no direct wall-clock API is introduced into Simulation;
- standard factories remain explicit.

The focused gate must provide runtime proof of exact trigger/trajectory equivalence, real node reuse and material predictor-cost reduction.

## Package-time static delta

Against validated H.28.1-C Hotfix 2, the candidate changes/adds 31 paths, including 9 C# files and 6 paths under `src/`. The numerical change is limited to historical-predictor exact reuse plumbing plus diagnostic reuse counters and milestone metadata/tests. Frozen H.28.1-C evidence remains canonical-fingerprint identical. The three deterministic H.9/H.21/H.22 result/telemetry record files remain byte-identical to the validated base. No `bin`, `obj` or `artifacts` directories are packaged.
