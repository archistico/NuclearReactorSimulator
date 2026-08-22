# M10 Final Long Failure Diagnostic 1

## Status

The first M10 final long campaign is **FAILED / ABORTED AFTER EVIDENCE COLLECTION**. LR-H1 failed with a production-path `WaterSteamStateOutOfRangeException` at node `outlet`. LR-M1 was manually stopped after reaching logical step 360000 / 440000 because wall cost had become operationally unacceptable.

This diagnostic does **not** modify production runtime, thermodynamic support, I.3 budgets, conservation ceilings, exact-version identities or the failed long evidence.

## Evidence from the aborted campaign

LR-H1 produced:

- node: `outlet`;
- `v = 0.0026153411609661885 m^3/kg`;
- `u = 1615124.4119888516 J/kg`;
- one unsupported-envelope excursion;
- no preceding unexpected trip/fault in the preserved classifier artifact.

The progress artifact contains only the 300 s LR-H1 checkpoint before failure, therefore the failure occurred after 300 s but before the next 600 s checkpoint. The exact failing logical step was not persisted by the original harness.

LR-M1 reached 3600 / 4400 simulated seconds. Equal 300 s simulated chunks became progressively more expensive, rising from about ten minutes early in the leg to about thirty-six minutes for the 3300->3600 s chunk. This is not a constant per-step cost.

## Static LR-M1 root-cause finding

The current live MISSION read path contains deterministic full-prefix scans at every `SingleStep` presentation:

1. `ControlRoomRuntimeCoordinator.Dispatch(SingleStep)` publishes both `DeterministicStepCompleted` and `SnapshotChanged` for every logical step.
2. `MissionPerformanceLiveSnapshotSource.OnDeterministicStepCompleted` appends one `ExternalEnergyDemandEvidenceSnapshot` per step to `_demandTimeline`.
3. `OnPresentationSnapshotChanged -> RefreshLocked -> BuildCurrent` runs on every step.
4. `OperationalChallengeScoreEvidenceProjector.ProjectLive` validates strict ordering by iterating the complete timeline, then `Demand(...)` filters/materializes the complete timeline and calculates aggregate averages.
5. `MissionPerformanceTimelineProjector.AddDemandChanges` again iterates the complete demand timeline even though the retained presentation output needs only demand change points.

Therefore step `n` performs O(n) prefix work and a long session performs O(n^2) aggregate projection work. The observed increasing 300 s chunk times are consistent with this code path.

This finding classifies the LR-M1 wall-cost issue as an **Application/MISSION live-projection scalability defect**, not a plant-physics cost increase. A production correction is not yet included in Diagnostic 1; exact live/replay scoring and timeline semantics must be preserved by an incremental replacement.

## Diagnostic 1 goals

### A. LR-H1 300 s equilibrium residual census

Replay only the already-validated 300 s exact-v4 domain and collect every second for every canonical fluid node:

- mass;
- internal energy;
- specific internal energy;
- specific volume;
- pressure;
- temperature;
- phase / vapor quality.

For the final 60 s, calculate node-by-node linear slopes. For `outlet`, preserve the distance from the observed LR-H1 failure coordinates. The purpose is to determine whether the pre-failure state already contains a secular drift toward the later unsupported point.

### B. LR-M1 projector prefix-scaling census

Do not run the plant for thousands of seconds. Feed deterministic synthetic demand histories of increasing lengths through the current score and timeline projectors and record wall cost/allocation. This isolates the already identified prefix-rescan path from plant physics and challenge command behavior.

## Next decision

After Diagnostic 1 artifacts are available:

- if LR-H1 shows clear secular `outlet`/inventory drift inside 240..300 s, proceed to the minimum equilibrium/owner residual census needed to identify the source term or controller bias before modifying physics;
- if the 300 s state is effectively flat but close to the failure point, investigate thermodynamic support / late branch transition and hydraulic-coupling behavior;
- for LR-M1, design an incremental live evidence accumulator/change-point projection that is exactly equivalent to current score/timeline semantics, then prove equivalence against full-prefix projection before activation.

No new final long campaign is authorized until both blockers are resolved. The replacement final long validation must target 35-45 minutes on the validation workstation and must be operationally capped at 60 minutes; this wall budget is not a physics tolerance.
