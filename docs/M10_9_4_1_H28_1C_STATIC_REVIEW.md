# M10.9.4.1-H.28.1-C Static Review

## Status

Package-time static review only. H.28.1-C remains CANDIDATE until local `dotnet build`, complete `dotnet test` and the focused H.28.1-C gate pass.

## Baseline discipline

H.28.1-C is stacked on user-validated H.28.1-A Hotfix 2 diagnostic evidence. The authoritative numerical continuation remains H.27 Hotfix 1; H.28 remains failed performance evidence and H.29 remains blocked.

## Numerical boundary

The candidate preserves the H.9 finite-difference Newton contract: 35 hydraulic evaluations, 32 probe evaluations, Jacobian dimension 32, unchanged residual equations/tolerances, P060/F040, 2%/5 K branch continuity, four-node target set, H.20 authority and H.22 commit ownership.

Implementation-only changes remove probe-path object-graph churn:

- transient trials carry canonical `FluidNodeState[]` until final candidate materialization;
- hydraulic topology bindings are cached by immutable `PlantDefinition` identity;
- per-evaluation lookup dictionaries, combined-balance maps and duplicate canonical copies are removed;
- internal saturation scans use a private value-type property carrier instead of allocating a public saturation record for each scan sample; public API and equations/search order are unchanged.

## Static checks

- deterministic H.9/H.21/H.22 result/telemetry record files are byte-identical to H.28.1-A Hotfix 2;
- no forbidden wall-clock/timer token is present under `NuclearReactorSimulator.Simulation`;
- all H.27, failed-H.28 and validated-H.28.1-A frozen evidence files match their canonical SHA-256 fingerprints;
- C# delimiter counts are balanced in every modified/new C# file;
- Markdown fences are balanced in modified/new documentation;
- no `bin`, `obj` or `artifacts` directory is packaged.

## Performance gate

The focused gate deliberately keeps the mathematical work count fixed and requires exact deterministic fingerprint `518BA948637F0C270C7F8228AB97FEF9148E29A4F5CE4376319AB5D1CFBE7F38`, while requiring average Jacobian allocation <=85% and total H.9 allocation <=88% of validated H.28.1-A.

H.28 remains the final performance authority. Because this candidate changes runtime implementation code, the rare H.24 long-horizon qualification must be rerun once after the optimization branch is stable and before H.29; it is not chained into this development gate.
