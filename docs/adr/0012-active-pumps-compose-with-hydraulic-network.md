# ADR 0012 — Active pumps compose with the existing hydraulic network

- Status: Accepted for M1.5 baseline candidate
- Date: 2026-07-20

## Context

M1.3 established one bidirectional quadratic pressure/flow law for passive paths. M1.4 preserved that law by treating valves as resistance modulation. M1.5 needs an active component capable of creating pressure rise and consuming shaft power without introducing an unrelated imposed-flow solver.

## Decision

Model a simplified centrifugal pump as:

1. an existing `PipeDefinition` hydraulic path;
2. an active pressure source whose magnitude follows normalized speed squared;
3. a positive quadratic internal resistance representing pump-curve droop;
4. a constant simplified efficiency used only to derive positive shaft-power demand.

The solved network relation is:

```text
P_from - P_to + Δp_rated·speed²
    = (R_pipe + R_internal) · m_dot · |m_dot|
```

Flow remains signed and may reverse naturally.

Mass transfer remains exactly conservative. The active pressure source is an external energy boundary, so endpoint energy balances sum to the signed hydraulic work rate supplied to or absorbed from the fluid. Negative hydraulic exchange does not produce regenerative shaft credit in M1.5.

## Consequences

- Pipes, valves and pumps share one hydraulic sign convention and pressure-driven network model.
- Pump speed affects pressure and flow through explicit affinity behavior rather than arbitrary flow commands.
- A stopped pump still behaves as a passive hydraulic restriction unless a future check valve isolates it.
- Shaft power is observable without introducing the later electrical system prematurely.
- Rotor inertia, cavitation, efficiency maps, loss heating and two-phase degradation remain replaceable future refinements.
