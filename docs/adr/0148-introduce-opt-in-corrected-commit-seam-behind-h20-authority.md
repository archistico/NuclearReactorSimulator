# ADR 0148 — Introduce corrected-state ownership only behind unchanged H.20 authority

## Status

Accepted. H.22 implementation was subsequently validated on 2026-08-18.

## Context

H.19 qualified the exact four-node H.9 + bounded branch-continuity policy over the complete long-horizon/cross-profile representative contract. H.20 qualified the fail-closed authority/rollback rules. H.21 Hotfix 1 then validated that the exact policy and H.20 supervisor can execute from the real `PlantNetworkOrchestrator` while the applied trajectory remains historically explicit.

The remaining integration question is corrected-state ownership. Reusing H.20's historical `ProductionCommitAuthorized=false` as if it had become a commit API would blur milestone authority and make rollback semantics harder to audit. Directly replacing the explicit path before authority evaluation would also remove the immediate known-good fallback.

## Decision

H.22 introduces a **separate corrected-candidate commit seam** and a separately opt-in numerical mode.

The orchestrator must:

1. evaluate the complete historical explicit candidate first;
2. run the unchanged H.21 sidecar only according to frozen P060/F040 semantics;
3. let the unchanged H.20 supervisor determine corrected-candidate eligibility/rollback;
4. allow the new H.22 seam to authorize ownership only for an H.20-qualified, triggered, evaluated and available candidate;
5. return the explicit candidate for every other reason in the same interval;
6. rebuild audit/accounting from the balances and pump work actually applied to whichever candidate is returned.

H.20's original decision type and `ProductionCommitAuthorized=false` semantics are retained unchanged. H.22 adds new typed commit reasons rather than silently reinterpreting H.20.

All normal current-v2 factories remain `ExplicitCommittedState`.

## Consequences

A positive H.22 result demonstrates that corrected state can own an opt-in step without bypassing H.20 and without losing immediate explicit fallback.

Because corrected ownership may change subsequent trajectory, H.22 qualification must use authority/conservation/determinism invariants rather than requiring future trigger counts to match the explicit H.21 trajectory.

A green H.22 gate still does not justify default activation. Recorder/replay, protection interaction, long-running/cross-profile committed operation and off-design/fallback robustness remain mandatory before any standard-production activation decision.
