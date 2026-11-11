# Technical documentation

## Using the simulator

- [User manual (Italian)](usermanual/MANUALE_UTENTE_NUCLEAR_REACTOR_SIMULATOR.md)
- [Model scope and limitations](KNOWN_MODEL_LIMITATIONS.md)
- [Control room application](CONTROL_ROOM_APPLICATION_SHELL.md)
- [Operator computer interface](OPERATOR_COMPUTER_INTEGRATED_UI.md)
- [Sessions, checkpoints, replay and saving](OPERATOR_COMPUTER_SESSION_CHECKPOINT_REPLAY_SAVE.md)

## Understanding the model

- [Architecture and ownership](ARCHITECTURE.md)
- [Physical quantities and units](PHYSICAL_QUANTITIES.md)
- [Plant composition](PLANT_COMPOSITION.md)
- [Plant network orchestration](PLANT_NETWORK_ORCHESTRATION.md)
- [Reactor and primary control loops](REACTOR_PRIMARY_CONTROL_LOOPS.md)
- [Turbine, steam and feedwater control loops](TURBINE_STEAM_FEEDWATER_CONTROL_LOOPS.md)
- [Instrumentation, control and faults](INSTRUMENTATION_CONTROL_FAULTS.md)

## Finding a reference

The [document index](TOP_LEVEL_DOCUMENT_INDEX.md) lists every live top-level
reference. [Architectural decisions](adr/README.md) explain design choices;
[historical records](history/README.md) retain completed investigations and archived
documentation. Subsystem references remain separate so each model has a clear owner.

Engineering records have their own entry points: [PROJECT.md](PROJECT.md) holds the
current checkpoint and validation commands; [ROADMAP.md](ROADMAP.md) holds future
sequencing. Milestone plans are under `milestones/` and source reviews under `research/`.

## Historical validator compatibility

The following retained identifiers and literal excerpts are interfaces for older
validators, not current status or instructions. The complete historical prose
is in the [documentation archive](history/archives/README.md).

<!-- NRS-MARKER:DIAG3-REV1-DOC-NAV -->

<!-- NRS-MARKER:DIAG3-REV1-RETURNED-ADJUDICATION-DOCNAV -->

- `M10974_FINGERPRINT_V1_CROSS_HOST_DIAGNOSTIC1_RETURNED_EVIDENCE_ADJUDICATION1.md` - returned local/hosted diff adjudication proving a single culture-sensitive `VoidText` leaf and zero physical numeric drift. NRS-MARKER:DOCNAV-FPV1-DIAG1-ADJUDICATION1

- `M10974_FINGERPRINT_V1_CROSS_HOST_DETERMINISM_HOTFIX1.md` - REV2 presentation-only invariant-culture repair with V1 compatibility preservation, Exact-V9 transitive-anchor preservation and cross-culture/hosted-GREEN qualification. NRS-MARKER:DOCNAV-FPV1-HOTFIX1

NRS-MARKER:DOCNAV-R3-ENERGY-TRANSPORT-REPAIR-PLANNING1

```text
M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN1_P2_DECISION.md
M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN1_P2R_DECISION_PLAN_AMENDMENT2.md
M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN1_P2R2_DECISION_REENTRY2.md
M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN.md
PRE_M11_DEEP_ENGINEERING_SECTION_REVIEW_2.md
PRE_M11_PLANT_DYNAMICS_THERMAL_HYDRAULICS_REACTOR_PHYSICS_REVIEW.md
PRE_M11_TODREAS_KAZIMI_THERMAL_HYDRAULIC_DEEP_REVIEW_PASS1.md
PRE_M11_TODREAS_KAZIMI_THERMAL_HYDRAULIC_DEEP_REVIEW_PASS2.md
```
