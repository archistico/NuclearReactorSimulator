# M10.9.4.1-A — Extended Operating-Envelope Audit

## Status

**AUDIT EXECUTED — FAILURE ATTRIBUTED — A.2 HOTFIX 1 CANDIDATE**

**Validated prerequisite:** M10.9.4 — Subsystem Engineering Schematics, including Hotfix 23.

Hotfix 1 corrected only the invariant diagnostic formatting compile error. The user subsequently confirmed that the solution compiles and the ordinary tests pass. The separately filtered explicit operational-envelope run repeatedly fails because the intended 300-second steady/5 MWe journey trips near 70 simulated seconds. A.2 Hotfix 1 is now the current correction candidate; validation is pending.

## Boundary

The original Phase A changed no production physics. A.2 Hotfix 1 changes only two current-v2 installed-capacity values: cooling-boundary ceiling 24.5→40 MW and maximum condensation flow 15→20 kg/s. `UA = 1.225 MW/K`, 20 °C cooling water, solver equations, protection thresholds, controller gains, external timestep, replay ordering, canonical ownership and all legacy/v1 seeds remain unchanged.

The only production-facing addition is an internal read-only test seam exposing the already-created latest canonical automatic-operation snapshot to `NuclearReactorSimulator.Application.Tests`. It is not public and cannot be consumed by Avalonia or other presentation code.

## Added explicit audit journeys

`OperationalEnvelopeExtendedAuditTests` adds separately runnable evidence for:

- 300 simulated seconds at the intended parallel 5 MWe point;
- deterministic generator load raise/lower and same-seed equivalence;
- breaker-open load rejection;
- generator-trip load rejection;
- turbine-trip load rejection;
- condenser-cooling degradation at 25% available capacity;
- per-step current-v2 condensate/feedwater pump non-return sampling after load rejection;
- mass, energy, balance-rate and balance-power closure;
- drum pressure and level envelope;
- condenser pressure envelope;
- turbine rotor-speed and generator-frequency envelope;
- protection state and exact latched protection-function evidence;
- turbine-stage flow, actual/inventory/thermal condensation limits, heat-rejection capacity, surface-transfer limit and exhaust mass;
- 120-second recording with 40/80/120-second checkpoints, full replay and checkpoint seek verification;
- wall-clock execution evidence for the 300-second journey under a deliberately broad audit ceiling.

Failures are evidence. Thresholds must not be weakened to make a journey green.

## Observed long-run failure — 24 July 2026

The explicit run reported:

```text
Test: DesktopFiveMWePoint_SustainsThreeHundredSecondsWithConservationAndPerformanceEvidence
Failure boundary: checkpoint 7/30
Logical step: 7000
Approximate simulated time: 70 s
Samples captured: 8 (initial plus 7 ten-second checkpoints)
Latched actions: TurbineTrip, GeneratorTrip

Maximum mass closure residual:        7.276E-012 kg
Maximum energy closure residual:      1.344E-005 J
Maximum balance mass-rate residual:   8.882E-015 kg/s
Maximum balance power residual:       3.679E-008 W
Drum pressure sampled range:          2.963 .. 7.821 MPa
Drum level sampled range:             96.708 .. 100 %
Condenser pressure sampled range:     7.386 .. 28.593 kPa
Rotor speed sampled range:            2999.228 .. 3000.241 rpm
Generator frequency sampled range:    49.987 .. 50.004 Hz
Minimum sampled pump flows:           13.143 / 0 kg/s
```

Filtered-run summary:

```text
226 discovered
9 passed
1 failed
216 skipped
elapsed approximately 7 min 44 s
```

## Interpretation

The failure is not a conservation breakdown: all reported closure residuals are many orders of magnitude below their audit ceilings.

It is an operating-envelope/control/protection failure:

- the journey fails at checkpoint 7, not at the nominal 300-second endpoint;
- drum pressure rises materially and level approaches the upper bound;
- condenser pressure approaches the 30 kPa current-v2 turbine/generator trip threshold;
- feedwater-pump sampled flow reaches zero;
- rotor speed and frequency remain close to nominal in the ten-second samples;
- the latched action pair could be produced by turbine overspeed or condenser high backpressure, but the observed rotor maximum of 3000.241 rpm excludes the 3300 rpm overspeed threshold; generator overfrequency is also excluded by 50.004 Hz and would not issue turbine trip.

The sampled maximum condenser pressure of 28.593 kPa does **not** prove that the 30 kPa trip threshold was never crossed. Protection is evaluated every committed step, while this audit samples the operating envelope only every 1,000 steps / 10 seconds. A short intra-checkpoint pressure excursion can trip and latch the protection, after which the pressure may fall before the next audit sample.

Therefore `condenser-high-backpressure` is the initiating function: the unchanged 30 kPa threshold was crossed between ten-second observations and then latched. A.2 adds one-second direct function evidence so future reports show the function and measurement explicitly.

The label `steady / 5 MWe` is therefore aspirational rather than demonstrated. The trajectory shows substantial slow drift even though conservation remains closed.

## M10.9.4.1-A.1 evidence completion and A.2 correction

A.2 Hotfix 1 implements the first high-value A.1 evidence items:

- one-second protection-function latch/measurement evidence;
- one-second condenser pressure, rotor speed and frequency sampling;
- stage flow plus condenser actual, inventory-limited and thermal-limited flow;
- effective heat-rejection capacity, surface-transfer limit and exhaust mass.

A.2 also preserves the 24.5 MW initial surface-transfer point while raising installed current-v2 headroom to 40 MW and maximum condensation flow to 20 kg/s. Controller tracking, suction/check-valve transition counts and final-window slopes remain follow-on evidence after this candidate is run.

See `M10_9_4_1_EXTERNAL_TECHNICAL_AUDIT_REVIEW.md` and `OPERATIONAL_ENVELOPE_NUMERICAL_HARDENING_PLAN.md`.

## Running

First run the ordinary gate and the unchanged 60-second gameplay gate:

```text
dotnet clean
dotnet restore
dotnet build --no-restore
dotnet test --no-build
scripts\run-gameplay-long-tests.cmd
```

Then run only the M10.9.4.1-A audit:

```text
scripts\run-operational-envelope-audit.cmd
```

PowerShell:

```text
.\scripts\run-operational-envelope-audit.ps1
```

Direct invocation:

```text
dotnet test --project tests/NuclearReactorSimulator.Application.Tests/NuclearReactorSimulator.Application.Tests.csproj --no-build -- --explicit only --filter-trait "Category=OperationalEnvelopeAudit" --parallel none
```

## Promotion gate

M10.9.4.1-A cannot be marked validated while the unexplained trip remains. Promotion requires:

```text
clean restore/build succeeds with warnings as errors
complete ordinary suite passes
existing GameplayLong explicit trait passes
OperationalEnvelopeAudit explicit trait passes
no unexplained protection action in the intended healthy reference journey
replay/checkpoint verification passes
all audit evidence is documented and assigned
```

A.2 is not a threshold relaxation: the 30 kPa trip remains unchanged. Promotion requires the full healthy 300-second journey and all other gates to pass with the new direct diagnostics.
