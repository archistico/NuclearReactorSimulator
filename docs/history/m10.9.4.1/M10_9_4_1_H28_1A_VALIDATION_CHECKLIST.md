# M10.9.4.1-H.28.1-A Hotfix 2 Validation Checklist


## Hotfix 1 compile repair

- [ ] The eight original CS0136 local-variable shadowing errors are gone.
- [ ] Non-trigger attribution locals use distinct `noTrigger*` names; timing/allocation formulas are unchanged.
- [ ] No other production/numerical behavior is changed by the compile repair.

## Hotfix 2 architecture repair

- [ ] H.28.1-A Hotfix 1 compile-shadowing repair remains intact.
- [ ] `src/NuclearReactorSimulator.Simulation` contains no direct wall-clock/timer/delay API token.
- [ ] Timing/allocation readers are injected only by the explicit Application.Tests attribution gate through `PerformanceAttributionMeasurement`.
- [ ] With no provider installed, measurement reads are zero and cannot affect trigger, authority, commit or physics.

## Preconditions

- [ ] Authoritative validated baseline is H.27 Hotfix 1.
- [ ] H.28 remains a failed performance candidate, not a baseline.
- [ ] User-validated H.27 baseline artifacts are frozen and fingerprint-checked.
- [ ] Failed H.28 artifacts are frozen and fingerprint-checked.
- [ ] Standard current-v2 remains `ExplicitCommittedState` at 10 ms.
- [ ] No numerical threshold, solver equation, target set or physical coefficient is changed.

## Local gate

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-four-node-performance-attribution-audit.cmd
```


## Ordinary architecture contract

- [ ] `ArchitectureRulesTests.SimulationProject_DoesNotUseWallClockTimerOrDelayApis` passes.
- [ ] No direct `Stopwatch`, `DateTime.Now/UtcNow`, timer or delay API appears under `src/NuclearReactorSimulator.Simulation`.

## Focused requirements

- [ ] Validated H.27 summary/telemetry/envelope/metrics fingerprints match.
- [ ] Failed H.28 summary/benchmark/soak/metrics fingerprints match.
- [ ] 256 corrected attribution steps complete after 64-step warmup.
- [ ] At least one P060/F040 trigger is observed.
- [ ] Every trigger exposes H.9 timing/allocation and work counters.
- [ ] Non-trigger steps expose predictor attribution without H.9 attribution.
- [ ] Zero rollback, unsafe commit and fallback-commit violations in the attribution control.
- [ ] Zero untargeted branch disagreements and unexpected trips.
- [ ] H.22 closure/ownership limits remain green.
- [ ] 128-step deterministic fingerprint equals `518BA948637F0C270C7F8228AB97FEF9148E29A4F5CE4376319AB5D1CFBE7F38`.
- [ ] Standard factory remains `ExplicitCommittedState`.
- [ ] `h28.1a-performance-attribution-passes=True`.
- [ ] `h28-remains-failed=True` and `H29-default-activation-blocked=True`.

## Interpretation

This is an attribution gate, not a performance qualification gate. A large measured cost does not make H.28.1-A fail; missing/inconsistent attribution or changed numerical behavior does.
