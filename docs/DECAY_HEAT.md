# Decay Heat Model

## Scope

M2.5 introduces a deterministic reduced-order decay-heat model with explicit memory of prior fission power.

It is intentionally **not** an implementation of ANS-5.1, MELCOR, ORIGEN, isotope depletion or plant-specific licensing correlations. It provides a replaceable educational dynamic boundary that can later be calibrated by plant configuration.

## Equivalent-group state

Each configured group contains:

```text
Id
GenerationFraction f_i
DecayConstant lambda_i
StoredDecayEnergy E_i
```

The state variable is latent decay energy, not an arbitrary percentage of reactor power.

For constant fission thermal power during one fixed timestep:

```text
dE_i/dt = f_i * P_fission - lambda_i * E_i
```

The instantaneous heat emitted by the group is:

```text
P_decay_i = lambda_i * E_i
```

At long steady operation:

```text
E_eq_i = f_i * P_fission / lambda_i
P_decay_eq_i = f_i * P_fission
```

Therefore the configured generation fraction has a direct and inspectable meaning: it is the group's equilibrium decay-heat power fraction relative to the driving fission-power level.

## Exact finite-step evolution

`DecayHeatSolver` uses the analytic solution for each first-order group over the caller's deterministic timestep rather than Euler integration:

```text
E_new = E_old * exp(-lambda*dt)
      + (f*P_fission/lambda) * (1 - exp(-lambda*dt))
```

The exact finite-step energy bookkeeping is then:

```text
ProducedDecayEnergy = f * P_fission * dt
EmittedDecayEnergy  = E_old + ProducedDecayEnergy - E_new
```

This gives the invariant:

```text
E_old + ProducedDecayEnergy
=
E_new + EmittedDecayEnergy
```

within floating-point precision.

## Average power versus instantaneous power

A fixed step has two distinct decay-heat quantities:

```text
AverageDecayHeatPower
    = integrated emitted energy / dt
    → use for same-step ThermalBodyIntegrator / FluidNodeIntegrator

InstantaneousDecayHeatPower at end of step
    = sum(lambda_i * E_i,new)
    → use for diagnostics / snapshots / trends
```

Keeping these distinct avoids depositing an end-point power value as though it had been constant across the whole timestep.

## Shutdown behavior

When fission power falls to zero:

```text
production term = 0
stored latent inventory remains > 0
decay heat continues
inventory and heat rate decrease over time
```

For one isolated group after shutdown:

```text
E(t) = E0 * exp(-lambda*t)
P_decay(t) = lambda * E(t)
```

Thus after one configured half-life, both group inventory and instantaneous group decay power are halved.

## Power-history memory

The model naturally distinguishes:

```text
reactor operated at power for a long time
    → large equilibrium inventories

reactor recently started
    → inventories still building

reactor shut down
    → fission source collapses but inventories keep releasing heat
```

No scripted "post-SCRAM percentage" is required.

## Deposition boundary

Emitted decay heat is partitioned across named destinations whose fractions must sum to unity:

```text
decay heat
   ├── fuel
   ├── structures
   └── coolant
```

`DecayHeatDeposition` adapts directly to:

```text
ThermalEnergyBalance
FluidNodeBalance (zero mass flow, positive energy rate)
```

The solver does not mutate thermal bodies or fluid nodes itself.

## Relationship to fission power

M2.4 and M2.5 remain separate source models:

```text
M2.4 current neutron population
    → direct/current fission thermal power

M2.5 fission-power history
    → latent radioactive-decay energy inventory
    → delayed decay heat
```

The current educational model treats the decay-energy production term as a separate nuclear-energy source tied to fission history; it is not subtracted from the M2.4 deposition path. Future calibrated plant configurations may define the desired prompt/direct versus delayed energy accounting convention without changing the runtime architecture.

## Explicit non-goals for M2.5

M2.5 does not yet model:

- isotope-by-isotope fission-product inventories;
- actinide decay heat;
- neutron-capture corrections;
- fuel burnup or irradiation-history standards;
- ANS-5.1 uncertainty bands;
- spatial decay-heat redistribution;
- fission-product transport or release.

Those require higher-fidelity backends and/or plant-specific models.
