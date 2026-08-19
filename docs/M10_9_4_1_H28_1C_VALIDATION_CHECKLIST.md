# M10.9.4.1-H.28.1-C Hotfix 2 Validation Checklist

- [x] `APPLY_UPDATE.cmd`
- [x] `dotnet build`
- [x] `dotnet test`
- [x] `scripts\run-h9-jacobian-probe-hot-path-optimization-audit.cmd`
- [x] Frozen H.28.1-A evidence fingerprints pass.
- [x] `SemiImplicitHydraulicPrototypeSolverTests`, `SimplifiedWaterSteamThermodynamicModelTests` and `JacobianHydraulicCorrectorSolverTests` pass unchanged.
- [x] Focused window observes exactly 20 triggers and 20 corrected commits.
- [x] Zero rollback, fallback-commit violation and unsafe corrected commit.
- [x] Every trigger records 35 hydraulic evaluations.
- [x] Every trigger records 32 probe evaluations and Jacobian dimension 32.
- [x] Deterministic fingerprint remains `518BA948637F0C270C7F8228AB97FEF9148E29A4F5CE4376319AB5D1CFBE7F38`.
- [x] Average Jacobian-build allocated bytes ≤ 85% of H.28.1-A validated baseline (`39,071,378 B`).
- [x] Average H.9 total allocated bytes ≤ 88% of H.28.1-A validated baseline (`41,523,908 B`).
- [x] Standard factory remains `ExplicitCommittedState` at 10 ms.
- [x] H.28 remains FAILED until its original performance gate is rerun and passes.
- [x] H.29 remains blocked.
- [x] H.24 is not chained into H.28.1-C, but is scheduled for one rerun after the performance optimization branch stabilizes and before H.29 because runtime implementation code changed.

## Hotfix 1 compile repair

- [x] First H.28.1-C local build failure recorded: eight CS0246 errors were all unresolved `FluidNodeState` references in `JacobianHydraulicCorrectorSolver.cs`.
- [x] Cause localized to missing `using NuclearReactorSimulator.Domain.Physics.Fluids;`.
- [x] Hotfix 1 changes only that namespace import in C#; no H.9 mathematics, probe count, residual contract or optimization logic changes.


## Hotfix 2 compile repair



- [x] Hotfix 1 resolved the missing `FluidNodeState` namespace; the next local build exposed one CS0266 at the line-search acceptance assignment.

- [x] `iterateFluidNodes` is now declared as `IReadOnlyList<FluidNodeState>` from initialization, matching `LineSearchAcceptance.FluidNodes`.

- [x] No cast, `.ToArray()` copy, probe-count change, residual change or H.9 mathematical change is introduced.

- [x] `dotnet build` passes.

- [x] `dotnet test` passes.

- [x] `scripts\run-h9-jacobian-probe-hot-path-optimization-audit.cmd` passes.


## Final validated metrics

- Jacobian/probe allocation: `925,328 B` average per trigger.
- H.9 total allocation: `1,004,460.4 B` average per trigger.
- Deterministic fingerprint matched H.28/H.28.1-A exactly.
- Focused H.28.1-C result: `h28.1c-jacobian-probe-hot-path-optimization-passes=True`.
