# Alarms & Annunciator State

M5.6 adds deterministic operator-facing alarm memory without changing any physical plant or protection ownership.

## Boundary

```text
MeasuredSignalFrame ───────────────┐
                                   ├─> AlarmSystemSolver -> AlarmSystemState / AlarmSystemSnapshot
M5.5 ProtectionSystemSnapshot ─────┘

Alarm ACK / alarm RESET -> annunciator memory only
                         -> never SCRAM/trip/interlock state
```

Alarm conditions may observe canonical M5.1 measured channels or already-decided M5.5 protection function/action/interlock state. They never become a second protection trigger path.

## Semantics

- `NonLatching`: annunciation follows the active condition and clears automatically when the condition clears.
- `LatchedUntilReset`: activation remains annunciated after the condition returns safe until an explicit alarm reset is accepted.
- Acknowledgement changes operator presentation memory only; it does not clear the alarm condition, alarm latch, protection latch or physical action.
- Alarm reset is accepted only for an acknowledged latched alarm whose underlying condition is no longer active.
- M5.5 protection reset remains a separate command and ownership boundary.

## First-out and event ordering

First-out alarms must be latched alarms and may belong to a named first-out group. The first activation in a group owns the first-out indication until that alarm is safely reset.

All alarm events use monotonic logical sequence numbers. Within a deterministic step, alarms are evaluated in canonical alarm-id order. No wall-clock timestamps are used by Simulation.

Event kinds are:

- activation;
- condition clear;
- acknowledgement;
- reset.

The resulting immutable event snapshots are designed for later M6 annunciator and timeline/recorder views.

## State ownership

`AlarmSystemState` contains only annunciator memory:

- previous condition state for edge detection;
- latch state;
- acknowledgement state;
- first-out ownership;
- activation/event sequence memory.

It contains no plant inventories, actuator positions, rod state, breaker state or protection actions.

## Composition

`AlarmedProtectedAutomaticFullPlantSolver` executes the validated M5.5 protected full-plant step exactly once, then advances M5.6 alarm memory from the same measured frame and the resulting protection snapshot. Alarm processing cannot change the already-computed protected physical candidate state.


## M5.7 integration

The M5.7 automatic-operation boundary preserves M5.6 ownership. Alarm processing still consumes the committed measured frame and resulting M5.5 protection snapshot. Candidate true-state instrumentation is published only for the next logical step, and alarm ACK/reset remains incapable of altering protection or physical state.
