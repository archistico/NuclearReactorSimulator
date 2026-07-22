# ADR 0067 — Checkpoints are versioned replay anchors, not opaque state dumps

## Status

Accepted — M9.1 validated.

## Context

M0 provides deterministic logical command traces, M7 provides exact versioned initial conditions/scenarios and M8 provides deterministic fault scheduling/lifecycle. M9.1 needs richer recording, checkpoints and seek/replay support.

Serializing the complete private runtime/solver object graph would tightly couple persisted sessions to implementation details, risk duplicate ownership and make migration semantics unclear. Recording only a final state would also be insufficient to detect the first point of replay divergence.

## Decision

M9.1 shall:

- record every deterministic fixed-step presentation snapshot independently from UI publication cadence;
- retain accepted typed operator actions and deterministic event history;
- assign each recorded frame a versioned deterministic fingerprint;
- define checkpoints as versioned identity/action-prefix/fingerprint replay anchors;
- reconstruct checkpoint state by replay from the exact versioned scenario seed rather than restoring opaque internal objects;
- verify every replayed frame and fail closed on the first fingerprint mismatch;
- reconstruct M8 fault lifecycle from scenario definitions instead of persisting a second fault command trace.

Host run/pause state is normalized out of the fingerprint because it controls execution orchestration, not physical deterministic state.

## Consequences

- Checkpoints are portable across process restarts as long as their declared schema/fingerprint/initial-condition/scenario versions remain supported.
- Seek cost is proportional to replay distance from the initial condition in M9.1.
- Replay correctness is stronger than final-state-only comparison because divergence is localized to the first mismatching logical step.
- A future optimized authoritative-state checkpoint format is possible only through a separately versioned restore contract with explicit ownership/invariant validation.
