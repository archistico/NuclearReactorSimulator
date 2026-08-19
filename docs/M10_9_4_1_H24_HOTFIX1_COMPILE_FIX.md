# M10.9.4.1-H.24 Hotfix 1 — Focused Audit Recording-Namespace Compile Fix

## Status

**VALIDATED** on 2026-08-19 after build, complete ordinary suite and focused H.24 gate passed.

## Observed failure

The first local H.24 build on 2026-08-18 failed only in `NuclearReactorSimulator.Application.Tests`:

```text
FourNodeCommittedLongHorizonCrossProfileQualificationAuditTests.cs(315,13):
error CS0103: The name 'ControlRoomSnapshotFingerprint' does not exist in the current context
```

All reported non-Application.Tests projects compiled successfully.

## Root cause

`ControlRoomSnapshotFingerprint` is declared in:

```text
NuclearReactorSimulator.Application.Scenarios.Recording
```

The H.24 focused audit imported `Application.ControlRoom` and the other required namespaces, but omitted `Application.Scenarios.Recording`. Existing H.22/H.23 audit tests that use the same fingerprint type already import this namespace.

## Fix

Hotfix 1 adds exactly:

```csharp
using NuclearReactorSimulator.Application.Scenarios.Recording;
```

to `FourNodeCommittedLongHorizonCrossProfileQualificationAuditTests.cs`.

No calculation, assertion, profile definition, evidence fingerprint, runtime numerical source, H.20 authority rule, H.22 commit seam, P060/F040 trigger, H.9 tolerance, 2%/5 K hysteresis limit, target node set, coefficient or standard factory changes.

## Validation required

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-four-node-committed-long-horizon-cross-profile-qualification-audit.cmd
```

H.24 may be promoted only after all three executable gates pass locally.


## Validation result

Hotfix 1 compiled and the complete suite passed. The focused H.24 gate also passed with 30,008 runtime steps, 9,626 corrected commits, all four profiles trip-free and zero unsafe/fallback-commit violations.
