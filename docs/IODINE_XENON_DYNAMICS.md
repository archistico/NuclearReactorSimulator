# Iodine/Xenon Dynamics

## Scope

M2.8 introduces a reduced, deterministic I-135 / Xe-135 poisoning model. It is an educational state model, not a depletion or core-analysis code. The generic engine deliberately contains no RBMK-specific yields, cross sections, half-lives or worth constants; all numerical parameters belong to plant configuration.

## State boundary

The model owns two explicit normalized inventories:

```text
IodineInventory  I
XenonInventory   X
```

The normalized inventory scale is configuration-relative. Production rates, burnup coefficient and xenon reactivity coefficient must use the same scale consistently.

The state is immutable:

```text
fission-power history
        ↓
I-135 inventory ── decay ──► Xe-135 inventory
                              │
                              ├── natural decay
neutron population ──────────┴── burnup
                              ↓
                       xenon reactivity
```

## Dynamic equations

For one fixed step, fission thermal power and normalized neutron population are treated as committed, constant inputs:

```text
dI/dt  = S_I - lambda_I I

dXe/dt = S_Xe + lambda_I I - (lambda_Xe + k_burn n) Xe
```

where:

- `S_I` is I-135 production scaled from configured reference fission power;
- `S_Xe` is direct Xe-135 production scaled from configured reference fission power;
- `lambda_I` and `lambda_Xe` are first-order decay constants;
- `k_burn` is the xenon-removal coefficient per unit normalized neutron population;
- `n` is normalized neutron population.

`IodineXenonSolver` uses the closed-form finite-step solution of this coupled linear system for constant inputs over the caller timestep. No wall clock, random source or iteration tolerance participates in the result.

## Equilibrium initialization

`IodineXenonState.CreateEquilibrium` constructs the steady inventories for a supplied fission power and neutron population:

```text
I_eq  = S_I / lambda_I

Xe_eq = (S_Xe + S_I) / (lambda_Xe + k_burn n)
```

This supports initial conditions such as long-running steady power without inventing arbitrary poison inventories.

## Xenon reactivity

Only Xe-135 inventory maps to reactivity in M2.8:

```text
rho_Xe = alpha_Xe * XeInventory
```

`XenonReactivityCoefficient` is signed configuration data. A conventional poisoning setup uses a negative coefficient, but the generic solver does not hardcode sign or magnitude.

The snapshot emits one named contribution:

```text
xenon/<definition-id>
kind = ReactivityContributionKind.Xenon
```

Composition remains the responsibility of the validated M2.1 `ReactivityModel`.

## Fixed-step coupling

M2.8 follows the same committed-state staging rule used by temperature and void feedback:

```text
committed kinetics + committed poison inventories
        ↓
fission power and xenon reactivity diagnostics
        ↓
ReactivityModel / PointKineticsSolver
        ↓
new neutron population / fission power
        ↓
IodineXenonSolver finite-step inventory evolution
        ↓
candidate I/Xe state
```

There is no hidden same-step nonlinear iteration. This preserves deterministic replay and transactional step semantics.

## Expected qualitative behavior

The reduced model can produce, from state rather than scripts:

- iodine buildup during operation;
- xenon buildup during operation;
- xenon burnup that increases with neutron population;
- continued I-135 to Xe-135 feeding after a power reduction;
- post-shutdown xenon rise when iodine feeding dominates xenon removal;
- later xenon decay;
- negative poisoning reactivity when configured with a negative xenon worth coefficient.

## Deliberate limitations

M2.8 does not model:

- spatial xenon oscillations;
- isotope transport between zones;
- microscopic cross sections or absolute atom densities;
- burnup-dependent fission yields;
- samarium or other poisons;
- plant-specific RBMK constants;
- high-fidelity depletion physics.

These can be introduced later behind the same state/reactivity boundaries.
