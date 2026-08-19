# M10.9.4.1-H.28.1-A — Corrected-Path Performance Attribution

## Status

**CANDIDATE**, built directly on user-validated **M10.9.4.1-H.27 Hotfix 1**.

H.28 itself is **not validated**. Its build and ordinary suite passed, but the focused performance/soak gate correctly failed as `unbounded-regression`. H.28.1-A freezes that failed evidence and diagnoses it; it does not inherit H.28 as a baseline.

## Why H.28.1-A exists

The failed H.28 gate established that numerical correctness remains green while corrected-path cost is unacceptable for default activation:

```text
median wall ratio                         9.1252571494799053
p95 wall ratio                          100.01553278882017
triggered average step               1,702,179.99 us
triggered average allocation          43,460,418 bytes
soak steps                                  1,536
soak commits                                  379
unsafe/fallback commits                         0
deterministic repeat                         True
```

Those aggregate ratios do not identify the implementation cost center. H.28.1-A answers that narrower question before any optimization is attempted.

## Non-negotiable scope

H.28.1-A does **not** change:

- P060/F040;
- H.9 finite-difference Newton mathematics or tolerances;
- bounded 2% pressure / 5 K previous-phase hysteresis;
- target set `steam|stop-out|header|turbine-inlet`;
- H.20 authority/rollback rules;
- H.22 corrected-commit ownership;
- protection logic;
- physical coefficients;
- the 10 ms simulated fixed step;
- the standard `ExplicitCommittedState` factory mode.

No performance ceiling is raised. H.29 remains blocked.

## Attribution model

For each corrected-path step H.28.1-A observes:

1. historical explicit fallback preparation already performed by `PlantNetworkOrchestrator`;
2. sidecar predictor / P060-F040 evaluation;
3. corrector setup wrapper;
4. H.9 coordinate-layout construction;
5. H.9 initial evaluation/residual construction;
6. H.9 finite-difference Jacobian build/probes;
7. H.9 damped-Newton line search;
8. H.9 residual-fallback line search;
9. remaining H.9 work;
10. untargeted branch-disagreement scan;
11. H.20 authority evaluation;
12. H.22 commit/accounting work.

It also records H.9's existing deterministic work counters:

- hydraulic evaluation count;
- probe evaluation count;
- maximum Jacobian dimension;
- Jacobian build attempts/acceptances/rejections;
- residual fallback attempts/acceptances;
- backtracking trials.

## Determinism-safe instrumentation

Timing and allocation values are deliberately **not embedded in deterministic result/telemetry records**. Existing validated tests compare some of those records by full value equality.

H.28.1-A stores attribution in `ConditionalWeakTable` registries keyed by object identity. The attribution types/registries remain `internal`; `NuclearReactorSimulator.Application.Tests` receives friend-assembly access solely to read this diagnostic evidence. This provides three properties:

- timing/allocation values cannot influence numerical equality;
- diagnostics do not extend telemetry/result lifetime;
- the simulation never reads attribution values when deciding trigger, authority or commit.

The instrumentation itself necessarily adds a small observer overhead from timestamp/allocation reads and weak-registry writes. H.28.1-A therefore uses the frozen H.28 measurements as the authoritative aggregate cost baseline and uses the new timings to attribute relative cost centers, not to replace H.28's absolute performance result. Named phase measurements are closed before registry writes wherever possible.

A fresh 128-step deterministic trace must still reproduce the failed-H.28 fingerprint:

```text
518BA948637F0C270C7F8228AB97FEF9148E29A4F5CE4376319AB5D1CFBE7F38
```

## Focused gate

The attribution run uses the same corrected benchmark manoeuvre as H.28:

```text
64 warmup steps
256 measured corrected steps
5 -> 0 -> 5 MWe manoeuvre repeated twice
128-step deterministic control
```

This is intentionally much shorter than the failed H.28 soak.

The gate has **no wall-time pass threshold**. It passes if:

- frozen validated-H.27 baseline evidence and failed-H.28 diagnostic evidence match exactly;
- attribution exists for every measured corrected step;
- trigger steps expose H.9 attribution and work counters;
- no unsafe/fallback commit occurs;
- closure/ownership remain within H.22 limits;
- no unexpected trip or untargeted disagreement occurs;
- deterministic fingerprint remains unchanged;
- standard current-v2 remains explicit.

## Evidence products

```text
artifacts/h28-1a-four-node-performance-attribution/
  00-progress.txt
  01-four-node-performance-attribution.summary.txt
  02-performance-attribution-steps.csv
  03-performance-attribution-cost-centers.csv
  04-performance-attribution-metrics.csv
```

The principal decision evidence is the primary wall-time and allocation cost center plus the H.9 Jacobian/probe share.

## Decision after H.28.1-A

- If duplicated predictor/non-trigger work dominates, proceed first with a conservative predictor-reuse optimization.
- If finite-difference Jacobian/probes dominate, optimize allocations/layout/buffers before considering mathematical changes.
- If the irreducible H.9 evaluation count remains dominant after conservative implementation cleanup, keep open the possibility that H.9 is too costly for production-default activation.

Do not change H.9 mathematics from H.28.1-A attribution alone.
