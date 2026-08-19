# M10.9.4.1-H.28.1-A Static Review

## Baseline discipline

H.28.1-A is built directly on validated H.27 Hotfix 1. The failed H.28 package is not used as a code baseline; only its user-produced artifacts are frozen as diagnostic evidence.

## Runtime delta

The numerical equations and decisions are unchanged. Runtime source changes are observational instrumentation only:

- H.9 accumulates timing/allocation around existing layout, initial residual, Jacobian build, Newton line search and residual fallback phases;
- the four-node sidecar times predictor/corrector/disagreement/authority phases;
- the orchestrator times historical explicit preparation and post-sidecar commit/accounting;
- `ConditionalWeakTable` registries keep nondeterministic measurements outside deterministic record equality;
- attribution types/registries remain internal and are exposed only to the Application test assembly through `InternalsVisibleTo`.
- Hotfix 2 preserves the Simulation wall-clock ban: direct timing/allocation APIs are absent from Simulation; the focused Application test injects temporary readers through the internal `PerformanceAttributionMeasurement` scope.

No factory exposes a new numerical mode and no attribution value is consumed by numerical logic. Timestamp/allocation reads and weak-registry writes add diagnostic observer overhead, so the frozen H.28 evidence remains authoritative for aggregate performance magnitude while H.28.1-A is used for cost-center attribution.

## Required regression sentinels

- full ordinary suite;
- existing H.9 solver tests;
- H.26 public-constructor/telemetry-equality sentinel;
- validated-H.27 baseline-evidence fingerprint contract;
- failed-H.28 diagnostic-evidence fingerprint contract;
- fresh deterministic fingerprint equality.

## Static conclusion

Candidate is suitable for local compilation/test and focused attribution. It must not be interpreted as an H.28 performance pass or authorization to start H.29.
