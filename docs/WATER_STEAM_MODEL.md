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
- a reduced-order vapor pressure relation for the superheated branch;
- constant effective vapor `cv` for superheat above the local saturation reference;
- constant effective liquid bulk modulus for compressed/subcooled liquid pressure response.

`SimplifiedWaterSteamThermodynamicModel` now has two explicit closure modes:

- `HistoricalCorrelationTopology` — compatibility mode used by historical exact desktop `@2` and `@3`; the parameterless constructor deliberately preserves this mode so old exact-version behavior is not silently reinterpreted;
- `CorrelationConsistentInverseDomain` — authoritative desktop exact `@4` mode, validated in I.5 after the historical vapor phase-boundary mismatch and low-temperature inverse-search blind spot were mapped.

Every approximation remains isolated inside `SimplifiedWaterSteamThermodynamicModel` so a future high-fidelity backend can replace it behind `IFluidThermodynamicModel`.

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

### Saturation-boundary bracketing and interval-aware inversion

The original coarse scan spans the complete supported saturation-temperature range. Near quality endpoints (`x → 0` or `x → 1`), the temperature interval in which a fixed specific volume is physically admissible can end between two coarse samples. A valid two-phase root can therefore exist in a narrow terminal interval without producing a sampled sign change.

`HistoricalCorrelationTopology` preserves the earlier boundary-aware fallback for exact-version compatibility. That fallback assumes the valid interval is connected to the triple point. The saturated-liquid density correlation, however, contains the physical water-density maximum near 4 °C; a fixed volume can therefore have a valid local saturation interval that is not triple-point-connected. The validated I.5 census found 83/83 historical misses from 4.01–8.16 °C despite independently bracketed roots.

`CorrelationConsistentInverseDomain` instead locates the saturated-liquid density maximum and solves the cold liquid, warm liquid and vapor specific-volume boundaries separately. Their intersection defines the complete valid saturation-temperature interval for the conserved specific volume. The same deterministic scan/bisection machinery is then applied inside that interval. This repairs root discovery without clamping mass/energy or expanding the declared thermodynamic envelope.

## Subcooled/compressed liquid closure

For dense states below the saturation-volume boundary:

- temperature is derived from specific internal energy using the simplified liquid heat-capacity model;
- saturation pressure/density are evaluated at that temperature;
- excess density above saturated-liquid density produces an additional pressure response through the effective bulk modulus.

## Superheated vapor closure

For low-density/high-energy states, both modes derive the local saturation temperature from pressure, use saturated-vapor internal energy at that pressure as the reference, and add superheat with the effective vapor `cv`. They differ in the pressure relation used to connect the superheated branch to saturation.

### Historical compatibility relation

`HistoricalCorrelationTopology` uses the original ideal-gas-style relation:

```text
p = R T / v
```

while saturated vapor uses the independently correlated `v_g(T) = 1 / rho_g(T)`. I.5 topology auditing proved that these two boundaries do not coincide: the historical model contains a low-pressure no-root band and a higher-pressure overlap/multiple-root band. Exact desktop `@2` and `@3` retain this behavior only for replay compatibility.

### Correlation-consistent relation used by exact @4

`CorrelationConsistentInverseDomain` anchors the superheated branch to the same saturated-vapor boundary. For a boundary temperature `Tb` define:

```text
Δv(Tb) = R Tb / Psat(Tb) - vg(Tb)
p(T, v) = R T / (v + Δv(Tb))
```

For temperatures inside the supported saturation range, `Tb = T`; above the 640 K saturation ceiling, the 640 K shift is retained so the saturation domain is not implicitly extended. At `v = vg(Tb)`, the repaired pressure is exactly `Psat(Tb)`, so saturated and superheated branches meet on one boundary instead of leaving a gap or overlap.

A deterministic root solve finds the temperature consistent with conserved specific volume/internal energy. Boundary-aware superheated bracketing remains deterministic and fail-closed: states with no mathematical root in the declared simplified domain still raise `WaterSteamStateOutOfRangeException`.

## Supported envelope

The simplified saturation correlations are intentionally bounded below the critical point. States outside the supported educational envelope fail fast with `WaterSteamStateOutOfRangeException` rather than returning `NaN`, silently clamping or extrapolating arbitrary properties.

The saturation and vapor branches remain bounded below critical pressure. The simplified compressed-liquid branch may cross the critical isobar while its derived temperature remains below the supported saturation-temperature ceiling; this is still classified as compressed/subcooled liquid, not as supercritical fluid, and uses the same finite bulk-modulus response without clamping pressure.

Supercritical-temperature water, metastable states, detailed compressed-liquid properties and high-fidelity transport properties are outside M1.7.


## Exact-version thermodynamic identity

Desktop exact-version semantics are intentionally preserved:

```text
integrated-operations-desktop-stable@2 -> HistoricalCorrelationTopology + ExplicitCommittedState
integrated-operations-desktop-stable@3 -> HistoricalCorrelationTopology + FourNodeBranchContinuityCorrectedCommitOptIn
integrated-operations-desktop-stable@4 -> CorrelationConsistentInverseDomain + FourNodeBranchContinuityCorrectedCommitOptIn
```

Exact `@4` is authoritative production after I.5 Hotfix 16.2 validation. Exact `@2` remains fail-closed rollback/reference and exact `@3` remains immutable H.29/H.30/I.3 replay provenance. The separate `pre-synchronization-grid-loading` family keeps its own exact versions; its supported corrected identity remains synchronization `@3` and is not renamed to desktop `@4`.

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
