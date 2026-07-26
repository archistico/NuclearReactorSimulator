# New Chat Start — Nuclear Reactor Simulator

We are continuing the **Nuclear Reactor Simulator** project.

## Read first

1. `docs/PROJECT_HANDOFF.md`
2. `docs/PROJECT_STATUS.md`
3. `docs/ROADMAP.md`
4. `docs/milestones/M10.9.4.1.md`
5. `docs/M10_9_4_1_E3_1_VALIDATION_CHECKLIST.md`
6. `docs/ELECTRICAL_PROTECTION_TRAJECTORY_AUDIT.md`
7. `docs/REFERENCE_PLANT_SCALE_CONTRACT.md`
8. `docs/REFERENCE_PLANT_SCALE_EVIDENCE.md`
9. `docs/OPERATIONAL_ENVELOPE_NUMERICAL_HARDENING_PLAN.md`
10. `docs/KNOWN_MODEL_LIMITATIONS.md`

## Exact checkpoint

- Current validated continuation: **M10.9.4.1-E.2 Hotfix 1**.
- User confirmed compilation and all requested ordinary, focused and long-running gates passed on **2026-07-26**.
- Working source: **M10.9.4.1-E.3.1 Hotfix 1 Signed Electrical Protection Trajectory Audit CANDIDATE**.
- Current-v2 sustained profiles use **10 MWe**, **5 MWe = 50%**, **1.5 rpm full-load droop**, and **Bidirectional** grid coupling.
- Signed convention: positive = export/generation; negative = import/motoring; conversion loss remains non-negative.
- E.3.1 adds four explicit trajectory tests and writes CSV/text evidence under `artifacts/e3-protection-trajectories`.
- E.3.1 adds no reverse-power, underfrequency or loss-of-synchronism function.
- Historical/default profiles remain **1,000 MWe**, null/GenerationOnly and non-negative in presentation.

## Next work

Validate E.3.1 with:

```text
dotnet build
scripts\run-electrical-protection-trajectory-audit.cmd
dotnet test
scripts\run-generator-grid-bidirectional-tests.cmd
scripts\run-turbine-admission-authority-audit.cmd
scripts\run-turbine-governor-actuator-tracking-audit.cmd
scripts\run-gameplay-long-tests.cmd
scripts\run-operational-envelope-audit.cmd
scripts\run-reference-plant-scale-audit.cmd
```

Preserve or paste the generated `*.summary.txt` files. Use those values to design E.3.2 pickup, reset, delay and supervision. Do not invent relay thresholds before the evidence is reviewed.

## Non-negotiable rules

- deterministic fixed timestep;
- no plant physics in Avalonia;
- canonical simulation owners remain unique;
- protection has priority;
- explicit legacy/current versioning;
- no hidden repair;
- no weakened tests, floors or protection thresholds;
- deliver the complete project ZIP and include a `.cmd` script for any deletion/rename operation.
