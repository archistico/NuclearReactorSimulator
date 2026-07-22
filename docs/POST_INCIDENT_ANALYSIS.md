# Post-Incident Analysis

M9.2 adds an observational analysis layer over validated M9.1 recordings.

## Evidence model

The authoritative source is `ScenarioRecording`:

- contiguous fixed-step frames;
- deterministic recorder event stream;
- accepted typed operator actions;
- replay-backed checkpoints;
- exact scenario and initial-condition identity.

The analyzer never reads private solver state and never mutates runtime state.

## Incident anchor

An analysis can specify an exact recorder event sequence. Without an explicit anchor, selection is deterministic:

1. first fault transition to `Active`;
2. otherwise first protection transition to `Active`;
3. otherwise first alarm activation;
4. otherwise first operator action;
5. otherwise earliest remaining recorder event.

This ordering is a selection policy, **not a causal claim**.

## Timeline

Each event in the selected logical-step window records:

- recorder sequence;
- absolute logical step;
- logical-step offset from the anchor;
- before / anchor / after relation;
- event kind;
- source id;
- recorded detail;
- the original typed `ControlRoomCommand` for accepted operator actions.

Events sharing the same logical step retain recorder-sequence ordering.


## Synchronized trend samples

Every fixed-step frame in the selected window is retained as a synchronized trend sample. M9.2 uses only numeric values already exposed by the M6 `ControlRoomSnapshot` presentation contract:

| Field | Unit |
|---|---|
| Reactor thermal power | MWth |
| Total primary mass | kg |
| Total feedwater flow | kg/s |
| Total steam export flow | kg/s |
| Total turbine steam flow | kg/s |
| Total turbine shaft power | MW |
| Total condenser heat rejection | MW |
| Gross electrical output | MWe |

Unavailable presentation values remain `null`; M9.2 never synthesizes missing measurements or diagnostics. Each sample also carries invalid-signal/alarm/fault counts and trip-latch states at the same logical step.

## Response metrics

M9.2 derives only directly observable temporal metrics:

- first alarm-activation latency;
- first protection activation latency;
- first operator-action latency;
- first fault-clear latency;
- peak invalid measured signals;
- peak annunciated and unacknowledged alarms;
- peak active fault count.

A missing latency means “not observed inside this analysis window”, not “never occurred”.

## Checkpoint linkage

The report references the nearest preceding M9.1 checkpoint when available. Restoration remains owned by `ScenarioFullReplayRunner.SeekAndVerify`; M9.2 does not create a second restore path.

## Fidelity boundary

Post-incident analysis is intended for deterministic educational debrief. Temporal ordering may support human investigation, but the software does not infer root cause merely from event proximity.


## Deliberate omission: conservation audits

M9.1 recordings currently retain `ControlRoomSnapshot`, not private M4/M5 solver audit objects. M9.2 therefore does not reach into Simulation to fabricate conservation diagnostics after the fact. If conservation residuals are required in debrief reports, they must first be promoted through an explicit, versioned presentation/recording contract in a later milestone.
