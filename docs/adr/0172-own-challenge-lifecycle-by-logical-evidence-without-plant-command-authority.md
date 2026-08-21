# ADR-0172 — Own challenge lifecycle by logical evidence without plant-command authority

## Status

Accepted — M10.9.6.1 validated 2026-08-20

## Context

M10.9.6 needs operational challenges that can become ready, active, complete, fail, cancel and reset deterministically. Existing M7.7 training code already owns checkpoint/criterion assessment and historical scoring; it must not be replaced. The new challenge layer also must not become another plant-control owner or use wall-clock timing.

## Decision

Introduce a versioned Application-layer `ChallengeDefinition` and `ScenarioChallengeTracker` with these boundaries:

1. lifecycle states are `NotStarted`, `Ready`, `Active`, `Completed`, `Failed`, `Cancelled`;
2. readiness, target windows and hard deadlines use logical simulation steps only;
3. challenge conditions are authored references evaluated only from immutable `ControlRoomSnapshot` evidence and accepted scenario operator-action history; required observations gate completion rather than acting as decorative telemetry;
4. the tracker consumes a read-only `IChallengeEvidenceSource` and receives no command dispatcher or control-authority seam;
5. target windows are observational; failure by time requires an explicit hard deadline;
6. declared failure conditions take precedence over completion if both are satisfied at the same logical step;
7. cancel/reset are explicit lifecycle operations, never presentation-navigation side effects;
8. challenge definitions own allowed assistance modes and a scoring-policy identity, but M10.9.6.1 performs no score arithmetic;
9. challenge state is replay-derivable from exact identities plus deterministic logical/action evidence rather than treated as an opaque physical-state dump.

## Consequences

- UI refresh rate and machine wall time cannot change challenge outcomes.
- M10.9.6.2 can add deterministic demand evidence without changing lifecycle ownership.
- M10.9.6.3 can resolve the declared scoring-policy identity without moving scoring into UI code.
- M10.9.6.5 can reconstruct challenge state through replay/checkpoint using the same logical evidence contract.
- A future challenge can classify a protection event as failure, protected completion or neither by its own authored conditions; there is no global hidden classification.

## Non-decision

This ADR does not choose score weights, grade thresholds, external demand profiles, initial challenge packs or Mission/Performance UI placement.
