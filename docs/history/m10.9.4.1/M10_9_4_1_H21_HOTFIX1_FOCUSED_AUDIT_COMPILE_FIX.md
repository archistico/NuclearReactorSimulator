# M10.9.4.1-H.21 Hotfix 1 — Focused Audit Local-Variable Shadowing Compile Fix

## Status

**VALIDATED — 2026-08-18**, stacked directly on H.21 Documentation Static Review 1. At candidate-package time H.20 was the validated baseline; after the successful post-fix build/test/focused gate, **H.21 Hotfix 1 became the authoritative validated baseline**.

## Local failure evidence

The user's first local build on 2026-08-18 compiled the Domain, Simulation, Application, Infrastructure, App and their other test projects, but `NuclearReactorSimulator.Application.Tests` failed with:

```text
FourNodeOrchestratorShadowIntegrationAuditTests.cs(92,17): error CS0136
Non è possibile dichiarare in questo ambito una variabile locale o un parametro denominato 'repeatFingerprint' perché tale nome viene usato in un ambito locale di inclusione per definire una variabile locale o un parametro
```

## Cause

Inside the 2,000-interval lockstep loop, the audit declared:

```csharp
var repeatFingerprint = ControlRoomSnapshotFingerprint.Compute(repeatPresentation);
```

Later in the same test method, the aggregate repeat telemetry fingerprint was also declared as `repeatFingerprint`. C# local-variable declaration-space rules reject this nested/containing-scope name reuse.

## Fix

Rename only the loop-local presentation value:

```csharp
var repeatPresentationFingerprint = ControlRoomSnapshotFingerprint.Compute(repeatPresentation);
```

and update its two subsequent uses. The later aggregate `repeatFingerprint = Fingerprint(repeatRows)` remains unchanged.

## Non-changes

Hotfix 1 does not change:

- `PlantNetworkOrchestrator` or any production source behavior;
- P060/F040;
- H.9;
- the 2% / 5 K bounded previous-phase hysteresis;
- target nodes `steam|stop-out|header|turbine-inlet`;
- H.20 authority/rollback logic;
- residual, closure or ownership guards;
- expected 2,000-interval H.21 evidence;
- H.19/H.20 prerequisite gates;
- the 10 ms production timestep;
- corrected-candidate commit prohibition.

## Validation

Run from repository root:

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-four-node-orchestrator-shadow-integration-audit.cmd
```

H.21 Hotfix 1 must not be promoted until all three gates pass.

## Post-hotfix validation

The user reran the local gate after this compile-only rename. `dotnet build`, the complete `dotnet test` suite and the cumulative H.21 focused gate all passed. H.21 Hotfix 1 is therefore the authoritative validated baseline for H.22.
