# ADR 0150 — Qualify committed long-horizon operation before broader protection and activation work

## Status

Accepted and validated by M10.9.4.1-H.24 Hotfix 1 on 2026-08-19.

## Context

H.19 validated the four-node branch-continuity policy in shadow over a 30,000-interval four-profile domain. H.20 validated fail-closed eligibility/rollback, H.21 integrated the mechanism observationally in the real orchestrator, H.22 validated actual corrected commits, and H.23 Hotfix 2 validated deterministic recording/full replay/checkpoint continuation plus reverse-power protection interaction.

The remaining activation risk is no longer whether a corrected candidate can be computed or replayed. It is whether repeated corrected ownership remains safe and conservative over long nominal operation and cross-profile transients.

## Decision

Before expanding to a broader protection matrix or off-design operation, qualify the unchanged H.22/H.23 committed opt-in runtime over the H.19 four-profile operational domain at the unchanged 10 ms fixed step.

Do not require the original H.19 trigger census to remain identical: corrected ownership legitimately changes the committed trajectory. Measure the new census and qualify fail-closed safety instead.

Safe H.20 rollback/fallback is allowed. Unsafe corrected commits, fallback-commit violations, new untargeted branch disagreements in the nominal domain, conservation/ownership violations or profile trips fail H.24.

Standard current-v2 remains explicit.

## Consequences

- H.24 may be computationally expensive because it exercises real H.9 corrected ownership rather than sparse shadow representatives.
- A positive H.24 result qualifies duration and nominal cross-profile operation only.
- H.25 must broaden protection/transient coverage.
- H.26 must deliberately exercise integrated rollback paths.
- H.27–H.30 retain off-design, cost/soak, activation and closure authority.
