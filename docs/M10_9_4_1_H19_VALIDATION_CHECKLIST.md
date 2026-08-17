# M10.9.4.1-H.19 Validation Checklist

**VALIDATED 2026-08-17:** local build, complete ordinary suite and focused H.19 gate passed; 473/473 qualified representatives converged and all qualification safeguards were green.

## Authoritative baseline

- [x] Candidate is built only on user-validated **H.18 Hotfix 1**.
- [x] `APPLY_UPDATE.cmd` removes stale local `bin` / `obj` outputs.
- [x] Production remains `ExplicitCommittedState` at 10 ms.
- [x] `SimplifiedWaterSteamThermodynamicModel.Resolve()` is unchanged.
- [x] `ThermodynamicBranchContinuityModel` implementation and 2% / 5 K limits are unchanged.
- [x] H.9, P060/F040, physical coefficients and `PlantNetworkOrchestrator` are unchanged.

## Ordinary gates

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
```

- [x] build passes.
- [x] ordinary suite passes.
- [x] H.18 frozen-evidence regression remains green.

## Focused gate

```bat
scripts\run-four-node-long-horizon-cross-profile-qualification-audit.cmd
```

Required structural evidence:

- [x] 30,000 explicit reference intervals reconstructed.
- [x] same four H.17/H.18 profiles reconstructed.
- [x] P060/F040 census remains exactly 3,046 trigger intervals.
- [x] trigger stratification remains exactly 92 episodes.
- [x] qualified representative set remains exactly 473 samples.
- [x] regenerated 473 representative keys exactly match frozen H.17 evidence.
- [x] target set is exactly `steam|stop-out|header|turbine-inlet`.
- [x] every selected sample uses unchanged H.9 and unchanged 2% / 5 K bounded hysteresis.
- [x] deterministic policy sentinel repeat passes.
- [x] committed observation determinism passes.
- [x] all-node inverse-scan determinism passes.
- [x] inherited hold/release challenges pass deterministically.
- [x] committed phase-state checks = 120,000.
- [x] no shadow candidate is committed.
- [x] `h19-audit-passes=True`.

Positive four-node long-horizon qualification additionally requires:

- [x] 473/473 qualified representatives converge.
- [x] 245/245 frozen H.17 failures recover.
- [x] 228/228 frozen H.17 successes remain convergent.
- [x] 120/120 frozen turbine-inlet-mismatch failures recover.
- [x] 125/125 frozen non-mismatch failures recover.
- [x] committed selection remains transparent.
- [x] no untargeted candidate-only late-shadow node remains.
- [x] no untargeted candidate-vs-explicit phase-mismatch node remains.
- [x] `cross-profile-stratified-policy-qualifies=True`.
- [x] `four-node-long-horizon-cross-profile-shadow-qualification-passes=True`.

A negative qualification flag is valid evidence and does not by itself mean the focused diagnostic infrastructure failed. Production must remain explicit either way.

## Expected artifacts

```text
artifacts\h19-four-node-long-horizon-cross-profile-qualification\
  00-progress.txt
  01-current-v2-four-node-long-horizon-cross-profile-qualification.summary.txt
  02-profile-qualification-summary.csv
  03-triggered-event-cross-profile-results.csv
  03a-trigger-episode-stratification.csv
  04-committed-target-selection-observations.csv
  05-triggered-all-node-inverse-branch-scan.csv
  06-hysteresis-release-challenges.csv
  07-four-node-long-horizon-cross-profile-metrics.csv
```
