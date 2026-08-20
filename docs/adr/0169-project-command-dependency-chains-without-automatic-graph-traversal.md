# ADR-0169 — Project command dependency chains without automatic graph traversal

## Status

Accepted. M10.9.5.2 passed local build, complete ordinary tests and the focused dependency-chain gate.

## Context

M10.9.5.1 validated a complete authored qualitative consequence catalog for all current typed commands. The next step must show how a command relates to existing actuator/control state, plant paths, monitor signals and protection/alarm state without turning the Application layer into a causal inference engine or a duplicate plant topology owner.

## Decision

- Project a bounded authored chain for every command shape already mapped by M10.9.5.1.
- Give each step one explicit semantic kind: command intent, control/actuator state, physical process path, measurement/model observation or protection/alarm relation.
- Reuse the canonical typed command target for addressed devices/groups.
- Limit static topology references to existing whole-plant mimic element IDs, mimic connection IDs and published `ControlRoomSnapshot` paths.
- Reuse M10.9.5.1 monitor targets and provenance rather than creating a second measurement catalog.
- Do not run shortest-path, reachability or automatic causal graph traversal.
- Invalid/future command shapes fail closed as `NO AUTHORED DEPENDENCY CHAIN`.
- Chain projection is presentation-only and has no dispatch/runtime side effect.

## Consequences

M10.9.5.3 can highlight these explicit chains in COMMANDS/context-inspector/schematic surfaces without inventing new topology. Adding a new command or physical path requires an authored mapping and test rather than implicit graph inference.
