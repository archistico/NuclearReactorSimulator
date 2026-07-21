# ADR 0004 — Avalonia presentation boundary

- Status: Accepted
- Date: 2026-07-20

## Context

The simulator requires a rich control-room UI, but its physical model must remain independently executable and testable.

## Decision

Only `NuclearReactorSimulator.App` may reference Avalonia packages. UI code communicates with the application/simulation layers through commands and immutable snapshots.

## Consequences

- no simulation calculations in Views or ViewModels;
- core libraries remain UI-framework agnostic;
- headless test execution remains possible.
