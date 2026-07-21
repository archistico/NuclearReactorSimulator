# Neutron Kinetics

M2.3 introduces generic point-reactor kinetics as the dynamic boundary between total reactivity and neutron population. The kinetics solver itself deliberately does not convert neutron population directly into thermal power; M2.4 now supplies that downstream coupling through a separate `FissionPowerSolver`.

## State and parameters

The parameter set is injected and plant-independent:

```text
Prompt-neutron generation time Λ
+
Delayed-neutron groups i = 1..N
    β_i  delayed-neutron fraction
    λ_i  precursor decay constant
```

No RBMK-specific kinetic constants are hardcoded in the engine. Plant-specific values belong to later plant configuration.

The immutable state contains:

```text
n      normalized neutron population
C_i    normalized precursor population for each delayed-neutron group
```

`PointKineticsState.CreateCriticalEquilibrium(...)` initializes precursor populations so a zero-reactivity state begins in exact point-kinetics equilibrium for the selected parameter set.

## Point-kinetics equations

M2.3 evaluates the standard space-independent point-kinetics form:

```text
dn/dt   = ((ρ - β_eff) / Λ) n + Σ λ_i C_i

dC_i/dt = (β_i / Λ) n - λ_i C_i

β_eff = Σ β_i
```

The total `Reactivity` supplied to the solver is the output of the compositional M2.1 model. Control rods, temperature, void and xenon therefore remain independent contributors upstream of kinetics.

## Numerical integration

The simulation runtime keeps its externally visible fixed timestep. Inside one physical step, `PointKineticsSolver` performs a deterministic number of RK4 substeps derived only from:

- the requested physical timestep;
- `ρ`;
- `Λ`;
- delayed-group `β_i` and `λ_i` coefficients.

There is no adaptive wall-clock behaviour, random tolerance path or UI-dependent cadence. Identical state, parameters, reactivity and timestep produce identical output.

The internal substep count is bounded. Non-finite or materially negative populations fail closed through `NeutronKineticsNumericalException` rather than silently clamping an unsupported transient.

## Diagnostics

`PointKineticsSnapshot` exposes:

- normalized neutron population;
- effective delayed-neutron fraction `β_eff`;
- applied reactivity;
- prompt-critical margin `ρ - β_eff`;
- prompt-critical boolean;
- reactivity in dollars/cents relative to the active `β_eff`;
- instantaneous logarithmic population rate `d(ln n)/dt`;
- signed instantaneous reactor period `T = 1 / d(ln n)/dt` when finite.

A positive finite period indicates growth; a negative finite period indicates decay. A null period represents an effectively infinite/undefined period at zero logarithmic rate or zero neutron population.

Dollars/cents are diagnostics relative to the selected kinetic parameter set. They are intentionally not stored as an intrinsic unit on `Reactivity`.

## Scope boundary

M2.3 itself does not implement:

- neutron-to-fission thermal power conversion;
- decay heat;
- temperature feedback;
- void feedback;
- iodine/xenon inventory dynamics;
- spatial neutron diffusion or channel-wise flux shape;
- plant-specific RBMK kinetic constants.

M2.4 now supplies the separate neutron-to-fission-power boundary. The remaining items stay separate roadmap concerns so fidelity can increase without coupling the kinetics solver to plant UI or thermal-hydraulic implementation details.
