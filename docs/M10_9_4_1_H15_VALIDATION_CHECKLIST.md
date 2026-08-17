# M10.9.4.1-H.15 Validation Checklist

## Baseline and isolation

- [x] Candidate is built only on user-validated H.14 Hotfix 1.
- [ ] Production remains `ExplicitCommittedState` at 10 ms.
- [ ] `SimplifiedWaterSteamThermodynamicModel.Resolve()` is unchanged.
- [ ] `ThermodynamicBranchContinuityModel` and the bounded 2% / 5 K policy are unchanged.
- [ ] H.3-H.9 correctors and `PlantNetworkOrchestrator` routing are unchanged.
- [ ] No target node is assumed in advance for interval 723 diagnosis.

## Ordinary gates

- [x] Run `APPLY_UPDATE.cmd`; it removes stale local `bin`/`obj` outputs before validation.
- [x] `dotnet build` passes with warnings-as-errors.
- [x] `dotnet test` passes.

## Focused H.15 gate

Run:

```bat
scripts\run-extended-trigger-723-root-cause-audit.cmd
```

Expected structural invariants:

- [ ] 724 committed production intervals are reconstructed.
- [ ] P060/F040 produces nine trigger events through interval 724.
- [ ] H.4 primary converges 6/9.
- [ ] interval 723 is a non-convergent H.4 trigger.
- [ ] unchanged H.9 plus unchanged H.13 bounded hysteresis reproduces interval 723 as non-convergent with line-search exhaustion.
- [ ] interval 723 records zero branch overrides and zero hysteresis releases.
- [ ] intervals 721, 722, 723 and 724 are all evaluated under the same H.9+hysteresis path.
- [ ] every fluid node receives mapped-minus-applied mass/energy residual evidence.
- [ ] every hydraulic path receives H.10 two-scale branch/smoothness probes at candidate and explicit endpoint.
- [ ] every thermodynamic node receives H.10 two-scale phase/envelope probes at candidate and explicit endpoint.
- [ ] every candidate fluid node receives H.12 inverse-branch diagnostic evidence.
- [ ] exact deterministic repeat passes.
- [ ] no shadow state is committed.

## Interpretation

A passing H.15 audit means the interval-723 diagnosis is complete and deterministic. It does **not** require a particular root cause to be found and does not authorize production activation.

If switching/non-smooth/inverse-branch evidence is absent, proceed to fixed-point existence/residual-floor and basin analysis instead of further solver or hysteresis tuning.

## Validated result

User validation passed build, ordinary tests and focused H.15 audit. Interval 723 localized to `header` with thermodynamic phase-boundary/non-smooth evidence, overlapping saturated/superheated roots and late boundary-aware saturated-root shadowing; hydraulic paths remained locally smooth. H.15 Hotfix 1 is the validated continuation baseline for H.16.
