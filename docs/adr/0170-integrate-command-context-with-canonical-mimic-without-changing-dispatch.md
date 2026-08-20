# ADR-0170 — Integrate command context with the canonical mimic without changing dispatch

## Status

Proposed in M10.9.5.3; becomes Accepted only after local build, complete ordinary tests, focused HMI-contract tests and manual HMI inspection are explicitly green.

## Context

M10.9.5.1 and M10.9.5.2 validated authored command consequences and bounded dependency chains. The operator now needs to inspect those relationships inside F4 COMMANDS without turning navigation into command execution or creating a second plant topology.

## Decision

- Keep command selection non-dispatching; ENTER/EXECUTE remains the explicit typed-command boundary.
- Present direct effect, expected influence and monitor evidence as separate sections.
- Expose the authored dependency chain as a selectable presentation list.
- Reuse `ControlRoomPlantMimicProjector` output in the Operator Computer snapshot.
- Map only authored graphical dependency references to canonical mimic focus; no plant state or command target is changed by inspection. `PlantMimicElement` focuses that element directly, `PlantMimicConnection` uses a visibly labelled source-element proxy, and non-graphical `CommandTarget`/`PublishedState` references clear focus instead of falling back to an unrelated element.
- Blocked/unavailable commands remain inspectable.
- Do not add numerical prediction, automatic graph traversal or new permissive/protection ownership.

## Consequences

M10.9.5.4 can attach observed post-dispatch evidence to the same authored monitor set while keeping expected influence and observed response structurally distinct.
