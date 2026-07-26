# New Chat Start — Nuclear Reactor Simulator

Continue the Nuclear Reactor Simulator from this exact checkpoint.

## Current truth

- **Validated continuation baseline:** M10.9.4.1-E.3.2 Hotfix 3.
- **Working source:** M10.9.4.1-F.1 Choked Steam-Flow Capacity Law & Audit CANDIDATE.
- E.3.2 compilation, focused tests, ordinary suite and cumulative gates all passed on 2026-07-26.
- Reviewed E.3.2 evidence confirms:
  - normal 5→0→5 MWe: no trip; reverse pickup 0.080 s; underfrequency pickup 0.640 s;
  - turbine trip: reverse pickup 2.000 s; generator trip at step 701 / 7.010 s; breaker opened;
  - breaker-open coastdown: 43.154407 Hz and 6.845593 Hz slip with zero pickup/trip.

## F.1 candidate

F.1 adds:

- `SpecificGasConstant`;
- a typed ideal-vapor compressible steam-flow definition;
- a one-way subcritical/choked capacity solver;
- ordinary equation/validation/scaling tests;
- one explicit pressure-ratio sweep with printed summary and CSV output.

F.1 does **not** add relief or bypass topology, valve commands, plant source terms, two-phase critical flow, enthalpy migration or HMI changes.

## Validation

```text
dotnet build
scripts/run-choked-steam-flow-tests.cmd
dotnet test
scripts/run-electrical-protection-implementation-tests.cmd
scripts/run-electrical-protection-trajectory-audit.cmd
scripts/run-generator-grid-bidirectional-tests.cmd
scripts/run-turbine-admission-authority-audit.cmd
scripts/run-turbine-governor-actuator-tracking-audit.cmd
scripts/run-gameplay-long-tests.cmd
scripts/run-operational-envelope-audit.cmd
scripts/run-reference-plant-scale-audit.cmd
```

Expected ordinary inventory: 970 passed, 27 explicit skipped, 0 failed, 997 total.

## Next after validation

Promote F.1, review its capacity summary, then implement F.2 as one conservative main-steam relief path over the validated capacity seam. Do not combine relief, bypass and enthalpy migration in one candidate.

Use `docs/PROJECT_HANDOFF.md` as the authoritative checkpoint.
