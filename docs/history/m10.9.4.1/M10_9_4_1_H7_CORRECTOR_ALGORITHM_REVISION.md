# M10.9.4.1-H.7 — Corrector Algorithm Revision

**Status:** VALIDATED — H.7 Hotfix 1


## Validation outcome

User validation passed build, ordinary tests and the focused H.7 audit. The solver converged 5/7 frozen trigger events and exhausted its deterministic line search on two events. Maximum fixed-point residuals were pressure `0.303946536` and flow `28.424177648 kg/s`; deterministic hydraulic-evaluation work ratio was `1.308000`; accepted merit strictly decreased; exact repeat and conservation/ownership passed. `corrector-algorithm-revision-qualification-passes=False`. Hotfix 1 changed only the xUnit2031 assertion contract. Production remained explicit.

## Purpose

H.6 is user-validated and gives a negative rescue-envelope result: over the frozen 500-step explicit current-v2 trajectory, `P060-F040` still selects exactly 7 intervals; H.4 `R015-I072` converges on 5/7; the best bounded Picard rescue `R0125-I096` reaches only 6/7 and `refined-envelope-qualification-passes=False`.

H.7 therefore changes the **nonlinear corrector algorithm**, not its production activation state. Production remains the validated 10 ms `ExplicitCommittedState` path.

The central correction is conceptual: convergence is no longer inferred from the movement between consecutive already-relaxed iterates. H.7 evaluates the residual of the unrelaxed fixed-point map itself.

## Frozen evidence contract

H.7 reuses the exact H.5/H.6 committed evidence:

- 500 explicit current-v2 intervals at 10 ms;
- unchanged pressure trigger `0.060`;
- unchanged flow trigger `40 kg/s`;
- exactly 7 triggered intervals;
- H.4 primary `R015-I072` converges on 5/7;
- H.6 selected rescue `R0125-I096` converges on 6/7;
- no shadow candidate becomes the committed start state of a later production interval.

A change in these frozen counts is a regression in the H.7 audit setup, not an H.7 success.

## Separate algorithm

H.7 adds `ResidualBacktrackingHydraulicCorrectorSolver` alongside the historical `SemiImplicitHydraulicPrototypeSolver`.

The new solver is deliberately **not** called by:

- `PlantNetworkOrchestrator`;
- `HybridSemiImplicitHydraulicGateSolver`;
- current-v2 production routing.

The Picard solver remains intact for historical evidence and comparison.

## Fixed-point residual

Let `b` be the currently accepted hydraulic balance iterate. H.7 first integrates a provisional end state from the original committed inventories using `b` plus the frozen non-hydraulic balances.

It then evaluates the existing hydraulic laws on that state and applies those balances **without relaxation** to the same committed start inventory. This defines the nonlinear fixed-point map.

H.7 measures two residuals:

1. **pressure fixed-point residual** — maximum relative pressure difference between the accepted iterate state and the unrelaxed mapped state;
2. **flow fixed-point residual** — maximum absolute pipe/valve/pump difference between the currently applied hydraulic-flow iterate and the unrelaxed hydraulic evaluation returned by the map.

The normalized merit residual is:

```text
max(
    pressure residual / pressure tolerance,
    flow residual / flow tolerance
)
```

The audit keeps the previous numerical tolerances:

- relative pressure tolerance `1e-5`;
- absolute flow tolerance `1e-2 kg/s`.

A candidate converges only when **both** raw residuals meet their tolerances.

## Deterministic backtracking line search

For a non-converged iterate, the solver forms a search direction from the currently accepted hydraulic balances toward the unrelaxed hydraulic evaluation.

The H.7 audit profile uses:

| Control | Value |
|---|---:|
| maximum accepted iterations | 96 |
| initial relaxation | 1.0 |
| backtracking factor | 0.5 |
| minimum relaxation | 1/1024 |
| pressure tolerance | 1e-5 relative |
| flow tolerance | 1e-2 kg/s |

Each iteration begins with relaxation 1.0. If the trial does not reduce the normalized merit residual, the relaxation is multiplied by 0.5 and retried. A trial is accepted only when merit strictly decreases.

If the unrelaxed fixed-point map leaves the supported fluid-state envelope, that residual is treated as non-finite evidence and the line search may still backtrack toward a valid residual-reducing trial. Invalid/rejected trials never become authoritative candidates.

## Conservation contract

Backtracking is numerical only. For every accepted iterate:

- node mass and internal energy are reintegrated from the original committed start state exactly once;
- accepted hydraulic balances plus frozen non-hydraulic balances must reproduce the returned candidate inventory;
- mass-rate closure of the **accepted/applied blended hydraulic iterate** remains within `1e-8 kg/s`;
- energy ownership of that accepted/applied iterate, including consistently blended pump hydraulic power, remains within `1e-3 W`;
- no Phase G work/enthalpy ownership changes are introduced.

## H.7 qualification

The H.7 audit records qualification as positive only if all seven frozen trigger events:

- converge against the true fixed-point residual;
- do not exhaust the line search;
- satisfy both residual tolerances;
- show strictly decreasing merit for every accepted line-search step;
- reproduce exactly on deterministic repeat;
- preserve inventory/conservation/ownership residual limits;
- keep deterministic hydraulic-evaluation work ratio <= 8.0 over the 500-step evidence window.

The explicit end-state mass/energy/pressure gaps remain reported, but they are **diagnostic rather than an H.7 activation criterion**. H.7 is testing whether the nonlinear equation can be solved reliably. Broader trajectory compatibility belongs to the next shadow-qualification phase.

## Interpretation

`corrector-algorithm-revision-qualification-passes=True` means the revised algorithm resolves all seven known H.5/H.6 trigger events under a deterministic bounded line search. Production still stays explicit. The next permitted step is broader free-running/scenario shadow qualification.

`corrector-algorithm-revision-qualification-passes=False` is also a valid H.7 audit outcome. It means residual/backtracking is still insufficient and the next step must remain algorithmic — for example a more capable nonlinear/root solver — before broader shadow qualification.

## Non-goals

H.7 does not:

- activate hybrid production;
- replace the historical Picard implementation;
- change `PlantNetworkOrchestrator` routing;
- change the external 10 ms timestep;
- change P060/F040;
- change pipe, valve, pump or thermodynamic physical coefficients;
- add hidden flow filtering;
- branch on wall-clock cost;
- change controller, turbine, generator or protection settings;
- close Phase H.
