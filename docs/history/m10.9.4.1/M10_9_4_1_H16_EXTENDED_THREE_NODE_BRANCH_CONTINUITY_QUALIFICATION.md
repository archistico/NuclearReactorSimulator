# M10.9.4.1-H.16 — Extended Three-Node Branch-Continuity Shadow Qualification

**Status:** VALIDATED — build, ordinary suite and focused audit passed; 15/15 triggers converged.

## Baseline

H.16 is built only on the user-validated M10.9.4.1-H.15 Hotfix 1 baseline.

H.15 confirmed that the sole H.14 extended-window failure at interval 723 is the same thermodynamic inverse-map mechanism already proven at `steam` and `stop-out`, but localized to `header`: overlapping saturated/superheated roots, coarse saturated detection failure and an otherwise valid later boundary-aware saturated root shadowed by earlier coarse-superheated selection.

## Purpose

H.16 tests one narrow hypothesis only: the already validated H.13 bounded previous-phase hysteresis policy remains valid when its target set is extended from:

```text
steam | stop-out
```

to:

```text
steam | stop-out | header
```

No hysteresis limit, solver tolerance, trigger, physical coefficient, timestep or production routing is changed.

## Internal control

The focused audit first reruns the H.14 two-node policy over the same 2,000 committed explicit intervals and requires the validated control evidence:

- 15 P060/F040 trigger events;
- 14/15 H.9 + two-node bounded-hysteresis convergence;
- one line-search exhaustion;
- interval 723 remains non-convergent;
- interval 723 records zero branch overrides under the two-node target set.

Only after that control is reproduced is the three-node policy evaluated.

## Three-node qualification

The same H.9 Jacobian corrector is evaluated on all 15 triggers with the same bounded branch-continuity policy and target IDs `steam`, `stop-out`, `header`.

A positive H.16 qualification requires:

- every trigger converges at the unchanged H.9 pressure/flow tolerances;
- zero line-search exhaustion;
- interval 723 is recovered;
- at least one concrete `header` branch override is recorded at interval 723;
- exact deterministic repeat;
- unchanged mass-closure and energy-ownership tolerances;
- all 6,000 committed target observations remain transparent to production re-resolution;
- the inherited two hold and two release challenges remain deterministic and pass.

The focused audit itself may pass while `three-node-shadow-qualification-passes=False`; that is a valid diagnostic result and does not authorize activation.

## Production isolation

H.16 does not modify:

- `SimplifiedWaterSteamThermodynamicModel.Resolve()`;
- `ThermodynamicBranchContinuityModel`;
- H.9 `JacobianHydraulicCorrectorSolver`;
- the H.13 bounded limits of 2% relative pressure drift and 5 K temperature drift;
- P060/F040;
- `PlantNetworkOrchestrator`;
- the 10 ms `ExplicitCommittedState` production path.

No shadow candidate is committed.

## Next decision

If H.16 qualifies 15/15, production still remains explicit. The next step is a longer-horizon and/or cross-profile shadow qualification to test whether another node exposes the same inverse-map mechanism outside the 2,000-interval window before any activation candidate is designed.

If H.16 does not qualify, inspect its per-event comparison before changing any limits or solver behavior.
