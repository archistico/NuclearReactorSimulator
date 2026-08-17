# M10.9.4.1-H.8 — Accelerated Nonlinear Hydraulic Corrector — VALIDATED


## Validation result

The user validated H.8 with successful compilation, ordinary tests and focused audit. Over the frozen seven P060/F040 events, H.8 reproduced H.4 5/7, H.6 6/7 and H.7 5/7, then safeguarded Anderson also converged 5/7 with two line-search exhaustions. Anderson attempts/acceptances were 30/24, residual fallback attempts/acceptances 13/11, six least-squares systems were rejected, maximum coefficient L1 norm was 7.188310311, maximum pressure/flow fixed-point residuals were 0.303946566 / 28.444475059 kg/s, deterministic hydraulic-evaluation work ratio was 1.212000, accepted merit strictly decreased and deterministic repeat was exact. Inventory, hydraulic closure and energy-ownership residuals remained effectively zero. `accelerated-corrector-qualification-passes=False`.

This negative qualification result is authoritative evidence that safeguarded Anderson does not resolve the two persistent events; H.9 therefore moves to a Jacobian-informed method while production remains explicit.

## Purpose

H.8 is a shadow-only nonlinear-method revision built on the user-validated H.7 Hotfix 1 baseline. H.7 proved that a true fixed-point residual and deterministic monotone backtracking are necessary but not sufficient: on the frozen seven P060/F040 events it converged 5/7 and exhausted the line search on two events.

H.8 therefore changes the search direction rather than weakening convergence or production safeguards.

## Algorithm

`AndersonHydraulicCorrectorSolver` is separate from the historical Picard solver and from `ResidualBacktrackingHydraulicCorrectorSolver`. It is not routed through `PlantNetworkOrchestrator`.

For each accepted iterate it evaluates the same unrelaxed hydraulic fixed-point map used by H.7. Convergence remains:

- maximum relative pressure fixed-point residual <= `1e-5`;
- maximum absolute pipe/valve/pump flow fixed-point residual <= `0.01 kg/s`.

The acceleration history contains a bounded number of recent unrelaxed mapped hydraulic iterates. Anderson coefficients are obtained by regularized affine least-squares minimization of a deterministic residual signature containing normalized node-pressure defects and normalized component-flow defects. The coefficients must sum to one and remain within a bounded L1 norm.

A valid Anderson target is never accepted directly. A deterministic line search starts at relaxation 1.0 and halves to 1/1024; a trial is accepted only if the unchanged H.7 normalized merit strictly decreases. If the least-squares problem is rejected or the accelerated direction cannot reduce merit, H.8 falls back deterministically to the H.7 residual direction with the same line search.

Because the accelerated target is an affine combination of mapped hydraulic evaluations whose coefficients sum to one, linear hydraulic mass closure and pump-energy ownership remain preserved up to floating-point roundoff. Every candidate state is integrated once from the original committed inventory.

## Frozen audit contract

The focused audit must replay exactly the same 500 committed explicit current-v2 intervals used by H.5-H.7 and must first reproduce:

- trigger P060/F040;
- 7 triggered events;
- H.4 primary convergence 5/7;
- H.6 selected rescue convergence 6/7;
- H.7 residual/backtracking convergence 5/7;
- H.7 line-search exhaustion 2/7.

The H.8 result is then measured for convergence, residuals, deterministic repeat, Anderson/fallback usage, coefficient bounds, work ratio, inventory closure, hydraulic conservation/ownership and gap versus the committed explicit trajectory.

`accelerated-corrector-qualification-passes=False` is a valid audit outcome. It means the method still needs algorithmic work; it does not invalidate the audit implementation if build, ordinary tests and focused evidence all pass.

## Production invariants

H.8 does not:

- activate hybrid production;
- replace Picard or H.7;
- change `PlantNetworkOrchestrator` routing;
- retune P060/F040;
- change physical coefficients;
- add hidden flow filtering;
- change the 10 ms production timestep;
- commit any shadow candidate.

A positive H.8 result permits only broader free-running/scenario shadow qualification. A negative result points to a Jacobian-informed Newton/quasi-Newton corrector as the next method family.
