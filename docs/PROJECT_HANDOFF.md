# Project Handoff — Nuclear Reactor Simulator

> **Current validated continuation:** M10.9.4.1-E.3.1 Hotfix 1. **Working source:** M10.9.4.1-E.3.2 Hotfix 3 CANDIDATE — typed breaker-command target correction for the final E.3.2 explicit coastdown audit; the evidence-derived electrical-protection runtime is unchanged.

## 1. Exact current truth

```text
M7 gate — COMPLETE / VALIDATED
M8 gate — COMPLETE / VALIDATED
M9 gate — COMPLETE / VALIDATED
M10.1–M10.9.4 — VALIDATED
M10.9.4.1 A–D.4.1 — VALIDATED IN THE CUMULATIVE SOURCE
M10.9.4.1-E.2 Hotfix 1 — VALIDATED
M10.9.4.1-E.3.1 Hotfix 1 — CURRENT VALIDATED CONTINUATION BASELINE
M10.9.4.1-E.3.2 Hotfix 3 — WORKING CANDIDATE, VALIDATION PENDING
```

M10 remains in progress and closes only at M10.9.8.

## 2. E.3.1 validation and reviewed evidence

On 2026-07-26 the user confirmed E.3.1 Hotfix 1 compiled and all ordinary and cumulative long-running gates passed. The complete generated CSV/summary bundle was supplied and reviewed.

Observed discriminator margins:

- normal 5 -> 0 -> 5 MWe minimum exchange: **-0.247872 MWe for one sampled 0.1 s interval**;
- turbine-trip reverse power: minimum **-0.593835 MWe**, persistent near **-0.51 MWe for about 30 s**;
- normal minimum frequency: **48.720072 Hz**, at/below 48.8 Hz for about **0.6 s**;
- breaker-open coastdown minimum frequency: **42.989737 Hz**, never protection-eligible;
- normal maximum absolute slip: **1.279928 Hz**;
- phase-offset sweep maximum absolute slip: **2.770386–2.776667 Hz**, with at least about **4.3 s** above 1.5 Hz;
- raw wrapped phase angle is not a usable discriminator because both normal and offset trajectories approach 180 degrees.

The authoritative evidence record is `docs/M10_9_4_1_E3_2_PROTECTION_EVIDENCE.md`.

## 3. E.3.2 candidate contract

E.3.2 extends the generic M5.5 protection function with:

- optional measured supervision;
- deterministic committed pickup elapsed time;
- zero-delay/no-supervision defaults preserving historical behavior.

Both current-v2 sustained profiles enable:

| Function | Pickup | Reset | Delay | Supervision | Action |
|---|---:|---:|---:|---|---|
| Reverse power | -0.30 MWe | -0.10 MWe | 2.0 s | breaker closed | Generator trip |
| Underfrequency | 48.8 Hz | 49.5 Hz | 1.0 s | breaker closed | Generator trip |
| Loss of synchronism | 1.5 Hz absolute slip | 0.5 Hz | 0.5 s | breaker closed | Generator trip |

The E.3.1 desktop and synchronization evidence factories keep the original unprotected calibration trajectories reproducible. Production current-v2 factories enable the relay set.

Hotfix 1 added the two logical-step-zero measured signals that correspond to the new E.3.2 instrumentation channels. Hotfix 2 corrects the grid-frequency contract used by the absolute-slip seed: the definition owns `NominalFrequency`, while `Frequency` belongs to runtime snapshots. Initial breaker state remains seeded as 0/1 from the canonical generator state, and initial absolute slip is computed from generator electrical frequency versus `grid.NominalFrequency`. Runtime sources and protection calibration are unchanged.

## 4. Ownership and integration

- breaker state and absolute frequency slip are canonical measured instrumentation sources;
- M5.5 remains the only trip owner;
- the fixed logical timestep advances pickup timers;
- generator trip uses the existing breaker-open arbitration path;
- inactive supervision clears an incomplete pickup and makes a latched function reset-safe;
- reset remains explicit and canonical;
- HMI markers are projected from protection definitions, not recomputed in Avalonia;
- replay/checkpoint regression covers an in-flight reverse-power pickup timer;
- the focused gate writes and prints three implementation summaries plus detailed CSV evidence under `artifacts/e3-protection-implementation`.

## 5. Primary E.3.2 files

- `src/NuclearReactorSimulator.Domain/Physics/Control/Protection/ProtectionFunctionDefinition.cs`
- `src/NuclearReactorSimulator.Domain/Physics/Control/Protection/ProtectionFunctionSupervisionDefinition.cs`
- `src/NuclearReactorSimulator.Simulation/Physics/Control/Protection/ProtectionSystemSolver.cs`
- `src/NuclearReactorSimulator.Simulation/Physics/Control/Protection/ProtectionFunctionLatchState.cs`
- `src/NuclearReactorSimulator.Simulation/Physics/Instrumentation/InstrumentSignalSourceCatalog.cs`
- `src/NuclearReactorSimulator.Application/Scenarios/PreStartup/ColdShutdownInitialConditionFactory.cs`
- `src/NuclearReactorSimulator.Application/Scenarios/Training/DesktopSustainedGenerationInitialConditionFactory.cs`
- `src/NuclearReactorSimulator.Application/Scenarios/Synchronization/GridSynchronizationSustainedInitialConditionFactory.cs`
- `tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/ElectricalProtectionImplementationTests.cs`
- `scripts/run-electrical-protection-implementation-tests.cmd`
- `docs/M10_9_4_1_E3_2_PROTECTION_EVIDENCE.md`
- `docs/M10_9_4_1_E3_2_VALIDATION_CHECKLIST.md`
- `docs/adr/0114-evidence-derived-electrical-protection-uses-supervised-delayed-m5-functions.md`

## 6. Validation commands

```text
dotnet build
scripts/run-electrical-protection-implementation-tests.cmd
dotnet test
scripts/run-electrical-protection-trajectory-audit.cmd
scripts/run-generator-grid-bidirectional-tests.cmd
scripts/run-turbine-admission-authority-audit.cmd
scripts/run-turbine-governor-actuator-tracking-audit.cmd
scripts/run-gameplay-long-tests.cmd
scripts/run-operational-envelope-audit.cmd
scripts/run-reference-plant-scale-audit.cmd
```

Expected ordinary discovery if no unrelated tests changed:

- 960 passed;
- 26 explicit skipped;
- 0 failed;
- 986 total.

## 7. Promotion criteria

Promote E.3.2 only after:

1. build and focused script pass;
2. ordinary suite passes;
3. all three explicit E.3.2 journeys pass;
4. the E.3.1 artifact pack reproduces unchanged;
5. all cumulative long-running gates pass;
6. the GENERATOR panel shows the -0.3 MWe, 48.8 Hz and 53 Hz markers correctly;
7. generator trip/reset behavior is manually coherent.

## 8. Approved forward sequence

1. Validate E.3.2.
2. If green, promote E.3.2 as the continuation baseline.
3. Continue to Phase F: relief, bypass and choked-flow fidelity.
4. Then proceed through G enthalpy/flow-work, H numerical-stiffness decision and I compatibility/engineering hardening.
5. Finish M10.9.5–M10.9.8 only after the physical hardening gate is closed.

## 9. Non-negotiable architecture rules

- deterministic fixed timestep independent of UI cadence and wall clock;
- one canonical owner per physical/control state;
- Application owns typed intents and immutable presentation projection;
- Avalonia contains no plant physics;
- protection consumes measured signals, not true-state shortcuts;
- protection overrides normal and supervisory control;
- no hidden runtime steady-state repair;
- historical/current behavior is versioned explicitly;
- replay/checkpoint behavior remains fail-closed and deterministic;
- no acceptance floor or protection threshold is weakened to make a test pass.

## 10. Delivery convention

- always deliver one ZIP containing the complete project;
- preserve repository-relative paths and complete files;
- include a root `.cmd` script whenever files must be deleted or renamed;
- keep validated baseline and candidate identity distinct.
