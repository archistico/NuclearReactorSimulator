# New Chat Start — Nuclear Reactor Simulator

We are continuing the **Nuclear Reactor Simulator** project.

## Read first

1. `docs/PROJECT_HANDOFF.md`
2. `docs/PROJECT_STATUS.md`
3. `docs/ROADMAP.md`
4. `docs/milestones/M10.9.4.1.md`
5. `docs/M10_9_4_1_D4_VALIDATION_CHECKLIST.md`
6. `docs/OPERATIONAL_ENVELOPE_NUMERICAL_HARDENING_PLAN.md`
7. `docs/REFERENCE_PLANT_SCALE_CONTRACT.md`
8. `docs/REFERENCE_PLANT_SCALE_EVIDENCE.md`
9. `docs/REFERENCE_PLANT_SCALE_MIGRATION_PLAN.md`
10. `docs/KNOWN_MODEL_LIMITATIONS.md`

## Exact checkpoint

- Current validated continuation: **M10.9.4.1-D.4**.
- D.3.2 Hotfix 3: loaded desktop main-steam line **850 Pa·s²/kg²**; synchronization **1,000 Pa·s²/kg²**; 28% control-valve bias and 276.7 °C stop-out retained.
- D.4: typed STOP/ADMISSION OPEN/CLOSE, control-valve AUTO/MANUAL and explicit manual demand; finite travel and protection priority preserved.
- Ordinary suite: **944 passed / 0 failed / 17 explicit skipped**.
- All **17 unique explicit tests passed**: admission 3/3, governor 2/2, gameplay long runs 2/2, operational envelope 9/9, reference scale 2/2.
- Active scale contract: **1,000 MW nameplate, 150 rpm droop, 0.5 MW synchronizing correction, 2 MW/Hz damping, correction-only/generation-only coupling**.
- E.1: **10 MWe target accepted as a decision only**.
- E.2: **NOT IMPLEMENTED**. Do not describe 1.5 rpm droop, bidirectional motoring, signed power or -10..+10 MWe HMI as current behavior.
- ADR 0109 records E.1; ADR 0110–0111 are proposed E.2 designs.

## Next work

Implement **M10.9.4.1-D.4.1** first:

1. replay/checkpoint coverage for every valve command;
2. checkpoint during finite valve travel;
3. trip → request preserved → reset → travel resumes;
4. explicit stop-valve travel-rate ownership;
5. manual TURBINE-station usability validation.

Then implement **E.2** as one coordinated versioned migration covering 10 MWe nameplate, governor normalization, bidirectional coupling, signed torque/power, positive losses, HMI ranges and replay/checkpoint behavior. E.3 protection starts only after E.2 signed trajectories are validated.

## Non-negotiable architecture rules

- deterministic fixed timestep;
- no plant physics in Avalonia;
- Application dispatches typed intents and projects immutable presentation state;
- canonical simulation owners remain unique;
- protection has priority over normal/supervisory authority;
- historical/current behavior is versioned explicitly;
- no hidden runtime repair;
- no weakening of tests, floors or protection thresholds to obtain a pass.
