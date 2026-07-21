# Simplified Water/Steam Model

M1.7 provides the first production `IFluidThermodynamicModel` implementation for ordinary water/steam.

## Purpose

The model is an educational deterministic closure for the simulator's lumped control volumes. It converts:

```text
fixed volume
+ conserved mass
+ conserved internal energy
        ↓
pressure
+ temperature
+ coarse phase
+ saturated-mixture vapor quality
```

It is intentionally not a complete steam-table package and must not be used for engineering design, licensing, safety analysis or plant operation.

## Reference boundary

The saturation-pressure boundary follows the Region-4 saturation equation published with IAPWS-IF97. M1.7 does **not** implement the complete multi-region IAPWS-IF97 property formulation.

The remaining closure uses compact, deterministic approximations:

- critical-scaling correlations for saturated liquid/vapor density;
- constant liquid specific heat for the liquid internal-energy reference;
- Watson-style latent-heat decay toward the critical point;
- ideal-gas-style vapor pressure relationship for the superheated branch;
- constant effective vapor `cv` for superheat above the local saturation reference;
- constant effective liquid bulk modulus for compressed/subcooled liquid pressure response.

Every approximation is isolated inside `SimplifiedWaterSteamThermodynamicModel` so a future high-fidelity backend can replace it behind `IFluidThermodynamicModel`.

## Phase regions

`FluidThermodynamicState` now exposes:

```text
FluidPhase.Unspecified
FluidPhase.SubcooledLiquid
FluidPhase.SaturatedMixture
FluidPhase.SuperheatedVapor
```

Only `SaturatedMixture` carries `VaporQuality`.

`VaporMassFraction` is derived as:

```text
SubcooledLiquid    -> 0
SaturatedMixture   -> VaporQuality
SuperheatedVapor   -> 1
Unspecified        -> null
```

## Saturated mixture closure

For a candidate saturation temperature, the model derives saturated liquid/vapor specific volumes and internal energies. The node's fixed specific volume determines candidate vapor quality:

```text
v = (1 - x) vf + x vg
```

The corresponding mixture internal energy is:

```text
u = (1 - x) uf + x ug
```

A deterministic bracket scan plus fixed-iteration bisection finds the temperature where both conserved specific volume and conserved specific internal energy are satisfied.

## Subcooled/compressed liquid closure

For dense states below the saturation-volume boundary:

- temperature is derived from specific internal energy using the simplified liquid heat-capacity model;
- saturation pressure/density are evaluated at that temperature;
- excess density above saturated-liquid density produces an additional pressure response through the effective bulk modulus.

## Superheated vapor closure

For low-density/high-energy states:

- vapor pressure follows the ideal-gas-style `p = rho R T` relationship;
- the local saturation temperature is found from pressure;
- saturated-vapor internal energy at that pressure is the reference;
- superheat adds internal energy using the effective vapor `cv`.

A deterministic root solve finds the temperature consistent with the conserved inventory and fixed volume.

## Supported envelope

The simplified saturation correlations are intentionally bounded below the critical point. States outside the supported educational envelope fail fast with `WaterSteamStateOutOfRangeException` rather than returning `NaN`, silently clamping or extrapolating arbitrary properties.

Supercritical water, metastable states, detailed compressed-liquid properties and high-fidelity transport properties are outside M1.7.

## Architectural consequence

M1.2's seam is now exercised by a real production closure:

```text
FluidNodeIntegrator
        ↓
IFluidThermodynamicModel
        ↓
SimplifiedWaterSteamThermodynamicModel
```

No pipe, valve, pump, heat-transfer or runtime API needs to know how the water/steam properties are calculated.


## M2.7 void interpretation

M1.7 `VaporQuality` remains a saturated-mixture **mass fraction**. M2.7 does not reinterpret it as neutron-physics void. `WaterSteamVoidFractionSolver` converts quality to volumetric `VoidFraction` using the same simplified saturation liquid/vapor densities, while subcooled liquid and superheated vapor map to exact zero/full void endpoints. See `docs/VOID_FEEDBACK.md`.
