# M10.9.4.1-H.28.1-C — H.9 Jacobian/Probe Allocation & Hot-Path Optimization

## Status

VALIDATED as Hotfix 2 on 2026-08-19 after build, complete ordinary tests and the focused H.28.1-C gate passed. H.28 remains a failed performance qualification and H.29 remains blocked.


## Validated result

```text
triggered / committed                    20 / 20
hydraulic evaluations / trigger          35
probe evaluations / trigger               32
Jacobian dimension                        32
Jacobian allocation                 925,328 B
H.9 total allocation              ~1,004,460 B
Jacobian allocation fraction of H.28.1-A  0.023683
H.9 allocation fraction of H.28.1-A       0.024190
Jacobian wall time                 ~1.558 s
deterministic fingerprint          518BA948637F0C270C7F8228AB97FEF9148E29A4F5CE4376319AB5D1CFBE7F38
```

The allocation objective was exceeded by a wide margin (~97.6% reduction), while wall time remained essentially unchanged. H.28.1-C therefore proved that heap churn was real but not the dominant CPU cause of the trigger cost.

## Evidence driving this milestone

Validated H.28.1-A localized the triggered-step cost to H.9 Jacobian build/probes:

- 20 triggers / 20 corrected commits in the 256-step attribution window;
- 35 hydraulic evaluations per trigger;
- 32 finite-difference probe evaluations and Jacobian dimension 32;
- average H.9 total about 1.654 s per trigger;
- average Jacobian build/probes about 1.556 s per trigger;
- average H.9 allocation about 41.52 MB per trigger;
- average Jacobian build/probes allocation about 39.07 MB per trigger;
- deterministic fingerprint `518BA948637F0C270C7F8228AB97FEF9148E29A4F5CE4376319AB5D1CFBE7F38`.

The evidence showed regular work rather than pathological retry: one Jacobian build, one accepted Newton direction, no Jacobian rejection or residual fallback, one backtracking trial and two shadow iterations.

## Optimization boundary

H.28.1-C changes implementation cost only. It does not change:

- H.9 finite-difference Newton equations or probe count;
- coordinate layout semantics;
- residual definitions or tolerances;
- P060/F040;
- 2% pressure / 5 K bounded previous-phase hysteresis;
- target set `steam|stop-out|header|turbine-inlet`;
- H.20 authority or H.22 commit seam;
- production coefficients or 10 ms simulated fixed step.

The optimization removes work that was not part of the numerical map:

1. Jacobian trial/probe evaluation no longer constructs a complete `PlantState` just to evaluate hydraulics. It integrates canonical fluid-node states and evaluates the hydraulic map directly over those nodes.
2. A complete immutable `PlantState` is materialized only at the final H.9 candidate boundary.
3. `SemiImplicitHydraulicPrototypeSolver` caches immutable topology index bindings for the current `PlantDefinition` instead of rebuilding input lookup dictionaries on every H.9 hydraulic evaluation.
4. Instantaneous hydraulic evaluation builds canonical sorted result dictionaries once and `SemiImplicitHydraulicEvaluation` wraps them directly instead of immediately canonical-copying them again.
5. H.9 total hydraulic balances are combined directly during fluid-node integration instead of allocating an intermediate combined-balance dictionary for every probe/mapped integration.
6. The simplified water/steam inverse scan now carries saturation properties internally as a private value type instead of allocating a public `WaterSteamSaturationProperties` record for every coarse/boundary scan sample. Public saturation-property APIs still materialize the same public record at their boundary; equations, search segmentation, root order and returned numerical values are unchanged. This targets the dominant per-probe allocation churn without changing thermodynamic branch semantics.

## Qualification contract

The focused H.28.1-C gate repeats the validated 64-warmup + 256-step 5→0→5 manoeuvre and requires:

- exactly 20 trigger/commit steps;
- zero rollback, fallback-commit violation or unsafe commit;
- exactly 35 hydraulic evaluations per trigger;
- exactly 32 probe evaluations per trigger;
- Jacobian dimension 32;
- same numerical safety/closure limits;
- exact deterministic fingerprint from H.28/H.28.1-A;
- average Jacobian-build allocation no greater than 85% of the validated H.28.1-A value;
- average total H.9 allocation no greater than 88% of the validated H.28.1-A value.

Wall-clock improvement is reported but is not used as a machine-specific hard threshold here. The full H.28 rerun remains the authority for performance qualification.

## Interpretation

A green H.28.1-C means the allocation/hot-path optimization preserved the numerical contract and produced a material implementation improvement. It does **not** make H.28 green and does not authorize H.29.

After H.28.1-C, decide whether H.28.1-B predictor reuse is still warranted from measured non-trigger overhead, then rerun the unchanged H.28 performance/cost/soak gate. Because H.28.1-C changes committed-runtime implementation code even though its mathematics is frozen, the rare H.24 long-horizon qualification must be rerun once after the performance optimization branch is stable and before H.29 default-activation work; it is deliberately not chained into this development gate.
