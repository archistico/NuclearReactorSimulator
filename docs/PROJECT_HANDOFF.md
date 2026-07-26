# Project Handoff — Nuclear Reactor Simulator

> **Current validated continuation:** M10.9.4.1-E.3.2 Hotfix 3. **Working source:** M10.9.4.1-F.1 CANDIDATE — isolated ideal-vapor subcritical/choked steam-flow capacity law and deterministic sizing audit; no relief/bypass topology is active yet.

## 1. Exact current truth

```text
M7 gate — COMPLETE / VALIDATED
M8 gate — COMPLETE / VALIDATED
M9 gate — COMPLETE / VALIDATED
M10.1–M10.9.4 — VALIDATED
M10.9.4.1 A–D.4.1 — VALIDATED IN THE CUMULATIVE SOURCE
M10.9.4.1-E.2 Hotfix 1 — VALIDATED
M10.9.4.1-E.3.1 Hotfix 1 — VALIDATED
M10.9.4.1-E.3.2 Hotfix 3 — CURRENT VALIDATED CONTINUATION BASELINE
M10.9.4.1-F.1 — WORKING CANDIDATE, VALIDATION PENDING
```

M10 remains in progress and closes only at M10.9.8.

## 2. E.3.2 validation evidence

On 2026-07-26 the user confirmed compilation, the focused electrical-protection gate, ordinary tests and all cumulative gates passed. The complete `e3-protection-implementation` CSV/summary bundle was supplied and reviewed.

Observed implementation behavior:

- normal 5 -> 0 -> 5 MWe: no trip, reverse pickup 0.080 s maximum, underfrequency pickup 0.640 s maximum;
- turbine trip: reverse-power pickup reached exactly 2.000 s, generator trip occurred at step 701 / 7.010 s and opened the breaker;
- breaker-open coastdown: 43.154407 Hz minimum and 6.845593 Hz maximum absolute slip with zero pickup and no generator trip.

E.3.2 Hotfix 3 is therefore the current validated continuation baseline. See `M10_9_4_1_E3_2_VALIDATION_CHECKLIST.md` and ADR 0114.

## 3. F.1 candidate contract

F.1 introduces only a numerical capacity seam:

- typed `SpecificGasConstant` in J/(kg K);
- `CompressibleSteamFlowDefinition` with full-open throat area, discharge coefficient, gas constant and heat-capacity ratio;
- `CompressibleSteamFlowSolver` with continuous subcritical behavior and a sonic/choked plateau below the analytic critical pressure ratio;
- one-way zero flow for closed area or non-positive forward head;
- no `PlantState` mutation and no source-term integration.

The representative audit sweeps downstream/upstream pressure ratio from 1.00 to 0.00 and writes CSV/summary artifacts under `artifacts/f1-choked-steam-flow`.

## 4. Explicit F.1 non-scope

F.1 does not add:

- relief/safety valve topology;
- turbine bypass topology;
- discharge receiver or condenser connection;
- operator controls, valve travel, protection or alarms;
- two-phase critical flow;
- enthalpy/flow-work migration;
- HMI changes.

## 5. Primary F.1 files

- `src/NuclearReactorSimulator.Domain/Physics/Quantities/SpecificGasConstant.cs`
- `src/NuclearReactorSimulator.Domain/Physics/TurbineIsland/MainSteam/CompressibleSteamFlowDefinition.cs`
- `src/NuclearReactorSimulator.Simulation/Physics/TurbineIsland/MainSteam/CompressibleSteamFlowSolver.cs`
- `src/NuclearReactorSimulator.Simulation/Physics/TurbineIsland/MainSteam/CompressibleSteamFlowResult.cs`
- `tests/NuclearReactorSimulator.Domain.Tests/Physics/SpecificGasConstantTests.cs`
- `tests/NuclearReactorSimulator.Domain.Tests/Physics/TurbineIsland/MainSteam/CompressibleSteamFlowDefinitionTests.cs`
- `tests/NuclearReactorSimulator.Simulation.Tests/Physics/TurbineIsland/MainSteam/CompressibleSteamFlowSolverTests.cs`
- `tests/NuclearReactorSimulator.Simulation.Tests/Physics/TurbineIsland/MainSteam/CompressibleSteamFlowCapacityAuditTests.cs`
- `scripts/run-choked-steam-flow-tests.cmd`
- `docs/M10_9_4_1_F1_CHOKED_STEAM_FLOW.md`
- `docs/M10_9_4_1_F1_VALIDATION_CHECKLIST.md`
- `docs/adr/0115-choked-steam-flow-is-an-isolated-one-way-capacity-seam-before-relief-bypass-topology.md`

## 6. Validation commands

```text
dotnet build
scripts/run-choked-steam-flow-tests.cmd
dotnet test
scripts/run-electrical-protection-implementation-tests.cmd
scripts/run-electrical-protection-trajectory-audit.cmd
scripts/run-generator-grid-bidirectional-tests.cmd
scripts/run-turbine-admission-authority-audit.cmd
scripts/run-turbine-governor-actuator-tracking-audit.cmd
scripts/run-gameplay-long-tests.cmd
scripts/run-operational-envelope-audit.cmd
scripts/run-reference-plant-scale-audit.cmd
```

Expected ordinary discovery if no unrelated tests change:

- 970 passed;
- 27 explicit skipped;
- 0 failed;
- 997 total.

## 7. Approved forward sequence

1. Validate F.1 capacity law and review the printed pressure-ratio summary.
2. F.2: add one conservative header-relief path over the validated capacity seam, with explicit receiver/boundary ownership and no bypass mixing.
3. F.3: add turbine bypass topology and authority only after relief conservation is green.
4. Phase G: migrate flow-work/enthalpy transport as a separate contract.
5. Phase H: measure timestep convergence and stiffness before choosing adaptive substepping.
6. Phase I: compatibility, audit consolidation and engineering hardening.

## 8. Non-negotiable architecture rules

- deterministic fixed timestep independent of UI cadence and wall clock;
- one canonical owner per physical/control state;
- one committed plant-network integration boundary;
- Application owns typed intents and immutable presentation projection;
- Avalonia contains no plant physics;
- no hidden runtime steady-state repair;
- historical/current behavior is versioned explicitly;
- replay/checkpoint behavior remains fail-closed and deterministic;
- no acceptance floor or protection threshold is weakened to make a test pass.

## 9. Delivery convention

- always deliver one ZIP containing the complete project;
- preserve repository-relative paths and complete files;
- include a root `.cmd` script whenever files must be deleted or renamed;
- keep validated baseline and candidate identity distinct.
