# Documentation

The documentation has one rule: **current state is written once**. Historical milestone detail is preserved, but it is not repeated across status, handoff, roadmap and candidate files.

## Read these first

1. **`PROJECT.md`** — authoritative current checkpoint, production policy, active candidate, validation commands and continuation rule.
2. **`ROADMAP.md`** — future work only.
3. **`KNOWN_MODEL_LIMITATIONS.md`** — unresolved limitations only.
4. **`ARCHITECTURE.md`** — stable architecture, layers and ownership boundaries.

If two historical documents disagree with `PROJECT.md`, `PROJECT.md` is the current source.

## Forward execution plans

The approved post-Phase-I implementation sequence is documented in `ROADMAP.md`. Detailed milestone contracts are:

- `milestones/M10.9.5.md` — Contextual Command Consequence Model;
- `milestones/M10.9.6.md` — Operational Challenge & Energy-Demand Framework;
- `milestones/M10.9.7.md` — Mission & Performance Workstation;
- `milestones/M10.9.8.md` — Integrated Human-Automation-HMI Validation Gate;
- `milestones/M11.md` — Release Hardening.

These are planning contracts, not current-status sources. `PROJECT.md` remains authoritative for what is active now. The future extreme-operation/damage/accident sequence remains non-blocking and is documented in `FUTURE_GAMEPLAY_CONTROL_ROOM_AND_ACCIDENT_DIRECTION.md`.

## Technical reference

The remaining top-level technical documents describe model areas rather than project chronology. Use them when working on a subsystem.

### Physics and plant model

`PHYSICAL_QUANTITIES.md`, `WATER_STEAM_MODEL.md`, `FLUID_NODES.md`, `PIPES_AND_FLOW.md`, `PLANT_COMPOSITION.md`, `PLANT_NETWORK_ORCHESTRATION.md`, `NEUTRON_KINETICS.md`, `REACTIVITY_MODEL.md`, `THERMAL_POWER.md`, `DECAY_HEAT.md`, `IODINE_XENON_DYNAMICS.md`, `INTEGRATED_PRIMARY_CIRCUIT.md`, `STEAM_DRUMS.md`, `MAIN_STEAM_NETWORK.md`, `TURBINE_EXPANSION_AND_ROTOR.md`, `CONDENSER_VACUUM_HOTWELL.md`, `CONDENSATE_FEEDWATER_TRAIN.md`, `GENERATOR_GRID_SYNCHRONIZATION.md`.

### Operations, protection and scenarios

`INITIAL_CONDITIONS_SCENARIO_FRAMEWORK.md`, `OPERATIONAL_CHALLENGE_LIFECYCLE.md`, `OPERATIONAL_CHALLENGE_ENERGY_DEMAND.md`, `OPERATIONAL_CHALLENGE_SCORING.md`, `PROTECTION_INTERLOCKS_TRIPS_SCRAM.md`, `DETERMINISTIC_FAULT_INJECTION_FRAMEWORK.md`, `RECORDER_CHECKPOINT_FULL_REPLAY.md`, `POST_INCIDENT_ANALYSIS.md`, `GAMEPLAY_LONG_RUNNING_SYSTEM_TESTS.md`, `REFERENCE_PLANT_SCALE_CONTRACT.md`.

### HMI and operator computer

`OPERATOR_EXPERIENCE_HMI_ARCHITECTURE.md`, `HMI_VISUAL_DESIGN_SYSTEM.md`, `INTERACTIVE_FULL_PLANT_MIMIC.md`, `SUBSYSTEM_ENGINEERING_SCHEMATICS.md`, `OPERATOR_COMPUTER_INTEGRATED_UI.md`, `OPERATOR_CONTROL_STATE_SYNCHRONIZATION_USABILITY.md`.

## Structured collections

- `adr/` — architectural decisions, including superseded decisions when provenance matters;
- `milestones/` — milestone summaries and approved forward milestone planning contracts; **not** current-status sources;
- `reference/` — supporting reference assets;
- `research/` — non-authoritative research notes;
- `usermanual/` — user-facing manual;
- `history/` — completed/superseded engineering chronology and old administrative snapshots.

## Evidence policy

Large generated audit payloads are not documentation and are not bundled in source candidates. Current contracts use:

- `../eng/frozen-evidence/ordinary/` for small immutable ordinary-test prerequisites;
- `../eng/frozen-evidence/large-payload-manifest.csv` for omitted large-trace identities;
- `../eng/evidence-manifests/` for compact decision/reference provenance;
- local `artifacts/` directories for generated validation output.

## Maintenance rule

Do not create another status/handoff/restart/candidate-summary file when the information belongs in `PROJECT.md`. Create a new document only when it has a distinct long-lived responsibility: architecture, subsystem reference, limitation register, ADR, user manual, research or historical provenance.

- `M10_9_5_3_MANUAL_VALIDATION_CHECKLIST.md` — focused manual HMI acceptance for M10.9.5.3 COMMANDS context-inspector/schematic integration.

- `M10_9_5_5_MANUAL_VALIDATION_CHECKLIST.md` — final manual HMI acceptance for M10.9.5 contextual command-consequence closure.
