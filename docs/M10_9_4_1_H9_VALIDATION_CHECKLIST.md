# M10.9.4.1-H.9 — Validation Checklist

## Baseline

- [ ] Candidate is applied over the user-validated M10.9.4.1-H.8 baseline.
- [ ] Production current-v2 still uses `ExplicitCommittedState` at 10 ms.
- [ ] `PlantNetworkOrchestrator`, historical Picard, H.7 and H.8 production isolation are unchanged.

## Ordinary validation

```bat
dotnet build
dotnet test
```

- [ ] Build passes with warnings-as-errors.
- [ ] Ordinary test suite passes.

## Focused H.9 gate

```bat
scripts\run-jacobian-informed-corrector-audit.cmd
```

- [ ] H.9 ordinary solver regressions pass.
- [ ] Application descriptor contract passes.
- [ ] Explicit focused audit passes and emits all four H.9 artifacts.

## Frozen evidence reproduced

- [ ] 500 committed explicit intervals.
- [ ] P060/F040 remains frozen.
- [ ] 7 trigger events.
- [ ] H.4 primary 5/7.
- [ ] H.6 rescue 6/7.
- [ ] H.7 residual/backtracking 5/7 and two line-search exhaustions.
- [ ] H.8 safeguarded Anderson 5/7 and two line-search exhaustions.

## H.9 numerical contract

- [ ] Finite-difference Jacobian uses conservative coordinates.
- [ ] Probe perturbations preserve hydraulic mass closure and pump-energy ownership by construction.
- [ ] Scaled deterministic pivoting and conditioning rejection are reported.
- [ ] Accepted Newton/fallback iterates strictly reduce the unchanged H.7 pressure/flow merit.
- [ ] Exact deterministic repeat is true.
- [ ] Inventory mass residual <= `1e-6 kg`.
- [ ] Inventory energy residual <= `1e-2 J`.
- [ ] Applied hydraulic mass closure <= `1e-8 kg/s`.
- [ ] Applied hydraulic energy-ownership residual <= `1e-3 W`.
- [ ] Deterministic hydraulic-evaluation work ratio <= `32`.

## Decision

- `jacobian-informed-corrector-qualification-passes=True`: keep production explicit; proceed only to broader free-running/scenario shadow qualification.
- `jacobian-informed-corrector-qualification-passes=False`: keep production explicit; diagnose switching/discontinuity/non-smoothness of the difficult hydraulic-map intervals before further solver complexity.
