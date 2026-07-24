# M10.9.4.1-B.1 — Validation Checklist

**USER VALIDATION RESULT: PASSED — compilation and requested tests confirmed green. B.1 promoted as the validated base for B.2.**

## Status

Completed validation gate. The source baseline was the user-validated corrected A.3 current-v2 operating seed; B.1 is now the validated base for B.2.

## 1. Clean build

```bat
dotnet clean
dotnet restore
dotnet build --no-restore
```

Required:

- 0 errors;
- 0 warnings under the repository warnings-as-errors policy.

## 2. Focused regressions

Run the steam-drum, game-scoring, command-policy and HMI contract regressions before the complete suite.

```bat
dotnet test tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj --no-build --filter FullyQualifiedName~SteamDrumSeparationSolverTests
dotnet test tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj --no-build --filter "FullyQualifiedName~TrainingFrameworkTests|FullyQualifiedName~ControlRoomRuntimeCommandPolicyTests|FullyQualifiedName~ApplicationDescriptorTests"
dotnet test tests\NuclearReactorSimulator.App.Tests\NuclearReactorSimulator.App.Tests.csproj --no-build --filter FullyQualifiedName~MainWindowXamlContractTests
```

Required B.1 evidence:

- fully vaporized current-v2 drum: requested pump recirculation may exist, actual **liquid** recirculation is 0;
- near-dryout current-v2 drum: actual liquid recirculation does not exceed same-step incoming liquid plus committed separable liquid inventory;
- sufficient-inventory current-v2 point: historical requested pump demand is still satisfied;
- `LegacyReturnSplit`: unchanged even when an integration interval is supplied;
- automatic protection state alone does not trigger a manual-command game penalty;
- default command increments remain exactly 10 rpm for SPEED and 5 MWe for LOAD;
- XAML exposes committed SPEED and requested LOAD references.

## 3. Ordinary suite

```bat
dotnet test --no-build
```

Expected reference from the A.3 source checkpoint before B.1 changes: **895 passed / 11 explicit skipped / 0 failed**. The exact passed count may increase because B.1 adds regressions; zero failures is mandatory.

## 4. Explicit long-running gates

```bat
scripts\run-gameplay-long-tests.cmd
scripts\run-operational-envelope-audit.cmd
scripts\run-reference-plant-scale-audit.cmd
```

Required:

- both validated 60-second gameplay journeys remain green;
- exact 300-second sustained 5 MWe reference remains healthy;
- no new protection trip, depletion exception or conservation regression;
- reference-scale evidence remains unchanged.

## 5. Manual HMI check

In the generator/grid control workspace:

1. verify `SPEED REFERENCE · MODEL` is visible;
2. press `SPEED RAISE` once and advance the deterministic runtime: the committed reference must increase by 10 rpm;
3. press `SPEED LOWER` once: the committed reference must decrease by 10 rpm;
4. verify `REQUESTED LOAD · MODEL` is visible;
5. press `LOAD RAISE` once and advance the runtime: requested load must increase by 5 MWe;
6. press `LOAD LOWER` once: requested load must decrease by 5 MWe;
7. verify the explanatory text distinguishes these references from actual rotor speed and actual electrical output.

For training scoring, automatic protection trips may still cause failed objectives or unsafe scenario outcomes, but they must not directly trigger a penalty whose trigger is a manual `ControlRoomCommandKind` unless that manual command was actually accepted.

## 6. Promotion rule

Promote B.1 only after all sections above are green. Do not begin B.2 by changing the drum-to-main-steam source law on top of an unvalidated B.1 candidate.
