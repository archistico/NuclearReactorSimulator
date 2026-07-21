# Void Feedback

M2.7 connects the validated M1.7 water/steam thermodynamic state to the M2 reactivity-composition boundary.

## Two different fractions

`VaporQuality` and `VoidFraction` are intentionally different domain concepts:

```text
VaporQuality  = vapor mass / total mixture mass
VoidFraction  = vapor volume / total mixture volume
```

For a saturated homogeneous liquid-vapor mixture, M2.7 converts quality to void fraction using the phase densities from the same simplified water/steam saturation model used by M1.7:

```text
alpha = (x / rho_v) / ((x / rho_v) + ((1 - x) / rho_l))
```

where `x` is vapor quality, `rho_v` saturated-vapor density and `rho_l` saturated-liquid density.

Because vapor is much less dense than liquid, a small vapor mass fraction can occupy a large fraction of the control-volume volume. Code must therefore never use `VaporQuality.Fraction` directly as neutron-physics void fraction.

## Phase mapping

`WaterSteamVoidFractionSolver` maps committed coarse phase state as follows:

```text
SubcooledLiquid     -> 0 void
SaturatedMixture    -> density-weighted homogeneous-equilibrium void fraction
SuperheatedVapor    -> 1 void
Unspecified         -> fail fast
```

The solver is deterministic and stateless. It consumes the committed thermodynamic state; it does not mutate fluid inventory or perform phase closure itself.

## Reactivity mapping

`VoidReactivityFeedbackDefinition` configures:

- a stable contribution id;
- a reference void fraction;
- a signed `VoidReactivityCoefficient`.

The M2.7 feedback law is deliberately linear:

```text
rho_void = alpha_void * (void - void_reference)
```

The coefficient is stored canonically as `delta-k/k` per unit void fraction and can be configured explicitly in `pcm` per percentage-point void.

The generic engine assumes neither sign nor magnitude. Positive, negative and zero coefficients are all valid configuration choices.

## Fixed-step coupling

M2.7 follows the committed-state rule established by M2.6:

```text
committed water/steam state N
        |
        v
resolve void fraction
        |
        v
void reactivity contribution
        |
        v
ReactivityModel -> PointKineticsSolver
        |
        v
fission / decay heat and plant evolution
        |
        v
candidate thermohydraulic state N+1
```

No hidden same-step nonlinear iteration is introduced.

## Scope boundary

M2.7 does not yet model:

- spatial channel-by-channel void distributions;
- slip ratio or separated two-phase flow;
- drift-flux correlations;
- pressure-drop coupling to local void;
- RBMK-specific void coefficients or operating-condition tables;
- axial/local power peaking.

Those belong to later plant-specific and higher-fidelity milestones. The current model establishes the typed and testable seam from thermohydraulic state to reactivity.

## References

- IAPWS formulations and vapor-liquid equilibrium background: https://iapws.org/documents/newform
- U.S. NRC glossary, Void coefficient of reactivity: https://www.nrc.gov/reading-rm/basic-ref/glossary/void-coefficient-of-reactivity
