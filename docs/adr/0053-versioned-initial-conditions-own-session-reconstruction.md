# ADR 0053 — Versioned initial conditions own deterministic session reconstruction

- Status: Accepted; M7.1 locally validated on 2026-07-21
- Date: 2026-07-21

## Context

M6.7 completed the live control-room runtime boundary but intentionally did not invent an initialized plant session. Starting M7 requires a stable way to select and reconstruct initial conditions without duplicating physical state ownership in Application/UI code or making scenario files silently depend on whichever configuration happens to be newest.

The project also already has deterministic logical command traces from M0, so a second wall-clock-oriented replay mechanism would create conflicting ownership.

## Decision

1. Every initial condition is addressed by an immutable exact `(InitialConditionId, Version)` reference.
2. `IVersionedInitialConditionFactory` owns reconstruction of a fresh runtime engine for that exact version.
3. `VersionedInitialConditionRegistry` requires exact-version resolution and rejects duplicates; no implicit "latest" fallback exists.
4. Scenario definitions contain metadata, descriptive objectives, one exact initial-condition reference and an explicit whitelist of operator command kinds.
5. Scenario schema persistence is versioned. Infrastructure owns JSON representation and deterministic schema migration. Migrations must preserve initial-condition identity/version and unknown future schemas fail closed.
6. `ScenarioSessionFactory` is the canonical load/start boundary and always starts a newly loaded session paused.
7. Scenario command gating is observational/policy-level only; it never mutates physical state or implements actuator physics.
8. Deterministic replay reuses the existing M0 `SimulationCommandTrace` and advances by explicit fixed logical steps only.
9. Full arbitrary state checkpoints/seek are not introduced here; M9.1 retains that ownership.

## Consequences

- Scenario content can evolve independently while old definitions continue to name the exact runtime seed they were authored against.
- Reproducibility does not depend on file load order, UI cadence or wall-clock time.
- Application/UI layers do not deserialize or synthesize authoritative M1–M5 state piecemeal.
- M7.2 can add a concrete cold-shutdown initial-condition factory without changing the session/replay architecture.
- Scenario authors cannot accidentally permit a command by omission: non-host operator actions fail closed unless declared.

## Rejected alternatives

### Serialize the complete current object graph as the M7.1 initial condition

Rejected. The full graph includes canonical definitions, cross-references and state owners whose serialization policy is not yet a validated checkpoint contract. General checkpoints remain M9.1 scope.

### Resolve an initial-condition ID to the newest registered version

Rejected. This would make old scenarios silently change behavior after a new initial-condition revision is introduced.

### Put replay timing in the scenario file as wall-clock timestamps

Rejected. This would violate deterministic logical-time ownership and duplicate the validated M0 command-trace seam.
