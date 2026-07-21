# Physical quantities and units

M1.1 establishes the unit boundary for all future physical models.

## Canonical SI rule

Internally, quantities store canonical SI values:

| Quantity | Canonical storage |
|---|---|
| Length | metre (m) |
| Area | square metre (m²) |
| Volume | cubic metre (m³) |
| Mass | kilogram (kg) |
| Density | kilogram per cubic metre (kg/m³) |
| Temperature | kelvin (K) |
| Temperature difference | kelvin difference (K) |
| Pressure | pascal absolute (Pa) |
| Pressure difference | pascal difference (Pa) |
| Energy | joule (J) |
| Specific energy | joule per kilogram (J/kg) |
| Power | watt (W) |
| Mass flow rate | kilogram per second (kg/s) |
| Volumetric flow rate | cubic metre per second (m³/s) |
| Quadratic hydraulic resistance | pascal second squared per kilogram squared (Pa·s²/kg²) |

Non-SI units are explicit factories/conversions, for example:

```csharp
var pressure = Pressure.FromBar(70);
var temperature = Temperature.FromDegreesCelsius(280);
var thermalPower = Power.FromMegawatts(3_200);
```

No implicit conversion from/to `double` is provided.

## Absolute quantities versus differences

The model deliberately distinguishes absolute values from signed changes.

```text
Temperature           >= 0 K
TemperatureDifference signed

Pressure              >= 0 Pa absolute
PressureDifference    signed
```

This permits physically meaningful expressions such as:

```csharp
var deltaT = outletTemperature - inletTemperature;
var outlet = inletTemperature + deltaT;

var deltaP = upstreamPressure - downstreamPressure;
```

while preventing accidental construction of an absolute temperature below absolute zero or an absolute pressure below vacuum.

## Finite-number boundary

All quantity factories reject:

```text
NaN
+Infinity
-Infinity
```

This is the first numerical safety boundary. Model-specific constraints remain the responsibility of physical invariants and solvers.

## Dimensional operations

Only useful, unambiguous operations are exposed in M1.1, including:

```text
Length × Length                 → Area
Area × Length                   → Volume
Mass / Volume                   → Density
Density × Volume                → Mass
Energy / Mass                   → SpecificEnergy
SpecificEnergy × Mass           → Energy
Energy / duration               → Power
Power integrated over duration  → Energy
PressureDifference × Volume     → Energy
Mass / duration                 → MassFlowRate
SpecificEnergy × MassFlowRate   → Power
MassFlowRate / Density           → VolumetricFlowRate
PressureDifference × VolumetricFlowRate → Power
HeatCapacity × TemperatureDifference → Energy
ThermalConductance × TemperatureDifference → Power
```

The API is intentionally not a full compile-time dimensional-analysis algebra. New relationships are added only when demanded by an implemented model. M1.3 adds the signed `SpecificEnergy × MassFlowRate -> Power` relationship used by conservative pipe energy advection, plus the strictly positive `QuadraticHydraulicResistance` quantity used by passive pipe flow. M1.5 adds `MassFlowRate / Density -> VolumetricFlowRate` and `PressureDifference × VolumetricFlowRate -> Power` for explicit pump hydraulic-work accounting.

## Solver boundary

Future numerical solvers may use primitive numeric arrays internally for performance and numerical algorithms. The boundary rule is:

```text
strong quantity types
      ↓ unwrap canonical SI
numerical solver internals
      ↓ construct/validate
strong quantity types
```

This keeps unit semantics explicit at model boundaries without forcing every low-level floating-point operation through wrapper types.


## M1.6 thermal-transfer quantities

M1.6 adds:

```text
HeatCapacity        J/K
ThermalConductance  W/K
```

Both use canonical SI storage and explicit engineering-unit factories. `HeatCapacity × TemperatureDifference` produces `Energy`; `ThermalConductance × TemperatureDifference` produces signed `Power`.

## M2.1 reactor-physics quantity

M2.1 adds signed dimensionless `Reactivity`, stored canonically as `delta-k/k` and exposed through explicit conversions:

```text
DeltaKOverK
PercentDeltaKOverK
Pcm
```

`1 pcm = 1e-5 delta-k/k`. Reactivity accepts positive, negative and zero finite values. Dollars/cents are deliberately deferred until the effective delayed-neutron fraction exists in the neutron-kinetics model.


## M2.3 neutronics quantities

M2.3 adds strongly validated dimensionless/time-rate quantities used by point kinetics:

- `DelayedNeutronFraction` — dimensionless `β_i` in `[0,1]`, with explicit percent view;
- `DecayConstant` — positive first-order decay constant in `s^-1`, with half-life/mean-lifetime views;
- `NeutronPopulation` — non-negative normalized neutron population;
- `DelayedNeutronPrecursorPopulation` — non-negative normalized precursor population consistent with the selected point-kinetics parameter set.

These are normalized kinetic state variables, not absolute neutron-count metrology. Plant-specific normalization and detector calibration remain outside M2.3.

## M2.4 thermal-power partition value

M2.4 adds `HeatDepositionFraction`, a strongly validated dimensionless reactor-domain value in `[0,1]` with explicit fraction/percent views. It is intentionally not a general-purpose implicit scalar: it exists to make fission-heat partition definitions explicit and validated.

`Power` remains the canonical quantity for instantaneous fission thermal power and per-destination heat deposition. Integrated energy continues to use the existing explicit `Power.Over(TimeSpan) -> Energy` relationship.



## M2.5 decay-heat generation fraction

M2.5 adds `DecayHeatGenerationFraction`, a strongly validated dimensionless reactor-domain value in `[0,1]`. It is deliberately distinct from `HeatDepositionFraction`:

- `DecayHeatGenerationFraction` describes how strongly current fission power feeds one equivalent latent decay-energy group;
- `HeatDepositionFraction` describes how already-emitted thermal power is partitioned among destination domains.

Keeping these semantics separate prevents accidental reuse of a deposition partition as a decay-inventory production model.


## Temperature reactivity coefficient (M2.6)

`TemperatureReactivityCoefficient` is a signed intensive feedback coefficient stored canonically as `delta-k/k per kelvin`, with explicit `pcm/K` conversion. Multiplying it by `TemperatureDifference` yields a typed `Reactivity`. The generic engine imposes no sign or plant-specific magnitude.


## M2.7 void-feedback quantities

`VoidFraction` is a bounded volumetric fraction in `[0,1]` and is intentionally distinct from `VaporQuality`, which is a mass fraction. `VoidFractionDifference` is signed. `VoidReactivityCoefficient` stores `delta-k/k` per unit void fraction and exposes explicit `pcm` per percentage-point void conversion.

## M4.2 rotational-mechanical quantities

M4.2 adds three strongly typed quantities for turbine rotor dynamics:

```text
AngularSpeed      rad/s canonical, explicit rpm conversion
Torque            N·m signed mechanical torque
MomentOfInertia   kg·m² positive rotational inertia
```

`Torque.At(AngularSpeed)` produces `Power`, and `MomentOfInertia.KineticEnergyAt(AngularSpeed)` produces `Energy`. `AngularSpeed` is modeled as a non-negative rotation-speed magnitude in M4.2; numerical reverse rotation is not part of the current turbine operating domain.


## M4.5 electrical quantities

M4.5 adds four strongly typed electrical/synchronization quantities:

```text
Frequency             Hz canonical, non-negative
ElectricPotential     V canonical, explicit kV conversion
PhaseAngle            rad canonical, normalized to [0, 2π)
PhaseAngleDifference  rad canonical, shortest magnitude in [0, π]
```

`PhaseAngle` provides deterministic cyclic phase state while `PhaseAngleDifference` represents the typed shortest separation used by synchronization windows. Generator electrical frequency remains derived from mechanical `AngularSpeed` and configured pole-pair count rather than being duplicated as an independently integrated machine-speed state.
