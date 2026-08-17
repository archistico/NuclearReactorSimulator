# M10.9.4.1-H.18 Hotfix 1 Validation Checklist

## Authoritative baseline

- [ ] Candidate is built only on user-validated **H.17 Hotfix 6**.
- [ ] `APPLY_UPDATE.cmd` removes stale local `bin`/`obj` outputs.
- [ ] Production remains `ExplicitCommittedState` at 10 ms.
- [ ] `SimplifiedWaterSteamThermodynamicModel.Resolve()` is unchanged.
- [ ] `ThermodynamicBranchContinuityModel` implementation and 2%/5 K limits are unchanged.
- [ ] H.9, P060/F040, physical coefficients and `PlantNetworkOrchestrator` are unchanged.

## Frozen evidence contract

- [ ] `H17_Hotfix6_FrozenQualifiedRepresentativeEvidence.csv` is present.
- [ ] 473 frozen representatives.
- [ ] 245 H.17 failures / 228 H.17 successes.
- [ ] 120 failed representatives with `turbine-inlet` phase mismatch.
- [ ] 125 failed representatives without `turbine-inlet` phase mismatch.
- [ ] ordinary frozen-evidence regression passes.

## Ordinary gates

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
```

- [ ] build passes.
- [ ] ordinary suite passes.

## Focused gate

```bat
scripts\run-turbine-inlet-continuity-residual-floor-split-audit.cmd
```

Required structural evidence:

- [ ] same four H.17 profiles are reconstructed.
- [ ] load profiles use validated 5→0→5 MWe requests.
- [ ] 261 H.18 nonlinear samples = all 245 H.17 failures + 16 success controls.
- [ ] target set is exactly `steam|stop-out|header|turbine-inlet`.
- [ ] every selected sample uses unchanged H.9 and H.13 2%/5 K bounded hysteresis.
- [ ] deterministic sentinel repeat passes.
- [ ] every remaining failure receives all-node residual ranking and inverse-branch comparison.
- [ ] committed `turbine-inlet` transparency is reported.
- [ ] no shadow candidate is committed.
- [ ] `residual-floor-split-diagnostic-passes=True`.

Positive four-node qualification additionally requires:

- [ ] `recovered-turbine-inlet-mismatch=120/120`;
- [ ] all H.17 success controls remain convergent;
- [ ] committed `turbine-inlet` selection remains transparent;
- [ ] `four-node-extension-qualifies=True`.

A negative `four-node-extension-qualifies` is valid diagnostic evidence and does not by itself fail H.18.

## Expected artifacts

```text
artifacts\h18-turbine-inlet-continuity-residual-floor-split\
  00-progress.txt
  01-current-v2-turbine-inlet-continuity-residual-floor-split.summary.txt
  02-frozen-h17-evidence-selection.csv
  03-four-node-policy-results.csv
  04-four-node-recovery-matrix.csv
  05-remaining-failure-residual-floor-ranking.csv
  06-remaining-failure-inverse-branch-scan.csv
  07-turbine-inlet-committed-transparency.csv
  08-h18-split-diagnosis-metrics.csv
```
