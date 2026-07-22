# Operator Computer — Unified Information, Guidance & Diagnostics

## Status

**M10.2 IMPLEMENTATION CANDIDATE — built on validated M10.1.**

M10.2 activates the GUIDANCE, INFO and DIAGNOSTICS pages of the fixed M10.1 terminal without introducing new physics, new checklist/readiness criteria or scenario-specific UI logic.

## Ownership

The terminal remains an Application aggregation + App presentation surface:

```text
M7 guidance plans + canonical checklist evaluators
ControlRoomSnapshot / M6 presentation values
        ↓
Application generic M10.2 projection contracts
        ↓
OperatorComputerSnapshot
        ↓
App OperatorComputerViewModel / terminal text presentation
```

The terminal does not own or recompute:

- reactor/plant physics;
- measured signals;
- procedure readiness criteria;
- training scoring;
- scenario progression;
- control permissives/interlocks.

## GUIDANCE

M10.2 adapts the existing M7.2–M7.6 procedure plans through a generic immutable terminal contract and, when M7.7 training is active, projects its existing checkpoint/score assessment as read-only guidance evidence:

- Pre-startup preparation;
- First criticality / low-power operation;
- Heat-up / steam raising / turbine startup;
- Grid synchronization / initial loading;
- Power manoeuvring / normal shutdown.

`TrainingGuidanceMode` semantics remain authoritative:

- `Hidden`: step-by-step procedure text is suppressed;
- `ChecklistOnly`: step-by-step procedure text is suppressed while checklist/diagnostic evidence remains available;
- `Guided`: procedure steps are shown.

M10.2 does not duplicate plan rules. Step status is derived from the same canonical checklist results already produced by the corresponding M7 evaluator:

```text
[OK] completed/currently satisfied
[>>] first currently incomplete procedure step
[--] later pending step
```

For M7.7 sessions, `TrainingCheckpointProgress` and the existing assessment score are also projected read-only. M10.2 does not create a second checkpoint evaluator or score owner.

This is presentation guidance only. It cannot change plant state or scoring.

## INFO

The INFO page projects only values already present in `ControlRoomSnapshot`/M6 panel contracts. M10.2 adds no hidden read of Simulation state.

The compact sections are:

- REACTOR;
- PRIMARY;
- TURBINE / SECONDARY;
- ELECTRICAL;
- PROTECTION / SIGNAL HEALTH.

Each item carries explicit presentation provenance:

- `[MEASURED]` — value already published as an operational measured presentation value;
- `[MODEL]` — already-published model diagnostic/presentation value;
- `[STATE]` — canonical discrete presentation state such as trip/clear;
- `[UNAVAILABLE]` — the underlying presentation contract marks the value unavailable.

Unavailable values remain unavailable (`—`). M10.2 never substitutes zero, a true-state value or an inferred estimate.

## DIAGNOSTICS

DIAGNOSTICS is a generic projection over the active scenario/procedure checklist evaluator.

It reports:

- each canonical check title;
- satisfied/not-satisfied status;
- the evaluator's existing observation text;
- a deterministic satisfied/not-satisfied count over the declared checks.

M10.2 deliberately does not collapse a multi-phase procedure into a universal READY/NOT READY flag because some canonical checks describe mutually successive operating states.

The diagnostic list is procedure/checklist evidence only. It is not a universal plant-health or permissive claim and does not bypass runtime permissives/interlocks.

If no canonical evaluator is available for the loaded scenario, DIAGNOSTICS reports that content is unavailable. It does not invent generic criteria.

## Fixed-page staging

After M10.2:

```text
GUIDANCE      AVAILABLE where canonical guidance exists
INFO          AVAILABLE from ControlRoomSnapshot presentation contracts
ALARMS        reserved for M10.3
COMMANDS      reserved for M10.4
MODES         reserved for M10.5
DIAGNOSTICS   AVAILABLE where canonical evaluator exists
LOG           reserved for M10.3
SESSION       reserved for M10.7
```

## Determinism and replay

M10.2 is observational. Projection order and text derive only from immutable current snapshots, versioned guidance plans and deterministic checklist evaluation. It does not add wall-clock state, random behavior, recorder events or replay inputs.

## Deliberate non-goals

M10.2 does not:

- add alarm history/ACK/RESET to the terminal;
- add contextual plant commands;
- change `TrainingGuidanceMode` from the terminal;
- add Manual/Assisted/Supervisory authority;
- create a universal diagnostic engine;
- expose private Simulation state;
- add save/load/checkpoint/replay actions.
