# M10.9.4.1-H.18 — Turbine-Inlet Continuity Extension & Residual-Floor Split Diagnosis

## Validation status

**VALIDATED — Hotfix 1, 2026-08-17.** Local compilation, complete ordinary `dotnet test` suite and the focused H.18 audit passed. The focused result is authoritative: 261/261 converged, 0 remaining failures, 120/120 turbine-inlet-mismatch failures recovered, 125/125 non-mismatch failures recovered, 16/16 success controls preserved, 14,746 turbine-inlet overrides, committed selection transparent, deterministic repeat true, no new untargeted late-shadow node and no new untargeted phase-mismatch node. `four-node-extension-qualifies=True`, `residual-floor-split-diagnostic-passes=True`, `h18-audit-passes=True`. Production remained explicit and unchanged.

## Baseline

H.18 is built only on the user-validated **M10.9.4.1-H.17 Hotfix 6** baseline. Production remains `ExplicitCommittedState` at 10 ms.

H.17 Hotfix 6 validated the long-horizon/cross-profile diagnostic infrastructure but did **not** qualify the three-node shadow policy across the extended representative set:

- 30,000 explicit intervals over four profiles;
- 3,046 P060/F040 trigger intervals grouped into 92 deterministic episodes;
- 473 qualified representatives;
- 228/473 converged and 245/473 exhausted line search;
- H.16 2,000-interval control remained 15/15;
- committed target selection remained transparent;
- hold/release challenges remained green;
- a new untargeted `turbine-inlet` branch-disagreement class was discovered.

Analysis of the validated H.17 artifacts shows two distinct H.17 failure classes:

1. **120 failures with `turbine-inlet` candidate-vs-explicit phase mismatch**, usually candidate `SuperheatedVapor` while explicit remains `SaturatedMixture`;
2. **125 failures without `turbine-inlet` phase mismatch**, with materially larger flow residuals and therefore not explained by the fourth-node branch disagreement alone.

## Purpose

H.18 deliberately splits those two classes rather than assuming that adding a fourth target solves the entire long-horizon problem.

The experiment extends the unchanged H.13 bounded previous-phase hysteresis target set from:

```text
steam | stop-out | header
```

to:

```text
steam | stop-out | header | turbine-inlet
```

The hysteresis limits remain exactly:

- 2% relative pressure drift;
- 5 K temperature drift.

H.9, P060/F040, the production inverse-map order and all physical coefficients remain unchanged.

## Frozen H.17 evidence contract

H.18 checks in a compact CSV derived only from the validated H.17 Hotfix 6 focused artifacts:

```text
tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/Evidence/
H17_Hotfix6_FrozenQualifiedRepresentativeEvidence.csv
```

It contains all 473 validated H.17 representatives and freezes, per representative:

- profile and interval;
- H.17 convergence/exhaustion result;
- H.17 pressure/flow/merit residuals;
- `turbine-inlet` candidate/explicit selected branch and phase;
- candidate-vs-explicit phase mismatch;
- candidate-only late saturated-root shadow marker.

An ordinary regression test requires the frozen evidence to retain exactly:

- 473 representatives;
- 245 H.17 failures;
- 228 H.17 successes;
- 120 failed representatives with `turbine-inlet` phase mismatch;
- 125 failed representatives without that mismatch;
- one H.17 success with `turbine-inlet` mismatch;
- one failed representative with explicit candidate-only late-shadow evidence at `turbine-inlet`.

The frozen file is evidence input only. It does not replace runtime state reconstruction.

## Runtime scope

H.18 reconstructs the same four H.17 reference profiles:

- `steady-long` — 12,000 intervals;
- `load-pulse` — 6,000 intervals using validated 5→0→5 MWe requests;
- `cooling-pulse` — 6,000 intervals at 100%→75%→100% condenser cooling capacity;
- `combined-load-cooling` — 6,000 intervals combining those validated load/cooling perturbations.

The runtime still walks all 30,000 explicit intervals so selected states are reproduced exactly. Unlike H.17, it does **not** repeat the expensive P060/F040/H.4 census because the validated H.17 representative set is frozen. Hydraulic evaluation and H.9 are performed only at selected H.18 intervals.

The selected H.18 nonlinear set is:

- all 245 H.17 failures;
- four temporally distributed H.17 success controls per profile (16 total).

Total H.18 H.9 evaluations: **261**.

## Experiment A — fourth-node continuity

Every selected representative is evaluated using unchanged H.9 plus the four-node bounded hysteresis target set.

The report separates:

- recovery among the 120 `turbine-inlet` mismatch failures;
- recovery among the 125 non-mismatch failures;
- preservation of the 16 H.17 success controls;
- concrete `turbine-inlet` branch overrides;
- line-search exhaustion and final residuals.

`four-node-extension-qualifies=True` requires:

- all 120 H.17 `turbine-inlet` mismatch failures recover;
- all 16 success controls remain convergent;
- committed `turbine-inlet` branch selection remains transparent.

A false qualification flag does not fail the diagnostic audit by itself.

## Experiment B — residual-floor split diagnosis

Every H.18 failure that remains after the fourth-node experiment is diagnosed without changing the solver.

For each remaining failure H.18 records:

- mapped-minus-applied mass/energy residual for every fluid node;
- absolute mass and energy residual rank;
- final pressure/flow/normalized-merit residual;
- first, penultimate and final accepted-iterate merit;
- minimum accepted relaxation;
- candidate-vs-explicit inverse-map selected branch/phase for every fluid node;
- candidate-only late saturated-root shadow markers;
- any new untargeted phase-mismatch node.

This evidence is intended to answer one of two questions:

1. does another thermodynamic branch-selection target remain undiscovered? or
2. after the branch-disagreement class is removed, do the remaining failures form a genuine fixed-point residual-floor / solution-existence problem?

H.18 does **not** introduce a new fixed-point solver or alter H.9.

## Committed `turbine-inlet` transparency

Because H.18 adds `turbine-inlet` to the shadow target set, its committed phase is checked across the four reference trajectories. Selection is re-resolved at 10-interval stride and at every real committed `turbine-inlet` phase transition.

The report records any committed override. A transparent result is required for positive four-node qualification.

## Determinism

The expensive four-node solve is repeated only on deterministic sentinels drawn from all three evidence classes and all profiles. The canonical fingerprint is ordered by profile and interval.

## Production isolation

H.18 does not modify:

- `SimplifiedWaterSteamThermodynamicModel.Resolve()`;
- `ThermodynamicBranchContinuityModel` behavior or 2%/5 K limits;
- H.9 `JacobianHydraulicCorrectorSolver`;
- P060/F040;
- physical coefficients;
- `PlantNetworkOrchestrator`;
- production 10 ms `ExplicitCommittedState` integration.

No shadow state is committed.

## Decision after H.18

- If the fourth target recovers the mismatch class and the remaining failures show no new untargeted branch disagreement, the next milestone should focus on fixed-point residual-floor / solution-existence analysis of that remaining class.
- If additional untargeted branch-disagreement nodes appear, localize them before changing H.9 or hysteresis limits.
- If all 245 failures recover, return to a bounded four-node long-horizon qualification before any activation candidate.
