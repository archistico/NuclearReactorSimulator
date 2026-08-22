# M10 LR-H1 Equilibrium / Long-Drift Diagnostic Plan

## Status

**IMMEDIATE DIAGNOSTIC PLAN — applicable before M10 closure if the final long gate is red.**

This document is intentionally separate from the post-M11 M12.0 feature plan. Its purpose is to resolve the current long healthy-leg failure with the smallest evidence-first investigation possible.

The first observed LR-H1 failure occurred in the authoritative exact-v4 healthy composition when fluid node `outlet` left the supported simplified water/steam envelope after a long period of apparently healthy operation. The failure must remain classified as a real long-gate failure until evidence proves a harness defect. The fact that the final state crossed the envelope only slightly does not authorize widening the envelope or clamping the conserved state.

## 1. Preserve the current run

Allow the fail-collect runner to finish all remaining legs unless the local machine must be stopped for an external reason. Preserve the complete generated artifact directory before changing any code.

Required first-pass evidence:

- per-leg summary;
- progress heartbeat / last completed logical checkpoint;
- conservation maxima;
- I.3 window-budget comparisons;
- numerical-coupling telemetry;
- trip/fault/protection classification;
- performance diagnostics;
- failure artifact and stack trace.

## 2. Do not fix the exception site first

The exception is thrown in `SimplifiedWaterSteamThermodynamicModel.Resolve`, but the first investigation question is not “how do we make Resolve accept this point?”. The investigation must determine which owner drove `(v,u)` toward the unsupported region.

Candidate classes:

```text
A. PHYSICAL/SEED INVENTORY DRIFT
B. CLOSED-LOOP CONTROL BIAS / INTEGRATOR DRIFT
C. THERMODYNAMIC SUPPORT/INVERSE-DOMAIN LIMIT
D. HYDRAULIC/COUPLING DISCONTINUITY OR LATE BRANCH EVENT
E. REAL CAPACITY/BALANCE MISMATCH IN THE REDUCED PLANT MODEL
F. TEST/HARNESS DEFECT
```

Class F is allowed only with evidence that production semantics are unaffected.

## 3. Reproduce deterministically and bracket the failure

After the original run completes:

1. rerun only LR-H1 with unchanged exact-v4 composition;
2. confirm same failure class and deterministic logical region;
3. use the existing progress cadence to narrow the failure interval;
4. if useful, add a temporary explicit diagnostic route with configurable **stop-after logical step** while keeping the production runtime unchanged;
5. do not change physical coefficients, tolerance budgets or thermodynamic bounds during bracketing.

The goal is to identify a compact pre-failure window, not to make the test faster by altering the workload semantics.

## 4. Test-only equilibrium residual census first

Preferred first implementation is additive test/harness evidence only.

For exact-v4, sample canonical committed state and snapshot at deterministic intervals and write:

- all fluid-node mass and total internal energy;
- per-node `dm/dt`, `dU/dt` over 1 s, 60 s and 300 s windows;
- pressure, temperature, phase, vapor quality/void where available;
- key path flows and direction;
- drum/hotwell/feedwater inventory trends;
- reactor power/reactivity trend;
- turbine speed/net torque;
- generator requested/actual output;
- controller error, integral term, output and saturation state;
- global mass/energy closure and balance-rate residuals;
- corrected-commit trigger/commit/rollback/fallback/unsafe/disagreement telemetry;
- `outlet` specific volume/internal energy trajectory.

The first census should not add a production-state correction or new exact version.

## 5. Deterministic slope classification

For each sampled metric, compute both instantaneous and window behavior:

```text
instantaneous derivative
60 s signed slope
300 s signed slope
min / max / mean / RMS / peak-to-peak
```

Then rank stationary-required residual contributors.

Interpretation examples:

### 5.1 Global closure green + node drift persistent

Likely operating-point/inventory redistribution rather than accounting loss.

### 5.2 Controller integral ramps while plant inventory follows

Likely closed-loop target/bias mismatch. Investigate controller measurement/setpoint/bias ownership before changing plant physics.

### 5.3 Node trend nearly flat then sudden branch jump

Investigate thermodynamic support topology / hydraulic branch transition / coupling event.

### 5.4 Coupling telemetry becomes unsafe/fallback/disagreement-active before drift accelerates

Investigate numerical/coupling owner first.

### 5.5 Stable `(v,u)` trend reaches a finite support boundary monotonically

Determine whether the authored operating point is slowly walking toward a genuine reduced-model limit or whether the property model support is artificially truncating an otherwise coherent trajectory. Do not decide this from the final exception alone.

## 6. Closed-loop vs physical-seed separation

The first diagnostic is `CLOSED_LOOP_REFERENCE`, because LR-H1 failed on the normal integrated runtime.

Only if the census cannot distinguish seed balance from controller compensation should a validation-only `FIXED_INPUT_PLANT_HOLD` seam be added. That seam must use canonical physical owners and must not become a second solver.

## 7. Versioning decision gate

### 7.1 If no production semantics change

A pure diagnostic/test correction may retain exact-v4 and the already validated cumulative prerequisite according to the change-impact policy.

### 7.2 If the authored production operating point changes

Do **not** edit `integrated-operations-desktop-stable@4` in place.

Create a new exact version, provisionally:

```text
integrated-operations-desktop-stable@5
```

Preserve exact-v4 replay/archive behavior unchanged.

If a production mission/scenario currently binds exact-v4 and must adopt the repaired reference, create a new version of that mission/scenario contract rather than rebinding its old exact identity.

### 7.3 If thermodynamic/coupling production behavior changes

Treat it as a production semantic change even if the seed stays identical. Exact-version compatibility and replay/fingerprint consequences must be audited explicitly.

## 8. Revalidation ladder after a production fix

A production fix discovered through this diagnostic requires, at minimum:

```text
focused owner gate
→ ordinary suite
→ affected reference/operating-point equilibrium census
→ exact-version replay/checkpoint evidence
→ M10 final cumulative rerun
→ complete M10 final long rerun
```

No partial continuation of the failed long run can promote M10.

## 9. Acceptance rule

Do not freeze new equilibrium tolerances from the failed trajectory. First establish the residual taxonomy and characterize the repaired candidate. Any new pass/fail budgets must be justified and frozen before the final promotion run.

Existing conservation ceilings and the 19 I.3 budgets remain unchanged unless a separately scoped, independently justified model contract explicitly replaces them; this diagnostic does not authorize such replacement.

## 10. Relationship to M12.0

The diagnostic prototype is intentionally minimal. The formal reusable implementation is planned under `M12.0 — Reference Operating-Point Equilibrium & Stability Qualification` after M11 release hardening, unless the current M10 blocker requires a small subset earlier.
