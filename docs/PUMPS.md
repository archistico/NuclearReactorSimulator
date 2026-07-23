# Pumps

M1.5 introduces the first active hydraulic machine in the plant model.

## Composition model

A pump is composed over an existing `PipeDefinition` rather than replacing the validated hydraulic network law:

```text
from node pressure
      +
active pump pressure source
      ↓
combined passive resistance
(pipe + pump internal droop)
      ↓
signed mass flow
```

The simplified centrifugal-pump equation is:

```text
P_from - P_to + Δp_active = (R_pipe + R_internal) · m_dot · |m_dot|
```

This keeps one pressure/flow convention across pipes, valves and pumps.

## Speed and affinity law

`PumpSpeed` is normalized in `[0, 1]`.

The active pressure source follows the speed-squared affinity law:

```text
Δp_active = Δp_rated · speed²
```

For equal endpoint pressures and fixed quadratic resistance this naturally gives:

```text
mass flow ∝ speed
hydraulic power ∝ speed³
```

No flow is imposed directly by the pump.

## Definition and state

`PumpDefinition` contains:

- identity;
- underlying hydraulic `PipeDefinition`;
- rated active pressure boost;
- positive quadratic internal hydraulic resistance;
- simplified constant efficiency.

`PumpState` contains:

- pump identity;
- commanded/mechanical normalized speed;
- running state.

When `IsRunning == false`, effective hydraulic speed is exactly zero. The hydraulic path remains open and can still carry passive forward or reverse flow according to endpoint pressure difference and total resistance.

## Energy boundary

A pump exchanges external mechanical work with the fluid.

`PumpFlowResult` therefore distinguishes:

- conservative advected internal-energy transport;
- signed hydraulic power exchanged by the active pressure source;
- non-regenerative shaft power demand.

For forward pumping, active work is applied to the actual downstream fluid node. Across both endpoint balances:

```text
sum(mass rates)   = 0
sum(energy rates) = hydraulic power exchange
```

Positive hydraulic power increases fluid internal-energy inventory. Negative exchange can occur during reverse flow, but M1.5 deliberately does not credit regenerative shaft generation; `ShaftPowerDemand` is zero for non-positive hydraulic exchange.

The pump's quadratic internal hydraulic resistance participates in the flow curve but its dissipative heating is not converted into fluid internal energy in M1.5. Detailed loss heating belongs to later thermal-hydraulic fidelity work.

## Density and volumetric flow

Hydraulic power requires volumetric flow. M1.5 adds explicit dimensional operations:

```text
MassFlowRate / Density                   -> VolumetricFlowRate
PressureDifference × VolumetricFlowRate -> Power
```

The density used is the actual upstream node density for the solved flow direction.

## Deliberate scope limits

M1.5 does not yet model:

- rotor inertia or acceleration dynamics;
- motor electrical transients;
- cavitation or NPSH;
- detailed efficiency maps;
- check valves;
- pump trips/interlocks;
- thermal conversion of hydraulic losses;
- two-phase pump degradation.

Those behaviors will be added only when their surrounding physical systems exist.


## M10.9.4 Hotfix 19 — optional discharge check valve

`PumpDefinition.HasDischargeCheckValve` is an opt-in hydraulic topology property. The default is `false`, preserving the historical bidirectional pump-path model. When enabled, `PumpFlowSolver` first solves the same active-head plus quadratic-resistance relation; if that unconstrained solution would be negative relative to the pump reference direction, the discharge check valve is considered closed and the committed transfer is zero mass / zero advected energy / zero hydraulic exchange for that step. Positive forward flow remains unchanged, including passive forward flow through a stopped pump when upstream pressure is sufficient.

The current-v2 sustained-generation and synchronization definitions enable this only on `condensate-pump` and `feedwater-pump`. The main circulation pump and legacy/default definitions remain unchanged. This removes nonphysical secondary-train backflow such as a stopped condensate pump flowing from the pressurized feedwater inventory back into the hotwell without globally forbidding reverse flow in the hydraulic framework.
