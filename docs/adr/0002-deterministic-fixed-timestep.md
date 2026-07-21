# ADR 0002 — Deterministic fixed-timestep simulation

- Status: Accepted
- Date: 2026-07-20

## Context

A physical simulator must produce repeatable behaviour for testing, replay and incident analysis. Coupling physics updates to UI rendering or wall-clock jitter would undermine repeatability and numerical reasoning.

## Decision

The simulation engine will use a fixed physical timestep. Simulation time, wall-clock time and UI refresh cadence are separate concepts.

## Consequences

- pause, single-step and accelerated simulation become first-class behaviours;
- deterministic replay is feasible;
- tests can execute the engine headlessly without sleeping;
- UI performance cannot directly change physical results.
