# ADR 0147 — Validate orchestrator sidecar wiring before permitting corrected-state commitment

## Status

Accepted and validated by M10.9.4.1-H.21 Hotfix 1 on 2026-08-18.

## Context

H.19 qualified the exact four-node branch-continuity policy over the complete 30,000-interval/four-profile P060/F040 census and 473 representative set. H.20 then qualified a deterministic fail-closed authority/rollback/telemetry contract against that evidence, while deliberately exposing no production commit API.

The next risk is integration risk rather than solver risk: a correct shadow algorithm can still change behavior if the production orchestrator reconstructs forcing differently, evaluates trigger semantics differently, mutates state while observing, or hides authority/rollback decisions.

The failed H.5 Hotfix 1 activation is prior evidence that numerical qualification and production integration must not be collapsed into one step.

## Decision

Introduce an explicitly selectable H.21 `FourNodeBranchContinuityShadowIntegrated` numerical mode that:

- reuses the exact existing explicit predictor and P060/F040 trigger seam;
- runs unchanged H.9 with unchanged four-node 2% / 5 K branch continuity only on triggered intervals;
- evaluates the exact H.20 supervisor with the H.19 qualification prerequisite;
- records typed per-step sidecar telemetry in the canonical network numerical snapshot;
- preserves the historical explicit pipe/valve/pump accumulation and fluid/thermal integration path as the returned production state; the sidecar predictor and corrected candidate remain observational;
- exposes no corrected-state commit authority;
- leaves all standard current-v2 factories on `ExplicitCommittedState`.

H.21 must demonstrate state/presentation equivalence against explicit production and deterministic sidecar telemetry before any corrected-state commit seam is created.

## Consequences

A positive H.21 result proves that the qualified policy and authority contract can execute inside the real orchestrator without changing current production behavior.

It does **not** prove corrected-state production safety because no corrected state is committed.

A later milestone may introduce a separately opt-in commit seam only by consuming the unchanged H.20 authority decision and preserving immediate explicit fallback. Replay, protection, long-running, off-design and full H.19 regression gates remain mandatory before default activation can be considered.

## Validation outcome

H.21 Hotfix 1 passed local build, complete ordinary tests and its cumulative focused gate: 2,000/2,000 explicit-vs-sidecar presentation equivalence, 15/15 H.20 eligibility, zero rollback, zero corrected commits, zero untargeted disagreement and deterministic telemetry. ADR 0148 therefore governs the next separately opt-in corrected-commit seam.
