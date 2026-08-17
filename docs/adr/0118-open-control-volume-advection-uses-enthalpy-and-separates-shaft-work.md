# ADR 0118 — Open-control-volume advection uses enthalpy and keeps shaft work separate

**Status:** Accepted with validated M10.9.4.1-G.1

## Context

The current network inventory contract stores mass and internal energy in every fluid node. Passive pipes, valve paths, boundaries and the new F.3 turbine bypass currently advect committed specific internal energy `u`. This is globally conservative for an internal equal-and-opposite transfer, but it omits the pressure-volume flow-work contribution required by the standard open-control-volume energy equation.

Phase G must introduce the missing contribution without double counting pump work, turbine work, heat transfer or external boundary power.

## Decision

The accepted target convention is:

```text
specific flow work = p / rho
specific enthalpy  = h = u + p / rho
advective energy rate = h * m_dot
```

Fluid-node inventories continue to store internal energy, not enthalpy. For an internal connection, the same signed enthalpy rate is removed from the upstream node and added to the downstream node, so global internal-transfer closure remains exact.

Shaft work, heat transfer and externally imposed boundary power remain separate source terms and must appear exactly once.

M10.9.4.1-G.1 is audit-only. It adds an isolated deterministic solver and representative current-v2 evidence but does not migrate any runtime component.

## Consequences

- G.1 quantifies the current `u*m_dot` versus target `h*m_dot` gap before physical trajectories change.
- Low-density high-pressure steam is expected to show a much larger gap than dense liquid.
- Later G increments can migrate passive transport, boundaries and work-producing/consuming components in controlled groups.
- Pump and turbine implementations must be reviewed during migration so their shaft-work terms are neither omitted nor counted twice.
- Legacy replay and reference trajectories remain unchanged in G.1.
