# M10.9.4.1-G.4 — Turbine Expansion Enthalpy & Shaft-Work Ownership

## Status

**CANDIDATE** on the user-validated M10.9.4.1-G.3 baseline.

## Purpose

G.4 closes the final Phase G runtime gap: turbine expansion is the last current-v2 open-control-volume owner still using the historical `u*m_dot` convention after G.3.

The milestone does not introduce a new turbine-work law. It changes only the advected energy convention while preserving exact shaft-work ownership.

## Current-v2 control-volume convention

For one turbine stage group with positive flow:

```text
inlet advection      = h_in * m_dot
shaft transfer       = P_shaft
exhaust advection    = h_in * m_dot - P_shaft

therefore
inlet - exhaust - shaft = 0
```

Equivalently per unit mass:

```text
h_exhaust,advected = h_in - w_shaft
w_shaft = P_shaft / m_dot
```

Fluid nodes continue to store internal energy. The enthalpy appears only in the open-boundary transport term, exactly as established by G.1–G.3.

## Definition ownership

`TurbineStageGroupDefinition` now owns `FluidEnergyTransportMode`:

- `SpecificInternalEnergy` is the backward-compatible default;
- `SpecificEnthalpy` is enabled only by the two current-v2 sustained profiles.

## Snapshot evidence

`TurbineStageGroupSnapshot` retains the historical fields and adds:

- `EnergyTransportMode`;
- `InletSpecificFlowWork`;
- `InletSpecificEnthalpy`;
- `InletAdvectedSpecificEnergy`;
- `ExhaustAdvectedSpecificEnergy`;
- `FlowWorkRate`;
- `TurbineEnergyOwnershipResidual`.

The historical `ExhaustSpecificInternalEnergy` field remains a backward-compatible diagnostic. Under current-v2 enthalpy transport it is not the source-term energy applied to the exhaust node; `ExhaustAdvectedSpecificEnergy` is authoritative for that boundary transport.

## Work ownership

G.4 deliberately preserves:

- thermodynamic work definition: 2.1 kJ/(kg K), gamma 1.3, maximum internal-energy extraction fraction 0.8 where configured;
- stage nominal specific work: 500 kJ/kg;
- current-v2 stage efficiency: 86%;
- rotor integration;
- passive mechanical loss;
- governor;
- generator/grid coupling;
- protection settings.

No compensating retune is introduced.

## Compatibility

Historical and current-v1 definitions remain `SpecificInternalEnergy` by default. Only the current-v2 desktop sustained-generation and grid-synchronization sustained profiles opt into turbine enthalpy advection.

## Audit

The explicit G.4 audit writes:

```text
artifacts/g4-turbine-expansion-enthalpy/
    01-current-v2-turbine-expansion-enthalpy-and-shaft-work.csv
    01-current-v2-turbine-expansion-enthalpy-and-shaft-work.summary.txt
```

It records both current-v2 profiles and proves:

- stage transport mode is enthalpy;
- `h = u + p/rho` at the inlet;
- inlet advected energy equals inlet enthalpy;
- exhaust advected energy equals inlet enthalpy minus extracted specific work;
- inlet energy rate equals exhaust energy rate plus shaft power;
- ownership residual is zero within floating-point tolerance;
- at least one audited operating point has positive flow and measurable flow work;
- turbine work parameters are unchanged.

## Phase boundary

If G.4 passes its focused, ordinary and cumulative gates, Phase G is complete. The next milestone is Phase H, which measures numerical stiffness and determines whether the existing fixed-step explicit composition remains adequate before any adaptive-substep or semi-implicit change is considered.
