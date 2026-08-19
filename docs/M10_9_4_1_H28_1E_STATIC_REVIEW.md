# H.28.1-E Static Review

Pre-build review performed before packaging because the assistant environment does not contain the .NET SDK.

Checked:

- H.28.1-E is stacked on the validated H.28.1-D full source, not on the failed H.28 Requalification candidate;
- the public simplified water/steam model does not implement a less-accessible internal interface; the fused continuity seam is an `internal` method on the concrete public class;
- `FluidNodeState` namespace/type usage remains compatible with the H.28.1-C compile hotfixes;
- no new array/`IReadOnlyList<FluidNodeState>` assignment mismatch was introduced;
- incremental hydraulic reuse requires exact reference identity for every component dependency;
- component accumulation order remains pipe → valve → pump and per-layout order is unchanged;
- standard production `Resolve()` and the public inverse-branch diagnostic remain on the historical superheated scan; the fast rejection is scoped to the internal continuity traversal;
- focused thermodynamic test compares optimized continuity decisions to the legacy `Resolve + Diagnose` path exactly;
- focused hydraulic unit test compares full and incremental evaluation maps/closure exactly;
- frozen H.28.1-D and H.28 Requalification 1 canonical SHA-256 values match the user-produced artifacts;
- deterministic result records H.9/H.21/H.22 are not extended by E; new reuse counters live only in diagnostic attribution;
- no forbidden wall-clock/timer token was added under `NuclearReactorSimulator.Simulation`;
- batch runner uses CRLF;
- no `bin`, `obj` or `artifacts` directory is included in the candidate.
- final delta versus H.28.1-D: 29 paths, 11 C# files, 7 paths under `src/`; only 6 Simulation runtime files plus Application descriptor metadata change under `src/`;

A real `dotnet build`, `dotnet test` and focused audit are still mandatory before validation.

## Hotfix 2 preflight extension

After Hotfix 1 exposed one CS0103 in the H.9 probe helper chain, the counter wiring was reviewed end-to-end rather than patching only the failing line.

- `probeHydraulicComponentReuse` is created once in the outer corrector invocation and is now passed through every Jacobian finite-difference helper that needs it.
- `TryBuildNewtonTarget`: declaration/call arity 12/12.
- `TryEvaluateProbeResidual`: declaration/call arity 18/18.
- `TryEvaluateProbeResidualWithSignedStep`: declaration/two calls arity 17/17.
- `TryEvaluateProbeTrial`: declaration/call arity 13/13.
- both +step and -step finite-difference paths pass the same counter.
- `EvaluateFixedPointResidual` keeps the component-reuse counter optional so non-probe initial/line-search paths are unchanged.
- the only runtime semantic delta versus Hotfix 1 is diagnostic counter plumbing; no hydraulic or thermodynamic calculation changes.
