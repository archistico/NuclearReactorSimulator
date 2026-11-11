# Nuclear Reactor Simulator

An educational desktop simulator of a reduced, RBMK-inspired nuclear power plant,
built with **C#**, **.NET 10** and **Avalonia**.

Follow the plant from reactor heat to electrical generation: control reactivity,
watch coolant circulation and steam production, operate the turbine and generator,
and observe how automation and protection systems respond to changing conditions.
The control room brings plant instruments, alarms, trends and operator commands
together in one interface.

## Explore the plant

- **Reactor core:** neutron kinetics, control rods, decay heat and xenon dynamics.
- **Water and steam cycle:** coolant circulation, steam drums, turbine admission,
  condenser and feedwater systems.
- **Electrical generation:** turbine and rotor dynamics, generator operation and
  connection to the grid.
- **Control room:** plant overview, subsystem panels, instrumentation, automatic
  control loops, alarms and protective actions.
- **Scenarios and sessions:** fault scenarios, training missions and scoring,
  session saving, checkpoints and replay.

The models emphasize internal consistency, conservation and repeatable behaviour.
They use reduced physical approximations for education; their scope and limitations
are described in [Known model limitations](docs/KNOWN_MODEL_LIMITATIONS.md).

## Run the simulator

Install the **.NET 10 SDK**; the SDK selection policy is in `global.json`.
From the repository directory, run:

```powershell
dotnet restore
dotnet run --project src/NuclearReactorSimulator.App
```

The [Italian user manual](docs/usermanual/MANUALE_UTENTE_NUCLEAR_REACTOR_SIMULATOR.md)
explains the controls and operating workflows.

## How it is built

The simulation advances in deterministic 10 ms steps, independently of the UI
refresh rate. Physical state and component balances belong to the simulation;
application services coordinate operator commands and sessions. Versioned session
data supports reproducible replay, and the plant can be exercised without the UI.

| Project | Responsibility |
| --- | --- |
| `NuclearReactorSimulator.Domain` | Plant state, quantities and domain contracts |
| `NuclearReactorSimulator.Simulation` | Physical models, integration and plant dynamics |
| `NuclearReactorSimulator.Application` | Runtime orchestration, commands and presentation data |
| `NuclearReactorSimulator.Infrastructure` | Persistence and external adapters |
| `NuclearReactorSimulator.App` | Avalonia desktop application and control room |

Read the [architecture](docs/ARCHITECTURE.md),
[plant composition](docs/PLANT_COMPOSITION.md) or
[technical documentation](docs/README.md) for details.

## Build and test

```powershell
dotnet build --configuration Release
dotnet test --configuration Release
```
