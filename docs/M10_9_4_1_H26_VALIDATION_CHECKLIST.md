# M10.9.4.1-H.26 Validation Checklist

## Preconditions

- [x] Source is stacked directly on validated H.25.
- [x] Standard current-v2 remains `ExplicitCommittedState` at 10 ms.
- [x] H.20 reason semantics and H.22 commit seam are unchanged.
- [x] H.24 is not rerun.
- [x] The H.26 decision transform is `internal` test infrastructure only and unreachable from standard factories.

## Commands

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-four-node-integrated-rollback-fail-closed-stress-audit.cmd
```

## Focused evidence

- [x] frozen H.25 summary/telemetry/metrics fingerprints pass;
- [x] frozen H.25 telemetry contains exactly 837 data rows;
- [x] standard factory remains explicit;
- [x] unchanged H.20 typed rollback reason tests pass;
- [x] unchanged H.22 commit seam denial tests pass;
- [x] public orchestrator path is identical to an identity audit-hook path;
- [x] natural untriggered control falls through explicit;
- [x] activation-arm-disabled denial falls through explicit;
- [x] H.20 authority-denied control falls through explicit;
- [x] shadow-correction-not-evaluated denial falls through explicit;
- [x] all 8 H.20 rollback reasons are exercised inside `PlantNetworkOrchestrator`;
- [x] explicit fallback physical equivalence passes for every challenge;
- [x] corrected commits = 0 across forced-denial/rollback challenges;
- [x] partial-commit violations = 0;
- [x] deterministic repeat passes;
- [x] `four-node-integrated-rollback-fail-closed-stress-passes=True`;
- [x] `h26-audit-passes=True`.

## Interpretation

H.26 is expected to create rollback deliberately. Rollback is success when it is typed, immediate and atomic. A single corrected or mixed commit during a rollback challenge fails the gate. A green H.26 advances to H.27 off-design robustness; it does not activate corrected ownership by default.


## H.26 Hotfix 1 validation note

The first H.26 candidate passed build and the complete ordinary suite on 2026-08-19. The focused gate failed only because the `ShadowCorrectionNotEvaluated` challenge asserted that H.20 `ProposedAuthority` must become explicit after H.22 denial. Hotfix 1 corrects that audit contract: the H.20 proposal remains `CorrectedCandidate`, while H.22 must deny commit and the applied physical state must be the same-step explicit fallback. H.26 Hotfix 1 passed the focused gate on 2026-08-19 and is now the validated baseline entering H.27.
