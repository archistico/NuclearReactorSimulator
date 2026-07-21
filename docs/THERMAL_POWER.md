# Thermal Power Model

M2.4 introduces the explicit boundary between neutron kinetics and thermal energy generation.

## Core rule

Normalized neutron population is converted to instantaneous fission thermal power through an explicit calibration:

```text
neutron population n
       +
reference population n_ref
       +
reference thermal power P_ref
       ↓
P_fission = P_ref × n / n_ref
```

The model is intentionally linear at this stage. It establishes ownership and conservation boundaries before plant-specific calibration, spatial peaking and feedback are introduced.

## Separation of responsibilities

```text
ReactivityModel
      ↓
PointKineticsSolver
      ↓
NeutronPopulation
      ↓
FissionPowerSolver
      ↓
instantaneous fission thermal power
      ↓
heat-deposition partition
      ↓
fuel / structures / coolant thermal-energy boundaries
```

M2.4 does not feed thermal state back into reactivity. Fuel-temperature, coolant-temperature and void feedback remain M2.6/M2.7 responsibilities.

## Calibration

`FissionPowerCalibration` contains:

- a strictly positive reference neutron population;
- a strictly positive reference fission thermal power.

No RBMK-specific nominal power is hardcoded in the solver. Plant configurations will provide their own calibration.

## Heat-deposition partition

`FissionPowerDefinition` owns one or more `FissionHeatDestinationDefinition` entries.

Each destination contains:

- a stable target-domain id;
- a `HeatDepositionFraction` in `(0, 1]`.

The complete destination set must sum to `1.0` within a strict construction tolerance. Destination ids are unique and canonicalized ordinally, so caller collection order cannot influence the result.

At solve time, tiny floating-point representation error in the configured fraction sum is normalized deterministically. The final canonical destination receives the exact residual power so that:

```text
Σ deposition powers == total fission thermal power
```

under the solver's own arithmetic order.

## Energy boundaries

A `FissionHeatDeposition` can be converted directly to either existing energy boundary:

```text
thermal body target
    → ThermalEnergyBalance(+P)

fluid-node target
    → FluidNodeBalance(0 kg/s, +P)
```

Fission heating never creates fluid mass.

Integrated deposited energy remains an explicit consequence of power over the fixed timestep:

```text
E_deposited = P_deposition × Δt
```

The already validated thermal/fluid integrators own state integration.

## No decay heat in M2.4

M2.4 represents prompt/instantaneous fission thermal power only.

It does not retain post-fission heat inventory. Therefore, when neutron population falls, fission thermal power follows the calibrated neutron population directly. Delayed decay heat is introduced separately in M2.5 so that the two physical sources remain independently inspectable and testable.

## Determinism

`FissionPowerSolver` is stateless and deterministic:

```text
same FissionPowerDefinition
+ same NeutronPopulation
= same FissionPowerSnapshot
```

Runtime integration tests compose M2.3 point kinetics with M2.4 thermal power and verify that irregular external pulse segmentation produces the same fixed-step final state.

## Fidelity boundary

M2.4 deliberately does not model:

- spatial neutron-flux shape;
- axial/radial power peaking;
- channel-wise fuel power;
- fuel burnup dependence;
- prompt gamma/neutron deposition splits;
- decay heat;
- thermal feedback to reactivity.

Those refinements can be added behind the same explicit neutron-to-power and power-to-heat-deposition boundaries.


## Relationship to M2.5 decay heat

M2.4 remains the direct/current-fission thermal source boundary. M2.5 does not fold decay heat into `FissionPowerSolver`; it consumes fission-power history as a driver for separate latent decay-energy inventories. This preserves a clean distinction between immediate fission heat and delayed radioactive-decay heat, while allowing future plant configurations to calibrate both models independently.
