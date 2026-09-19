# ADR-0215 — Version Exact-V9 cross-host determinism instead of freezing host-sensitive raw doubles

## Status

Accepted by candidate; requires local focused qualification and hosted ordinary-ci GREEN.

## Context

The Exact-V9 128-step raw V1 aggregate was frozen from a local Windows host. GitHub Windows produced a different raw
aggregate while selector/direct remained exact on each host. Diagnostics localized the entire difference to one step
and a floating-point seam of a few ULPs; the trajectories reconverged immediately. A one-shot GitHub probe forced the
same .NET 10.0.5 framework as the local host and reproduced the same hosted trace, excluding runtime patch mismatch.

## Decision

Keep `sha256-control-room-snapshot-v1` unchanged for historical provenance and exact same-host selector/direct
comparison.

Introduce `sha256-control-room-snapshot-v2-presentation-canonical` for frozen cross-host Exact-V9 comparison. V2
canonicalizes only the hidden `numericValue` JSON-number token of presentation measurement records while preserving
`null`, `valueText`, `unit`, `state`, structure and every other payload field.

The frozen V2 Exact-V9 aggregate is
`99B9D27A8F5791A194771D698E8A0740F7024058C172D645E2DF74F1B3C09E73`.

Do not whitelist multiple host-specific raw V1 hashes and do not change reactor physics to manufacture byte-identical
floating-point intermediates.

## Consequences

Cross-host CI freezes stable presentation semantics rather than host-specific low-order floating-point bits. Raw V1
still detects exact selector/direct divergence on each host. Physical health and conservation assertions remain the
authoritative numerical safety envelope.
