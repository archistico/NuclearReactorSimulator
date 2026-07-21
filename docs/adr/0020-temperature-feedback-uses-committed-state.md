# ADR 0020 — Temperature feedback uses committed-state measurements

## Status

Accepted for M2.6.

## Context

Temperature feedback closes the first bidirectional loop between reactor heat generation and neutron kinetics. A naive same-step iterative coupling would introduce convergence policy, iteration-count dependence and hidden numerical state before the project has a coupled multiphysics solver.

## Decision

1. Temperature feedback is represented by explicit named `ReactivityContribution` values.
2. Each feedback definition owns a reference temperature and a signed linear `TemperatureReactivityCoefficient`.
3. Coefficients are configuration data; the generic engine hardcodes no RBMK-specific sign or magnitude.
4. M2.6 evaluates fuel/coolant temperatures from the committed state at the beginning of the fixed timestep.
5. Reactivity is composed before point kinetics; subsequent fission/decay heat changes candidate thermal states for the next commit.
6. Multiple feedback sources are canonicalized by contribution kind and ID before composition.
7. No temperature feedback may write neutron population, power or thermal state directly.

## Consequences

- The first thermal-neutronic feedback loop is deterministic and replay-safe.
- A deliberate one-fixed-step coupling lag exists.
- Feedback diagnostics remain decomposable through the M2.1 reactivity model.
- Higher-fidelity nonlinear or iteratively coupled feedback can be introduced later without changing the current architectural boundaries.
