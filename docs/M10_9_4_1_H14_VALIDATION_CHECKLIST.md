# M10.9.4.1-H.14 Validation Checklist

## Baseline and isolation

- [ ] Candidate is built only on user-validated H.13 Hotfix 2.
- [ ] Production remains `ExplicitCommittedState` at 10 ms.
- [ ] Production `SimplifiedWaterSteamThermodynamicModel.Resolve()` remains unchanged.
- [ ] `ThermodynamicBranchContinuityModel` is unchanged from H.13 Hotfix 2.
- [ ] `PlantNetworkOrchestrator` and H.3-H.9 hydraulic correctors are unchanged.
- [ ] Bounded hysteresis remains restricted to `steam` and `stop-out`.

## Ordinary gates

- [ ] `dotnet build` passes with warnings-as-errors.
- [ ] `dotnet test` passes.

## Focused H.14 gate

Run:

```bat
scripts\run-broader-thermodynamic-branch-continuity-audit.cmd
```

Expected structural invariants:

- [ ] 2,000 production explicit intervals are reconstructed.
- [ ] the first 500 intervals reproduce exactly seven P060/F040 events.
- [ ] H.4 primary still converges 5/7 in that first control window.
- [ ] unchanged production H.9 still reproduces 5/7 with two line-search exhaustions in the H.13 control window.
- [ ] every broader P060/F040 event is evaluated with unchanged H.9 plus targeted bounded hysteresis.
- [ ] the broader policy evaluation is exactly deterministic.
- [ ] 4,000 committed `steam`/`stop-out` branch observations are produced and exactly repeatable.
- [ ] two near-boundary hold challenges pass.
- [ ] two deliberately out-of-band release challenges pass.
- [ ] release challenges cover both SaturatedMixture→SuperheatedVapor and SuperheatedVapor→SaturatedMixture directions.
- [ ] no shadow result is committed.
- [ ] mass closure and energy ownership remain within existing H.9 tolerances.

## Qualification interpretation

A positive `broader-shadow-qualification-passes=True` requires all broader trigger events to converge under unchanged H.9 residual/merit gates and all explicit hold/release challenges to pass deterministically.

A negative qualification result is valid H.14 evidence but does not authorize an activation candidate.
