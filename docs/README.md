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

## Validation and engineering decisions

Start with the [validation summary](validation/README.md) and
[model decisions](validation/MODEL_DECISIONS.md). Exact-text M10 inputs
used by validators are indexed under [engineering contracts](../eng/contracts/m10/README.md).
