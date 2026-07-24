# M10.9.4.1-E.2 validation checklist

## Hotfix 1 prerequisite — signed torque seam

The initial E.2 candidate exposed a contract mismatch: bidirectional `GeneratorGridSolver` could command negative electromagnetic torque for motoring, while the historical public `TurbineRotorInput` constructor rejected negative external-load torque. Hotfix 1 preserves that public/manual legacy restriction and introduces an internal generator/grid-only signed torque seam.

Before accepting E.2, verify specifically:

- `Step_BidirectionalGridCoupling_MotorsSlowConnectedRotorAndKeepsLossesPositive` passes;
- the public `TurbineRotorInput` constructor still rejects negative manually supplied load torque;
- operational-envelope turbine-trip, replay and load-step audits no longer fail with `External turbine load torque cannot be negative`;
- generation-only legacy behavior remains unchanged.

E.2 is a coordinated physical migration. Do not mark it validated from compilation alone.

## 1. Build and ordinary suite

```bat
dotnet clean
dotnet restore
dotnet build --no-restore
dotnet test --no-build
```

Required:

- 0 build errors;
- 0 warnings under the repository policy;
- ordinary suite green.

## 2. Scale-contract regressions

Verify the current-v2 sustained profiles expose:

- generator nameplate = 10 MWe;
- requested sustained point = 5 MWe = 50%;
- rotor inertia = 1,000 kg·m²;
- full-load governor rise = 1.5 rpm;
- 5 MWe droop displacement = 0.75 rpm;
- grid coupling mode = Bidirectional;
- legacy/default generator nameplate remains 1,000 MW where historically defined;
- legacy coupling remains generation-only/default.

## 3. Bidirectional generator/grid unit tests

Required:

- slow connected rotor can receive negative electromagnetic load torque (motoring torque on the rotor);
- signed mechanical shaft exchange is negative in motoring;
- signed electrical export is negative in motoring;
- conversion loss remains positive;
- electrical audit closure remains within tolerance;
- generation-only mode still clamps negative correction at zero.

## 4. HMI and command semantics

Verify manually or by tests:

- current-v2 generator and gross-output scales are -10..+10 MWe;
- 5 MWe is visibly mid-scale;
- LOAD RAISE from 5 MWe requests 10 MWe;
- a further LOAD RAISE remains clamped at 10 MWe;
- LOAD LOWER remains 5 MWe per accepted command;
- negative MWe is described as grid import/motoring rather than an invalid reading.

## 5. Explicit journeys

Run the repository's current long-running gameplay/audit gates using the Microsoft.Testing.Platform/xUnit v3 syntax already established by the scripts:

```bat
scripts\run-gameplay-long-tests.cmd
scripts\run-operational-envelope-audit.cmd
scripts\run-reference-plant-scale-audit.cmd
```

Required:

- 60-second synchronization/load journey green;
- 300-second sustained operating-envelope journey green physically;
- no unexpected trip at the 5 MWe normal point;
- mass/energy audit remains within established tolerances;
- rotor remains near synchronous speed while paralleled;
- no unexpected persistent motoring at the healthy 5 MWe point.

The historical wall-clock performance-budget observation remains tracked separately and must not be confused with a physical failure.

## 6. Migration acceptance

E.2 may be marked locally validated only when all above gates are green. E.3 reverse-power, supervised-underfrequency and loss-of-synchronism protection must not begin before E.2 is green.
