# M10.9.4.1-H.8 Validation Checklist — VALIDATED

Validated milestone: **Accelerated Nonlinear Hydraulic Corrector**

Baseline: **M10.9.4.1-H.7 Hotfix 1 — VALIDATED**

Run from repository root:

```bat
dotnet build
dotnet test
scripts\run-accelerated-nonlinear-corrector-audit.cmd
```

Validation requires:

- build succeeds with warnings-as-errors;
- ordinary test suite succeeds;
- focused H.8 gate succeeds;
- audit reproduces 500 explicit shadow intervals, P060/F040, 7 trigger events, H.4 5/7, H.6 6/7 and H.7 5/7 with two H.7 line-search exhaustions;
- deterministic repeat is `True`;
- accepted merit is strictly decreasing;
- mass/energy inventory residuals and hydraulic closure/ownership remain within the encoded limits;
- production remains `ExplicitCommittedState` at 10 ms;
- no shadow candidate is committed;
- Picard, H.7 and `PlantNetworkOrchestrator` production routing remain unchanged.

`accelerated-corrector-qualification-passes` may be either `True` or `False`. A `False` result is a legitimate numerical decision outcome and keeps production explicit.


## Recorded validation result

User validation passed build, ordinary tests and focused audit. H.8 converged 5/7 with two line-search exhaustions, deterministic work ratio 1.212000, exact repeat and strict merit decrease; `accelerated-corrector-qualification-passes=False`. Production remained explicit and no shadow candidate was committed.
