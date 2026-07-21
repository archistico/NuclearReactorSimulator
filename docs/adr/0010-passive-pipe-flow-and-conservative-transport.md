# ADR 0010 — Passive pipe flow and conservative transport

- Status: Accepted for M1.3
- Date: 2026-07-20

## Context

M1.2 established lumped fluid nodes with conserved mass/internal energy and thermodynamic closure. M1.3 must connect nodes without introducing pumps, valves, a premature detailed pipe momentum solver, or a water/steam property model that belongs to later milestones.

The connection model must be deterministic, bidirectional and exactly conservative at the network-balance boundary.

## Decision

Represent each passive connection with an immutable `PipeDefinition` containing stable endpoint identifiers and a strictly positive `QuadraticHydraulicResistance`.

Use the lumped relation:

```text
Δp = R · m_dot · |m_dot|
```

The declared from/to endpoints define only the positive sign convention. The actual flow direction follows the sign of the pressure difference.

`PipeFlowSolver` is stateless and solves from two committed endpoint states. It returns a `PipeFlowResult` containing signed mass flow, signed advected internal-energy rate, and equal-and-opposite `FluidNodeBalance` values.

Until the M1.7 water/steam property model exists, transported energy uses the upstream node's specific internal energy. This preserves exact energy transfer between lumped nodes without pretending that M1.3 already has a complete enthalpy/phase formulation.

All future network connections in one physical step must be evaluated from the same committed pre-step state before node balances are integrated.

## Consequences

Positive:

- flow reversal emerges naturally from pressure reversal;
- mass and energy transfers are exactly conservative by construction;
- solver output composes directly with `FluidNodeBalance` and `FluidNodeIntegrator`;
- no connection-order dependence is introduced at the pipe-solver boundary;
- later valves can modulate effective resistance without changing node ownership;
- later detailed pressure-loss models can replace the aggregate coefficient behind a stable boundary.

Trade-offs:

- the pipe has no stored mass, momentum or transport delay;
- quadratic resistance is an educational lumped approximation;
- advected internal energy is not yet the final open-system enthalpy treatment;
- elevation, friction correlations, choking and two-phase losses are deferred.
