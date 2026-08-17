# ADR 0129 — Activate H.4-selected hybrid hydraulics only in versioned current-v2 production

**Status:** REJECTED BY VALIDATION / superseded by H.5 Hotfix 2 shadow qualification

## Context

H.1 showed that halving the explicit timestep did not improve final-state convergence monotonically. H.2 selected deterministic semi-implicit pressure/flow coupling. H.3 demonstrated strong chatter reduction and exact conservation but excessive full-time cost. H.4 then demonstrated that the P060-F040-R015 hybrid configuration meets the numerical-quality gate using only two corrections in fifty audited intervals, with deterministic work ratio 2.14 and no wall-clock-driven branch.

## Decision

1. Keep `PlantNetworkOrchestrator` as the single production owner of fluid/thermal inventory integration.
2. Add a versioned `HydraulicNumericalCouplingDefinition` to `PlantDefinition`; default is the historical explicit committed-state method.
3. Enable the H.4-selected deterministic hybrid method only in the two sustained current-v2 profiles.
4. Keep legacy/current-v1 and the numerical-evidence reference profile explicitly on the historical path.
5. Freeze all non-hydraulic source terms over a triggered corrector iteration.
6. Rebuild every provisional candidate from the original committed logical-step state; never commit an iteration.
7. Expose the numerical path and residuals in immutable per-step diagnostics.
8. Treat failure of a triggered bounded corrector to converge as an explicit integration failure during H.5 validation; do not silently hide it with an automatic fallback.
9. Keep the production logical timestep at 10 ms and prohibit wall-clock adaptation, hidden filtering and physical retuning.

## Consequences

If H.5 validates, the current-v2 gameplay path will differ numerically from legacy profiles while keeping identical physical laws and one canonical integration owner. Replay identity can therefore include the versioned plant definition and deterministic numerical branch. Phase I must document compatibility, replay/checkpoint implications and supported profile combinations after H.5 is validated.


## Validation outcome

Direct free-running activation was rejected after ordinary tests exposed bounded-corrector non-convergence outside the H.4 frozen-forcing qualification window. H.5 Hotfix 2 restores explicit current-v2 production and requires extended shadow evidence before any future activation attempt.
