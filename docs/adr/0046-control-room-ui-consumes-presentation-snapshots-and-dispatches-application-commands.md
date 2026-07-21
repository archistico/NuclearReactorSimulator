# ADR 0046 — Control-room UI consumes presentation snapshots and dispatches application commands

## Status

Accepted for M6.1 baseline candidate.

## Context

M5.7 completes deterministic automatic operation and exposes stable measured, control, protection and alarm boundaries. M6 now needs a production Avalonia shell without allowing views/view models to traverse authoritative true state or execute simulation physics.

## Decision

M6.1 introduces a narrow Application-layer `ControlRoomSnapshot` projection and snapshot-source boundary. Avalonia view models consume only this presentation contract. Operator actions leave the UI through typed `ControlRoomCommand` values and `IControlRoomCommandDispatcher`.

The Avalonia project removes its direct project reference to `NuclearReactorSimulator.Simulation`. Detailed workspaces remain placeholders until later M6 milestones.

Presentation performance budgets are explicit but cannot influence simulation timestep or results.

## Consequences

- UI code cannot directly depend on Simulation namespaces through an approved direct project reference.
- The control room can evolve independently from physical snapshot structure.
- Runtime orchestration can later replace the M6.1 shell command/snapshot adapters without rewriting views.
- Rendering frequency remains an output concern only.
- M6.2 can focus on reusable instruments and operator controls over a stable shell contract.
