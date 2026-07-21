# Turbine, Generator & Electrical Panels

M6.5 completes the main process/electrical operating workspaces above the validated M4/M5 ownership boundaries.

## Presentation flow

```text
M5.7 immutable automatic-operation snapshot
        ↓
ControlRoomSnapshotProjector
        ├─ measured M5.1 instruments
        ├─ explicitly labelled M4/M5 model diagnostics
        └─ M5.5 trip state
        ↓
TurbineSecondaryPanelSnapshot + ElectricalPanelSnapshot
        ↓
Avalonia workspaces + typed Application command intents
```

Avalonia continues to reference Application presentation records only. It does not reference Simulation assemblies, execute turbine/generator physics, derive synchronization rules or mutate breaker/rotor state.

## Turbine and secondary-cycle workspace

Measured instruments are resolved from canonical M5.1 semantic sources:

- `plant/turbine/total-shaft-power`;
- `plant/condenser/total-heat-rejection`;
- `turbine-rotor/{id}/speed`;
- `condenser/{id}/pressure`;
- `condenser/{id}/vacuum`;
- `condenser/{id}/hotwell-mass`.

The following remain explicitly labelled **model diagnostics** because M5.1 does not expose dedicated channels for them: main-steam line flow/ΔP, admission-valve positions/flows, turbine-inlet state, stage-group flow/power, condensation flow, secondary temperatures, condensate/feedwater pump details and inventories.

The mnemonic preserves the authoritative M4 path:

```text
steam drums
    ↓
main steam lines / headers
    ↓
stop → control → admission valves
    ↓
turbine stage groups / rotor
    ↓
condenser / hotwell
    ↓
condensate + feedwater pumps
    ↓
steam drums
```

Turbine trip presentation observes M5.5 state. A turbine-trip button emits an Application intent only; M5.5 remains the protection owner and M4.1/M4.2 remain the physical valve/rotor owners.

## Generator and electrical workspace

Measured instruments:

- `plant/generator/gross-electrical-output`;
- `generator/{id}/frequency`;
- `generator/{id}/electrical-output`.

Explicit model diagnostics include grid reference frequency/voltage/phase, generator terminal/grid voltage, phase difference, mechanical input, conversion loss, synchronization-window result and breaker state.

Breaker close is fail-closed at the presentation boundary when:

- no runtime/generator target exists;
- generator trip is active;
- the published M4.5 synchronization permissive is false;
- the breaker is already closed.

This UI gating is only an operator-safety presentation rule. **M4.5 remains the final authority** that validates synchronization and accepts or rejects breaker closure.

## Synchronization and load command intents

M6.5 adds typed operator intents for:

- turbine speed raise/lower, targeted to a canonical turbine rotor;
- generator load raise/lower, targeted to a canonical generator;
- generator breaker close/open, targeted to a canonical breaker;
- turbine trip and generator trip.

These are command seams only. The shell dispatcher records them; later runtime integration may translate speed/load intents into validated M5.4 setpoint changes and trip/breaker intents into M5.5/M4.5 paths. The UI does not define physical ramp rates, MW increments, governor laws or synchronization tolerances.

## Ownership rules

- M4.1 owns canonical steam/admission topology and valve mechanics.
- M4.2 owns turbine expansion and rotor dynamics.
- M4.3 owns condenser/vacuum/hotwell physics.
- M4.4 owns condensate/feedwater return through canonical pumps/inventories.
- M4.5 owns generator/grid phase, synchronization and breaker physics.
- M5.4 owns normal automatic turbine/feedwater control.
- M5.5 owns trip/interlock arbitration.
- M6.5 owns presentation and operator command intents only.
