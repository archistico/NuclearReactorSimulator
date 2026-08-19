# ADR 0156 — Reuse the historical explicit predictor before changing the trigger contract

## Status

Accepted and validated by M10.9.4.1-H.28.1-B.

## Context

Validated H.28.1-A attributed about half of non-trigger four-node orchestrator time to the sidecar predictor. Validated H.28.1-C removed almost all H.9/Jacobian allocation churn but did not reduce the dominant triggered CPU time. The historical explicit path already integrates the fallback fluid-node candidate, while H.4 independently builds another predictor candidate.

Directly reusing the whole historical candidate is not automatically safe: the historical explicit path and H.4 can accumulate physically identical hydraulic and non-hydraulic contributions in different floating-point associativity/order. Even tiny numerical changes could move P060/F040 at a boundary.

## Decision

Reuse the already-computed committed hydraulic evaluation and historical explicit fluid-node candidate only through an exact-balance selective seam.

For every fluid node, reconstruct the canonical H.4 balance exactly as before. Reuse the historical node only when its historical applied total balance exactly equals that canonical balance. Reintegrate every mismatched node through the unchanged H.4 path.

Retain the predictor-end hydraulic evaluation unchanged because F040 requires the committed-to-predictor flow delta.

Do not change P060/F040 thresholds, H.9 mathematics, H.20 authority, H.22 ownership or production defaults.

## Consequences

- historical explicit fluid-node integration is performed once per step and can be reused where bit-exactly safe;
- exact-balance mismatches fail conservatively to the historical H.4 integration rather than accepting floating-point drift;
- focused telemetry must expose node reuse counts;
- non-trigger predictor overhead should decrease materially if real reuse occurs;
- triggered H.9 cost is expected to remain dominated by finite-difference probes;
- exact predictor-equivalence and deterministic fingerprint tests are mandatory;
- H.28 remains failed until its original gate is rerun successfully;
- H.29 remains blocked.
