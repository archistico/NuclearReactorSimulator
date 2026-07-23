# Project Handoff — Nuclear Reactor Simulator

This is the **authoritative continuity checkpoint** for restarting the project in a new conversation.

## 1. Exact current truth

### Official validated baseline

**M10.9.3 — Interactive Full-Plant Mimic — VALIDATED**

The user explicitly confirmed local compilation and the complete automated suite passed.

```text
M7 gate — COMPLETE / VALIDATED
M8 gate — COMPLETE / VALIDATED
M9 gate — COMPLETE / VALIDATED
        ↓
M10.1–M10.9.3 — VALIDATED
        ↓
M10.9.4 Hotfix 17 — Condenser UA·ΔT Pressure Feedback — CURRENT CANDIDATE
        ↓
M10.9.5 Contextual Command Consequence Model
M10.9.6 Operational Challenge & Energy-Demand Framework
M10.9.7 Mission & Performance Workstation
M10.9.8 Integrated Human-Automation-HMI Validation Gate
        ↓
M10 COMPLETE
```


## 1A. Latest green checkpoint and current structural step

The user supplied **Hotfix 16 — Conservative Main-Steam Supply Closure** as the latest green working checkpoint. Its package changelog records:

- solution build: 0 warnings/errors;
- ordinary suite: 870 passed, 2 explicit journeys skipped by normal filtering;
- both explicit 60-second gameplay journeys: passed separately.

Hotfix 16 closes current-v2 drum/main-steam continuity conservatively and fixes the artificial compressed-liquid rejection above the critical isobar. It is the base for Hotfix 17.

**Hotfix 17 changes one structural item only:** current-v2 condenser heat rejection becomes `min(Q_available, UA·ΔT)` with `UA = 1.225 MW/K` and cooling water at 20 °C, chosen to reproduce the existing 24.5 MW / 40 °C design point exactly at initialization. Legacy null-UA definitions retain capacity-only behavior as an isolated compatibility seam.

Next after Hotfix 17, if ordinary + explicit gates remain green: **generator-grid synchronous coupling**. Do not mix pump check valves, protection expansion, actuator travel rates or adaptive substepping into the condenser change.

## 2. Operator-experience objective

The simulator remains educational through **learning by operating**: the user must execute startup/shutdown/testing/power/stability/fault-recovery tasks efficiently and later track deterministic external electrical demand with safety-dominant scoring.

The HMI must make plant connectivity, operating ranges, command effects and the difference between process state, alarms and protection immediately understandable.

## 3. Validated HMI foundation

- M10.9.1: five-region HMI shell and formal range semantics.
- M10.9.2 Hotfix 2: advanced linear/circular gauges, target/setpoint/protection bands, provenance/quality/off-scale and logical-step trends.
- M10.9.3: Application-owned interactive whole-plant mimic with equipment IN/OUT, directional medium-aware paths, connected-path emphasis and navigation-only subsystem drill-down.

## 3A. M10.9.4 Hotfix 13 current finding

The user confirmed Hotfix 4 compiled and the ordinary/classic test suite passed locally. Hotfix 5 then fixed cooperative batching in the explicit long-gameplay harness; the subsequent explicit run finally reached plant behavior and exposed two canonical operating-seed defects rather than a UI problem:

- desktop at logical step 1000 / 10 simulated seconds: breaker closed, 5 MWe requested, ~2.406 MWe actual, ~2.455 MW generator mechanical input, rotor ~1442.615 rpm, MODEL rotor shaft 0 MW;
- synchronization/load journey: `WaterSteamStateOutOfRangeException` at `control-out` (`v≈2.758 m³/kg`, `u≈2.497 MJ/kg`).

Root-cause review found the historical v1 desktop/synchronization seeds were designed for numerical/runtime stability, not sustained low-load generation: a ~120 °C nearly isobaric steam path, 100,000 Pa·s²/kg² admission resistance and proportional-only speed governor could not maintain the ~12.75 kg/s turbine flow required by the simplified 5 MWe stage/generator model. The legacy `TotalSteamFlow` HMI field also came from the M4.1 turbine-admission boundary seam, which is zero while M5.4 derives actual turbine stage flow from canonical stop/control/admission valve hydraulics.

Hotfix 6 does **not mutate v1**. It adds exact-version v2 initial-condition factories, keeps v1 registered for archive/replay, points new desktop sessions to v2, and gives the explicit sync journey its own v2 origin. The v2 recipe adds a staged pressurized steam path, matched admission resistance, bumpless PI governor bias, measured aggregate shaft channel, condenser capacity/heat rejection and condensate/feedwater pump capacity/bias. `EffectiveTurbineSteamFlow` is new `[JsonIgnore]` presentation metadata derived from turbine stage effective flow; fingerprint-v1 retains the historical serialized field.


Hotfix 7 corrected condenser capacity but the same `exhaust` depletion persisted. Hotfix 8 identifies the upstream root cause: v2 initialized `steam` and `header` at the same 280 °C saturated state, so the canonical main-steam line supplied 0 kg/s while the admission train temporarily drained only preloaded downstream inventories. Hotfix 8 adds a backward-compatible optional header steam temperature and uses a continuous v2 pressure staircase (280 → 275 → 269.5 → 253 → 246 °C from drum steam through turbine inlet), producing approximately 13 kg/s through every canonical steam-path segment with existing v2 resistances. Historical v1 defaults remain exact.

The ordinary synchronization regression now exercises the intended operator path (close breaker → raise load → run) rather than treating prolonged breaker-open/no-load operation as the generation endurance point. M10.9.4 is still **NOT VALIDATED** until both the ordinary suite and the explicit 60-second gameplay pack pass on Hotfix 8.

Hotfix 8 established continuous upstream replenishment but local tests still showed `exhaust` leaving the simplified thermodynamic envelope and the initial shaft-support assertion failing. The remaining structural mismatch is the generic 10 m³ low-pressure exhaust node: at ~40 °C it contains only about 0.5 kg of vapor while the v2 turbine moves ~13 kg/s, so each 0.01 s step moves a large fraction of the entire inventory. Hotfix 9 changed only v2 to a 1,000 m³ condenser steam-space, preserved the historical 10 m³ default for v1 and retained the then-current 1,800 Pa·s²/kg² steam/admission resistance with 24.5 MW initial condenser rejection. The next local run proved the thermodynamic crash was removed but exposed two remaining issues: the first public seed snapshot still showed zero turbine-stage flow, and after 10 simulated seconds the rotor settled low at ~2928 rpm with ~4.13 MW shaft power. Hotfix 10 therefore adds deterministic v2-only seed preconditioning and increases available steam-path control authority without changing v1 or any M4/M5 solver law.

### Structural root cause confirmed after Hotfix 10

The explicit gameplay runs proved the remaining failure was not a condenser/seed tuning problem. Both M5.4 and M5.5 duplicated the same stage-flow law: `min(stopFlow, controlFlow, admissionFlow)`. Because the canonical plant orchestrator already integrates each valve as a real mass transfer and the turbine expansion source term drains only `turbine-inlet`, the combined `stop-out + control-out + turbine-inlet` inventory obeyed `dM/dt = F_stop - F_stage >= 0` under that law. The admission train therefore behaved as a monotonic accumulator, equalized toward the steam header and inevitably drove stage flow to zero.

Hotfix 13 rebases on Hotfix 10 and withdraws unvalidated Hotfix 11/12 workaround branches. `TurbineStageGroupDefinition` now optionally owns an expansion resistance; current v2 uses 21,400 Pa·s²/kg² and one shared M4 `TurbineStageMassFlowResolver` computes pressure-driven inlet→exhaust flow with no reverse flow and a per-step inventory guard. Null remains only as an isolated legacy law. An ordinary 200-step invariant regression checks admission-train inventory boundedness and admission/stage-flow agreement.

Replay policy is also clarified: legacy replay compatibility is a read/migration concern, not a veto on correcting current physics. Pre-release legacy versions may be isolated, migrated or deprecated rather than contaminating the active model.

## 4. Current M10.9.4 Hotfix 13 candidate

M10.9.4 adds five detailed engineering schematic families:

1. Reactor / Core
2. Primary Circuit / Steam Drums
3. Turbine / Secondary
4. Generator / Grid
5. Instrumentation / Control / Protection

New Application-owned contracts/projector:

- `ControlRoomSubsystemSchematic*`
- `ControlRoomSubsystemSchematicProjector`

New Avalonia renderer:

- `ControlRoomSubsystemSchematicControl`

Avalonia renders supplied presentation topology only. It does not infer physics, process topology, alarm/protection rules or command consequences.

## 5. Turbine → generator → grid investigation

The user observed that the M10.9.3 whole-plant SHAFT path is amber and that continued simulation can eventually show `0 MWe`.

Important findings from code review:

- amber SHAFT is the **mechanical-energy medium color**, not a warning;
- electrical output is not produced merely because the rotor is spinning;
- with the breaker closed, requested electrical load produces electromagnetic load torque;
- actual generator output derives from mechanical power transferred by the rotor to that load;
- if sustained steam/turbine shaft production is insufficient, rotor kinetic energy can be consumed, speed can decay and output can collapse;
- therefore the observation is not automatically operator error.

The previous ordinary desktop stability test ran 1,000 steps / 10 simulated seconds but only required finite rotor speed; it did not assert sustained positive MWe. M10.9.4 closes this coverage gap.

### Generator/Grid HMI diagnostic

The GRID workspace now distinguishes:

- sync ready/not ready;
- breaker open/closed/paralleled;
- requested electrical load;
- actual electrical output;
- turbine shaft power / generator mechanical input;
- turbine/generator trip state.

The diagnostic tells the operator whether 0 MWe is expected because the breaker is open or requested load is zero, or whether a requested-load/shaft/output mismatch needs investigation.

## 6. Separately runnable long gameplay/system tests

`GameplayJourneyLongRunningTests` contains xUnit v3 **explicit** acceptance tests so they do not run in the ordinary fast suite.

They cover:

- actual desktop integrated seed for 60 simulated seconds;
- deliberate synchronization → close breaker → load raise → 60-second sustained export journey.

Normal suite:

```text
dotnet test --no-build
```

Explicit long gameplay pack only:

```text
scripts\run-gameplay-long-tests.cmd
```

or:

```text
dotnet test --project tests/NuclearReactorSimulator.Application.Tests/NuclearReactorSimulator.Application.Tests.csproj --no-build -- --explicit only
```

For M10.9.4 promotion, run the explicit pack once in addition to the normal build/test gate. If it fails, use its checkpoint diagnostic and patch the smallest canonical owner; do not weaken the test merely to promote the milestone.

## 7. Non-negotiable architecture rules

- fixed deterministic timestep; wall clock/UI cadence never changes physics;
- M2 owns reactor physics;
- M3 owns primary thermohydraulics/inventories;
- M4 owns secondary/turbine/condenser/feedwater/generator/grid;
- M5 owns instrumentation/control/protection/alarms/supervisory automation;
- M7 owns guidance/checklists/training semantics;
- M9.1 owns recorder/checkpoints/full replay;
- M9.2 owns immutable post-incident analysis;
- UI/ViewModels own no physics, alarm, protection, controller or topology algorithms;
- measured consumers never silently read true/model state;
- protection always overrides normal/supervisory control;
- training assistance and plant-control authority remain independent axes;
- mimic/schematic selection is presentation-only;
- no free-form/NLP command surface.

## 8. Primary M10.9.4 implementation files

- `src/NuclearReactorSimulator.Application/ControlRoom/Hmi/ControlRoomSubsystemSchematic*.cs`
- `src/NuclearReactorSimulator.Application/ControlRoom/Hmi/ControlRoomSubsystemSchematicProjector.cs`
- `src/NuclearReactorSimulator.Application/ControlRoom/GeneratorPresentationSnapshot.cs`
- `src/NuclearReactorSimulator.Application/ControlRoom/ControlRoomSnapshotProjector.cs`
- `src/NuclearReactorSimulator.App/Controls/ControlRoomSubsystemSchematicControl.cs`
- `src/NuclearReactorSimulator.App/ViewModels/MainWindowViewModel.cs`
- `src/NuclearReactorSimulator.App/Views/MainWindow.axaml`
- `tests/NuclearReactorSimulator.Application.Tests/ControlRoom/ControlRoomSubsystemSchematicProjectionTests.cs`
- `tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/GameplayJourneyLongRunningTests.cs`
- `tests/NuclearReactorSimulator.App.Tests/OperatorExperienceM1094SubsystemSchematicsTests.cs`
- `scripts/run-gameplay-long-tests.cmd`
- `scripts/run-gameplay-long-tests.ps1`
- `docs/milestones/M10.9.4.md`
- `docs/SUBSYSTEM_ENGINEERING_SCHEMATICS.md`
- `docs/GAMEPLAY_LONG_RUNNING_SYSTEM_TESTS.md`
- ADR 0078
- ADR 0080 — turbine expansion is a pressure-driven hydraulic element
- ADR 0081 — legacy replay compatibility does not constrain current-model correctness

The four user-provided SVG schematics remain design references under `docs/reference/hmi/`; they are not authoritative runtime topology.

## 9. Validate current candidate

Run locally:

```text
dotnet clean
dotnet restore
dotnet build --no-restore
dotnet test --no-build
```

Then run the separate long gameplay acceptance pack:

```text
scripts\run-gameplay-long-tests.cmd
```

Then manually verify `docs/milestones/M10.9.4.md`.

Do not mark M10.9.4 validated until the user explicitly confirms the normal gate **and** the requested long gameplay test outcome is understood/green.

## 10. Next after validation

**M10.9.5 — Contextual Command Consequence Model**.

## 11. Delivery convention

- deliver complete ZIP packages;
- keep validated baseline and current candidate distinct;
- patch the smallest responsible layer when failures appear;
- never create a second physics/topology/control owner in UI code.


## 3B. M10.9.4 Hotfix 14 current test-contract correction

Hotfix 14 keeps Hotfix 13 production code unchanged. The 200-step turbine hydraulic regression no longer requires `AdmissionFlow == StageFlow` to 3 decimals because a compressible `turbine-inlet` plenum may accumulate/deplete transiently. Instead it keeps the ±5% final combined admission-train inventory bound, samples the trajectory and requires at least one negative inventory increment to directly refute the historical monotonic-accumulator invariant. Finite positive/in-range admission and stage flows remain required.

The structural audit and ordered remediation plan are authoritative in `docs/STRUCTURAL_PLANT_MODEL_STABILIZATION_PLAN.md`. Do not tune seeds/boundaries to compensate for missing feedback laws.

## M10.9.4 Hotfix 15 historical structural finding

Hotfix 14 compiled and the complete ordinary suite passed. The explicit long gameplay journeys then failed in the canonical `drum` node with specific volume about 0.001307 m^3/kg. The root cause is another algebraic inventory ratchet: the physical return pipe adds `F_return`, historical separator source terms remove exactly `F_return`, and canonical feedwater adds `F_feedwater`, leaving `dm_drum/dt = F_feedwater >= 0`.

Hotfix 15 introduces explicit `SteamDrumLiquidRecirculationMode`. Historical profiles remain `LegacyReturnSplit`; current v2 sustained-generation and synchronization profiles use `CirculationDemandBalanced`, where liquid recirculation follows positive committed MCP demand. The drum balance is therefore `F_return + F_feedwater - F_MCP - F_steam`, so inventory is no longer forced to increase by construction. See ADR 0082 and `STRUCTURAL_PLANT_MODEL_STABILIZATION_PLAN.md`.
