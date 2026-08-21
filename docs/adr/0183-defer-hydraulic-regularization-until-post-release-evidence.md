# ADR-0183 — Defer hydraulic constitutive regularization until a post-release evidence gate

## Status

Accepted — planning/architecture decision, 2026-08-21.

## Context

A post-M10.9.7 static review confirmed that the reduced quadratic hydraulic inversion has unbounded derivative as driving pressure approaches zero and that several one-way/near-closed component maps are non-smooth. The same review also identified hot-path allocation opportunities and stale “shadow-only” wording around the branch-continuity wrapper.

Directly smoothing the pipe law, introducing check-valve leakage, changing Jacobian conditioning rules or changing deterministic summation would alter numerical/physical trajectories that were already qualified through Phase H/I and exact-version replay contracts.

## Decision

1. M10.9.7/M10.9.8 remain on the validated numerical baseline; no hydraulic constitutive regularization is introduced as presentation/replay cleanup.
2. M11.3 may optimize only measured hot-path costs where exact physical outputs can remain equivalent: effective-resistance plumbing, trigger-path allocations, expected infeasible-probe control flow, root-search ceilings and generic runtime API cost/policy.
3. M12.1 inventories component-owned directionality.
4. M12.2 performs the dedicated near-zero hydraulic constitutive/conditioning audit before any physical/numerical law change. It owns the quadratic near-zero law, ideal check-valve non-smoothness, valve near-close conditioning, normalized Jacobian regularization/pivot diagnostics and summation-semantics requalification.
5. M12.4 closes pump shaft/electrical/loss-to-heat ownership before severe full-plant energy/consequence claims.
6. The base water/steam resolver remains memoryless. Bounded previous-phase continuity remains conditional to the validated corrected path and may contribute to committed state when corrected-commit authority is granted. Retirement/unification requires separate evidence.

## Consequences

- No speculative epsilon/deadband/leakage constant is introduced now.
- Exact-version and Phase-I provenance remain stable.
- Numerical concerns are not forgotten: they have explicit owners, gates and requalification requirements.
- Historical source comments using “shadow-only” are treated as provenance wording, not the current architectural truth; current documentation states the conditional committed role.
- A future M12 numerical change must identify and rerun the affected long-horizon, conservation, replay/checkpoint and performance evidence before promotion.
