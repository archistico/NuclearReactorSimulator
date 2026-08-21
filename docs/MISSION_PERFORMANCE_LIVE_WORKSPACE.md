# Mission & Performance live workspace

M10.9.7.3 Hotfix 1 REV2 activates the dedicated `MISSION` / `Mission & Performance` workspace chosen and validated in M10.9.7.2. The workspace is a read-only HMI consumer of M10.9.6 challenge, demand and scoring owners plus canonical control-room/protection evidence. It is not a new gameplay authority.

## Ownership boundary

The live path is:

```text
ScenarioSession deterministic evidence
        │
        ├─ ScenarioChallengeTracker / lifecycle owner
        ├─ ScenarioChallengeExternalDemandProjector
        ├─ OperationalChallengeScoreEvidenceProjector + ChallengeScoreCalculator
        ├─ plant-control authority snapshot
        └─ optional ScenarioRecorder events
                 ↓
MissionPerformanceLiveSnapshotSource
                 ↓
MissionPerformanceSnapshotProjector
                 ↓
explicit structural presentation comparison
                 ↓
MissionPerformanceViewModel
                 ↓
MISSION XAML workspace
```

The live source has no command dispatcher. `OPEN MISSION` changes only `SelectedWorkspace`. MISSION contains no plant command controls.

## Deterministic sampling versus presentation cadence

Demand/scoring evidence is accumulated on every deterministic completed step so presentation throttling cannot change the score input. Immutable HMI snapshots are published at the existing presentation cadence, plus same-step lifecycle/authority/assistance changes where operator-visible context changes immediately.

This deliberately separates simulation evidence cadence from UI refresh cadence.

`AdvanceRunning` emits every deterministic-step event before it emits that batch's presentation snapshots. Therefore an intermediate presentation snapshot can arrive after the demand/scoring timeline has already advanced beyond that step. The REV1 live-source fix, retained unchanged in Hotfix 1 REV2, treats such snapshots as stale presentation traffic and ignores them; it never rewinds deterministic evidence. A presentation snapshot ahead of deterministic evidence is a contract violation and fails closed.

## Explicit presentation change detection

`MissionPerformanceSnapshot` contains `IReadOnlyList<>` members. Generated record equality would compare those list references rather than their contents. M10.9.7.3 therefore uses `MissionPerformancePresentationComparer`, which compares scalar fields and sequence contents explicitly. Recreated-but-equivalent lists do not force a publication; a real scalar or element change does.

## Mission binding

The normal desktop startup does **not** infer a challenge pack from the current scenario. Some M10.9.6 packs share the same scenario identity, so scenario-to-pack inference would be ambiguous and would move gameplay semantics into presentation composition.

For deterministic/manual validation an exact pack may be supplied explicitly at startup:

```bat
dotnet run --project src\NuclearReactorSimulator.App\NuclearReactorSimulator.App.csproj -- --mission-pack=bounded-demand-following-5-10-5@1
```

Only exact IDs from `InitialOperationalChallengePack.All` are accepted. Unknown or duplicate `--mission-pack` selections fail closed. A normal startup has a live MISSION shell but presents `NO ACTIVE MISSION` until a caller binds a pack explicitly.

A user-facing challenge launcher is not introduced by 7.3.

## UI hierarchy

The primary workspace is intentionally ordered as:

1. mission/objective and lifecycle;
2. safety/protection significance plus assistance/authority context;
3. three independent values: `GRID DEMAND`, `REQUESTED LOAD`, `ACTUAL OUTPUT`;
4. score/classification and dimension decomposition;
5. bounded deterministic recent evidence.

Unavailable values remain `UNAVAILABLE`; they are never silently converted to zero.

## Session archive boundary

M10.9.7.2 Hotfix 3 REV1 validated command-payload persistence and adapter error contracts before this route was activated. M10.9.7.3 does not persist opaque challenge state and does not infer a selected pack from an archive. Reconstructing archive-restored mission binding and deterministic timeline/drill-down equivalence is M10.9.7.4 scope.

## Validation

Automated gate:

```bat
scripts\run-m10973-mission-performance-live-workspace-audit.cmd
```

Manual HMI acceptance:

`M10_9_7_3_MANUAL_VALIDATION_CHECKLIST.md`.
