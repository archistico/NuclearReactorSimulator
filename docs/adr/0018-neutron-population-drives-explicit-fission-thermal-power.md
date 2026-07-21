# ADR 0018 — Neutron population drives explicit fission thermal power

## Status

Accepted for M2.4 baseline candidate.

## Context

M2.3 establishes normalized neutron population as the dynamic output of point kinetics. The simulator now needs a clear boundary that converts that kinetic state into instantaneous fission thermal power and deposits the resulting energy into thermal domains.

A direct hidden mutation of fuel/coolant energy from the neutron solver would couple kinetics, thermal integration and plant topology. It would also make later decay-heat and spatial-power models difficult to separate and test.

## Decision

Introduce an explicit `FissionPowerSolver` between point kinetics and thermal-energy balances.

The solver:

1. receives `NeutronPopulation`;
2. applies a plant-supplied `FissionPowerCalibration`;
3. produces non-negative instantaneous fission thermal power;
4. partitions that power across a complete configured set of named heat destinations;
5. exposes immutable depositions that adapt to existing `ThermalEnergyBalance` or zero-mass `FluidNodeBalance` boundaries.

The initial calibration is linear:

```text
P_fission = P_reference × n / n_reference
```

No plant-specific rated power is hardcoded in the generic simulation engine.

Heat-deposition fractions are explicit, unique by target id, canonicalized, and required to sum to unity. The solver closes its own allocation exactly by assigning the deterministic residual to the final canonical destination.

## Consequences

Positive:

- point kinetics remains independent from thermal topology;
- fission power is directly inspectable and testable;
- thermal energy input is explicit at existing conservation boundaries;
- no fluid mass is created by heating;
- plant-specific calibration and heat partitioning remain configuration data;
- M2.5 decay heat can be added as a second independent thermal source instead of being hidden in fission power.

Trade-offs:

- the initial neutron-population-to-power relation is a lumped linear calibration;
- heat deposition is spatially lumped into configured target domains;
- no feedback loop is closed until later M2 milestones.

## Rejected alternatives

### Convert reactivity directly to thermal power

Rejected because reactivity must first evolve neutron population through kinetics.

### Let `PointKineticsSolver` mutate thermal bodies

Rejected because it couples neutronics to plant topology and violates subsystem separation.

### Include decay heat in the same power value

Rejected because fission power and post-fission decay heat have different state/history semantics and must remain independently observable.
