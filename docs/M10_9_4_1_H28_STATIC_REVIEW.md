# M10.9.4.1-H.28 Requalification 1 Static Review

## Scope

The package is built directly on the user-validated H.28.1-D Preflight Hotfix 1 source. It restores the original H.28 performance/cost/soak test and runner, freezes the validated H.28.1-D evidence, and updates metadata/documentation.

## Numerical isolation

No H.28 requalification change is made to:

- `PlantNetworkOrchestrator`;
- H.9 finite-difference corrector;
- H.20 supervisor;
- H.22 corrected commit seam;
- water/steam model;
- hydraulic solver;
- protection solvers;
- standard runtime factories.

The optimized runtime under test is exactly H.28.1-D. `ApplicationDescriptor.cs` is metadata only.

## Qualification contract

The original H.28 hard ceilings remain byte-for-byte numerically unchanged in the focused test: median wall ratio 8.0, p95 ratio 12.0 and median allocation ratio 16.0. Benchmark sizes remain 64 warmup + 256 paired steps; soak remains 1,536 steps; determinism control remains 128 steps repeated twice.

Wall-clock and allocation measurement remain in `Application.Tests`, not `Simulation`. Timing values do not enter deterministic fingerprints.

## Provenance

Four validated H.28.1-D evidence artifacts are copied into the test Evidence directory and canonical-SHA-256 checked before the explicit H.28 benchmark. H.27 frozen evidence remains checked as in the original H.28 gate.

## Long-horizon policy

H.24 is not run by this performance gate. If H.28 becomes green, H.24 must be repeated once before H.29 because runtime implementation changed during H.28.1-B/C/D.

## Validation limitation

Static review cannot substitute for local .NET build/analyzers/test execution. This package remains CANDIDATE until build, complete ordinary tests and the focused H.28 gate pass.
