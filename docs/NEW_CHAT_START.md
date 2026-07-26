# New Chat Start — Nuclear Reactor Simulator

We are continuing the **Nuclear Reactor Simulator** project.

## Read first

1. `docs/PROJECT_HANDOFF.md`
2. `docs/PROJECT_STATUS.md`
3. `docs/ROADMAP.md`
4. `docs/milestones/M10.9.4.1.md`
5. `docs/M10_9_4_1_D4_VALIDATION_CHECKLIST.md`
6. `docs/M10_9_4_1_D4_1_VALIDATION_CHECKLIST.md`
7. `docs/OPERATIONAL_ENVELOPE_NUMERICAL_HARDENING_PLAN.md`
8. `docs/REFERENCE_PLANT_SCALE_CONTRACT.md`
9. `docs/REFERENCE_PLANT_SCALE_EVIDENCE.md`
10. `docs/REFERENCE_PLANT_SCALE_MIGRATION_PLAN.md`
11. `docs/KNOWN_MODEL_LIMITATIONS.md`

## Exact checkpoint

- Current validated continuation: **M10.9.4.1-D.4**.
- Working source: **M10.9.4.1-D.4.1 CANDIDATE**; validation is pending.
- D.3.2 Hotfix 3: loaded desktop main-steam line **850 Pa·s²/kg²**; synchronization **1,000 Pa·s²/kg²**; 28% control-valve bias and 276.7 °C stop-out retained.
- D.4: typed STOP/ADMISSION OPEN/CLOSE, control-valve AUTO/MANUAL and explicit manual demand; finite travel and protection priority preserved.
- Ordinary suite: **944 passed / 0 failed / 17 explicit skipped**.
- All **17 unique explicit tests passed**: admission 3/3, governor 2/2, gameplay long runs 2/2, operational envelope 9/9, reference scale 2/2.
- Active scale contract: **1,000 MW nameplate, 150 rpm droop, 0.5 MW synchronizing correction, 2 MW/Hz damping, correction-only/generation-only coupling**.
- E.1: **10 MWe target accepted as a decision only**.
- E.2: **NOT IMPLEMENTED**. Do not describe 1.5 rpm droop, bidirectional motoring, signed power or -10..+10 MWe HMI as current behavior.
- ADR 0109 records E.1; ADR 0110–0111 are proposed E.2 designs.

## Next work

Validate **M10.9.4.1-D.4.1** first. The code now contains explicit STOP travel ownership, replay/checkpoint regressions, in-flight restoration and trip-reset travel resumption. Run the focused script, complete ordinary suite, all explicit audits and the manual TURBINE-station checklist. Do not promote it before explicit user confirmation.

After D.4.1 validation, implement **E.2** as one coordinated versioned migration covering 10 MWe nameplate, governor normalization, bidirectional coupling, signed torque/power, positive losses, HMI ranges and replay/checkpoint behavior. E.3 protection starts only after E.2 signed trajectories are validated.

## Non-negotiable architecture rules

- deterministic fixed timestep;
- no plant physics in Avalonia;
- Application dispatches typed intents and projects immutable presentation state;
- canonical simulation owners remain unique;
- protection has priority over normal/supervisory authority;
- historical/current behavior is versioned explicitly;
- no hidden runtime repair;
- no weakening of tests, floors or protection thresholds to obtain a pass.
