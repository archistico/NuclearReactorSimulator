# Hydraulic Component Faults

M8.2 is the first concrete fault-effect layer on top of the validated M8.1 deterministic scheduler. It adds no new hydraulic graph and no second inventory integrator.

## Fault type IDs

| Fault type | Target | Parameter | Active effect |
|---|---|---|---|
| `hydraulic.pump-trip` | canonical pump ID | none | forces pump not running and zero effective speed |
| `hydraulic.pump-degradation` | canonical pump ID | `capacityFraction` in `[0,1]` | scales commanded pump speed/capability |
| `hydraulic.valve-fail-open` | canonical valve ID | none | forces fully open |
| `hydraulic.valve-fail-closed` | canonical valve ID | none | forces closed |
| `hydraulic.valve-stuck` | canonical valve ID | none | holds committed position captured at activation |
| `hydraulic.path-restriction` | canonical valve ID | `maximumOpenFraction` in `[0,1]` | clamps valve-controlled path opening |
| `hydraulic.path-blockage` | canonical valve ID | none | clamps valve-controlled path to zero opening |
| `hydraulic.node-leak` | canonical fluid-node ID | `massFlowKgPerSecond > 0` | removes declared mass flow and carried internal energy |

Parameters are invariant-culture strings because they live in versioned `ScenarioFaultDefinition` data. Invalid or missing parameters fail closed at activation.

## Per-step ordering

```text
committed measured frame/state
        ↓
M5 normal control + protection command arbitration
        ↓
M8.2 active hydraulic component constraints
        ↓
existing M4/M3 physical solvers
        ↓
one PlantNetworkOrchestrator inventory integration
        ↓
next committed snapshot
```

This distinction matters: a protection system can correctly command a stop valve closed while a fail-open mechanical fault prevents the actual valve from reaching that commanded state. Protection remains the command owner; the component fault owns only the failure constraint.

## Pumps

Pump faults never write mass flow or pressure. They constrain the canonical `PumpState` seen by the already validated `PumpFlowSolver`. A trip forces `IsRunning = false` and zero effective speed. Degradation multiplies the normally commanded speed by a deterministic capacity fraction.

## Valves and restricted paths

Valve faults never calculate flow. They constrain canonical mechanical position before `ValveFlowSolver`. `valve-stuck` captures the committed position at the exact activation boundary. Restriction/blockage is intentionally limited in M8.2 to valve-controlled paths so topology and resistance ownership are not duplicated.

## Selected leaks

A selected leak is a bounded prescribed outflow from a canonical fluid node. Each active leak contributes:

- negative external mass flow;
- negative node energy flow equal to the removed mass flow times the committed source-node specific internal energy.

These terms are combined with existing staged source terms before the single plant-network integration. Mass and energy therefore remain visible in the existing audit. M8.5 owns larger educational break/LOCA-class models, depressurization refinements and explicit break-flow limitations.

## Clearance semantics

Clearing a fault removes the active forcing. It does not restore a saved physical state. For example, a valve forced closed remains physically closed until an authoritative command later moves it. This avoids hidden state teleportation and preserves deterministic state ownership.

## Demonstration pack

`HydraulicComponentFaultScenarioPack.Demonstration` reuses the validated M7.6 `stable-low-load-parallel-operation` v1 initial condition and schedules non-overlapping examples of every M8.2 fault type. The pack is deterministic scenario data; it does not force expected plant outcomes.
