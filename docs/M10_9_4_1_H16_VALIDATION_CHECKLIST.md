# M10.9.4.1-H.16 Validation Checklist

## Baseline and isolation

- [ ] Candidate is built only on user-validated H.15 Hotfix 1.
- [ ] `APPLY_UPDATE.cmd` removes stale `bin`/`obj` before validation.
- [ ] Production remains `ExplicitCommittedState` at 10 ms.
- [ ] `SimplifiedWaterSteamThermodynamicModel.Resolve()` is unchanged.
- [ ] `ThermodynamicBranchContinuityModel` is unchanged.
- [ ] H.13 bounded limits remain 2% relative pressure drift / 5 K temperature drift.
- [ ] H.9, P060/F040 and `PlantNetworkOrchestrator` are unchanged.

## Ordinary gates

- [ ] `dotnet build` passes.
- [ ] `dotnet test` passes.

## Focused gate

Run:

```bat
scripts\run-three-node-branch-continuity-audit.cmd
```

Required structural evidence:

- [ ] 2,000 committed explicit intervals.
- [ ] 15 P060/F040 triggers.
- [ ] H.14 control targets `steam|stop-out` reproduce 14/15 and one exhaustion.
- [ ] H.14 control interval 723 remains non-convergent with zero branch override.
- [ ] H.16 target set is exactly `steam|stop-out|header`.
- [ ] Three-node result is exactly deterministic.
- [ ] Every accepted H.9 iterate strictly decreases normalized merit.
- [ ] Closure/ownership remain within H.9 tolerances.
- [ ] 6,000 committed target observations are recorded.
- [ ] Two inherited hold and two inherited release challenges pass.
- [ ] No shadow candidate is committed.

Positive qualification additionally requires:

- [ ] three-node H.9 converges 15/15;
- [ ] zero line-search exhaustion;
- [ ] interval 723 is recovered;
- [ ] interval 723 records at least one `header` override;
- [ ] committed target selection is transparent to production;
- [ ] `three-node-shadow-qualification-passes=True`.

A negative qualification does not fail the diagnostic audit if all structural safeguards pass.
