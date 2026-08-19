# M10.9.4.1-H.26 Hotfix 1 — Focused Audit Proposed-Authority Contract Fix

**Status:** VALIDATED — 2026-08-19

## Failure observed

On 2026-08-19 the H.26 candidate passed `dotnet build` and the complete ordinary `dotnet test` suite (1170 total, 0 failed). The focused script then failed only in `FourNodeIntegratedRollbackFailClosedStressAuditTests` for the `shadow-correction-not-evaluated` challenge.

The failing assertion expected `telemetry.ProposedAuthority == ExplicitCommittedState`, but this challenge intentionally injects an H.20 decision with `ProposedAuthority=CorrectedCandidate`. H.22 then correctly denies the commit because `ShadowCorrectionEvaluated=false`, yielding `CorrectedCommitReason=ShadowCorrectionNotEvaluated`, `CorrectedCommitAuthorized=false`, `CorrectedCandidateCommitted=false` and physical same-step explicit fallback.

## Fix

The focused test now parameterizes the expected proposed authority. All denial/rollback challenges continue to expect `ExplicitCommittedState` except `shadow-correction-not-evaluated`, which correctly expects `CorrectedCandidate`. The final ownership requirements remain unchanged: no corrected commit and exact explicit physical fallback.

## Scope

No runtime file under `src/` is changed. H.20 authority semantics, H.22 commit semantics, P060/F040, H.9, 2%/5 K hysteresis, the four-node target set, fixed 10 ms step, physical coefficients and default `ExplicitCommittedState` remain unchanged.

## Validation

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-four-node-integrated-rollback-fail-closed-stress-audit.cmd
```

## Validation result

Build, complete ordinary tests and focused H.26 gate passed. The final audit recorded 12 challenges, 8 typed rollback challenges, 4 denial controls, 12/12 explicit fallback equivalence, zero corrected commits, zero partial-commit violations and deterministic repeat.
