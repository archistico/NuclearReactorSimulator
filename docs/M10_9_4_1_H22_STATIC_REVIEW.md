# M10.9.4.1-H.22 — Static Candidate Review

## Status

**STATIC REVIEW PASSED — 2026-08-18. Subsequent runtime validation also passed.**

This review checks source/document/evidence consistency only. The available environment does not contain the .NET SDK or a C# compiler, so it cannot establish build or runtime success.

## Checks performed

- H.22 is stacked on the H.21 Hotfix 1 source and documentation promotes H.21 to the user-reported validated result.
- The three frozen H.21 files reproduce the canonical SHA-256 fingerprints of the user-supplied H.21 focused artifacts.
- Standard current-v2 factory parameters default to `ExplicitCommittedState`; H.22 requires a dedicated audit-only opt-in factory path.
- H.5 hybrid, H.21 shadow integration and H.22 corrected commit selections are mutually exclusive in initial-condition construction.
- H.22 uses the same P060/F040, H.9 headline controls, 2% / 5 K branch continuity and four-node target contract.
- The H.20 supervisor is not broadened; H.22 adds a distinct commit seam and typed reasons.
- The historical explicit path is evaluated first and remains the fallback candidate.
- A corrected commit selects corrected H.9 fluid nodes, applied hydraulic balances and applied-iterate pump work, while non-hydraulic and thermal ownership stay on the existing orchestration boundary.
- H.21 telemetry positional construction remains source-compatible; H.22 commit fields are additive init properties.
- H.21 focused telemetry fingerprint computation does not include the new H.22-only init fields, preserving the validated H.21 regression contract.
- The H.22 focused audit does not incorrectly require the post-commit trigger count to stay at 15.
- The H.22 focused script chains the complete H.21 gate before the H.22 evidence run.
- H.22 includes ordinary seam tests for commit-arm disabled, H.20 arm disabled, H.20 rollback, H.20 authority denial, untriggered, correction-not-evaluated, missing-candidate and qualified-authority paths.

## Runtime validation addendum

The user subsequently ran the required local gate successfully:

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-four-node-corrected-commit-seam-audit.cmd
```

The focused result was 443/443 H.20-eligible/H.22-authorized corrected commits, zero rollback/fallback/unsafe/untargeted violations and exact repeat. The static review remains a package-time consistency record; H.22 promotion is based on the successful executable gate.
