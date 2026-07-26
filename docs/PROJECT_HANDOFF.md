# Project Handoff — Nuclear Reactor Simulator

> **Current validated continuation:** M10.9.4.1-D.4. **Working source:** M10.9.4.1-D.4.1 CANDIDATE. The candidate hardens STOP travel ownership, deterministic replay/checkpoint restoration and trip-reset travel resumption. E.1 is an accepted target decision; E.2 is not implemented.

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
M10.9.4.1-D.4.1 — WORKING CANDIDATE, VALIDATION PENDING
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

## 6. D.4.1 hardening candidate

The working source now implements the smallest isolated follow-up:

1. replay/checkpoint regressions for STOP, ADMISSION, AUTO/MANUAL and numeric manual demand;
2. checkpoint seek while requested and actual valve positions differ during finite travel;
3. turbine trip → STOP OPEN request preserved → canonical reset accepted → finite travel resumes;
4. STOP-valve-owned optional travel-rate configuration, with `null` preserving legacy instantaneous behavior even when other secondary valves are rate-limited;
5. the optional public factory parameter appended for positional source compatibility;
6. a differential-rate regression proving STOP and ADMISSION no longer share accidental travel ownership.

The remaining work is validation, not additional physics: local compilation, focused tests, complete ordinary and explicit gates, and manual TURBINE-station usability review. See `M10_9_4_1_D4_1_VALIDATION_CHECKLIST.md`. D.4 remains the official validated baseline until those gates are confirmed.

## 7. Approved forward sequence

1. Validate **M10.9.4.1-D.4.1** with the focused, ordinary, explicit and manual gates.
2. Promote D.4.1 only after explicit user confirmation.
3. Implement **M10.9.4.1-E.2** as the coordinated 10 MWe and bidirectional generator/grid migration.
4. Re-run the complete ordinary and explicit validation pack after E.2.
5. **E.3** reverse-power, supervised-underfrequency and loss-of-synchronism protection, derived from measured E.2 trajectories.
6. Phase F relief/bypass/choked flow.
7. Phase G flow-work/enthalpy migration.
8. Phase H numerical stiffness decision gate.
9. Phase I compatibility and engineering hardening.
10. M10.9.5–M10.9.8.

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

## 9. Primary files for the current candidate and next work

D.4.1 runtime, definition and regression files:

- `src/NuclearReactorSimulator.Application/ControlRoom/IntegratedAutomaticOperationRuntimeEngine.cs`
- `src/NuclearReactorSimulator.Domain/Physics/TurbineIsland/MainSteam/TurbineAdmissionTrainDefinition.cs`
- `src/NuclearReactorSimulator.Application/Scenarios/PreStartup/ColdShutdownInitialConditionFactory.cs`
- `tests/NuclearReactorSimulator.Application.Tests/ControlRoom/TurbineValveOperatorControlTests.cs`
- `tests/NuclearReactorSimulator.Application.Tests/Scenarios/Recording/TurbineValveReplayCheckpointTests.cs`
- `tests/NuclearReactorSimulator.Application.Tests/Scenarios/PreStartup/ColdShutdownInitialConditionFactoryTests.cs`
- `tests/NuclearReactorSimulator.Domain.Tests/Physics/TurbineIsland/MainSteam/MainSteamNetworkDefinitionTests.cs`
- `docs/M10_9_4_1_D4_1_VALIDATION_CHECKLIST.md`
- `docs/adr/0112-turbine-stop-valve-travel-rate-is-owned-by-the-admission-train.md`

Scale and coupling:

- `tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/ReferencePlantScaleAuditTests.cs`
- `tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/ReferencePlantScaleMigrationTests.cs`
- `docs/REFERENCE_PLANT_SCALE_CONTRACT.md`
- `docs/REFERENCE_PLANT_SCALE_EVIDENCE.md`
- `docs/REFERENCE_PLANT_SCALE_MIGRATION_PLAN.md`
- ADR 0109–0111.

## 10. Validation commands

```text
scripts\run-turbine-valve-hardening-tests.cmd
dotnet test
scripts\run-turbine-admission-authority-audit.cmd
scripts\run-turbine-governor-actuator-tracking-audit.cmd
scripts\run-gameplay-long-tests.cmd
scripts\run-operational-envelope-audit.cmd
scripts\run-reference-plant-scale-audit.cmd
```

Any production edit reopens the applicable gates. D.4.1 is not validated until the user explicitly confirms these results and the manual TURBINE-station checklist.

## 11. Delivery convention

- always deliver one ZIP containing the complete project;
- preserve the exact repository-relative structure and complete files;
- when files must be deleted or renamed, include a root-level `.cmd` script that performs those operations safely;
- keep validated baseline and candidate identity distinct;
- do not weaken acceptance floors or protection thresholds to make tests green.
