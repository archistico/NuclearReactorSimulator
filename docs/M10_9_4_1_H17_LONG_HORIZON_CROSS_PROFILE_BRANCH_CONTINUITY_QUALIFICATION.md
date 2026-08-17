# M10.9.4.1-H.17 Hotfix 4 — Trigger-Episode Stratified Long-Horizon & Cross-Profile Shadow Qualification

## Baseline

H.17 Hotfix 4 is built only on the user-validated M10.9.4.1-H.16 baseline. Hotfix 1 fixed only audit string-interpolation compilation. Hotfix 2 keeps the numerical qualification contract unchanged but replaces the unqualified 5→10→5 MWe reference excursion, which tripped the explicit plant at load-pulse interval 634, with the already validated normal 5→0→5 MWe breaker-closed trajectory.

H.16 qualified the unchanged H.13 bounded previous-phase hysteresis policy at targets `steam|stop-out|header`: 15/15 P060/F040 trigger events converged over 2,000 committed explicit intervals, interval 723 was recovered with concrete `header` overrides, committed target selection remained transparent and the hold/release challenges stayed green.

## Purpose

H.17 deliberately tries to falsify the three-node policy before any activation design. It changes neither the production inverse resolver nor the H.9 solver. Instead it expands the shadow evidence in two dimensions:

1. longer horizon;
2. multiple current-v2 operating profiles.

The complete reference set contains 30,000 committed explicit 10 ms intervals:

- `steady-long`: 12,000 intervals;
- `load-pulse`: 6,000 intervals, generator request 5→0→5 MWe;
- `cooling-pulse`: 6,000 intervals, condenser cooling capacity 100%→75%→100%;
- `combined-load-cooling`: 6,000 intervals combining the same validated 5→0→5 MWe load excursion and bounded cooling excursion.

All profiles remain production-explicit and no shadow result is committed.

## H.16 control reproduction

The first 2,000 intervals of the steady-long profile must reproduce the validated H.16 evidence:

- 15 P060/F040 triggers;
- 15/15 convergence under H.9 + bounded branch continuity;
- zero line-search exhaustion;
- interval 723 converges;
- interval 723 includes at least one `header` override.

## Exhaustive census and stratified cross-profile qualification

Hotfix 3 demonstrated that the 30,000-interval horizon contains **3,046 P060/F040 trigger intervals** (`837 / 1014 / 175 / 1020` by profile). Those intervals are often prolonged threshold episodes, not 3,046 independent physical events. Running a full H.9 Newton solve and all-node candidate inverse-map scan at every above-threshold timestep is therefore an unsuitable validator contract.

Hotfix 4 keeps P060/F040 unchanged and preserves the **exhaustive census of every trigger interval**, then groups trigger activity into deterministic episodes. Trigger intervals separated by no more than 25 quiet intervals belong to the same episode. Every episode contributes first, last and hardest representatives; the complete H.16 2,000-interval control remains mandatory; profile action boundaries are represented; and temporal samples are added per profile while the final H.9 qualification set remains bounded to at most 512 representatives.

Every selected representative is evaluated with the unchanged H.9 Jacobian corrector and H.13 bounded hysteresis policy at exactly:

```text
steam | stop-out | header
```

A positive stratified qualification requires:

- every trigger episode is represented;
- every qualified representative converges at unchanged H.9 pressure/flow tolerances;
- zero line-search exhaustion;
- strict normalized-merit decrease over accepted H.9 iterates;
- exact deterministic repeat on cross-profile sentinels;
- unchanged H.9 mass-closure and energy-ownership tolerances;
- every profile retains post-500 trigger coverage.

## Committed-state transparency

All three target-node committed phases are inspected on every reference interval. Branch-selection re-resolution is sampled every 10 intervals and additionally at trigger intervals and every real committed phase transition.

Positive qualification requires every recorded committed selection to remain identical to production selection. Real phase transitions remain allowed and are counted from the complete unsampled committed phase sequence.

## Automatic fourth-node search

At every **qualified trigger representative**, H.17 inspects every thermodynamic node in:

- the final H.9+hysteresis candidate state;
- the matching production-explicit endpoint.

The audit specifically searches for a new untargeted version of the mechanism proven in H.12/H.15:

```text
coarse-saturated miss
+ earlier coarse-superheated selection
+ still-valid later boundary-aware saturated root
```

A new node blocks positive qualification when that late saturated-root shadow exists in the candidate but not in the matching explicit endpoint and the node is outside `steam|stop-out|header`.

## Hysteresis release contract

The four inherited H.14/H.16 synthetic challenges remain unchanged:

- two near-boundary previous-phase holds;
- two distant releases back to production selection.

They must remain deterministic and pass.

## Production isolation

H.17 does not modify:

- `SimplifiedWaterSteamThermodynamicModel.Resolve()`;
- `ThermodynamicBranchContinuityModel`;
- the target set `steam|stop-out|header`;
- H.9 `JacobianHydraulicCorrectorSolver`;
- the 2% pressure / 5 K temperature hysteresis limits;
- P060/F040;
- physical coefficients;
- `PlantNetworkOrchestrator`;
- the 10 ms `ExplicitCommittedState` production path.

## Decision after H.17

A positive H.17 result is the first evidence strong enough to justify designing a deliberately reversible production activation candidate with rollback and retained shadow telemetry. It does not itself activate production.

A negative result keeps production explicit and must be diagnosed from the failing profile/trigger or newly discovered untargeted branch-selection node before any policy change.

## Hotfix 4 bounded-runtime contract

The long horizon remains exhaustive for reference generation and trigger census. Nonlinear qualification is deterministic and episode-stratified rather than repeated at every timestep in a prolonged trigger episode. Mandatory episode/control representatives can never be silently dropped: if they exceed the 512-event budget the gate fails with an explicit redesign request. The heartbeat file `00-progress.txt` remains authoritative for stage-level progress.
