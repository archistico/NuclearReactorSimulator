# Subsystem Engineering Schematics

M10.9.4 defines the detailed engineering-diagram layer of the operator HMI.

## Design rule

A schematic must answer, without requiring the operator to infer hidden topology:

- what component am I looking at;
- what enters it;
- what leaves it;
- what process/signal direction is active;
- which values are important now;
- whether a line represents fluid, mechanical/electrical energy, measurement, control, feedback, alarm or protection override.

## Presentation ownership

Subsystem diagrams are Application-owned presentation topology derived from existing immutable control-room snapshots. They are not a second simulation network.

`ControlRoomSubsystemSchematicControl` only renders the projected contract.

## Visual grammar

### Process / energy

- primary coolant — cyan;
- steam — pale vapor line;
- condensate — blue;
- feedwater — green-cyan;
- mechanical shaft — amber;
- electrical power — violet.

These colors identify medium/type, not severity. In particular **amber shaft does not mean warning**.

### Signal flow

- measurement;
- normal control;
- feedback;
- alarm;
- protection override.

Signal paths are visually thinner/different from process paths. Protection override is intentionally stronger and higher-priority.

## Generator power-path diagnostic

The GRID workspace publishes a dedicated operator-readable diagnostic based only on existing canonical presentation data. It evaluates breaker, sync readiness, requested electrical load, shaft power, actual output and protection status.

The diagnostic does not predict future physics and does not issue commands.

## M10.9.4 Hotfix 6 turbine-flow clarification

The turbine/secondary schematic and related HMI now distinguish two historically different seams:

- `TotalSteamFlow`: legacy M4.1 turbine-admission boundary total retained in fingerprint-v1 serialization;
- `EffectiveTurbineSteamFlow`: presentation-only sum of M5.4-derived turbine stage-group effective flow through the commanded canonical stop/control/admission path.

Operator-facing turbine admission uses the second value. This prevents the schematic from reporting `0 kg/s` merely because the legacy boundary input remains zero while actual stage flow is being derived from canonical valve hydraulics.


## M10.9.4 Hotfix 7 operating-point note

The power-path schematic semantics are unchanged. Hotfix 7 only corrects the generation-ready v2 operating-point boundaries: condenser cooling is balanced to 25 MW and the pre-synchronization seed uses a bumpless spinning-reserve governor bias. Amber SHAFT remains an energy-medium color, never an alarm-severity color.
