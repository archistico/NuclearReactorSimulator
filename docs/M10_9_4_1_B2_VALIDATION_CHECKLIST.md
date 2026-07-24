# M10.9.4.1-B.2 — Validation Checklist

## Candidate

**Drum-to-main-steam pressure/energy/inventory source closure**

B.1 is the last user-validated checkpoint. B.2 must not be promoted until every gate below is green in the local .NET 10 environment.

## 1. Clean build

```text
dotnet clean
dotnet restore
dotnet build --no-restore
```

Required: 0 warnings, 0 errors.

## 2. Ordinary suite

```text
dotnet test --no-build
```

Required: no unexpected failures. Explicit long-running categories may remain skipped by the ordinary filter.

## 3. Focused B.2 regressions

Verify the steam-drum/main-steam tests cover all of these contracts:

- no current-v2 steam source without return-energy surplus or separable vapor inventory;
- increasing positive-return energy increases available steam generation monotonically;
- drum-to-steam-outlet pressure head independently limits actual source flow;
- source transfer is internally mass/energy conservative;
- main-steam demand is not used to calculate the source;
- historical v1 / null-source behavior remains unchanged;
- B.1 liquid-inventory limits remain green.

## 4. Explicit 60-second journeys

```text
scripts\run-gameplay-long-tests.cmd
```

Required: sustained generation and synchronization journeys pass without thermodynamic exception, inventory depletion or unexpected trip.

## 5. 300-second operational-envelope audit

```text
scripts\run-operational-envelope-audit.cmd
```

Required:

- full 300 s journey passes;
- no automatic trip at the 5 MWe steady point;
- mass/energy closure remains within existing accepted tolerances;
- drum, steam outlet, condenser, turbine and pump trajectories remain finite;
- no evidence of monotonic steam-outlet depletion or unbounded drum accumulation.

## 6. Manual evidence to capture

Record at minimum:

- drum pressure and level range;
- `SeparatedSteamMassFlowRate` range;
- `SteamSourcePressureDrivenCapacityMassFlowRate` range;
- `SteamSourceAvailableMassFlowRate` range;
- active pressure-limited/availability-limited transitions;
- main-steam-line flow range;
- turbine stage flow and electrical output range.

Do not retune protection thresholds to pass this gate. Any required source resistance or seed change must be justified from the measured source/line evidence and isolated in a new hotfix.
