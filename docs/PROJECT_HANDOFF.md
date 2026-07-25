# Project Handoff — Nuclear Reactor Simulator

> **Current validated continuation:** M10.9.4.1-D.4. The cumulative D.3.2 Hotfix 3 + operator turbine-valve station passed the complete ordinary and explicit automated validation pack. E.1 is an accepted target decision; E.2 is not implemented.

## 1. Exact current truth

Validated sequence:

```text
M7 gate — COMPLETE / VALIDATED
M8 gate — COMPLETE / VALIDATED
M9 gate — COMPLETE / VALIDATED
M10.1–M10.9.4 — VALIDATED
M10.9.4.1 A–C — VALIDATED IN THE CUMULATIVE SOURCE
M10.9.4.1 D.1–D.3.2 Hotfix 3 — VALIDATED IN THE CUMULATIVE SOURCE
M10.9.4.1-D.4 — CURRENT VALIDATED CONTINUATION BASELINE
```

M10 remains in progress and closes only at M10.9.8.

## 2. Validation evidence

User-confirmed local result on 2026-07-25:

- ordinary test discovery: **961**;
- ordinary passed: **944**;
- ordinary failed: **0**;
- explicit/opt-in skipped by the ordinary run: **17**.

All 17 unique explicit tests were then executed and passed:

| Gate | Result |
|---|---:|
| Turbine admission authority | 3/3 |
| Governor/actuator tracking | 2/2 |
| Gameplay long-running journeys | 2/2 |
| Operational-envelope audit | 9/9 |
| Reference-plant scale audit | 2/2 |

The script totals contain one shared scale test, so 18 script executions correspond to 17 unique explicit tests.

## 3. Validated D.3.2 Hotfix 3

The loaded desktop main-steam path had remained the upstream flow bottleneck after the stop-valve pressure-grade correction.

The active contract is:

- loaded desktop main-steam-line resistance: **850 Pa·s²/kg²**;
- synchronization main-steam-line resistance: **1,000 Pa·s²/kg²**;
- loaded desktop control-valve seed: **28%**;
- loaded stop-out steam seed: **276.7 °C**;
- generation-ready flow and power floors unchanged;
- complete stop/control/admission train remains authoritative over pressure-driven stage flow.

No PID/PI gain, actuator travel, turbine work law, passive rotor-loss law, protection threshold, timestep or replay contract was changed by Hotfix 3.

## 4. Validated D.4 operator valve station

The TURBINE workstation now exposes canonical typed commands for:

- STOP valve OPEN / CLOSE;
- ADMISSION valve OPEN / CLOSE;
- control valve AUTO / MANUAL;
- explicit bounded 0–100% manual demand with APPLY.

Validated semantics:

- slider movement alone sends no command;
- requested, manual-demand and actual positions remain distinct;
- finite actuator travel remains authoritative;
- manual demand is rejected outside MANUAL mode;
- AUTO returns authority to the governor;
- protection is applied later and can force STOP closed without erasing the operator request;
- trip override is visible in presentation state.

See `M10_9_4_1_D4_VALIDATION_CHECKLIST.md`.

## 5. Active generator/grid scale contract

The source is still **pre-E**. The dedicated 2/2 scale audit proves:

- generator nameplate: **1,000 MW**;
- requested sustained load: **5 MWe = 0.5%**;
- rotor: **1,000 kg·m² at 3,000 rpm**;
- full-load governor rise: **150 rpm**;
- droop displacement at 5 MWe: **0.75 rpm**;
- maximum synchronizing correction: **0.5 MW**;
- frequency damping: **2 MW/Hz**;
- grid coupling remains correction-only/generation-only;
- no internal signed generator/grid torque seam exists;
- electrical output and HMI remain non-negative under this contract.

E.1 accepts a future **10 MWe educational target**. E.2 must still implement nameplate, governor normalization, bidirectional coupling, signed power/torque, positive losses, HMI ranges and replay/checkpoint behavior as one coordinated candidate.

ADR 0109 records the accepted target. ADR 0110–0111 are proposed E.2 designs, not current behavior.

## 6. Remaining D.4.1 hardening

Before the scale migration, implement the smallest isolated follow-up:

1. replay/checkpoint regressions for STOP, ADMISSION, AUTO/MANUAL and manual demand;
2. checkpoint while requested and actual valve positions differ during finite travel;
3. trip → request preserved → canonical reset → travel resumes;
4. stop-valve-owned travel-rate configuration instead of borrowing control-valve configuration;
5. manual TURBINE-station usability review for command enablement, pending/APPLY, target/actual feedback and trip override.

These are hardening items, not failures of the validated D.4 automated gate.

## 7. Approved forward sequence

1. **M10.9.4.1-D.4.1** operator-valve hardening.
2. **M10.9.4.1-E.2** coordinated 10 MWe and bidirectional generator/grid migration.
3. Re-run the complete ordinary and explicit validation pack.
4. **E.3** reverse-power, supervised-underfrequency and loss-of-synchronism protection, derived from measured E.2 trajectories.
5. Phase F relief/bypass/choked flow.
6. Phase G flow-work/enthalpy migration.
7. Phase H numerical stiffness decision gate.
8. Phase I compatibility and engineering hardening.
9. M10.9.5–M10.9.8.

## 8. Architecture rules

Do not break these boundaries:

- deterministic fixed timestep independent of UI cadence and wall clock;
- canonical M2/M3/M4/M5 plant ownership;
- Application owns presentation projection and typed intents;
- Avalonia renders and dispatches only;
- measured consumers do not substitute true/model state;
- protection overrides normal and supervisory control;
- no hidden runtime steady-state repair;
- legacy/current behavior is versioned explicitly;
- replay/checkpoint behavior is fail-closed and deterministic;
- one physical/control owner per state variable.

## 9. Primary files for the next work

D.4 runtime and presentation:

- `src/NuclearReactorSimulator.Application/ControlRoom/IntegratedAutomaticOperationRuntimeEngine.cs`
- `src/NuclearReactorSimulator.Application/ControlRoom/ControlRoomCommandKind.cs`
- `src/NuclearReactorSimulator.Application/ControlRoom/ControlRoomSnapshotProjector.cs`
- `src/NuclearReactorSimulator.Application/ControlRoom/TurbineAdmissionTrainPresentationSnapshot.cs`
- `src/NuclearReactorSimulator.App/ViewModels/MainWindowViewModel.cs`
- `src/NuclearReactorSimulator.App/Views/MainWindow.axaml`
- `tests/NuclearReactorSimulator.Application.Tests/ControlRoom/TurbineValveOperatorControlTests.cs`
- `tests/NuclearReactorSimulator.App.Tests/MainWindowViewModelTests.cs`

Scale and coupling:

- `tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/ReferencePlantScaleAuditTests.cs`
- `tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/ReferencePlantScaleMigrationTests.cs`
- `docs/REFERENCE_PLANT_SCALE_CONTRACT.md`
- `docs/REFERENCE_PLANT_SCALE_EVIDENCE.md`
- `docs/REFERENCE_PLANT_SCALE_MIGRATION_PLAN.md`
- ADR 0109–0111.

## 10. Validation commands

```text
dotnet test --no-build
scriptsun-turbine-admission-authority-audit.cmd
scriptsun-turbine-governor-actuator-tracking-audit.cmd
scriptsun-gameplay-long-tests.cmd
scriptsun-operational-envelope-audit.cmd
scriptsun-reference-plant-scale-audit.cmd
```

Any production edit reopens the applicable gates.

## 11. Delivery convention

- deliver a ZIP containing only changed/added files;
- include complete files and preserve project-relative paths;
- list files that must be deleted when a rename cannot be represented by extraction alone;
- keep validated baseline and candidate identity distinct;
- do not weaken acceptance floors or protection thresholds to make tests green.
