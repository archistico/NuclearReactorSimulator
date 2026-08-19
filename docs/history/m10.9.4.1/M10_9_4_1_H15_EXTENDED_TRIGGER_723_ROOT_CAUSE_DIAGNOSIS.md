# M10.9.4.1-H.15 — Extended Trigger 723 Root-Cause Diagnosis

## Baseline

H.15 is built only on the user-validated H.14 Hotfix 1 baseline.

H.14 established that the H.13 bounded previous-phase hysteresis policy is a real fix for the original interval-200/360 inverse-branch discontinuity, but does not yet qualify over the broader 2,000-interval horizon:

- 15 P060/F040 events are present;
- unchanged H.9 plus the targeted bounded policy converges 14/15;
- interval 723 is the sole line-search exhaustion;
- interval 723 records zero branch overrides and zero hysteresis releases;
- all four explicit hold/release qualification challenges pass;
- production remains `ExplicitCommittedState` at 10 ms.

Therefore H.15 does not retune the H.13 policy and does not add another nonlinear solver. It diagnoses interval 723 as a distinct extended-horizon failure.

## Question

What local hydraulic, thermodynamic or fixed-point structure distinguishes interval 723 from nearby intervals 721, 722 and 724?

The audit must not assume that `steam` or `stop-out` are responsible.

## Frozen reproduction

H.15 reconstructs the first 724 committed 10 ms intervals from the same sustained current-v2 reference runtime used by H.14. It re-evaluates P060/F040 and requires the H.14 prefix evidence:

- nine trigger events through interval 724;
- H.4 primary converges on six of those nine;
- interval 723 is a trigger and H.4 primary does not converge;
- unchanged H.9 plus unchanged H.13 bounded hysteresis reproduces interval 723 as non-convergent with line-search exhaustion;
- the bounded policy performs zero branch overrides and zero hysteresis releases at the target event.

## Neighborhood control

H.15 evaluates unchanged H.9 plus bounded hysteresis at intervals 721, 722, 723 and 724 regardless of trigger state. This separates the target failure from immediately adjacent committed states.

For every neighborhood interval the audit records:

- convergence and line-search exhaustion;
- fixed-point pressure/flow residual and normalized merit;
- Jacobian build/accept/reject counts;
- residual fallback behavior;
- branch overrides, previous-phase holds and hysteresis releases.

## All-node fixed-point residual ranking

For every fluid node, H.15 compares the hydraulic map returned at the final H.9 candidate with the hydraulic balance actually applied by that candidate:

`mapped hydraulic balance - applied hydraulic balance`

Mass and energy residuals are recorded independently and ranked by absolute magnitude. This is diagnostic evidence only; no arbitrary combined metric is introduced.

## H.10 generalized local probes

The validated H.10 `HydraulicMapSmoothnessAnalyzer` is applied to:

- each H.9+hysteresis candidate in the 721–724 neighborhood;
- the corresponding committed explicit endpoint.

It probes every pipe, valve and pump path and every thermodynamic fluid node, recording branch switches, derivative scale growth, one-sided slope asymmetry, phase/envelope switches and non-smooth evidence.

## H.12 generalized inverse-map inspection

For every fluid node in each neighborhood H.9 candidate, H.15 calls the existing diagnostic-only inverse-map provider and records:

- selected branch and selected phase;
- saturated/superheated root availability;
- overlapping roots;
- coarse and boundary-aware root availability;
- late boundary-aware saturated roots shadowed by earlier coarse-superheated selection;
- all individual branch candidates in deterministic attempt order.

This inspection does not change `Resolve()`.

## Interpretation

H.15 is a diagnosis, not a qualification for activation.

If interval 723 exposes localized switching/non-smoothness or inverse-map branch evidence, the next milestone should localize only the reported node/path mechanism before changing any solver or hysteresis policy.

If the H.10-H.12 probes are locally clean, the next step should test fixed-point existence, residual floor and basin-of-attraction structure for interval 723 rather than escalating nonlinear solver complexity.

## Production isolation

H.15 does not:

- modify `SimplifiedWaterSteamThermodynamicModel.Resolve()`;
- modify `ThermodynamicBranchContinuityModel` or its 2% / 5 K policy;
- modify H.3-H.9 hydraulic correctors;
- modify `PlantNetworkOrchestrator` routing;
- retune P060/F040;
- alter physical coefficients or H.9 tolerances;
- change the 10 ms production timestep;
- commit a shadow state;
- introduce active-set, semi-smooth, clamping or hidden filtering behavior.
