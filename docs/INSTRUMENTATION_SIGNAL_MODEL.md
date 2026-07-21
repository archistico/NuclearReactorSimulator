# Instrumentation & Signal Model

M5.1 establishes the observation boundary between the physical full-plant truth produced by M4.7 and every future controller, protection function and operator display.

## Core separation

```text
FullPlantSnapshot (true physical state)
        ↓
InstrumentSignalSourceCatalog
        ↓
InstrumentationSolver
        ├── range / scaling
        ├── deterministic first-order lag
        ├── validity / quality
        └── explicit sensor-fault seam
        ↓
MeasuredSignalFrame
        ↓
future M5 controllers / M6 UI
```

`MeasuredSignalFrame` deliberately contains no `FullPlantSnapshot` reference and no true-value field. Future control logic must consume measured channels rather than traverse the physical snapshot directly.

Diagnostic processing traces may expose the true source value for verification, but these diagnostics are kept outside the controller-facing measured frame.

## Canonical channel definition

Each `InstrumentChannelDefinition` binds:

- a stable channel id;
- a stable semantic true-state source id;
- engineering-unit symbol;
- finite measurement range;
- linear output/raw scale;
- deterministic first-order lag time constant;
- explicit range-clamping policy.

Multiple channels may intentionally observe the same source so redundant instrumentation can be modeled later without changing the source seam.

## Source catalog

`InstrumentSignalSourceCatalog.CreateFullPlantCatalog(...)` provides canonical aggregate and per-component seams over the M4.7 snapshot, including:

- reactor thermal power;
- gross electrical output;
- total primary mass;
- turbine shaft power;
- condenser heat rejection;
- per-drum pressure and level;
- per-rotor speed;
- per-condenser pressure/vacuum;
- per-generator frequency and electrical output.

The catalog validates unit compatibility before simulation starts.

## Deterministic lag

Instrumentation dynamics are stored separately in `InstrumentationState`.

For a positive lag time constant, one exact first-order discrete update is used:

```text
alpha = 1 - exp(-dt / tau)
filtered_next = filtered_committed + alpha * (true - filtered_committed)
```

The first sample initializes directly from truth, avoiding an artificial startup transient unless an initial instrument state is supplied explicitly.

No wall-clock timing is used.

## Range, scaling and quality

Every channel reports:

- engineering value when available;
- scaled/raw output value when available;
- `SignalValidity`;
- `SignalQuality`;
- out-of-range indication;
- active fault mode.

Clamping never hides a range violation: a clamped value retains degraded quality and an explicit out-of-range flag.

## Fault seam

M5.1 defines deterministic sensor behavior modes:

- `None`;
- `Bias`;
- `Freeze`;
- `FailedLow`;
- `FailedHigh`;
- `Unavailable`.

The seam is explicit input, not hidden randomness. M5.1 does not schedule faults. Scenario activation, conditional timing and fault orchestration remain deferred to M8.

## Full-plant composition

`InstrumentedFullPlantSolver` composes the validated M4.7 `FullPlantSolver` with instrumentation observation:

1. M4.7 advances physical state exactly once;
2. the immutable candidate `FullPlantSnapshot` is observed;
3. instrumentation/filter state advances exactly once;
4. `InstrumentedFullPlantSnapshot` exposes true state and measured state as separate boundaries.

Instrumentation state is observational. It is not a conserved plant inventory and cannot feed hidden corrections back into physics.

## Explicitly deferred

M5.1 does not add:

- P/PI/PID controllers;
- actuator dynamics or manual/auto ownership;
- automatic synchronizer, AVR or governor;
- reactor/feedwater/turbine automatic loops;
- interlocks, trips or SCRAM logic;
- alarm/annunciator semantics;
- scenario fault scheduling.

Those belong to M5.2 onward and M8.


## M5.2 downstream contract

M5.2 consumes `MeasuredSignalFrame` directly. Generic controllers bind only to measured channel ids and cannot traverse `FullPlantSnapshot`. Invalid/unavailable measurements therefore remain visible to the control layer instead of being bypassed through true-state fallback. See `CONTROLLER_ACTUATOR_PRIMITIVES.md`.


## M5.3 reactor/primary measured sources

M5.3 extends the canonical full-plant source catalog with per-main-circulation-loop signals used by primary-system controllers:

- `main-circulation-loop/{loopId}/total-pump-flow` in `kg/s`;
- `main-circulation-loop/{loopId}/header-pressure-rise` in `Pa`.

These remain ordinary M5.1 measured sources: plant-specific control consumes the resulting `MeasuredSignalFrame`, never the underlying true-state snapshot directly.

## M5.4 secondary-cycle measured sources

M5.4 reuses the existing rotor-speed, generator-output, steam-drum-pressure and steam-drum-level sources and adds one canonical condenser inventory source:

```text
condenser/{condenserId}/hotwell-mass   kg
```

This source is consumed through ordinary instrument channels; hotwell controllers do not read `FullPlantSnapshot` directly.
