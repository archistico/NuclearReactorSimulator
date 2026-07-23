# Gameplay Long-Running System Tests

## Purpose

The ordinary suite should remain fast enough to run after normal changes. Full operator-journey/endurance checks are therefore explicit opt-in tests.

M10.9.4 introduces `GameplayJourneyLongRunningTests` to exercise the turbine → shaft → generator → grid path over meaningful simulated time rather than only one or two fixed steps.

## Why this exists

The previous desktop continuity regression ran 1,000 steps / 10 simulated seconds and asserted finite rotor speed, but it did not assert sustained electrical export. A manual session exposed that MWe could later reach zero, so sustained power-path behavior now has a dedicated acceptance gate.

## Running

Normal suite:

```text
dotnet test --no-build
```

Long gameplay/system tests only:

```text
scripts\run-gameplay-long-tests.cmd
```

PowerShell:

```text
.\scripts\run-gameplay-long-tests.ps1
```

Direct invocation:

```text
dotnet test --project tests/NuclearReactorSimulator.Application.Tests/NuclearReactorSimulator.Application.Tests.csproj --no-build -- --explicit only
```

## M10.9.4 Hotfix 2 finding

The desktop seed's initial presentation shows approximately 5 MWe requested/actual electrical output, while the aggregate `TotalTurbineShaftPower` MEASURED presentation channel is currently unavailable because that measured source is not published by the desktop instrumentation definition. This must not be coerced to numeric zero or silently replaced with true/model state.

The explicit desktop journey therefore requires a finite MODEL rotor-shaft value at logical step 0, then runs the real session and requires sustained turbine shaft contribution and electrical export at the 10, 20, 30, 40, 50 and 60 second checkpoints. This distinguishes a presentation/provenance gap at startup from an actual long-run turbine→generator balance failure.


## M10.9.4 Hotfix 5 — cooperative runtime batching

A long-gameplay checkpoint spans 1,000 logical steps (10 simulated seconds), but `ControlRoomRuntimeCoordinator` intentionally limits one cooperative runtime batch to `ExecutionBudget.MaximumSimulationStepsPerBatch` (desktop default 256). The explicit tests therefore advance each checkpoint in repeated budget-sized batches rather than widening or bypassing the runtime safeguard. With the desktop default a checkpoint is 256 + 256 + 256 + 232 steps. Assertions remain at the same 10-second checkpoint boundary.

## Current journeys

### Desktop integrated sustained power path

Runs the actual desktop seed for 60 simulated seconds and requires:

- no trip;
- breaker remains closed;
- finite rotor speed;
- sustained turbine shaft power above the low-load acceptance floor (> 4.5 MW);
- sustained electrical export above 4.0 MWe while the requested load remains above 4.5 MWe.

### Synchronize → load → sustain

Starts from the pre-synchronization seed, deliberately:

1. closes the breaker;
2. commits one step;
3. raises generator load;
4. commits one step;
5. verifies immediate electrical output;
6. enters RUN mode for 60 simulated seconds;
7. verifies the export path remains alive at regular checkpoints.

## Failure interpretation

A failure is diagnostic evidence, not permission to weaken thresholds blindly. The failure message includes the logical checkpoint and the current generator/turbine power-path state so the smallest canonical owner can be investigated.

## M10.9.4 Hotfix 6 — first real failure and generation-ready v2 origins

The cumulative Hotfix 4 package compiled and passed the ordinary/classic suite locally. Hotfix 5 then fixed cooperative batching; the subsequent explicit pack reached actual plant evolution for the first time.

The desktop journey failed at 10 simulated seconds with approximately:

```text
BREAKER=CLOSED
REQUEST=5 MWe
MWe=2.406
MECH=2.455 MW
RPM=1442.615
MODEL SHAFT=0 MW
```

The synchronization/load journey independently failed with a `WaterSteamStateOutOfRangeException` at `control-out`.

This proved the historical v1 operating seeds were not sustained generation points. They remain immutable replay origins. Hotfix 6 introduces v2 current/acceptance seeds with a pressurized staged steam path, matched low-load admission hydraulics, bumpless governor bias, condenser capacity/heat rejection and condensate/feedwater return capacity/bias.

The diagnostic also now uses `EffectiveTurbineSteamFlow`, derived from actual turbine stage-group effective flow. The legacy `TotalSteamFlow` M4.1 boundary seam remains serialized for fingerprint-v1 compatibility but is no longer used to tell the operator that turbine admission is `0 kg/s` when actual stage flow exists.

### Promotion rule

For Hotfix 6 the expected sequence is:

```text
dotnet clean
dotnet restore
dotnet build --no-restore
dotnet test --no-build
scripts\run-gameplay-long-tests.cmd
```

The normal suite proves ordinary regressions and exact-version registration. The explicit pack proves that both the current desktop generation handoff and synchronize→close→load journey sustain the mechanical/electrical path for 60 simulated seconds. Any failure remains diagnostic evidence; do not lower the thresholds merely to make the candidate green.


## M10.9.4 Hotfix 7 — exhaust balance before rerunning the long gate

The Hotfix 6 ordinary 10-second desktop gate failed before the explicit gameplay pack because `exhaust` left the supported thermodynamic envelope. Analysis of the actual v2 operating point showed ~12.9 kg/s turbine flow but a 30 MW condenser boundary capable of removing more mass than the turbine replenished after expansion. Hotfix 7 reduces the v2 cooling boundary to 25 MW and starts the pre-synchronization v2 seed with a bumpless 61% governor bias.

The standard suite now includes a one-simulated-second pre-synchronization smoke regression. Only after the ordinary suite is green should the explicit 60-second gameplay pack be rerun.

## M10.9.4 Hotfix 8 — root cause of repeated exhaust depletion

Hotfix 7 still failed because condenser tuning was not the root cause. The v2 recipe initialized both the drum steam node and main-steam header at 280 °C saturation, producing zero canonical `steam → header` pressure difference and therefore zero main-steam-line replenishment. The admission train initially appeared healthy only because its downstream nodes had preloaded pressure gradients. Hotfix 8 initializes a continuous pressure staircase all the way from drum steam to turbine inlet and adds ordinary regressions for positive main-steam-line flow before the 1 s / 10 s / 60 s endurance gates are attempted.

## M10.9.4 Hotfix 16 — conservative source continuity

Hotfix 15 removed the sign-only feedwater accumulator, but the next full run exposed a separate source-continuity defect. The current drum remained compressed liquid, return separation supplied zero steam, and the turbine consumed only the finite preloaded `steam` inventory. Feedwater therefore compressed the drum to the old 22.064 MPa resolver cutoff and, once that artificial cutoff was corrected, to the real 25 MPa SCRAM threshold. With protection temporarily raised for diagnosis only, shaft power later fell below the acceptance floor as the preloaded steam path depleted.

For current v2 drums, M4.1 now treats positive main-steam-line flow as committed steam demand. It supplements any return-separated steam deficit with an internal `drum -> steam outlet` transfer carrying the outlet node's committed specific internal energy. Equal and opposite source terms preserve total modeled mass and energy, keep the canonical pipe as the only `steam -> header` transport and do not add external heat or mass. Legacy profiles do not receive the transfer.

The long-run failure report now accumulates every completed checkpoint and includes drum thermodynamic/flow evidence. This made the remaining governor behavior visible. Pre-synchronization v2 uses `P=0.5`, `I=0.02 s⁻¹` to prevent post-close 0%/100% cycling; the already-loaded desktop v2 retains `P=1`, `I=0.02 s⁻¹` and adds `D=0.2 s` to damp its small 10-second overshoot. Legacy defaults remain unchanged. Both explicit journeys pass through 60 simulated seconds without lowering the shaft/electrical thresholds.

## Hotfix 16 green checkpoint and Hotfix 17 rerun requirement

The user-supplied Hotfix 16 package records both explicit 60-second journeys passing separately after 870 ordinary tests passed. This is the base checkpoint for Hotfix 17.

Hotfix 17 changes only condenser heat-transfer feedback from capacity-only to current-v2 `min(Qavailable, UA*DeltaT)`. Because this changes long-horizon condenser backpressure/vacuum behavior, both explicit journeys must be rerun even if the ordinary suite is green. A failure must be diagnosed from condenser pressure/temperature, heat-rejection capacity, turbine exhaust flow and generator evidence; do not retune seed inventories merely to extend runtime.
