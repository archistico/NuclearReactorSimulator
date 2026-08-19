# M10.9.4.1-H.28.1-E — Incremental Hydraulic Probe Evaluation & CPU Tail Reduction

Status: **CANDIDATE**. Built directly on user-validated H.28.1-D Preflight Hotfix 1. The failed H.28 Requalification 1 is frozen diagnostic evidence only and is not used as a source baseline.

## Why E exists

H.28 Requalification 1 showed that the performance branch is now healthy in median cost and allocation cost but still fails the unchanged p95 wall-cost ceiling:

- median wall ratio: `4.5869950614157275 <= 8` — pass;
- median allocation ratio: `1.1156355393376458 <= 16` — pass;
- p95 wall ratio: `38.713649509171638 > 12` — fail;
- explicit p95: `7365.1 us`;
- corrected p95: `285129.9 us`;
- 20/256 corrected benchmark steps trigger H.9 and all 20 commit safely.

Because the 20 triggered steps occupy 7.8125% of the 256-step benchmark, the corrected p95 necessarily falls inside that trigger block. On the qualification machine, the unchanged H.28 limit corresponds to about `88381.2 us`. E therefore targets the remaining trigger CPU tail rather than changing the gate.

## Numerical contract that does not change

E does **not** change:

- H.9 finite-difference Newton mathematics;
- 32 finite-difference probes;
- 35 logical hydraulic evaluations per representative trigger;
- Jacobian dimension 32;
- residual definitions or tolerances;
- P060/F040 triggering;
- bounded branch-continuity hysteresis (2% / 5 K);
- the `steam|stop-out|header|turbine-inlet` target set;
- H.20 activation authority;
- H.22 corrected-commit ownership;
- physical coefficients;
- the 10 ms simulated fixed step;
- default `ExplicitCommittedState` production mode.

## Exact CPU optimizations

### 1. Fused branch-continuity inverse-map traversal

The H.13 branch-continuity wrapper previously asked the same `SimplifiedWaterSteamThermodynamicModel` to perform production `Resolve()` and then a second full `DiagnoseInverseBranchSelection()` traversal. E adds an internal same-assembly evaluation that returns exactly the three values the continuity wrapper needs: production state, phase-overlap availability, and the first candidate matching the previous phase.

The public diagnostic API remains unchanged and is still used by the H.13–H.19 audit surface. A unit contract compares the optimized path against an intentionally legacy `Resolve + Diagnose` proxy and requires exact state and decision equality on known steam/stop-out phase-switch cases.

### 2. Exact hydraulic component reuse inside probes

Each hydraulic evaluation now retains an internal snapshot of the pipe/valve/pump component results that produced it. A finite-difference probe may reuse a component result only when every dependency is the exact object reference used by the reference evaluation:

- pipe: both endpoint `FluidNodeState` references unchanged;
- valve: valve-state reference and both endpoint references unchanged;
- pump: pump-state reference and both endpoint references unchanged.

Any changed dependency is solved by the original component solver. Component reduction order and logical hydraulic-evaluation count are unchanged.

### 3. Conservative superheated-candidate rejection

Within the supported saturation interval, a coarse superheated candidate for which `p > psat(T)` by a guarded margin cannot be superheated because the inverse saturation temperature is necessarily above `T`. E rejects only that provable case before the historical inverse saturation-temperature bisection. Boundary-near candidates stay on the historical path.

## Focused gate

The focused E gate runs:

- 64 warmup steps;
- 256 attribution steps with the same 5→0→5 repeated maneuver;
- 128-step determinism control;
- exact-equivalence thermodynamic and hydraulic unit tests;
- the unchanged H.9 solver tests.

It requires 20/20 triggers/commits, 0 rollback/unsafe/fallback violations, 35 logical hydraulic evaluations, 32 probes, dimension 32, exact deterministic fingerprint `518BA948637F0C270C7F8228AB97FEF9148E29A4F5CE4376319AB5D1CFBE7F38`, hydraulic-component exact reuse, preserved D allocation/predictor gains, and a triggered p95 no greater than the frozen H.28 Requalification 1 ceiling of about `88381.2 us`.

If E is green, rerun the original H.28 with its unchanged thresholds. If E is red, keep H.28/H.29 blocked and use the emitted evidence to decide the next optimization; do not weaken H.28.
