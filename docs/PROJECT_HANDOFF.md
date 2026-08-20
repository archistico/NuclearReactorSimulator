# Project handoff

## Validated baseline

H.30 Requalification 1 is validated with `ACTIVATE`: exact v3 corrected-commit is the authoritative desktop default and exact v2 explicit remains rollback/reference.

I.3 Hotfix 2 is validated. Its authoritative 300 s / 30,000-step production reference freezes seven final-window slopes and 19 internal regression budgets with zero generation-health or targeted reverse-flow violations.

## Current candidate

`M10.9.4.1-I.4 — Known Limitations & Legacy Retirement Review`.

The focused review:

- verifies compact I.3 frozen evidence;
- records the validated non-zero final-window drift observations as current limitations;
- verifies H.5/H.21 modes have no production/exact-version/current-CI dependency;
- counts their remaining source/test seams;
- deliberately defers physical source deletion.

## Validate

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-phase-i-known-limitations-legacy-retirement-review-audit.cmd
```

Expected focused flags:

```text
phase-i-known-limitations-review-passes=True
phase-i-legacy-retirement-review-passes=True
i4-audit-passes=True
i5-closure-gate-unblocked=True
```

## Packaging rule

Do not bundle `tests\NuclearReactorSimulator.Application.Tests\Scenarios\Gameplay\Evidence`, `artifacts`, `bin` or `obj` into candidate ZIPs. Keep the compact immutable store under `eng\frozen-evidence\ordinary` bounded; large trace identities belong in the hash manifest rather than source packages.

## After I.4

Proceed to I.5 cumulative M10.9.4.1 closure. Do not begin M10.9.5 until I.5 is green.
