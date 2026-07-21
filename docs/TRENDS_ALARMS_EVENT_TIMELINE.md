# Trends, Alarms & Event Timeline

M6.6 turns the stable `Alarms & Events` workspace into a deterministic operator-history surface without moving simulation ownership into Avalonia.

## Data flow

```text
M5.7 immutable automatic-operation snapshot
        ↓
ControlRoomSnapshotProjector
        ├─ presentation values already used by M6.3–M6.5
        └─ M5.6 AlarmSystemSnapshot
                ↓
ControlRoomSnapshot
        ├─ AlarmEventsPanelSnapshot
        └─ presentation scalar values
                ↓
ControlRoomOperationalHistoryAccumulator
        ├─ logical-step trend samples
        └─ monotonic alarm-event sequences
                ↓
Avalonia Trends / Annunciator / Timeline
```

The history accumulator is Application-layer presentation state only. It owns no conserved inventory, control-loop state, protection latch, alarm latch or deterministic simulation clock.

## Trend semantics

Trend sources are declared by the stable `ControlRoomTrendSourceCatalog`. The default configured set contains reactor thermal power, primary steam-export flow, turbine shaft power and gross electrical output. Additional catalogued presentation sources can be enabled without changing Simulation state.

A trend point is `(LogicalStep, Value?)`. There are no wall-clock timestamps. When another presentation snapshot for the same logical step is observed, the most recent presentation value replaces the previous point rather than creating an artificial extra sample.

History is bounded. The default trend buffer stores at most 240 points per configured series, and the number of simultaneously configured series is constrained by `ControlRoomPerformanceBudget.MaximumVisibleTrendSeries`.

Unavailable values remain nullable and appear as gaps in the textual sparkline. No true-state fallback is used.

## Annunciator semantics

`AlarmEventsPanelSnapshot` mirrors M5.6 alarm semantics into presentation-only types:

- advisory / warning / trip severity;
- active/returned and acknowledged/unacknowledged annunciator state;
- latched state;
- first-out identity and group;
- activation sequence;
- reset eligibility derived from the published condition/ack/latching state.

ACK and RESET leave Avalonia as typed `ControlRoomCommand` intents. Targeted commands use `ControlRoomCommandTargetKind.Alarm`; bulk ACK/RESET remain explicit command kinds.

These commands address M5.6 annunciator memory only. They never reset M5.5 protection, clear SCRAM/trip latches, remove interlocks or mutate physical actuator state.

## First-out

First-out groups are displayed exactly as published by M5.6. M6.6 does not recompute the initiating alarm from current conditions. This preserves the deterministic first-out decision already made by the validated alarm layer.

## Event timeline

M5.6 events provide a monotonic logical `Sequence`. M6.6 adds the logical simulation step at which each event was published to the presentation boundary and retains a bounded, sequence-deduplicated timeline.

The UI therefore presents entries such as:

```text
#1046  STEP 8102  RESET         Steam drum pressure high
#1045  STEP 8099  ACKNOWLEDGED  Steam drum pressure high
#1042  STEP 8088  ACTIVATED     Steam drum pressure high
```

No `DateTime.Now`, `UtcNow`, `Stopwatch` or rendering timestamp participates in ordering.

## Replay compatibility

Given the same ordered `ControlRoomSnapshot` sequence, trend samples and alarm-event history are deterministic. Replaying a duplicate event sequence replaces/deduplicates the same entry rather than creating another event.

Rendering frequency can change without changing logical trend points or timeline ordering.

## Deliberate limits

M6.6 does not yet provide:

- persistent disk historian storage;
- arbitrary user-created expressions over true plant state;
- wall-clock timestamps;
- scenario markers beyond available logical events;
- runtime translation of the new alarm commands into a live coordinator.

The live coordinator, accelerated-run responsiveness and complete operator path are M6.7 responsibilities.

## M6.7 downstream integration

M6.7 routes ACK/RESET intents to the real M5.6 input seam through the live M5.7 runtime adapter. Trend/event history continues to observe only published `ControlRoomSnapshot` instances; sparse accelerated-run publication therefore reduces presentation traffic without changing the logical simulation steps that actually execute.
