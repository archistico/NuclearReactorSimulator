# M10.9.4.1-H.5 Hotfix 2 — Production Activation Rollback & Extended Shadow Qualification

**Status:** VALIDATED. **Validated prerequisite:** H.4.

## Why Hotfix 2 exists

H.4 qualified `P060-F040-R015` on a 0.5 s frozen-forcing window and correctly left production explicit. H.5 Hotfix 1 promoted that evidence directly into the free-running current-v2 runtime. Ordinary validation then exposed multiple deterministic corrector non-convergences (including normal desktop 10 s operation, replay/protection paths and host runtime pumping). Therefore H.5 Hotfix 1 is rejected as a production activation candidate.

The defect is not repaired by raising iteration counts, retuning physical coefficients or adding a silent fallback. The evidence boundary was too narrow for direct production ownership.

## Hotfix 2 contract

- current-v2 production: `ExplicitCommittedState`, fixed 10 ms;
- legacy/current-v1: unchanged explicit path;
- H.4 profile retained: pressure trigger 0.060, flow trigger 40 kg/s, relaxation 0.15, max 72 iterations, tolerances 1e-5 and 1e-2 kg/s;
- H.4 profile is evaluated only in shadow/audit mode;
- each shadow correction starts from the actual committed explicit interval and uses the reconstructed frozen non-hydraulic forcing for that exact interval;
- shadow candidate states are never committed and never influence the next production state;
- non-convergence is recorded, not hidden;
- physical coefficients, controllers, protections, Phase G energy ownership and timestep remain unchanged.

## Extended qualification

The H.5 audit covers 500 production intervals (5 s) and reports:

- correction count/fraction;
- converged vs non-converged corrections;
- iteration count and residuals;
- deterministic work ratio;
- predictor pressure/flow trigger values;
- hydraulic conservation and pump-work ownership residuals;
- deterministic repeat;
- shadow candidate differences versus the explicit committed reference.

`extended-shadow-qualification-passes=True` is evidence only. Production remains explicit even if true. A later activation candidate must separately demonstrate free-running stability over ordinary, replay, protection, long-running and off-design gates.

## User validation result

Compilation, ordinary tests and the focused Hotfix 2 gate passed. The validated 5 s shadow evidence is:

- 500 committed explicit intervals;
- 7 corrections triggered (1.4%);
- 5/7 correctors converged and 2/7 did not converge within 72 iterations at relaxation 0.15;
- deterministic work ratio 1.492000;
- observational shadow cost ratio 1.480162;
- chatter ratios pump/channel/return/pressure 0.861431/0.461425/0.393946/0.919349;
- zero reported inventory/conservation/ownership residuals;
- exact deterministic repeat;
- `extended-shadow-qualification-passes=False`;
- production remained explicit and no shadow candidate was committed.

The negative qualification result is the intended decision evidence and advances to H.6 corrector-envelope refinement; it does not invalidate the production rollback itself.
