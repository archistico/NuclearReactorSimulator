# M10.9.4.1-H.19 Validation Checklist

## Authoritative baseline

- [ ] Candidate is built only on user-validated **H.18 Hotfix 1**.
- [ ] `APPLY_UPDATE.cmd` removes stale local `bin` / `obj` outputs.
- [ ] Production remains `ExplicitCommittedState` at 10 ms.
- [ ] `SimplifiedWaterSteamThermodynamicModel.Resolve()` is unchanged.
- [ ] `ThermodynamicBranchContinuityModel` implementation and 2% / 5 K limits are unchanged.
- [ ] H.9, P060/F040, physical coefficients and `PlantNetworkOrchestrator` are unchanged.

## Ordinary gates

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
```

- [ ] build passes.
- [ ] ordinary suite passes.
- [ ] H.18 frozen-evidence regression remains green.

## Focused gate

```bat
scripts\run-four-node-long-horizon-cross-profile-qualification-audit.cmd
```

Required structural evidence:

- [ ] 30,000 explicit reference intervals reconstructed.
- [ ] same four H.17/H.18 profiles reconstructed.
- [ ] P060/F040 census remains exactly 3,046 trigger intervals.
- [ ] trigger stratification remains exactly 92 episodes.
- [ ] qualified representative set remains exactly 473 samples.
- [ ] regenerated 473 representative keys exactly match frozen H.17 evidence.
- [ ] target set is exactly `steam|stop-out|header|turbine-inlet`.
- [ ] every selected sample uses unchanged H.9 and unchanged 2% / 5 K bounded hysteresis.
- [ ] deterministic policy sentinel repeat passes.
- [ ] committed observation determinism passes.
- [ ] all-node inverse-scan determinism passes.
- [ ] inherited hold/release challenges pass deterministically.
- [ ] committed phase-state checks = 120,000.
- [ ] no shadow candidate is committed.
- [ ] `h19-audit-passes=True`.

Positive four-node long-horizon qualification additionally requires:

- [ ] 473/473 qualified representatives converge.
- [ ] 245/245 frozen H.17 failures recover.
- [ ] 228/228 frozen H.17 successes remain convergent.
- [ ] 120/120 frozen turbine-inlet-mismatch failures recover.
- [ ] 125/125 frozen non-mismatch failures recover.
- [ ] committed selection remains transparent.
- [ ] no untargeted candidate-only late-shadow node remains.
- [ ] no untargeted candidate-vs-explicit phase-mismatch node remains.
- [ ] `cross-profile-stratified-policy-qualifies=True`.
- [ ] `four-node-long-horizon-cross-profile-shadow-qualification-passes=True`.

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
