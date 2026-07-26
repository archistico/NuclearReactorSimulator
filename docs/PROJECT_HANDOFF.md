# Project Handoff — Nuclear Reactor Simulator

> **Current validated continuation:** M10.9.4.1-E.2 Hotfix 1. **Working source:** M10.9.4.1-E.3.1 Hotfix 1 CANDIDATE — evidence-only signed electrical protection trajectory audit.

## 1. Exact current truth

```text
M7 gate — COMPLETE / VALIDATED
M8 gate — COMPLETE / VALIDATED
M9 gate — COMPLETE / VALIDATED
M10.1–M10.9.4 — VALIDATED
M10.9.4.1 A–D.4.1 — VALIDATED IN THE CUMULATIVE SOURCE
M10.9.4.1-E.2 Hotfix 1 — CURRENT VALIDATED CONTINUATION BASELINE
M10.9.4.1-E.3.1 Hotfix 1 — WORKING CANDIDATE, VALIDATION PENDING
```

M10 remains in progress and closes only at M10.9.8.

## 2. E.2 validation

The user confirmed on 2026-07-26 that E.2 Hotfix 1 compiled and all requested ordinary, focused and long-running gates passed. E.2 Hotfix 1 is therefore promoted to VALIDATED. Exact console counts were not copied into this handoff, so no unreported count is inferred.

Validated current-v2 contract:

- generator nameplate **10 MWe**;
- normal point **5 MWe = 50%**;
- rotor **1,000 kg·m² at 3,000 rpm**;
- full-load governor rise **1.5 rpm**;
- coupling **Bidirectional**;
- synchronizing correction **0.5 MW**;
- frequency damping **2 MW/Hz**;
- signed range **-10..+10 MWe**;
- positive exchange = export/generation;
- negative exchange = import/motoring;
- conversion loss remains non-negative.

Historical/default definitions retain 1,000 MWe, null/GenerationOnly coupling, non-negative presentation and the public non-negative manual rotor-load contract.

## 3. E.3.1 Hotfix 1 candidate

E.3.1 changes no production runtime or protection definition. It adds four explicit Application audit trajectories:

1. normal breaker-closed 5→0→5 MWe load request;
2. turbine trip with the electrical request lowered to zero and the breaker intentionally left closed to expose reverse power/motoring;
3. breaker-open turbine coastdown to prove underfrequency supervision is required;
4. breaker-closed ±15/45/90/135° phase-offset sweep over the reduced-order coupling.

The audit records:

- signed grid and mechanical exchange;
- conversion loss;
- generator frequency and grid slip;
- absolute and signed phase error;
- breaker state;
- turbine/generator trip action;
- phase-wrap count in the synthetic sweep.

Outputs are written to:

```text
artifacts/e3-protection-trajectories
```

## 4. Primary E.3.1 files

- `tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/ElectricalProtectionTrajectoryAuditTests.cs`
- `scripts/run-electrical-protection-trajectory-audit.cmd`
- `docs/ELECTRICAL_PROTECTION_TRAJECTORY_AUDIT.md`
- `docs/M10_9_4_1_E3_1_VALIDATION_CHECKLIST.md`
- `docs/adr/0113-electrical-protection-thresholds-are-derived-from-signed-current-v2-trajectories.md`

## 5. Validation commands

```text
dotnet build
scripts/run-electrical-protection-trajectory-audit.cmd
dotnet test
scripts/run-generator-grid-bidirectional-tests.cmd
scripts/run-turbine-admission-authority-audit.cmd
scripts/run-turbine-governor-actuator-tracking-audit.cmd
scripts/run-gameplay-long-tests.cmd
scripts/run-operational-envelope-audit.cmd
scripts/run-reference-plant-scale-audit.cmd
```

If the validated E.2 ordinary count is unchanged, E.3.1 discovery is expected to be 952 passed, 23 explicit skipped and 975 total. This remains an expectation until the user supplies the local result.

## 6. Required evidence handoff

After the focused audit, preserve or paste:

```text
artifacts/e3-protection-trajectories/*.summary.txt
```

The CSV files remain the detailed source for threshold review.

## 7. Approved forward sequence

1. Validate E.3.1 and review its generated reports.
2. Design E.3.2 pickup/reset/delay/supervision from the observed envelopes.
3. Implement reverse-power protection with explicit prime-mover-loss timing.
4. Implement underfrequency only with breaker-closed operational supervision.
5. Implement only the loss-of-synchronism observables supported by the reduced-order model.
6. Validate replay/checkpoint behavior across timer pickup, trip and reset.
7. Continue to Phase F, then G, H and I.

## 8. Architecture rules

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

## 9. Delivery convention

- always deliver one ZIP containing the complete project;
- preserve repository-relative paths and complete files;
- include a root `.cmd` script whenever files must be deleted or renamed;
- keep validated baseline and candidate identity distinct.
