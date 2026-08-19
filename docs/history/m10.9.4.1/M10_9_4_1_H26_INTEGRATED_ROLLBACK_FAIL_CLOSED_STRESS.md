# M10.9.4.1-H.26 — Integrated Rollback & Fail-Closed Stress Qualification

## Status

**VALIDATED** on 2026-08-19 as **M10.9.4.1-H.26 Hotfix 1**, built directly on user-validated **M10.9.4.1-H.25**.

## Purpose

H.20 validated the semantic mapping from failed guards to eight typed rollback reasons. H.22 then introduced actual corrected ownership, but H.22–H.25 observed no real H.20 rollback. H.26 closes that integration gap: it proves that the real `PlantNetworkOrchestrator` consumes typed H.20 denial/rollback decisions atomically and falls back to the historical explicit candidate in the same step.

H.26 does **not** retune or replace H.20, H.22, P060/F040, H.9, bounded 2%/5 K hysteresis, the four-node target set, physical coefficients or the 10 ms fixed step.

## Test-only integration seam

H.26 adds one `internal` constructor path to `PlantNetworkOrchestrator` that accepts an authority-decision transform. The public production constructor always supplies no transform, and standard factories cannot configure it.

This is deliberate audit infrastructure, not a production fault-injection feature. It allows an already-typed H.20 decision to be substituted immediately before the unchanged H.22 commit seam.

The separation of responsibility is preserved:

```text
H.20 supervisor
    owns observation -> typed authority reason

H.26 test hook
    injects an already-typed decision for integration stress only

H.22 commit seam
    must fail closed

PlantNetworkOrchestrator
    must apply the historical explicit candidate for the complete step
```

## Stress matrix

The focused gate verifies:

- natural `NotTriggered` control;
- `ActivationArmDisabled` denial;
- H.20 authority denied control;
- integrated `ShadowCorrectionNotEvaluated` denial;
- all eight H.20 rollback reasons:
  - qualification evidence unavailable;
  - corrector non-convergence;
  - line-search exhaustion;
  - pressure residual exceeded;
  - flow residual exceeded;
  - mass closure exceeded;
  - energy ownership exceeded;
  - untargeted branch disagreement.

The unchanged H.22 unit contract remains responsible for `CorrectedCandidateUnavailable` as a seam-level denial.

## Atomic fallback invariant

For every challenge:

```text
corrected ownership = 0
explicit ownership  = 100% of the network step
```

H.26 compares the physical candidate state, applied fluid balances and conservation audit against a separately evaluated historical `ExplicitCommittedState` reference for the same deterministic state and timestep.

A green challenge requires:

- exact typed H.20/H.22 reason;
- `CorrectedCommitAuthorized=false`;
- `CorrectedCandidateCommitted=false`;
- physical result equivalent to explicit fallback;
- no partial/mixed ownership;
- exact deterministic repeat.

## H.25 provenance

H.26 freezes the user-validated H.25 summary, full 837-row telemetry and metrics with canonical SHA-256 checks. H.24 is not rerun.

Validated H.25 entry evidence:

```text
scenarios                         5
runtime steps                     837
corrected commits                 178
H.20 rollback                       0
fallback-commit violations          0
unsafe corrected commits            0
focused duration                5m29s
```

## Decision

H.26 Hotfix 1 is green. Build, the complete ordinary suite and focused audit passed. The integrated path demonstrated 12/12 same-step explicit fallbacks across all eight typed H.20 rollback reasons plus four denial controls, with zero corrected/partial commits and deterministic repeat. It does not qualify off-design operation or production-default activation.

Next milestone: **H.27 — Off-Design Robustness & Qualification Envelope**.
