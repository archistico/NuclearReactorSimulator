# M10.9.4.1-H.28.1-F — Jacobian Probe Coordinate-Residual Specialization

Status: **CANDIDATE**.

## Basis

Authoritative performance baseline remains H.28.1-D Preflight Hotfix 1 VALIDATED. H.28 Requalification 1 is FAILED only on p95. H.28.1-E compiled and its preliminary contracts passed, but the focused gate remained red: Jacobian 119.4045 ms and triggered p95 157.754 ms versus the unchanged H.28 readiness threshold 88.3812 ms. E is therefore frozen evidence, not a validated baseline.

## Change

The H.9 finite-difference Jacobian uses the normalized coordinate residual `(mappedCoordinates - appliedCoordinates) / scales`. In the legacy probe path, each probe also completed mapped thermodynamic integration and pressure/flow fixed-point merit calculation even though those values were discarded by the Jacobian builder. F introduces a probe-only coordinate-residual path. Applied fluid integration and hydraulic map evaluation remain exact and unchanged; mapped thermodynamic integration is omitted only for Jacobian probes. Full fixed-point residual evaluation remains unchanged for the initial residual, line search and residual fallback.

An internal `JacobianProbeResidualEvaluationMode.FullFixedPoint` is retained exclusively for tests. Public `JacobianHydraulicCorrectorSolver` construction selects `CoordinateOnly`.

## Frozen contract

- 32 finite-difference probes.
- 35 logical hydraulic evaluations.
- Jacobian dimension 32.
- Same H.9 Newton, scaling, diagonal regularization, damping, line search and tolerances.
- Same P060/F040 and 2%/5 K hysteresis.
- Same four target nodes.
- Same H.20 fail-closed authority and H.22 commit ownership.
- Same 10 ms simulated step.
- Expected deterministic fingerprint `518BA948637F0C270C7F8228AB97FEF9148E29A4F5CE4376319AB5D1CFBE7F38`.

## Performance gate

F is green only if the triggered p95 is at or below 88.3812 ms on the same machine-local evidence basis, with 20/20 trigger/commit, zero rollback/unsafe/fallback violation, exact fingerprint, and no material allocation/predictor regressions. H.28 thresholds are not changed.
