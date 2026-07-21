# ADR 0022 — Iodine/Xenon Is a Stateful Poison Inventory

## Status

Accepted for M2.8.

## Context

Xenon poisoning depends on operating history. Mapping current power directly to a xenon-reactivity percentage would erase iodine precursor memory, post-shutdown xenon buildup and neutron-flux-dependent burnup.

## Decision

Represent I-135 and Xe-135 as explicit immutable normalized inventories. Evolve them with a configurable reduced two-isotope model:

```text
dI/dt  = S_I - lambda_I I

dXe/dt = S_Xe + lambda_I I - (lambda_Xe + k_burn n) Xe
```

For constant committed fission power and neutron population during a fixed step, use the analytic finite-step solution.

Only Xe-135 inventory maps to a named `Xenon` reactivity contribution. The coefficient is signed configuration data. No plant-specific constants are embedded in the generic engine.

## Consequences

Positive:

- xenon becomes history-dependent rather than scripted;
- post-power-change transients emerge from state;
- neutron burnup is an explicit removal path;
- equilibrium initial conditions are reproducible;
- fixed-step determinism is preserved without solver tolerances;
- future zonal poison models can reuse the same reactivity composition boundary.

Trade-offs:

- inventories are normalized rather than absolute nuclide densities;
- spatial xenon oscillations are outside M2.8;
- constant-input-within-step staging introduces the same explicit one-step coupling boundary used elsewhere in M2.
