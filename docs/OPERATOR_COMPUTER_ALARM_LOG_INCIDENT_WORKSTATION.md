# Operator Computer — Alarm, Log & Incident Workstation

## Status

**M10.3 — IMPLEMENTATION CANDIDATE**

This capability was validated as part of the cumulative M10.2→M10.6 Hotfix 1 clean build/test gate.

## Purpose

M10.3 activates the fixed terminal pages `ALARMS` and `LOG` without creating new alarm, historian, recorder or incident-analysis owners.

Ownership remains:

- M5.6: canonical alarm/annunciator state and ACK/RESET semantics;
- M6.6: bounded live presentation history and logical-step alarm-event feed;
- M9.1: deterministic full-session recorder/checkpoint/replay evidence;
- M9.2: immutable post-incident evidence analysis.

The operator computer only projects these owners through immutable Application contracts.

## ALARMS page

The `ALARMS` terminal page is read-only in M10.3. It presents:

- annunciated and unacknowledged counts;
- current annunciated alarm rows;
- severity and annunciator state;
- first-out indication where available;
- recent logical-step alarm-event history from M6.6.

The terminal does not acknowledge or reset alarms in M10.3. Those typed actions remain canonical control-room commands and are staged for the M10.4 contextual command console. Alarm acknowledgement/reset never resets protection state.

## LOG page

The `LOG` page has three explicit evidence scopes.

### LIVE

Consumes `ControlRoomOperationalHistorySnapshot` from M6.6:

- bounded trend summaries;
- current/min/max values;
- presentation sparklines;
- bounded alarm-event history ordered by deterministic sequence/logical step.

No persistent disk historian or wall-clock timestamp owner is introduced.

### SESSION

Consumes M9.1 `ScenarioRecordingEvent` evidence only when a recorder is actually attached by the session lifecycle owner.

The default desktop composition does **not** automatically attach a full `ScenarioRecorder` merely to populate this page. This avoids silently adding per-fixed-step frame/fingerprint recording overhead to ordinary desktop operation. If no recorder evidence source is attached, the terminal states that explicitly and does not synthesize a second session log.

Recorder/session lifecycle remains staged for M10.7.

### INCIDENT

Consumes an immutable M9.2 `PostIncidentAnalysisReport` when one is explicitly supplied. It may present:

- selected anchor;
- ordered evidence timeline;
- relative logical-step offsets;
- response metrics;
- nearest preceding checkpoint reference.

If no finalized report exists, the page says so explicitly. M10.3 does not infer causality from temporal adjacency or run M9.2 analysis against mutable live state.

## Presentation bounds

The terminal view limits rendered event rows to recent bounded subsets for readability. This is presentation truncation only; it does not replace or mutate the canonical M6.6/M9.1 owners.

## Deliberate non-goals

M10.3 does not introduce:

- alarm ACK/RESET controls inside the terminal;
- a second alarm state machine;
- a second historian or persistent log format;
- automatic full-session recording in ordinary desktop mode;
- post-incident causal inference;
- checkpoint/session restoration;
- plant commands or supervisory automation.

These remain owned by existing layers or later M10 milestones.
