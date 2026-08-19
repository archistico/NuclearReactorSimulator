# M10.9.4.1-I.3 Hotfix 5 Compile Fix 1 — Recording Fingerprint Namespace Import

## Reported failure

The first local Hotfix 5 build failed only in `PhaseICorrectedHealthyReferenceRequalificationAuditTests.cs` with two CS0103 diagnostics because `ControlRoomSnapshotFingerprint` was referenced without importing its namespace.

`ControlRoomSnapshotFingerprint` is declared in `NuclearReactorSimulator.Application.Scenarios.Recording`. Existing validated gameplay audits import that namespace explicitly.

## Repair

The repair adds only:

```csharp
using NuclearReactorSimulator.Application.Scenarios.Recording;
```

No assertion, collector, runtime path, numerical contract, production policy or persistence behavior is changed.

## Validation contract

Run:

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-phase-i-corrected-300s-healthy-reference-requalification-audit.cmd
```

I.2 remains the authoritative validated baseline until the complete Hotfix 5 gate is green.
