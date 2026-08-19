# M10.9.4.1-H.28.1-A Hotfix 1 — Attribution Local-Variable Shadowing Compile Fix

## Status

CANDIDATE. Built directly on H.28.1-A, which itself is stacked on the validated H.27 Hotfix 1 baseline. H.28 remains failed performance evidence; H.29 remains blocked.

## Failure observed

The first local `dotnet build` of H.28.1-A failed in `FourNodeBranchContinuityShadowIntegrationSolver.Step()` with eight CS0136 errors. The new non-trigger attribution block declared locals named `authorityStartedTicks`, `authorityAllocatedBefore`, `authorityElapsedTicks`, `authorityAllocatedBytes`, `sidecarElapsedTicks`, `sidecarAllocatedBytes`, `result` and `attribution`. The triggered path later declared locals with the same names in the containing method scope. C# rejects that local-name shadowing even though the declarations are on mutually exclusive execution paths.

## Repair

Only the non-trigger locals are renamed:

- `noTriggerAuthorityStartedTicks`
- `noTriggerAuthorityAllocatedBefore`
- `noTriggerAuthorityElapsedTicks`
- `noTriggerAuthorityAllocatedBytes`
- `noTriggerSidecarElapsedTicks`
- `noTriggerSidecarAllocatedBytes`
- `noTriggerResult`
- `noTriggerAttribution`

The triggered-path names remain unchanged.

## Invariants

The repair does not change:

- predictor or trigger evaluation;
- timing/allocation formulas;
- attribution registry semantics;
- H.9 finite-difference Newton behavior or tolerances;
- H.20 authority decisions;
- H.22 commit decisions;
- P060/F040;
- 2%/5 K bounded hysteresis;
- four-node target set;
- physical coefficients;
- standard `ExplicitCommittedState` production mode at 10 ms.

## Validation

Run from repository root:

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-four-node-performance-attribution-audit.cmd
```

H.27 Hotfix 1 remains authoritative until this complete gate is reported green.
