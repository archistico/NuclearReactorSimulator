# ADR 0124 — Current-v2 turbine expansion advects enthalpy and keeps shaft work single-owned

## Status

Accepted as the M10.9.4.1-G.4 candidate decision on 2026-08-16. Validation is pending.

## Context

G.1 established the accepted open-control-volume relation `h = u + p/rho`. G.2 Hotfix 2 migrated passive hydraulic paths and proved separate pump-work ownership. G.3 migrated every remaining non-turbine current-v2 owner while leaving turbine expansion isolated because turbine expansion is also the explicit thermofluid-to-mechanical work boundary.

The historical M4.2 turbine solver removed `u*m_dot` from the inlet, added `(u-w_shaft)*m_dot` at exhaust and declared `-P_shaft` as thermofluid external power. That bookkeeping was internally conservative but remained on the pre-Phase-G internal-energy advection convention.

## Decision

1. `TurbineStageGroupDefinition` owns a backward-compatible `FluidEnergyTransportMode`.
2. Historical definitions default to `SpecificInternalEnergy`.
3. Both current-v2 sustained profiles select `SpecificEnthalpy` for turbine expansion.
4. In current-v2, turbine inlet advection is `h_in*m_dot`.
5. Exhaust advection is `(h_in-w_shaft)*m_dot`, where `w_shaft = P_shaft/m_dot` for positive flow.
6. Shaft work remains a separate, explicit thermofluid-to-rotor transfer exactly once through the existing source-term and mechanical-audit ownership.
7. Fluid-node inventories continue to store internal energy; G.4 changes boundary transport, not the conserved node quantity.
8. The validated thermodynamic-work law, efficiency, governor, generator/grid coupling and electrical protections are not retuned in G.4.
9. Historical snapshot diagnostics are preserved. New explicit fields expose selected inlet/exhaust advected specific energy, inlet flow work, inlet enthalpy and a turbine energy-ownership residual.
10. Successful G.4 validation closes the staged Phase G runtime migration. Phase H then measures stiffness before any numerical-integration change.

## Consequences

- Current-v2 completes the accepted open-control-volume energy convention through turbine expansion without double-counting shaft work.
- Legacy/current-v1 replay identity remains on the historical transport convention.
- Any post-G.4 operating-point movement is treated as physical evidence from the convention migration, not automatically compensated by turbine/governor retuning.
- Phase H owns any substepping/semi-implicit decision; G.4 does not change timestep integration.
