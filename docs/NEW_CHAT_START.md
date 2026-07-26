# New Chat Start — Nuclear Reactor Simulator

We are continuing the **Nuclear Reactor Simulator** project.

## Read first

1. `docs/PROJECT_HANDOFF.md`
2. `docs/PROJECT_STATUS.md`
3. `docs/ROADMAP.md`
4. `docs/milestones/M10.9.4.1.md`
5. `docs/M10_9_4_1_E3_2_PROTECTION_EVIDENCE.md`
6. `docs/M10_9_4_1_E3_2_VALIDATION_CHECKLIST.md`
7. `docs/ELECTRICAL_PROTECTION_TRAJECTORY_AUDIT.md`
8. `docs/PROTECTION_INTERLOCKS_TRIPS_SCRAM.md`
9. `docs/REFERENCE_PLANT_SCALE_CONTRACT.md`
10. `docs/KNOWN_MODEL_LIMITATIONS.md`

## Exact checkpoint

- Current validated continuation: **M10.9.4.1-E.3.1 Hotfix 1**.
- User confirmed compilation, ordinary tests and all cumulative long-running gates passed on **2026-07-26**.
- The complete E.3.1 CSV/summary trajectory bundle was supplied and reviewed.
- Working source: **M10.9.4.1-E.3.2 Hotfix 3 Typed Breaker-Command Target Regression Fix CANDIDATE**.
- Both current-v2 sustained profiles use **10 MWe**, **5 MWe = 50%**, **1.5 rpm full-load droop**, `Bidirectional` coupling and the E.3.2 relay set.
- Reverse power: **-0.30 MWe / -0.10 MWe reset / 2.0 s**.
- Underfrequency: **48.8 Hz / 49.5 Hz reset / 1.0 s**.
- Loss of synchronism: **1.5 Hz absolute slip / 0.5 Hz reset / 0.5 s**.
- All three require a measured closed generator breaker and issue canonical generator trip.
- Historical/default protection remains zero-delay and unsupervised unless explicitly configured.
- Dedicated evidence factories reproduce the original E.3.1 trajectories without the E.3.2 relay set.
- The focused E.3.2 script prints three relay-implementation summaries and writes detailed CSV files for review.
- Hotfix 1 added the two logical-step-zero signals; Hotfix 2 uses `ElectricalGridDefinition.NominalFrequency` for the initial absolute-slip calculation, matching the actual definition API and preserving exact measured-frame cardinality.

## Next work

Validate E.3.2 with:

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

Expected ordinary discovery: **960 passed, 26 explicit skipped, 0 failed, 986 total**.

After all automated gates, manually verify the reverse-power and frequency markers plus trip/reset behavior on the GENERATOR station.

## Non-negotiable rules

- deterministic fixed timestep;
- no plant physics in Avalonia;
- canonical simulation owners remain unique;
- protection consumes measured signals and has priority;
- explicit legacy/current versioning;
- no hidden repair;
- no weakened tests, floors or protection thresholds;
- deliver the complete project ZIP and include a `.cmd` script for any deletion/rename operation.
