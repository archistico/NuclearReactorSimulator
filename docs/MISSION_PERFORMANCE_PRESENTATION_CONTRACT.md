# Mission / Performance Presentation Contract

M10.9.7.1 introduces a read-only Application presentation seam for the future Mission & Performance workstation. It does not move any M10.9.6 semantic owner into the UI.

## Ownership

`MissionPerformanceSnapshotProjector` aggregates exact `OperationalChallengePackDefinition`, `ChallengeLifecycleSnapshot`, current `ControlRoomSnapshot`, same-step `ExternalEnergyDemandEvidenceSnapshot`, optional `ChallengeScoreEvaluationResult`, `TrainingGuidanceMode`, existing `PlantControlAuthorityPresentationSnapshot` and optional canonical `ScenarioRecordingEvent` evidence. A terminal lifecycle may remain frozen at its canonical terminal step; the shared read-only alignment helper projects that terminal state as-of the current evidence step without changing `TerminalLogicalStep`. Non-terminal step mismatches fail closed.

Challenge transitions remain M10.9.6.1-owned, external demand remains M10.9.6.2-owned, score arithmetic and dominance remain M10.9.6.3-owned, exact pack bindings remain M10.9.6.4-owned, and reconstruction remains M10.9.6.5-owned. The presentation projector copies and arranges those results only.

## Semantic separation

The contract preserves three independent electrical values: external grid demand, generator requested load and actual electrical output. External demand can be unavailable while requested load and actual output are still projected from the current canonical control-room snapshot. Demand error is present only when owned external-demand evidence provides it.

## Logical time and events

Elapsed progress is represented in deterministic logical steps. No `DateTime`, `DateTimeOffset` or `TimeSpan` participates in authoritative presentation state. Objective metadata comes from the matched `ScenarioObjectiveDefinition`, while objective events come from lifecycle transitions. Protection evidence may be copied from canonical recorder protection-transition events only when `event.LogicalStep <= presentation.LogicalStep`; future evidence is never surfaced. `RecentEvents` retains at most the 100 newest deterministically ordered objective/protection/scoring events for the live at-a-glance panel. It is not the full timeline owner: M10.9.7.4 must retain a separately bounded mission lifecycle spine so sparse activation/terminal/reset transitions cannot be evicted merely by dense protection/scoring evidence, then merge that spine with bounded recent operational evidence under deterministic ordering.

## Hotfix 2 pre-workstation hardening

Before workstation wiring, M10.9.7.1 Hotfix 2 removes two cross-boundary failure modes. `ChallengeLifecycleLogicalStepAlignment` is the single Application owner for the M10.9.6.5 terminal as-of-step rule and is reused by replay, external-demand and Mission/Performance projection. `ControlRoomElectricalEvidence` is the single read-only aggregate owner for requested generator load used by demand and presentation projectors.

Session-archive parsing is also hardened at the external-data boundary. Blank, truncated JSON and structurally invalid archive records are normalized to `InvalidDataException`; unsupported future archive schema versions remain `NotSupportedException`. The UI load seam handles normalized data failures plus `ArgumentException`, `KeyNotFoundException` and `OverflowException` defensively rather than allowing malformed user-selected content to escape an `async void` handler.

## Assistance and control authority

Training assistance and plant-control authority are displayed as separate observational fields. Requested and effective plant authority may differ. The presentation layer has no dispatcher, runtime engine, controller or protection mutator.

## Navigation non-decision

M10.9.7.1 deliberately does not alter the validated Operator Computer F1–F8 catalog and does not create a workstation. M10.9.7.2 must explicitly choose workstation placement before UI work begins.
