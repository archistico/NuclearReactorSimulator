# M10.9.4.1-G.1 — Open-Control-Volume Energy Convention & Gap Audit

## Purpose

G.1 begins the dedicated whole-network energy-transport migration without changing runtime physics. It freezes the accepted open-control-volume convention and measures the difference between the retained internal-energy advection law and enthalpy transport on representative current-v2 steam and liquid paths.

F.3 Hotfix 1 is the validated baseline.

## Accepted convention

Fluid-node inventories remain:

```text
mass M
internal energy U
specific internal energy u = U / M
```

For advective transport through an open control-volume boundary:

```text
specific flow work = p / rho
specific enthalpy  = h = u + p / rho
energy transport  = h * m_dot
```

For an internal transfer, the upstream and downstream energy rates are equal and opposite. Heat transfer, pump shaft work, turbine shaft work and external boundary power remain independent terms.

## New audit seam

`OpenControlVolumeEnergyTransportSolver` accepts two committed fluid-node states and a signed reference-direction mass flow. It publishes:

- actual upstream/downstream identity;
- upstream pressure and density;
- specific internal energy;
- specific flow work `p/rho`;
- specific enthalpy `h`;
- signed `u*m_dot`, `(p/rho)*m_dot` and `h*m_dot` rates;
- equal-and-opposite node balances for both the legacy and target conventions.

The solver does not mutate plant state and is not called by the runtime in G.1.

## Representative audit

The explicit audit samples four current-v2 paths:

1. `header` → `exhaust` at the validated F.3 full-open representative steam flow;
2. `header` → `stop-out` at the same steam flow;
3. `hotwell` → `feedwater-inventory` at 12 kg/s;
4. `feedwater-inventory` → `drum` at 12 kg/s.

The generated CSV quantifies the flow-work gap in kJ/kg and MW and confirms exact internal-transfer closure.

## Explicit exclusions

G.1 does not change:

- pipe, valve or pump source terms;
- turbine expansion energy accounting;
- condenser phase-change accounting;
- feedwater or steam-drum transfer accounting;
- F.2 relief or F.3 bypass transport;
- any current-v2 or legacy trajectory;
- protections, HMI, replay or checkpoints.

## Planned continuation

- G.2: migrate passive pipe/valve advection and audit pump hydraulic/shaft-work ownership;
- G.3: migrate internal/external boundaries, separation, condenser and bypass/relief paths;
- G.4: migrate turbine expansion with exact single-count shaft work and compare reference trajectories;
- Phase H: measure stiffness and timestep sensitivity only after the energy convention is coherent.
