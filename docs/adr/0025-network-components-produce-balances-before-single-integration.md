# ADR 0025: Network components produce balances before single integration

- Status: Accepted
- Date: 2026-07-20

## Context

M3.1 introduced canonical plant topology and complete state ownership. The next risk is order-dependent multiphysics execution: if a pipe updates a node before another pipe, valve or pump is solved, component enumeration order becomes part of the physical model.

## Decision

For one logical plant-network step:

1. all component solvers read the same committed `PlantState`;
2. component solvers return signed balances/diagnostics only;
3. balances are accumulated deterministically by canonical topology ID;
4. each conserved fluid or thermal inventory is integrated exactly once;
5. fluid thermodynamic closure occurs only during that single integration;
6. a candidate `PlantState` is built only after all integrations succeed;
7. global conservation accounting is computed without silently correcting state.

Internal transfers must cancel globally. Explicit external energy crossings remain separately observable.

## Consequences

### Positive

- component registration order cannot alter endpoint state seen by a solver;
- parallel branches behave as simultaneous lumped connections for one fixed step;
- conservation audits have one unambiguous accounting boundary;
- future core/channel/drum models can compose through the same balance seam;
- transactional runtime semantics remain compatible with plant-level execution.

### Trade-offs

- the coupling is explicit across fixed steps rather than an iterative nonlinear network solve;
- very stiff future plant configurations may require smaller fixed steps or a later deterministic coupled solver;
- operational state changes must be staged explicitly before solving rather than mutating components mid-step.

## Rejected alternatives

### Sequential mutate-as-you-go component updates

Rejected because component order would change pressures, temperatures and therefore subsequent flows in the same logical step.

### Hidden conservation correction after integration

Rejected because it would mask modelling/numerical errors and make diagnostics less trustworthy.

### Immediate nonlinear whole-network iteration in M3.2

Rejected as premature. The current architecture first establishes deterministic staged composition and observable conservation before adding higher-fidelity coupled iteration if later validation requires it.
