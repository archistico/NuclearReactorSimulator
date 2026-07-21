# Primary-Circuit Mnemonics

M6.4 adds the first process-mimic workspace for the canonical M3 primary circuit while preserving the M6 presentation boundary.

## Presentation flow

```text
M5.7 immutable automatic-operation snapshot
        ↓
ControlRoomSnapshotProjector
        ├─ measured M5.1 loop/drum instruments
        └─ explicitly labelled model diagnostics
        ↓
PrimaryCircuitPanelSnapshot
        ↓
Avalonia mnemonic + typed Application command intents
```

## Measured versus diagnostic data

Measured instruments are resolved by semantic M5.1 source id and preserve signal validity/quality:

- `main-circulation-loop/{id}/total-pump-flow`;
- `main-circulation-loop/{id}/header-pressure-rise`;
- `steam-drum/{id}/pressure`;
- `steam-drum/{id}/level`.

The following are intentionally presented as model diagnostics rather than pretending to be plant instrumentation: absolute header pressures, MCP effective speed/flow/boost, branch/channel flow and phase details, steam-drum temperature/separation flows, total primary inventory/boundary flows and canonical valve mechanical state.

## Topology-aware mnemonic

The panel projects canonical main-circulation loop identity, MCP membership, fuel-channel-group branches and steam-drum ownership. Valve presentation is filtered from the canonical plant topology: only valves whose hydraulic endpoints touch a primary-circuit node are shown. An empty valve list is valid and must not be filled with synthetic equipment.

## Operator commands

MCP START/RUN and STOP controls emit `ControlRoomCommand` values targeted to `ControlRoomCommandTargetKind.Pump`. They do not mutate `PumpState` and do not execute hydraulics. A runtime coordinator may later translate those intents into the validated M5.3 command/arbitration path. M3 remains the sole hydraulic/inventory integrator.

## Flow direction

Direction labels are derived only from the sign of the presented flow value. Measured loop direction uses the measured loop-flow channel; branch direction is explicitly a model diagnostic. No UI-side threshold or protection semantics are inferred.
