# M10.9.4.1-H.18 Hotfix 1 Validation Checklist

## Authoritative baseline

- [x] Candidate is built only on user-validated **H.17 Hotfix 6**.
- [x] `APPLY_UPDATE.cmd` removes stale local `bin`/`obj` outputs.
- [x] Production remains `ExplicitCommittedState` at 10 ms.
- [x] `SimplifiedWaterSteamThermodynamicModel.Resolve()` is unchanged.
- [x] `ThermodynamicBranchContinuityModel` implementation and 2%/5 K limits are unchanged.
- [x] H.9, P060/F040, physical coefficients and `PlantNetworkOrchestrator` are unchanged.

## Frozen evidence contract

- [x] `H17_Hotfix6_FrozenQualifiedRepresentativeEvidence.csv` is present.
- [x] 473 frozen representatives.
- [x] 245 H.17 failures / 228 H.17 successes.
- [x] 120 failed representatives with `turbine-inlet` phase mismatch.
- [x] 125 failed representatives without `turbine-inlet` phase mismatch.
- [x] ordinary frozen-evidence regression passes.

## Ordinary gates

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
```

- [x] build passes.
- [x] ordinary suite passes.

## Focused gate

```bat
scripts\run-turbine-inlet-continuity-residual-floor-split-audit.cmd
```

Required structural evidence:

- [x] same four H.17 profiles are reconstructed.
- [x] load profiles use validated 5→0→5 MWe requests.
- [x] 261 H.18 nonlinear samples = all 245 H.17 failures + 16 success controls.
- [x] target set is exactly `steam|stop-out|header|turbine-inlet`.
- [x] every selected sample uses unchanged H.9 and H.13 2%/5 K bounded hysteresis.
- [x] deterministic sentinel repeat passes.
- [x] every remaining failure receives all-node residual ranking and inverse-branch comparison.
- [x] committed `turbine-inlet` transparency is reported.
- [x] no shadow candidate is committed.
- [x] `residual-floor-split-diagnostic-passes=True`.

Positive four-node qualification additionally requires:

- [x] `recovered-turbine-inlet-mismatch=120/120`;
- [x] all H.17 success controls remain convergent;
- [x] committed `turbine-inlet` selection remains transparent;
- [x] `four-node-extension-qualifies=True`.

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
