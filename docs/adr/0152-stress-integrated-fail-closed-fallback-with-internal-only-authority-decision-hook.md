# ADR 0152 — Stress integrated fail-closed fallback with an internal-only authority-decision hook

## Status

Accepted and validated with H.26 Hotfix 1 on 2026-08-19.

## Context

H.20 validated eight rollback reasons in isolation. H.22–H.25 validated real corrected commits but observed no H.20 rollback. Relying on naturally occurring rollback would leave the complete integrated fallback path largely untested and could require long or unstable trajectories.

## Decision

Add an `internal` test-only authority-decision transform to `PlantNetworkOrchestrator`. The public constructor always uses no transform. H.26 uses the internal seam only from `NuclearReactorSimulator.Simulation.Tests` to inject already-typed H.20 decisions immediately before the unchanged H.22 commit seam.

The hook must not:

- generate or reinterpret H.20 reason semantics;
- be exposed through production configuration or standard factories;
- alter P060/F040, H.9, hysteresis, target nodes, coefficients or timestep;
- bypass the historical explicit candidate evaluation.

## Consequences

H.26 can deterministically prove atomic same-step explicit fallback for every H.20 rollback reason without retuning physics or rerunning long-horizon qualification. Any future exposure of the hook outside internal test construction requires a new ADR and requalification.
