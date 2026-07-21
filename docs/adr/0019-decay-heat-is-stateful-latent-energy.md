# ADR 0019 — Decay heat is a stateful latent-energy subsystem

## Status

Accepted for M2.5 baseline candidate.

## Context

A fixed post-shutdown percentage cannot represent the dependence of decay heat on previous operating power and time. Decay heat must remain non-zero after the prompt/current fission source collapses, and its release must integrate cleanly into the deterministic fixed-step runtime.

A full isotope-depletion or standards-grade decay-heat implementation is outside the current educational scope and would prematurely bind the architecture to a specific fuel history and nuclear-data backend.

## Decision

Represent decay heat with a configurable set of equivalent first-order groups.

Each group owns latent stored decay energy `E_i` and follows:

```text
dE_i/dt = f_i * P_fission - lambda_i * E_i
```

where:

- `f_i` is a configuration-supplied generation fraction;
- `P_fission` is the current M2.4 fission thermal-power signal used as the production driver;
- `lambda_i` is a configuration-supplied decay constant;
- `lambda_i * E_i` is instantaneous emitted decay-heat power.

Use the analytic finite-step solution for constant input over each simulation timestep.

The step result must expose both:

1. integrated/average emitted heat for exact same-step energy deposition;
2. end-of-step instantaneous heat for diagnostics.

All emitted heat is distributed through a complete canonical named deposition partition. No plant-specific group constants are hardcoded in the engine.

## Consequences

Positive:

- shutdown heat persists naturally from stored history;
- long-operation equilibrium can be initialized exactly;
- no dependence on UI cadence or adaptive solver timing;
- exact per-step latent-energy bookkeeping is testable;
- M2.4 fission power and M2.5 decay heat remain separate responsibilities;
- future standards-grade decay-heat backends can replace the reduced-order model without changing thermal consumers.

Trade-offs:

- equivalent groups are an approximation, not isotope depletion;
- coefficients require future calibration for any specific plant;
- the current educational accounting treats latent decay-energy production as a separate nuclear source rather than subtracting it from M2.4 direct-fission heat deposition.
