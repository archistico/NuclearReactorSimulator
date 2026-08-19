# Documentation map

This directory is split between **current engineering documentation** and **historical provenance**.

## Start here

- `PROJECT_STATUS.md` — current validated baseline, active candidate and policy state.
- `PROJECT_HANDOFF.md` — exact continuation point and validation instructions.
- `NEW_CHAT_START.md` — compact restart bootstrap for a new conversation.
- `ROADMAP.md` — remaining Phase-I work and the planned M10.9.5–M10.9.8 sequence.
- `ARCHITECTURE.md` — stable architecture and state-ownership boundaries.
- `KNOWN_MODEL_LIMITATIONS.md` — current model limitations and unresolved engineering debt.

## Active candidate

Current candidate documentation lives in `current/`.

- `current/H30_RQ1_PRODUCTION_POLICY_REREVIEW.md`
- `current/H30_RQ1_VALIDATION_CHECKLIST.md`
- `current/H30_RQ1_STATIC_REVIEW.md`

H.30 RQ1 proposes promoting exact v3 corrected-commit to the authoritative desktop production default because Phase-I evidence revealed a reproducible exact-v2 continuity defect. Exact v2 remains the rollback/reference identity. The proposal is not authoritative until its local validation gate is explicitly reported green.

## Stable technical documentation

Top-level subsystem documents describe the model itself rather than milestone chronology. Important entry points include:

### Simulation / physics

- `PHYSICAL_QUANTITIES.md`
- `WATER_STEAM_MODEL.md`
- `FLUID_NODES.md`
- `PIPES_AND_FLOW.md`
- `PLANT_COMPOSITION.md`
- `PLANT_NETWORK_ORCHESTRATION.md`
- `NEUTRON_KINETICS.md`
- `REACTIVITY_MODEL.md`
- `THERMAL_POWER.md`
- `DECAY_HEAT.md`
- `IODINE_XENON_DYNAMICS.md`
- `INTEGRATED_PRIMARY_CIRCUIT.md`
- `STEAM_DRUMS.md`
- `MAIN_STEAM_NETWORK.md`
- `TURBINE_EXPANSION_AND_ROTOR.md`
- `CONDENSER_VACUUM_HOTWELL.md`
- `CONDENSATE_FEEDWATER_TRAIN.md`
- `GENERATOR_GRID_SYNCHRONIZATION.md`

### Operations / protection / scenarios

- `INITIAL_CONDITIONS_SCENARIO_FRAMEWORK.md`
- `PROTECTION_INTERLOCKS_TRIPS_SCRAM.md`
- `DETERMINISTIC_FAULT_INJECTION_FRAMEWORK.md`
- `RECORDER_CHECKPOINT_FULL_REPLAY.md`
- `POST_INCIDENT_ANALYSIS.md`
- `GAMEPLAY_LONG_RUNNING_SYSTEM_TESTS.md`
- `REFERENCE_PLANT_SCALE_CONTRACT.md`

### HMI / operator computer

- `OPERATOR_EXPERIENCE_HMI_ARCHITECTURE.md`
- `HMI_VISUAL_DESIGN_SYSTEM.md`
- `INTERACTIVE_FULL_PLANT_MIMIC.md`
- `SUBSYSTEM_ENGINEERING_SCHEMATICS.md`
- `OPERATOR_COMPUTER_INTEGRATED_UI.md`
- `OPERATOR_CONTROL_STATE_SYNCHRONIZATION_USABILITY.md`

## Structured records

- `adr/` — architectural decision records. These are retained even when later decisions supersede earlier ones.
- `milestones/` — milestone summaries.
- `reference/` — reference material used by current docs.
- `research/` — research notes that are not production contracts.
- `usermanual/` — user-facing manual material.

## Historical archive

`history/` is deliberately **not** a current-status source.

- `history/m10.9.4.1/` contains the detailed A–I numerical-hardening notes, checklists, static reviews and hotfix records that previously crowded the `docs/` root.
- `history/project/` contains superseded snapshots of high-level documents kept for comparison.

Historical documents remain useful for provenance and frozen-evidence reasoning, but current decisions always come from `PROJECT_STATUS.md`, `PROJECT_HANDOFF.md` and the active candidate documentation.

## Documentation rule

When a milestone is complete, keep its detailed record for provenance but do not keep repeating its full chronology in `README.md`, `PROJECT_STATUS.md` or `ROADMAP.md`. Those files should answer only:

1. What is validated now?
2. What is the current candidate?
3. What production policy is authoritative?
4. What remains unresolved?
5. What is the next step?
