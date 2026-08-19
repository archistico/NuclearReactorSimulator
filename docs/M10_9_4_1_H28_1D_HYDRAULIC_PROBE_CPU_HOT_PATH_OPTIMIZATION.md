# M10.9.4.1-H.28.1-D — Hydraulic Probe CPU Hot-Path Analysis & Optimization

## Status

**CANDIDATE.** Built directly on user-validated H.28.1-B. H.28 remains a failed performance qualification and H.29 remains blocked.

## Evidence that motivates H.28.1-D

Validated H.28.1-B removed the duplicated predictor solve: non-trigger predictor wall cost fell from about 9.31 ms to about 0.39 ms while the exact deterministic fingerprint remained unchanged. The remaining trigger path is still dominated by unchanged H.9 finite-difference work:

- trigger engine: ~1.699 s;
- H.9 total: ~1.659 s;
- Jacobian build/probes: ~1.561 s;
- hydraulic evaluations: 35;
- finite-difference probes: 32;
- Jacobian dimension: 32.

H.28.1-C already removed ~97.6% of probe allocation pressure, so H.28.1-D targets CPU duplication rather than heap churn.

## Optimization contract

H.28.1-D does not change Newton mathematics, probe count or residual definitions.

### Exact probe fluid-node reuse

A finite-difference coordinate perturbation often leaves most fluid-node hydraulic balances exactly unchanged. H.28.1-D passes the current iterate's already-integrated fluid-node states into each probe. A node is reused by reference only when its probe hydraulic balance is exactly equal to the reference balance. Any changed node is integrated through the existing `FluidNodeIntegrator` path.

The mapped fixed-point integration applies the same rule against the mapped state from the unperturbed residual. No approximate comparison is permitted.

### Immutable coarse saturation-grid reuse

`SimplifiedWaterSteamThermodynamicModel.TryResolveSaturatedMixture()` always scans the same 513 temperatures between the triple point and 640 K. H.28.1-D precomputes the immutable saturation properties for this fixed grid once. The scan temperatures, branch order, quality/residual equations, root test and subsequent bisection are unchanged.

Boundary-aware saturated scans, superheated scans and all bisection temperatures continue to evaluate the original correlations dynamically.

## Frozen numerical contract

H.28.1-D must preserve:

- P060/F040;
- H.9 finite-difference relative step and tolerances;
- 35 hydraulic evaluations per triggered control sample;
- 32 probe evaluations;
- Jacobian dimension 32;
- 2% / 5 K bounded branch continuity;
- targets `steam|stop-out|header|turbine-inlet`;
- H.20 authority and H.22 ownership;
- fixed step 10 ms;
- deterministic fingerprint `518BA948637F0C270C7F8228AB97FEF9148E29A4F5CE4376319AB5D1CFBE7F38`.

## Performance gate

The focused audit uses 64 warmup + 256 attribution steps and a 128-step deterministic control. Relative to validated H.28.1-B it requires:

- average Jacobian wall cost <= 85%;
- average H.9 wall cost <= 87%;
- average triggered engine wall cost <= 90%;
- applied probe fluid-node exact-reuse fraction >= 80%;
- Jacobian and H.9 allocations <= 110% of the H.28.1-B values;
- non-trigger predictor cost <= 150% of H.28.1-B;
- exact 20/20 trigger/commit behavior and unchanged fingerprint.

The report is written before the final performance assertions so a negative optimization result still leaves diagnostic evidence.

## Interpretation

If H.28.1-D is green, rerun the original H.28 performance/cost/soak gate. If the Jacobian remains near the H.28.1-B CPU baseline despite exact duplicate-work removal, that becomes strong evidence that the 32-probe finite-difference method is intrinsically too costly for production-default activation and `OPT-IN ONLY` should remain a serious H.30 outcome.
