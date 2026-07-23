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

### Saturation-boundary bracketing robustness

The original coarse scan spans the complete supported saturation-temperature range. Near quality endpoints (`x → 0` or `x → 1`), the temperature interval in which a fixed specific volume is physically admissible can end between two coarse samples. A valid two-phase root can therefore exist in a narrow terminal interval without producing a sampled sign change.

The resolver preserves the original fast paths, but before failing out of range it now performs a deterministic boundary-aware saturated-mixture fallback: it first locates the upper temperature at which the node's specific volume still lies between saturated-liquid and saturated-vapor specific volumes, then rescans only that mathematically admissible interval and uses the same fixed-iteration bisection. This closes numerical root-bracketing gaps without clamping conserved state, widening the declared thermodynamic envelope or introducing a new property correlation.

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

### Superheated phase-boundary bracketing robustness

The superheated branch has the same numerical boundary hazard as the saturated branch. For a fixed specific volume, the first temperature that satisfies the branch's pressure/saturation admissibility test can lie between two coarse full-range scan samples. A valid superheated root may then exist in the narrow interval immediately above that onset while the first sampled valid point already lies beyond the residual sign change.

The resolver therefore preserves the original superheated fast path and, only before final out-of-range failure, locates the exact contiguous temperature interval in which the existing superheated equations are admissible. It injects those valid interval endpoints into a deterministic rescan and reuses the existing bisection/equations. The fallback does not clamp conserved state, interpolate across a correlation gap or widen the declared envelope; states for which neither the saturated nor superheated equations contain a real root still fail closed.

## Supported envelope

The simplified saturation correlations are intentionally bounded below the critical point. States outside the supported educational envelope fail fast with `WaterSteamStateOutOfRangeException` rather than returning `NaN`, silently clamping or extrapolating arbitrary properties.

The saturation and vapor branches remain bounded below critical pressure. The simplified compressed-liquid branch may cross the critical isobar while its derived temperature remains below the supported saturation-temperature ceiling; this is still classified as compressed/subcooled liquid, not as supercritical fluid, and uses the same finite bulk-modulus response without clamping pressure.

Supercritical-temperature water, metastable states, detailed compressed-liquid properties and high-fidelity transport properties are outside M1.7.

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
