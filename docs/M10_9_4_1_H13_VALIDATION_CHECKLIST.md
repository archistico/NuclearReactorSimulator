# M10.9.4.1-H.13 Validation Checklist

## Baseline and isolation

- [ ] Candidate is built on the user-validated H.12 baseline.
- [ ] Production remains `ExplicitCommittedState` at 10 ms.
- [ ] `SimplifiedWaterSteamThermodynamicModel.Resolve()` production body/order is unchanged.
- [ ] `PlantNetworkOrchestrator` and H.3-H.9 hydraulic correctors are unchanged.
- [ ] H.13 branch policy is restricted to `steam` and `stop-out`.

## Ordinary gates

- [ ] `dotnet build` passes with warnings-as-errors.
- [ ] `dotnet test` passes.

## Focused H.13 gate

Run:

```bat
scripts\run-thermodynamic-branch-continuity-audit.cmd
```

Expected audit invariants:

- [ ] 500 production shadow intervals reconstructed.
- [ ] P060/F040 selects exactly 7 events.
- [ ] unchanged production H.9 reproduces 5/7 convergence and 2 line-search exhaustions.
- [ ] previous-phase continuity is exactly deterministic.
- [ ] bounded hysteresis is exactly deterministic.
- [ ] both policies actually exercise at least one branch override versus production.
- [ ] no production result is committed.
- [ ] mass closure and energy ownership remain within existing H.9 tolerances.

## Qualification interpretation

`qualification-passes=False` for one or both policies does not by itself fail H.13. H.13 is a shadow experiment. Promotion to broader shadow qualification requires a policy to meet the unchanged H.9 convergence, residual, monotonic-merit, deterministic-work, no-chatter and conservation gates on all seven frozen events.
