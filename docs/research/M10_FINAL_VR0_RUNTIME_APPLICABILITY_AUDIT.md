# M10 Final VR0 — Runtime Applicability Audit

**Status:** static source audit supporting VR0. No runtime execution and no production change.

This audit answers a separate question from reference quality: **is the assessed subsystem actually active in the frozen exact-v9/P1B path?** That distinction is needed so VR5 can be impact-aware rather than treating every future subsystem as an M10 blocker.

| Work package | Production owner exists | Active in exact-v9/P1B | VR0 interpretation |
| --- | --- | --- | --- |
| VR1 point kinetics | yes, `PointKineticsSolver` | **yes** | solver implementation is M10-material; plant parameter calibration remains reduced |
| VR2 water/steam | yes, `SimplifiedWaterSteamThermodynamicModel` | **yes** | directly M10-material because exact-v9 inventories/pressures/phase closure depend on it |
| VR3 iodine/xenon | yes, `IodineXenonSolver` plus M9.3 configuration | **no in sustained-generation exact-v9 path** | important generic/M9.3 model assessment; not automatically an M10 P3-R1 blocker |
| VR4 decay heat | generic `DecayHeatSolver` exists | **no canonical production model in exact-v9; total decay heat input is zero** | reference/configuration gap is an M12.5 prerequisite, not automatically an M10 blocker |

## VR1 source ownership

`PointKineticsSolver` implements arbitrary delayed-neutron groups and the canonical point-reactor ODEs using deterministic RK4 internal substepping. The sustained-generation factory currently creates a deliberately reduced one-group parameter set:

```text
Lambda = 0.1 s
beta = 0.0065
lambda = 0.08 1/s
```

The external six-group VR1 case is therefore an **equation/solver assessment**, not a plant-parameter calibration.

## VR2 source ownership

`SimplifiedWaterSteamThermodynamicModel` explicitly documents itself as an educational deterministic closure. Source inspection confirms:

- official IF97 Region-4 saturation pressure is used as the saturation boundary;
- liquid/vapor densities are simplified correlations;
- liquid internal energy uses a constant 4200 J/(kg K) heat capacity from the triple point;
- latent enthalpy uses a Watson-type correlation;
- vapor internal energy is constructed from liquid internal energy plus latent internal energy;
- inverse state resolution and phase selection use these same reduced relations.

This model is heavily active in P1B's inventory/pressure/steam trajectory. VR2 is therefore the highest direct-reference priority for interpreting the slow state.

## VR3 source ownership

`IodineXenonSolver` implements the reduced linear I/Xe equations analytically over constant-input timesteps. `AdvancedXenonModelConfiguration` explicitly labels its M9.3 constants as educational and configuration-relative, not plant-specific isotope constants.

`DesktopSustainedGenerationInitialConditionFactory` invokes the operational cold-shutdown factory without supplying `iodineXenonDefinition` or `initialIodineXenonState`; their defaults are null/empty. The exact-v9 P1/P1A/P1B trajectory is therefore not driven by the M9.3 poison configuration.

## VR4 source ownership

`DecayHeatSolver` exists as a generic equivalent-group engine and has domain/simulation verification tests. Static production-source search finds no `new DecayHeatDefinition(...)` outside tests/domain use, and no canonical production integration owner that evolves decay heat for the exact-v9 sustained-generation path.

`ColdShutdownInitialConditionFactory` initializes:

```text
new IntegratedPrimaryCircuitInputs(..., Power.Zero, Power.Zero, ...)
```

for fission power and total decay heat before the control layers rewrite fission power. The control layers preserve `primary.TotalDecayHeatPower`; they do not instantiate/evolve a decay-heat model. Thus current exact-v9 carries zero decay heat.

This is an important fidelity gap for post-trip work, but it is not a plausible owner of the P1B 5→6 MWe slow redistribution.

## Consequence for VR5

VR5 must not confuse **reference quality** with **scenario materiality**. A blocking external discrepancy in VR1 or VR2 can stop P3-R1 because those owners are active. A VR3/VR4 gap is still recorded and constrains claims, but blocks M10 only if separate evidence demonstrates that the subsystem is active/material to the frozen exact-v9 trajectory.
