# M10.9.4.1-H.24 Requalification 1 — Post-H.28 Committed Long-Horizon & Cross-Profile Regression

## Status

**VALIDATED on 2026-08-19.** Compilation, complete ordinary tests and the focused post-H.28 long-horizon/cross-profile gate passed.

## Purpose

H.28 is now validated after the H.28.1 optimization branch. Because that branch changed committed-runtime implementation code, the Phase H roadmap requires one and only one rerun of the rare H.24 long-horizon/cross-profile committed-path qualification after performance optimization has stabilized and before H.29 can begin.

This requalification did not introduce another numerical algorithm and did not retune the plant. It reruns the original H.24 operational domain against the exact H.28-validated optimized runtime.

## Frozen prerequisite

The user-supplied green H.28 artifacts are copied into the ordinary-test Evidence directory and fingerprint-checked before the long gate:

```text
H28_ValidatedPerformanceCostSoakSummary.txt
H28_ValidatedPerformanceBenchmark.csv
H28_ValidatedOperationalSoakSamples.csv
H28_ValidatedPerformanceCostSoakMetrics.csv
```

The frozen H.28 result includes:

```text
median wall-cost ratio       4.6214685710690242 <= 8
p95 wall-cost ratio         10.684444741413872  <= 12
median allocation ratio      1.1164372201028363 <= 16
corrected trigger/commit     20/20
soak trigger/commit          379/379
rollback/fallback/unsafe     0/0/0
untargeted disagreements     0
deterministic repeat         True
fingerprint                  518BA948637F0C270C7F8228AB97FEF9148E29A4F5CE4376319AB5D1CFBE7F38
performance class            bounded-but-costly
```

## Requalification domain

The operational geometry is exactly the H.24/H.19 nominal domain:

```text
steady-long             12,000 intervals
load-pulse               6,000 intervals
cooling-pulse            6,000 intervals
combined-load-cooling    6,000 intervals
TOTAL                    30,000 qualification intervals
transition steps              8
fixed step                   10 ms
```

The gate does not freeze the historical H.24 trigger/commit count. Optimized runtime implementation may alter execution cost, but numerical behavior must still satisfy the same safety, authority, ownership, conservation and determinism contracts.

## Frozen numerical contract

This requalification does not change:

- `ExplicitCommittedState` as the standard/default current-v2 mode;
- `FourNodeBranchContinuityCorrectedCommitOptIn` as the separately opt-in corrected mode;
- the 10 ms production fixed step;
- P060/F040 triggering;
- H.9 finite-difference Jacobian + damped Newton mathematics;
- H.20 fail-closed authority and typed rollback behavior;
- H.22 corrected-candidate commit ownership;
- 2% pressure / 5 K bounded branch-continuity hysteresis;
- target nodes `steam|stop-out|header|turbine-inlet`;
- physical coefficients.

## Pass contract

The focused gate passes only if:

- all four profiles complete without trip;
- every profile observes at least one P060/F040 trigger and corrected commit;
- every corrected commit is eligible/authorized under H.20/H.22;
- fallback-commit violations = 0;
- unsafe corrected commits = 0;
- untargeted branch disagreements = 0;
- conservation/accounting residuals remain within the original H.24 limits;
- the 256-interval determinism control repeats exactly;
- the standard factory remains `ExplicitCommittedState`.

A safe H.20 rollback is permitted if it remains fully explicit in that interval.

## Boundary after a green result

A green result closes the single post-optimization H.24 regression required by the roadmap. H.29 may then begin from the validated H.24–H.28 evidence chain, but default activation is still not authorized. H.29 must explicitly evaluate whether the `bounded-but-costly` corrected path is suitable as a production-default candidate; H.30 owns the final Phase H decision.


## Validated result

```text
qualification-intervals=30000
action-transition-steps=8
committed-runtime-steps=30008
P060-F040-triggered=9626
H20-candidate-eligible=9626
H22-commit-authorized=9626
corrected-candidates-committed=9626
H20-rollbacks=0
safe-fallback-intervals=0
fallback-commit-violations=0
unsafe-corrected-commits=0
untargeted-branch-disagreements=0
deterministic-control-repeat=True
committed-telemetry-fingerprint=7AF233CE51A866B3E00C2C032AA58EEFBD7290DE0940725E5F4B7860EA6287BE
post-h28-four-node-committed-long-horizon-cross-profile-requalification-passes=True
h24-post-h28-requalification-audit-passes=True
```

All four profiles completed without trip. The single post-optimization H.24 regression is closed and H.29 is unblocked.
