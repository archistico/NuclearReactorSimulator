# M8.6 — Electrical Loss & Station Blackout-Class Scenarios

## Purpose

M8.6 adds deterministic educational scenarios for loss of the modeled external electrical-supply connection and station-blackout-class loss of powered plant functions.

The current reference plant has a validated M4.5 generator/infinite-grid boundary, canonical pump owners and M5 control/actuator seams. It **does not** contain a detailed station electrical distribution model: there are no modeled AC/DC buses, transformers, diesel generators, batteries, switchgear transfer logic or emergency-core-cooling electrical trains.

M8.6 therefore composes only capabilities that actually exist. It does not invent an unvalidated electrical network behind a scenario label.

## External-supply-loss boundary

The fault type is:

```text
electrical.external-supply-loss
```

Its target is the exact canonical `ElectricalGridDefinition.Id`.

While active, the Application runtime constrains the existing M4.5 generator/grid input seam so every generator connected to that modeled grid receives an open-breaker request and no close request for the physical step.

```text
committed scenario fault state
        ↓
electrical.external-supply-loss
        ↓
canonical GeneratorGridInputs
        ↓
open breaker command dominates operator close request
        ↓
M4.5 GeneratorGridSolver
        ↓
canonical breaker/electrical/rotor consequences
```

The fault never writes breaker state, electrical power, torque, rotor speed or grid phase directly. After fault clearance, the forcing disappears but the breaker remains in its actual committed state; reconnection requires the normal synchronization/close path.

The M4.5 infinite-bus grid reference still exists mathematically. M8.6 models loss of the **station connection to that external supply**, not collapse or electromagnetic transient behavior of a regional power grid.

## Station-blackout-class composition

`m86-station-blackout-class` is intentionally a composed scenario rather than a new monolithic physics solver.

At the same deterministic initiating boundary it declares:

- external-grid connection loss through the new M8.6 fault;
- canonical trip of the modeled main-circulation, feedwater and condensate pumps through validated M8.2 pump faults;
- powered pump-command-path fail-low effects through validated M8.3 actuator-command faults;
- turbine and generator trip through validated M8.4/M5.5 protection seams.

This makes each assumed unavailable function explicit and replay-visible. Nothing is inferred from an imaginary bus topology.

## Decay-heat limitation

M2.5 contains a validated stateful decay-heat model. However, the current M5.7 integrated automatic-operation runtime used by M7/M8 scenarios does **not** promote that M2.5 state into its canonical runtime envelope; its current primary-circuit operational recipe carries `TotalDecayHeatPower = 0`.

M8.6 therefore does **not** create a fake fixed post-shutdown percentage or a scenario-owned decay-heat integrator. The blackout-class exercise can train electrical isolation, loss of forced circulation/feedwater/condensate capability, protection response and use of the systems currently integrated, but it must not claim a quantitative decay-heat coastdown until the validated M2.5 state/history is composed into the full-plant runtime.

This limitation is deliberate and preferable to fabricating accident heat.

## Determinism and replay

All M8.6 initiating events remain ordinary M8.1 scenario faults:

- exact logical-step or committed-condition activation;
- single-pass `Pending → Active → Cleared` lifecycle;
- no wall clock;
- no randomness;
- normal M7.1 replay from the same initial condition, scenario and operator command trace.

## Explicit non-capabilities

M8.6 does not model:

- station AC/DC bus topology;
- loss-of-voltage relay physics;
- transformer/switchgear faults;
- emergency diesel start/transfer;
- battery depletion;
- inverter/DC control power;
- ECCS electrical trains;
- regional grid frequency/voltage collapse;
- licensing-grade station-blackout analysis.

These capabilities require explicit future physical/state ownership rather than scenario-layer inference.
