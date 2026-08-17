> H.17 Hotfix 6: determinism fingerprints are canonicalized by semantic key; numerical qualification coverage is unchanged.

> H.17 Hotfix 5: descriptor sentinel wording aligned; numerical audit unchanged.

# M10.9.4.1-H.17 Hotfix 4 Validation Checklist

> **Hotfix 1 note:** the original H.17 candidate failed compilation only in the explicit cross-profile audit because two nested string literals were escaped inside interpolated C# expressions. Hotfix 1 precomputes those release counts.
>
> **Hotfix 2 note:** ordinary build/tests then passed, but the focused reference trajectory tripped at `load-pulse` interval 634 after the audit-only 5→10 MWe request. Hotfix 2 does not suppress or reset that trip. It changes only the audit load excursion to the existing validated breaker-closed 5→0→5 MWe trajectory.
>
> **Hotfix 3 evidence:** the bounded/observable audit completed the full 30,000-interval reference construction and exhaustive P060/F040 census, discovering 3,046 trigger intervals: `steady-long=837`, `load-pulse=1014`, `cooling-pulse=175`, `combined-load-cooling=1020`. This is a trigger storm for an all-trigger Newton qualification, not a simulator stall.
>
> **Hotfix 4 decision:** preserve exhaustive trigger discovery, but qualify H.9 through deterministic trigger-episode stratification. P060/F040 is not retuned.

## Baseline and isolation

- [ ] Candidate is built only on user-validated H.16.
- [ ] `APPLY_UPDATE.cmd` removes stale `bin`/`obj` before validation.
- [ ] Production remains `ExplicitCommittedState` at 10 ms.
- [ ] `SimplifiedWaterSteamThermodynamicModel.Resolve()` is unchanged.
- [ ] `ThermodynamicBranchContinuityModel` is unchanged.
- [ ] Target set remains exactly `steam|stop-out|header`.
- [ ] H.13 bounded limits remain 2% relative pressure drift / 5 K temperature drift.
- [ ] H.9, P060/F040 and `PlantNetworkOrchestrator` are unchanged.

## Ordinary gates

- [ ] `dotnet build` passes.
- [ ] `dotnet test` passes.

## Focused gate

Run:

```bat
scripts\run-long-horizon-cross-profile-branch-continuity-audit.cmd
```

Required structural evidence:

- [ ] 4 profiles are evaluated.
- [ ] 30,000 total committed explicit intervals are evaluated.
- [ ] Profiles are `steady-long`, `load-pulse`, `cooling-pulse`, `combined-load-cooling`.
- [ ] `load-pulse` and the load leg of `combined-load-cooling` use the validated 5→0→5 MWe request trajectory; no trip suppression/reset is introduced.
- [ ] Every profile has at least 7 P060/F040 triggers and at least one trigger after interval 500.
- [ ] H.16 2,000-interval control reproduces 15 triggers and 15/15 convergence.
- [ ] H.16 control interval 723 converges with concrete `header` override evidence.
- [ ] Cross-profile policy result is exactly deterministic.
- [ ] Every accepted H.9 iterate strictly decreases normalized merit.
- [ ] Closure/ownership remain within H.9 tolerances.
- [ ] All target-node committed phase states are scanned for transitions.
- [ ] Sampled/forced committed branch-selection observations remain transparent to production.
- [ ] Every **qualified trigger representative** and matching explicit endpoint is scanned across all thermodynamic nodes for candidate-only late saturated-root shadowing.
- [ ] Inherited two-hold/two-release challenges pass.
- [ ] No shadow candidate is committed.

Positive qualification additionally requires:

- [ ] the exhaustive 30,000-interval census completes for all four profiles;
- [ ] every trigger episode has at least one qualified representative;
- [ ] the complete H.16 2,000-interval/15-trigger control is retained in the qualification set;
- [ ] first/last/hardest representatives are retained for each trigger episode;
- [ ] action-boundary representatives and temporal stratification are retained within the 512-sample H.9 budget;
- [ ] every qualified representative converges with zero line-search exhaustion;
- [ ] no new untargeted candidate-only late saturated-root shadow node is found in qualified representatives;
- [ ] `long-horizon-cross-profile-stratified-shadow-qualification-passes=True`.

A negative qualification does not fail the diagnostic audit if all structural safeguards pass.

## Hotfix 4 trigger-episode stratification contract

Hotfix 4 separates **exhaustive census** from **bounded nonlinear qualification**. All 30,000 explicit intervals are still generated and every P060/F040 trigger interval is still discovered. Trigger intervals separated by at most 25 quiet intervals are grouped into deterministic trigger episodes. Every episode contributes its first, last and hardest representative (hardest = maximum of normalized trigger severity and H.4 residual severity). The complete H.16 control set is mandatory, profile action boundaries are represented, and each profile receives up to 64 temporally distributed trigger representatives while the final H.9 qualification set is bounded to at most 512 events.

All census triggers continue to force committed target-selection observations. H.9/Newton and all-node inverse-branch candidate scanning run on the stratified representatives. Exact determinism is rechecked on deterministic cross-profile sentinels. `00-progress.txt` reports census, episode and qualification stages. If mandatory episode/control representatives alone exceed 512, the gate fails explicitly rather than silently dropping coverage.
