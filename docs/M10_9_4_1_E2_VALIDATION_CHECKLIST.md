# M10.9.4.1-E.2 Validation Checklist

## Candidate identity

**Milestone:** M10.9.4.1-E.2 — 10 MWe Reference Scale & Bidirectional Grid Coupling
**Validated parent:** M10.9.4.1-D.4.1
**Status:** VALIDATED on 2026-07-26 after the user confirmed compilation and all requested ordinary, focused and long-running gates passed.

## Focused gate

Run from the repository root:

```powershell
scripts\run-generator-grid-bidirectional-tests.cmd
```

Confirm:

- current-v2 sustained profiles own the 10 MWe nameplate;
- 5 MWe is 50% load;
- full-load governor rise is 1.5 rpm and the 5 MWe displacement remains 0.75 rpm;
- legacy/default profiles remain 1,000 MWe and generation-only;
- a slow connected rotor can be motored only in bidirectional mode;
- generation-only coupling cannot produce negative rotor load;
- public/manual rotor input still rejects negative torque;
- motoring is bounded to -10 MWe electrical import;
- conversion loss remains positive and power closure remains satisfied;
- current-v2 HMI ranges are -10..+10 MWe;
- LOAD RAISE clamps at 10 MWe.

## Complete automated gate

```powershell
dotnet test
scripts\run-turbine-admission-authority-audit.cmd
scripts\run-turbine-governor-actuator-tracking-audit.cmd
scripts\run-gameplay-long-tests.cmd
scripts\run-operational-envelope-audit.cmd
scripts\run-reference-plant-scale-audit.cmd
```

Expected discovery used during candidate review:

- ordinary: **952 passed / 0 failed / 19 explicit skipped**;
- reference-scale audit: **4/4**;
- unique explicit pack: **19/19**.

The user confirmed the complete requested gate passed. Exact console counts were not copied into this checklist, so no unreported count is inferred.

## Manual GENERATOR-station gate

Verify in the desktop application:

1. the current-v2 electrical scale visibly spans -10 to +10 MWe;
2. positive power is labelled and understood as export;
3. negative power is labelled and understood as import/motoring;
4. requested load remains a non-negative 0–10 MWe operator request;
5. mechanical exchange may become negative without conversion loss becoming negative;
6. historical/default profiles retain their non-negative 0–1,000 MWe scale;
7. breaker open removes electromagnetic exchange;
8. no E.3 protection is implied or displayed as already implemented.

## Promotion rule

E.2 Hotfix 1 is **VALIDATED**. Any later production edit to the scale, coupling, governor, signed torque seam or HMI range reopens the applicable gates.
