# H.28.1-F static review

- Base: H.28.1-D Preflight Hotfix 1 VALIDATED.
- H.28.1-E is included only as measured failed evidence.
- Public numerical API unchanged.
- Internal two-argument `JacobianHydraulicCorrectorSolver` constructor exists only to compare `CoordinateOnly` and `FullFixedPoint` probe modes in `Simulation.Tests`.
- Full fixed-point residual remains in initial residual and line-search/fallback paths.
- No H.9 tolerance, probe-count, Jacobian-dimension, P060/F040, H.20/H.22, hysteresis, target or physics retuning.
- No wall-clock API added under `NuclearReactorSimulator.Simulation`.
- H.28 and H.29 remain blocked.
