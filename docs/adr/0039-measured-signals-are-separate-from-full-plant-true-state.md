# ADR 0039 — Measured signals are separate from full-plant true state

## Status

Accepted for M5.1 baseline candidate.

## Context

M4.7 provides a canonical immutable `FullPlantSnapshot` representing the best available simulated physical truth. Automatic controllers, protection systems and operator displays must not become coupled directly to perfect internal state, otherwise later sensor lag, range limits, failure, redundancy and bad-quality behavior would be impossible to model consistently.

## Decision

M5.1 introduces a dedicated instrumentation boundary:

- true-state extraction occurs only through stable semantic `InstrumentSignalSource` seams;
- `InstrumentationState` owns only sensor/filter dynamics;
- `MeasuredSignalFrame` is the controller/UI-facing contract and does not expose `FullPlantSnapshot` or true values;
- true values may appear only in diagnostic processing snapshots for verification;
- range, scaling, lag, validity and quality are deterministic model behavior;
- sensor faults are explicit deterministic inputs, never hidden random events;
- `InstrumentedFullPlantSolver` delegates physical evolution to M4.7 exactly once and then observes the resulting immutable snapshot.

## Consequences

Future M5 controllers and M6 views must depend on measured channel IDs and quality rather than traversing physics snapshots directly.

Redundant sensors may share one true-state source while retaining independent lag/state/fault behavior.

M8 may schedule fault inputs without changing the instrumentation physics model.

No additional conserved physical state or plant integrator is introduced.
