# M10.9.4.1-H.23 Hotfix 1 — Focused Audit Domain.Plant Namespace Compile Fix

## Status

**CANDIDATE.** H.22 remains the authoritative validated baseline.

## Observed local failure

The user's first local H.23 build on 2026-08-18 compiled every project except `NuclearReactorSimulator.Application.Tests`, which failed with:

```text
FourNodeCommittedReplayProtectionQualificationAuditTests.cs(342,9): error CS0246:
HydraulicNumericalCouplingMode could not be found.
```

## Root cause

`HydraulicNumericalCouplingMode` is declared in `NuclearReactorSimulator.Domain.Plant`. The new H.23 focused audit referenced the type but did not include the namespace import already present in the H.21/H.22 focused audits.

## Hotfix

Only this line is added to `FourNodeCommittedReplayProtectionQualificationAuditTests.cs`:

```csharp
using NuclearReactorSimulator.Domain.Plant;
```

No runtime source, calculation, assertion, H.20/H.22 authority rule, replay/checkpoint/protection scenario, frozen evidence, tolerance, trigger, hysteresis, physical coefficient or standard production factory is changed.

## Validation gate

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-four-node-committed-replay-protection-qualification-audit.cmd
```

H.23 Hotfix 1 may be promoted only after all three executable gates pass locally.
