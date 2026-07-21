# ADR 0001 — Clean layered architecture

- Status: Accepted
- Date: 2026-07-20

## Context

The simulator is expected to grow from a small deterministic core into a multi-system full-plant educational simulator. Physics, UI, persistence and orchestration must remain independently testable.

## Decision

Use five production projects:

- Domain
- Simulation
- Application
- Infrastructure
- App

Dependencies are directional and enforced by automated architecture tests.

## Consequences

- physics remains headless and testable;
- Avalonia cannot leak into core layers;
- persistence can evolve independently;
- the composition root has explicit responsibility for wiring implementations.
