# Full-Plant Steady-State Baseline

M4.7 closes the manually commanded M4 phase by establishing one deterministic reactor-to-grid state/snapshot boundary and an explicit fixed-input steady-state verification gate.

## State ownership remains unchanged

`FullPlantState` is an immutable envelope over the three already validated physical state owners:

```text
FullPlantState
├── PlantState                 thermofluid / thermal inventories
├── TurbineExpansionState      rotor mechanical state
└── GeneratorGridState         generator breaker / electrical phase state
```

It is not a fourth physical state model. `FullPlantSolver` delegates evolution to `IntegratedSecondaryCycleSolver` M4.6 and then packages the three candidate states under one canonical definition.

## Full-plant snapshot boundary

`FullPlantSnapshot` exposes:

- the complete nested M4.6 `IntegratedSecondaryCycleSnapshot`;
- the canonical candidate `PlantSnapshot`;
- candidate turbine mechanical state;
- candidate generator/grid electrical state;
- first-law heat-balance audit;
- derived plant-performance diagnostics.

This is the true-state snapshot boundary that M5 instrumentation can observe. M5 sensors/controllers must not bypass it by reading mutable implementation details from subsystem solvers.

## Reference operating point

`FullPlantReferenceOperatingPoint` binds:

- one canonical integrated definition;
- one initial `FullPlantState`;
- one fixed manual `IntegratedSecondaryCycleInputs` set;
- one deterministic timestep;
- explicit `FullPlantSteadyStateCriteria`.

No controller, hidden trim algorithm or corrective bookkeeping is embedded in the operating point.

## Long-run gate

`FullPlantLongRunRunner` repeatedly propagates all three candidate states and reports raw drift:

- total fluid-mass inventory drift;
- coupled thermofluid + rotor stored-energy drift;
- maximum rotor-speed drift from the initial reference;
- electrical-output drift;
- maximum absolute mass-closure residual;
- maximum absolute full energy-path closure residual;
- average nuclear heat, turbine shaft and electrical output power.

The runner never resets inventories, rotor speed, breaker state, phase or any other state to keep the reference condition stable.

`SteadyStateCriteriaSatisfied` is only a comparison of measured drift against configured limits. Failing the criteria does not alter the simulation result.

## Performance diagnostics

`FullPlantPerformanceDiagnostics` derives gross performance only from the already audited M4.6 power path:

- reactor nuclear thermal power;
- turbine shaft power;
- generator mechanical input;
- gross electrical export;
- condenser heat rejection;
- generator conversion loss;
- gross thermal efficiency when nuclear thermal input is positive;
- turbine-shaft/reactor-heat fraction;
- generator conversion efficiency;
- gross heat rate.

Ratios with a zero or non-positive denominator are reported as undefined (`null`). No artificial epsilon denominator is introduced.

## M4 gate

M4.7 is the validated final M4 gate. The manually commanded reactor-to-grid model has:

- one deterministic top-level state boundary;
- one top-level immutable snapshot boundary;
- closed and inspectable mass/energy accounting;
- fixed-input long-run drift verification;
- explicit plant-performance diagnostics;
- no automatic controls hidden in physics.

Automatic instrumentation/control/protection begins only in M5.
