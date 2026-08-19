# ADR 0162 — Establish the Phase-I 300-second reference baseline before freezing regression budgets

## Status

Partially superseded. The budget methodology remains accepted, but the original exact-v2 reference identity was invalidated by I.3 continuity diagnostics and H.30 Requalification 1. The authoritative reference is now exact v3.

## Context

M9.6 already provides versioned reference cases and explicit tolerance budgets, while M10.9.4.1 Phase A proved that a 300-second integrated journey can expose defects hidden by shorter tests. Phase I requires reference trajectories and inventory-slope evidence before M10.9.5, but the project must not invent external calibration data or tune the runtime to fit an arbitrary regression file.

## Decision

1. Use the authoritative production selector. After validated H.30 Requalification 1 this resolves to exact v3 `integrated-operations-desktop-stable@3` with `FourNodeBranchContinuityCorrectedCommitOptIn`; exact v2 remains rollback/reference only.
2. Keep the production 10 ms fixed step and sample immutable/canonical evidence every 1 simulated second.
3. Consolidate existing conservation residuals and selected conserved fluid inventories without adding a new physics owner.
4. Compute final-window inventory slopes observationally.
5. Derive v1 regression budgets from the validated final 60-second behavior using explicit minimum floors and a two-times observed-deviation/slope envelope.
6. Require independent no-trip, generation-health and conservation limits before any derived budget may be frozen.
7. Treat generated trajectories/budgets as internal regression evidence only.
8. Never retune seed values, physics coefficients, protection thresholds or numerical tolerances to force a later run inside the stored budgets; a regression must be investigated or the budget explicitly superseded by a new versioned engineering decision.

## Consequences

- I.3 is a scheduled/manual long gate, not ordinary push/PR work.
- A green post-H.30-RQ1 I.3 produces the first authoritative-production Phase-I v1 trajectory and slope/tolerance artifacts suitable for immutable freezing in subsequent milestones.
- H.24/H.28 remain frozen and are not rerun by I.3.
- Legacy numerical-mode retirement remains a separate decision.
