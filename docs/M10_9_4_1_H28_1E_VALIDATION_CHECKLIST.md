# H.28.1-E Validation Checklist

Status before user validation: **CANDIDATE**.

Run from the repository root:

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-hydraulic-probe-cpu-tail-reduction-audit.cmd
```

Required focused evidence:

- `triggered=20`, `committed=20`;
- zero rollback, unsafe commits and fallback-commit violations;
- 35 logical hydraulic evaluations per trigger;
- 32 finite-difference probes per trigger;
- maximum Jacobian dimension 32;
- non-zero hydraulic-component exact-reference reuse and reuse fraction >= 0.50;
- Jacobian average wall <= 40% of validated H.28.1-D;
- H.9 average wall <= 40% of validated H.28.1-D;
- trigger-engine average wall <= 35% of validated H.28.1-D;
- triggered p95 <= `88381.2 us`, the frozen H.28 Requalification 1 `explicit p95 * 12` ceiling;
- Jacobian and H.9 allocations <= 125% of validated D;
- non-trigger predictor <= 150% of validated D;
- deterministic fingerprint exactly `518BA948637F0C270C7F8228AB97FEF9148E29A4F5CE4376319AB5D1CFBE7F38`.

The report is written before final performance assertions so a failed gate still leaves diagnostic evidence under `artifacts\h28-1e-hydraulic-probe-cpu-tail-reduction`.

Do not promote H.28 or start H.29 from an E-only result. A green E authorizes only a rerun of the unchanged H.28 performance/soak gate.

## Hotfix 1 compile repair

- [ ] `SemiImplicitHydraulicEvaluation.cs` imports `NuclearReactorSimulator.Domain.Physics.Fluids`.
- [ ] The first H.28.1-E build failure is reproduced/documented as exactly three CS0246 symbols: `FluidNodeState`, `ValveState`, `PumpState`.
- [ ] No H.28.1-E performance or numerical contract changed as part of Hotfix 1.

## Hotfix 2 compile repair

- [ ] `probeHydraulicComponentReuse` is threaded from the outer H.9 invocation through all four finite-difference helper levels.
- [ ] Both positive and negative signed-probe paths pass the counter.
- [ ] Modified helper call/declaration arities remain 12/12, 18/18, 17/17 and 13/13.
- [ ] No numerical/performance gate contract changed as part of Hotfix 2.
