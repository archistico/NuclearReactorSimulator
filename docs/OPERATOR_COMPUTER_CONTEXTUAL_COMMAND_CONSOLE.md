# Operator Computer — Contextual Command Console

## Status

**M10.4 — IMPLEMENTATION CANDIDATE**

Baseline lineage:

- M10.4 was validated as part of the cumulative M10.2→M10.6 Hotfix 1 clean build/test gate;
- M10.2 and M10.3 remain unvalidated implementation candidates;
- M10.4 is layered on that candidate chain and must not be marked validated until the whole chain passes a clean local build/test gate.

## Purpose

M10.4 activates the fixed `COMMANDS` terminal page as a contextual view over the already canonical `ControlRoomCommand` / `IControlRoomCommandDispatcher` boundary.

The computer does not become a second control owner. It may:

- enumerate typed commands relevant to the current presentation snapshot;
- expand commands per canonical target (rod/group, pump, rotor, generator, breaker, alarm);
- show current state, presentation availability and an explicit blocking reason when known;
- dispatch the exact selected `ControlRoomCommand` through `IControlRoomCommandDispatcher`.

It must never write Simulation state directly or bypass scenario/runtime validation.

## Availability semantics

M10.4 uses three presentation states:

- `AVAILABLE` — the presentation snapshot contains no known reason to suppress the command; dispatch is still revalidated by the canonical runtime/scenario boundary;
- `BLOCKED` — the already-published presentation state provides a definite reason not to offer dispatch (for example an already-open breaker, unsatisfied synchronization permissive, rod-withdrawal inhibition, or an alarm that is not resettable);
- `UNAVAILABLE` — no integrated runtime/required target is present.

This is not a new permissive/interlock owner. `AVAILABLE` never guarantees acceptance. The runtime/scenario remains authoritative and fail-closed.

## Command families

The console projects the current `ControlRoomCommandKind` surface in contextual groups:

- runtime: Run, Pause, Single Step;
- protection: reactor SCRAM, protection reset, turbine trip, generator trip;
- reactor: insert/hold/withdraw for each canonical rod or rod-group target;
- primary: start/stop for each operator-commandable MCP;
- turbine: raise/lower speed for canonical turbine-rotor targets;
- electrical: raise/lower generator load, close/open generator breakers;
- alarms: acknowledge/reset per alarm plus acknowledge-all/reset-all.

M10.4 does not add training-assistance intents or session-lifecycle intents to `ControlRoomCommandKind`. Those remain separate ownership seams as approved by ADR 0070.

## Keyboard/menu interaction

The terminal remains fixed-menu and deterministic:

- `F4` opens COMMANDS;
- focus the command list and use native Up/Down selection;
- `Enter` executes the selected command;
- the explicit `EXECUTE SELECTED [ENTER]` button provides the same action.

Blocked/unavailable entries are selectable for inspection but are not dispatched.

## Fail-closed dispatch

Dispatch flow:

```text
OperatorComputerCommandSnapshot
        ↓
selected typed ControlRoomCommand
        ↓
IControlRoomCommandDispatcher
        ↓
scenario/runtime validation
        ├─ accepted → canonical fixed-step handling
        └─ rejected → terminal reports rejection; no direct state mutation
```

Alarm ACK/RESET continues to affect annunciator state only. It never resets physical protection implicitly.

## Non-goals

M10.4 does not introduce:

- a free-form command prompt or parser;
- natural-language control;
- new physical commands or direct-state setters;
- new interlock/permissive logic;
- training guidance-mode mutation;
- Manual/Assisted/Supervisory control-authority switching;
- supervisory automation;
- session/checkpoint/replay lifecycle commands.

Those remain later M10 milestones under the approved ownership plan.
