# New Chat Start — Nuclear Reactor Simulator

Use this file together with `PROJECT_HANDOFF.md` as the authoritative continuation checkpoint.

## Current state

- **Validated continuation baseline:** M10.9.4.1-F.1.
- **Working source:** M10.9.4.1-F.2 Conservative Main-Steam Header Relief CANDIDATE.
- F.1 compilation, ordinary tests, focused tests and explicit capacity audit passed on 2026-07-26.
- Reviewed F.1 evidence: critical ratio `0.545728`, capacity `0.788008677 kg/s` per `100 mm²`, monotonic flow and stable choked plateau.

## F.2 candidate

F.2 adds one optional current-v2 main-steam relief boundary from `header` to `atmospheric-relief-receiver`:

```text
set pressure          6.5 MPa
full-lift pressure    6.7 MPa
full-open area        1,600 mm²
receiver pressure     standard atmosphere
```

The relief reads committed header state, applies stateless pressure lift, limits effective area by vapor availability, invokes the validated F.1 capacity solver and contributes one matching mass/internal-energy removal plus external exchange before the existing canonical plant-network commit.

Legacy profiles expose no relief boundary.

## Do not broaden F.2

Do not add turbine bypass, condenser receiver inventory, manual relief control, valve travel/hysteresis, alarms, protection, wet-steam critical flow, HMI changes or enthalpy migration in this candidate.

## Validation

```bat
dotnet build
scripts\run-main-steam-relief-tests.cmd
dotnet test
```

Then execute the cumulative gates in `M10_9_4_1_F2_VALIDATION_CHECKLIST.md` and review the generated F.2 summary/CSV.

## Next action

Promote F.2 only after all gates pass. Then implement F.3 as a separate turbine-bypass topology candidate. Phase G remains the sole owner of the later flow-work/enthalpy migration.
