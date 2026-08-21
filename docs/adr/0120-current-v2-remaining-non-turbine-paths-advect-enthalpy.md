# ADR 0120 — Current-v2 remaining non-turbine paths advect enthalpy with explicit work and boundary ownership

## Status

Accepted / validated with M10.9.4.1-G.3

## Context

G.1 validated `h = u + p/rho` as the target open-control-volume convention and quantified the missing flow-work term. G.2 Hotfix 2 validated enthalpy advection for passive current-v2 pipes and valve paths, with exact transfer closure and separate pump-work evidence.

Pump paths, steam-drum separation, external boundaries, condenser phase change, atmospheric relief and turbine bypass still used the historical internal-energy convention. Migrating these together requires explicit ownership rules so pump work, heat rejection and external power are not counted twice.

## Decision

Add definition-owned, backward-compatible `FluidEnergyTransportMode` arguments to every remaining non-turbine transport owner. The historical default is `SpecificInternalEnergy`; the two current-v2 sustained profiles explicitly select `SpecificEnthalpy`.

Fluid nodes continue to store mass and internal energy. Solvers calculate and expose internal energy, flow work, enthalpy and the selected applied energy rate.

Ownership remains:

- pump hydraulic fluid work: one separate fluid-network contribution;
- pump shaft demand: one separate mechanical demand;
- condenser heat rejection: selected steam removal minus selected condensate addition, declared once externally;
- relief: signed external mass and energy exchange;
- bypass and drum separation: equal-and-opposite internal transfers;
- turbine expansion and shaft work: unchanged and deferred to G.4.

A positive enthalpy-mode feedwater boundary requires explicit incoming enthalpy because its upstream external thermodynamic state is not represented by a canonical plant node.

## Consequences

- Current-v2 non-turbine trajectories change physically and require all cumulative gates.
- Legacy/current-v1 definitions preserve historical mode through optional defaults.
- Diagnostics can distinguish `u*m_dot`, `(p/rho)*m_dot` and selected `h*m_dot` for every migrated owner.
- G.3 can prove transfer, heat and external-boundary ownership independently before turbine work changes.
- G.4 remains a narrow turbine-expansion/shaft-work migration rather than a mixed whole-network retune.
