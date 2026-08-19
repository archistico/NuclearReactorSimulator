# ADR-0155 — Remove H.9 probe object-graph churn before changing Newton mathematics

## Status

Accepted; implemented and validated in M10.9.4.1-H.28.1-C Hotfix 2.

## Context

Validated H.28.1-A showed that finite-difference Jacobian probes dominate triggered cost, but the work pattern is regular: 32 probes, 35 hydraulic evaluations, one accepted Jacobian build and no retry pathology. Each probe currently materializes full immutable plant object graphs and rebuilds topology/canonical collection infrastructure that does not alter the hydraulic fixed-point mathematics.

## Decision

Optimize the implementation boundary before considering any algorithmic change. Evaluate H.9 transient probes over canonical fluid-node lists, cache immutable topology index bindings, remove intermediate balance dictionaries and duplicate canonical copies, and materialize a full `PlantState` only for the final candidate returned by H.9. Remove per-scan heap churn in the water/steam inverse closure by using a private value-type saturation carrier internally while preserving the existing public `WaterSteamSaturationProperties` API and the exact equations/search order.

Do not change finite-difference probe count, Newton equations, tolerances, branch-continuity policy, H.20 authority or H.22 ownership.

## Consequences

The optimization must reproduce the validated deterministic trajectory and H.9 work counts. H.28 remains the final performance authority; H.28.1-C cannot relax its ceilings or authorize production-default activation. Since runtime implementation changes, one H.24 long-horizon rerun is required after the optimization branch stabilizes and before H.29, rather than being chained to every intermediate hot-path iteration.
