# ADR-0176 — Reconstruct challenge state from canonical recordings instead of persisting opaque state

## Status

Proposed
**Date:** 2026-08-20

## Context

M10.9.6.1 made challenge lifecycle deterministic and derivable from logical-step plant evidence plus accepted operator actions. M10.9.6.2 and M10.9.6.3 made demand and score deterministic projections, while M10.9.6.4 composed six exact challenge packs. Closure now needs replay/checkpoint continuity without creating a second persistence authority beside the existing M9.1/M10.7 recording/archive system.

## Decision

Use canonical `ScenarioRecording` / `ScenarioSessionArchive` / `ScenarioCheckpoint` evidence as the only persisted replay source. Add `OperationalChallengeRecordingProjector` to reconstruct the selected exact challenge pack by feeding contiguous recorded frames and accepted action history through the existing M10.9.6.1 tracker.

Do not serialize an opaque challenge state blob into checkpoints or save archives.

Project external demand from the reconstructed lifecycle and immutable frame snapshot. Project score evidence deterministically from authored pack bindings and recorded observations, then evaluate it with the exact M10.9.6.3 scoring policy.

Add a challenge replay fingerprint for validation only. It does not become a plant exact-version identity and does not replace the existing snapshot/replay fingerprints.

## Consequences

Checkpoint restore remains owned by the existing replay runner. Challenge state can be rebuilt at any verified prefix and resumed deterministically without schema duplication. Presentation remains deferred to M10.9.7.

A future incompatible challenge replay/scoring semantic change requires a new challenge/scoring policy version or replay-fingerprint algorithm identity rather than silently reinterpreting historical evidence.

### Hotfix 1 clarification — 2026-08-21

Canonical recordings may extend beyond a challenge terminal transition. The M10.9.6.1 tracker preserves the true terminal logical step and does not continue lifecycle evaluation after terminal state. Replay projection therefore derives an as-of-frame terminal lifecycle view for later frames: only the view's current `LogicalStep` is advanced, while terminal state, `TerminalLogicalStep`, observations and transitions remain unchanged. This keeps the M10.9.6.2 same-step demand invariant and final scoring evidence coherent without changing live lifecycle semantics.
