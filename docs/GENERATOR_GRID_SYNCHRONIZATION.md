# Generator, Grid & Synchronization Physics

## Scope

M4.5 adds the first explicit electrical domain over the fully validated M4.4 secondary-cycle stack. It couples each embedded M4.2 turbine rotor to exactly one lumped synchronous generator and one deterministic infinite-bus grid boundary without bypassing M4.3/M4.4 composition.

M4.5 intentionally remains manual-first. Automatic excitation, governor/load control, synchronizer relays, reverse-power protection and breaker protection sequencing belong to M5.

## Canonical topology

```text
M4.2 turbine rotor
        ↓ shaft coupling
synchronous generator
        ↓ generator breaker
infinite-bus grid boundary
```

`GeneratorGridSystemDefinition` owns the validated `CondensateFeedwaterSystemDefinition` and requires exactly one `SynchronousGeneratorDefinition` per embedded M4.2 rotor. Generator and breaker identifiers are unique and validated eagerly.

## Electrical quantities

M4.5 adds strongly typed:

- `Frequency` in hertz;
- `ElectricPotential` in volts with explicit kV conversion;
- `PhaseAngle` normalized to `[0, 2π)` plus typed `PhaseAngleDifference` in `[0, π]` for deterministic synchronization windows.

No wall-clock phase source exists. Grid and generator phase are explicit simulation state and advance only by the fixed simulation timestep.

## Frequency and phase

Generator electrical frequency is derived directly from mechanical rotor speed and pole-pair count:

```text
f_e = polePairs × omega_mech / (2π)
```

Generator electrical phase advances from the rotor average angular speed over the deterministic step. Grid phase advances from the configured infinite-bus frequency over the same timestep.

This provides replay-stable diagnostics for:

- generator frequency;
- grid frequency;
- frequency mismatch;
- generator/grid phase angles;
- shortest phase mismatch.

## Synchronization and breaker closure

A breaker close command is accepted only when the committed pre-close state satisfies all configured windows:

- maximum frequency difference;
- maximum phase-angle difference;
- maximum line-voltage difference.

A rejected close command is observable in the snapshot and does not silently alter state. An explicit open command disconnects the breaker. M4.5 does not introduce automatic synchronizers or protection trips.

## Shaft-to-electrical conversion

While M4.5 is active, the legacy M4.2 manually commanded `ExternalLoadTorque` must be zero. This prevents double ownership of rotor loading.

For a connected generator, requested electrical power is converted to a deterministic electromagnetic load-torque command using generator efficiency and rated rotor speed. That torque is passed into the existing M4.2 rotor integrator; M4.5 does not create a second mechanical integrator.

After the turbine step, actual mechanical load power is taken from the M4.2 rotor snapshot. Electrical export and generator conversion losses are then reconciled explicitly:

```text
mechanical input power
= electrical export power
+ conversion loss power
```

`GeneratorElectricalAudit` exposes the residual instead of correcting it.

## State ownership

`GeneratorGridState` owns only electrical-domain state:

- deterministic grid phase;
- per-generator electrical phase;
- per-generator breaker closed/open state.

Rotor speed and kinetic energy remain exclusively in `TurbineExpansionState`. Fluid/thermal inventories remain exclusively in `PlantState`.

## Composition rule

M4.5 wraps the full validated M4.4 secondary-cycle solve and rewrites only the nested M4.2 rotor-load seam:

```text
committed PlantState
+ committed TurbineExpansionState
+ committed GeneratorGridState
+ complete M4.4 inputs
+ manual M4.5 generator inputs
        ↓
synchronization/breaker decision from committed state
        ↓
rewrite nested M4.2 external-load torque only
        ↓
M4.4 → M4.3 → M4.2 → M4.1 → M3 composition
        ↓
one plant-network integration + one rotor integration
        ↓
electrical power/loss audit + candidate electrical state
```

The higher-phase supplemental thermofluid source-term seam is preserved, so later M4 milestones can still compose before the same single `PlantNetworkOrchestrator` integration.

## Deferred fidelity

M4.5 intentionally does not yet model:

- detailed synchronous-machine dq-axis/transient reactance physics;
- field/excitation dynamics or AVR;
- governor/load controller dynamics;
- automatic synchronizer logic;
- protection relays, reverse power, pole slip or breaker-failure logic;
- detailed grid network/load-flow dynamics.

Those features can replace or extend the current seams without moving ownership of rotor, electrical or thermofluid state.
