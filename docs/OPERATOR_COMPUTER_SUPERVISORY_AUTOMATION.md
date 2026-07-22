# Operator Computer & Supervisory Automation — Approved M10 Plan

## Status

**APPROVED ARCHITECTURE / ROADMAP PLAN — M10 IN PROGRESS; M10.1 VALIDATED; M10.2 implementation candidate delivered.**

This document records the approved design direction for M10. M10.1–M10.6 are validated. M10.7 is the current implementation candidate adding replay-backed session/checkpoint/save/load lifecycle over the existing M7/M9 owners. M10.8–M10.9 remain governed by this plan.

## Purpose

The operator computer is a unified terminal metaphor over capabilities that are currently distributed across control-room panels, training guidance, alarms, operational history, scenarios, recorder/replay and post-incident analysis.

It is **not a new physical system owner**. Presentation aggregation belongs in Application/App. Real plant automation is a distinct M5-owned capability that the computer may select and observe but never implement itself.

```text
Existing canonical owners
M5 control/protection/alarms
M6 control-room presentation/history
M7 scenarios/guidance/evaluation
M9 recorder/checkpoint/replay/analysis
        ↓
Application aggregation
OperatorComputerSnapshot + typed page contracts
        ↓
App presentation
ControlRoomComputerControl / ViewModel
```

## Non-negotiable dual-axis model

M10 must never conflate student assistance with physical control authority.

### Training assistance

Reuses the validated M7.7 `TrainingGuidanceMode` concept:

- Hidden / None;
- ChecklistOnly / Checklist;
- Guided.

This axis changes only what educational guidance is presented. It must not alter physics, controller behavior, protection, deterministic state evolution or scoring semantics.

### Plant control authority

Adds an explicit physical-operation authority model:

- **Manual** — operator directly chooses normal plant actions; automatic protection remains active;
- **Assisted** — operator chooses goals/setpoints/controller modes and existing local M5 controllers regulate the plant;
- **Supervisory Automatic** — an M5-owned deterministic supervisor may coordinate existing local controllers toward bounded high-level operating objectives.

Requested mode, effective mode and health/degraded state must be distinguishable. A global mode must not hide per-loop controller mode or mixed-mode operation.

## Ownership of real automation

The operator computer does not own automation.

```text
Operator objective / mode request
        ↓
Application typed intent boundary
        ↓
M5 SupervisoryOperationCoordinator (provisional name)
        ↓
existing controller modes / setpoints / typed canonical commands
        ↓
M5.2 / M5.3 / M5.4 local controllers
        ↓
canonical actuators
        ↓
M2 / M3 / M4 physics
```

Forbidden shortcut:

```text
supervisor → set physical result directly
```

The supervisor must never directly assign reactor power, pressure, level, rotor speed, generator power, breaker outcome or other derived physical result.

Protection/interlocks remain superior to all normal/supervisory control. Supervisory automation must not automatically reset SCRAM/turbine/generator trips or acknowledge alarms.

## Command/intention taxonomy

Not every terminal action belongs in `ControlRoomCommandKind`.

M10 must preserve three distinct categories:

1. **Plant commands** — physical operator intents routed through canonical control-room dispatch/runtime validation.
2. **Training/presentation intents** — e.g. changing guidance mode; these do not own physical plant state.
3. **Session lifecycle intents** — scenario load, checkpoint, seek, replay, persistent session archive operations; these remain owned by M7/M9 session/recorder/replay services.

A UI availability catalog may explain whether a command appears available or blocked, but runtime validation/interlock enforcement remains authoritative and fail-closed.

## Terminal interaction model

The terminal uses fixed named pages, never a free-form command language:

```text
GUIDANCE · INFO · ALARMS · COMMANDS · MODES · DIAGNOSTICS · LOG · SESSION
```

No natural-language parser, LLM dependency or hidden nondeterministic command interpretation is introduced.

Presentation direction:

- monospace/HUD visual language consistent with the control room;
- `HudVoid` background and existing HUD semantic colors;
- tabular numeric presentation where appropriate;
- fixed menu at top and fixed status line at bottom;
- mouse supported, but complete keyboard-only operation is required;
- page selection, focus, scroll and cursor state remain App-only presentation state.

Recommended keyboard mapping may use F1–F8 for the eight pages plus arrows, Enter, Escape, PageUp/PageDown and Tab, subject to final Avalonia accessibility testing.

## Page responsibilities

### GUIDANCE

Projects the active M7 guidance plan/checklist through a generic Application contract. Scenario-specific evaluators/plans remain their own owners. The terminal must not reproduce guidance rules in UI code.

### INFO

Presents fixed compact pages such as REACTOR, PRIMARY, STEAM, TURBINE, FEEDWATER, ELECTRICAL and PROTECTION using values already promoted through canonical presentation contracts.

Every value must preserve provenance/quality semantics such as:

- Measured;
- Model Diagnostic;
- Unavailable.

The terminal must never fabricate a value or appear more instrumented than the validated plant model.

### ALARMS

Combines current canonical alarm/annunciator state with deterministic logical-step event history. ACK/RESET use existing canonical actions. Alarm acknowledgement/reset remains distinct from physical protection reset.

### COMMANDS

Shows a deterministic contextual command catalog with:

- command/target;
- display name;
- current state;
- available/blocked status;
- blocking reason where known.

The catalog is a presentation aid, not the final permissive/interlock authority.

### MODES

Shows both independent axes:

```text
TRAINING ASSISTANCE
None / Checklist / Guided

PLANT CONTROL
Manual / Assisted / Supervisory Automatic
```

It must also expose relevant per-loop controller modes, mixed-mode state, requested/effective authority and degraded status.

### DIAGNOSTICS

Projects existing scenario/procedure readiness evaluators through a generic adapter. It may answer procedure-specific readiness questions only where a canonical evaluator exists.

It must not silently invent a universal plant diagnostic engine. A future broader `Plant Health` concept, if desired, requires explicit contracts/ownership based on promoted measurements, alarms, protection, fault/equipment availability and audit data.

### LOG

Unifies three scopes without duplicating storage:

- **LIVE** — bounded M6.6 logical-step operational history;
- **SESSION** — M9.1 deterministic recording/events;
- **INCIDENT** — M9.2 post-incident analysis/evidence timeline.

Temporal ordering is evidence, not automatic causal inference.

Semantic provenance of important decisions should be visible where supported, e.g. Operator / Local Automatic Controller / Supervisory Automation / Protection / Scenario-Fault, without logging every PID sample as a high-level event.

### SESSION

Exposes existing exact-version scenario/session/recorder/checkpoint/replay capabilities.

M9.1 already owns replay-backed checkpoints and verified seek. M10 may add a persistent versioned session archive containing the exact scenario identity, recording/action history, checkpoints and metadata needed to reconstruct/verify a session.

Restoration must remain replay-backed. M10 must not introduce an opaque solver-state dump, second checkpoint owner, second fault trace or independent historian.

## Supervisory automation safety/behavior rules

### Measured-state discipline

Any supervisory consumer that operationally depends on instrumentation uses the validated measured-signal contracts only. No silent fallback to true internal state.

### Degraded/fail-closed operation

Supervisory control must distinguish:

- requested mode;
- effective mode;
- status/health;
- degradation reason.

Invalid required measurements, unavailable equipment or unsatisfied authority/permissive conditions must lead to explicit deterministic degraded/hold/fallback behavior. They must not trigger hidden guessing or true-state bypass.

### Manual takeover

Manual takeover is a first-class acceptance requirement:

```text
Supervisory active
        ↓ operator requests Manual
stop new supervisory decisions
        ↓
defined bumpless authority handover
        ↓
operator owns normal control requests
```

Existing M5.2 bumpless-transfer/controller semantics should be reused rather than recreated.

### Fault interaction

The supervisor must not inspect scenario/fault identities as a privileged source of physical truth. It reacts to canonical measured state, alarms, protection and explicitly published equipment availability/operational contracts.

This enables meaningful degraded automation under M8 sensor/component faults without coupling M5 control logic to scenario implementation details.

## Approved M10 milestone sequence

1. **M10.1 — Operator Computer Contracts & Terminal Shell**
2. **M10.2 — Unified Information, Guidance & Diagnostics**
3. **M10.3 — Alarm, Log & Incident Workstation**
4. **M10.4 — Contextual Command Console**
5. **M10.5 — Dual Assistance & Control-Authority Model**
6. **M10.6 — Supervisory Automatic Operation**
7. **M10.7 — Session, Checkpoint, Replay & Save Workspace**
8. **M10.8 — Integrated Operator Computer UI**
9. **M10.9 — Integrated Human-Automation Validation Gate**

M10 begins only after the planned M9 fidelity/calibration integration gate. Final product release hardening is moved to M11.

## Acceptance principles for the phase

- no page introduces a second owner for physics, alarms, guidance, checklist evaluation, history, replay or sessions;
- no UI/Avalonia physics or control algorithms;
- all physical actions reuse canonical typed control seams and remain subject to runtime validation/protection priority;
- guidance mode and plant control authority remain independent;
- keyboard-only operation is fully supported;
- deterministic logical-step semantics only: no wall-clock/random hidden behavior;
- supervisory automation uses measured signals, degrades fail-closed and supports deterministic bumpless manual takeover;
- session save/load remains exact-version and replay-backed;
- M10 closes only after integrated deterministic fault/protection/replay/automation validation.

## Deliberate non-goals

- no free-form text command prompt or natural-language parser;
- no LLM/external model dependency for plant control;
- no direct physical-state assignment by supervisory automation;
- no automatic bypass/reset of protection or interlocks;
- no second historian, recorder, checkpoint format or fault trace;
- no claim that existing scenario-specific checklist evaluators form a universal plant-health diagnostic engine.
