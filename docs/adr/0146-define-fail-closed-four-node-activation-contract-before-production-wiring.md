# ADR 0146 — Define a fail-closed four-node activation contract before production wiring

## Status

Accepted for M10.9.4.1-H.20 candidate design.

## Context

H.5 Hotfix 1 demonstrated that a numerical method can look promising in bounded evidence and still fail when given production authority. H.5 Hotfix 2 therefore restored explicit production and required broader shadow qualification.

H.19 now provides substantially stronger evidence: the exact four-node `steam|stop-out|header|turbine-inlet` policy, unchanged H.9 and unchanged 2% / 5 K bounded previous-phase hysteresis converged on all 473 representatives drawn from the exhaustive 30,000-interval/four-profile P060/F040 census, while preserving committed-state transparency and exposing no new untargeted branch disagreement.

That evidence is necessary but does not define who owns the committed state when a correction fails or when qualification assumptions are violated.

## Decision

Before any new production integration, introduce a shadow-only deterministic activation supervisor with:

- activation arm disabled by default;
- per-interval authority only; no persistent activation latch;
- corrected candidate eligibility only on a trigger with accepted H.19 qualification evidence and all numerical/closure/branch guards green;
- immediate explicit rollback on any failed guard;
- typed rollback reasons with deterministic priority;
- telemetry sufficient to distinguish disabled, untriggered, eligible and every rollback class;
- no API capable of authorizing production commit in H.20;
- no wiring into `PlantNetworkOrchestrator` in H.20.

The contract freezes the validated P060/F040 trigger values, H.9 residual limits, four-node target set, closure/ownership guards and H.19 qualification prerequisite. These are numerical authority controls, not new physical coefficients.

## Consequences

Positive H.20 validation means only that the authority decision is deterministic, observable and fail-closed against frozen H.19 evidence and explicit rollback challenges.

A future production candidate must explicitly wire this contract and requalify production behavior. It may not convert a rollback into a silent corrected-state fallback, broaden the target set, retune thresholds or bypass the H.19 long-horizon regression contract.

Default current-v2 production remains `ExplicitCommittedState` at 10 ms until a later milestone is separately validated.
