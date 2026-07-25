# M10.9.4.1-D.3.2 Hotfix 1 validation checklist — SUPERSEDED

## Status

Superseded by D.3.2 Hotfix 2 after local validation disproved the 30% bias-only hypothesis. Retained for audit history.

## Scope

D.3.2 Hotfix 1 is cumulative over D.1, D.2, D.2 Hotfix 1, D.3.1 and D.3.2. It preserves the admission-train isolation and PLANT schematic corrections, then realigns only the loaded desktop initial control-valve bias from 28% to 30% so the corrected series flow path still satisfies the existing generation-ready mechanical-support gates. The synchronization seed remains at 28%.

## Required automated validation

Run:

```powershell
dotnet clean
dotnet restore
dotnet build --no-restore
dotnet test --no-build
scripts\run-turbine-admission-authority-audit.cmd
scripts\run-turbine-governor-actuator-tracking-audit.cmd
scripts\run-gameplay-long-tests.cmd
scripts\run-operational-envelope-audit.cmd
```

Expected contracts:

- synchronization speed controller is PI with P=0.5, I=0.02 s^-1 and D=0;
- synchronization initial control-valve bias remains 28%;
- loaded desktop PID initial control-valve bias is 30%;
- desktop initial effective stage flow remains within 12.5–30 kg/s;
- desktop 10-second shaft power remains above 4.5 MW and gross electrical output above 4 MWe;
- a closed current-v2 control valve resolves zero pressure-driven stage flow;
- an open admission train retains positive pressure-driven stage capacity;
- breaker-open steam isolation produces zero effective stage flow and zero shaft power before passive-loss deceleration;
- the rotor enters the protection-clear governor-controllable band and the +10/-10 rpm audit completes;
- ordinary, long-running and operational-envelope gates remain green.

## Required manual PLANT validation

Open the PLANT workspace and verify:

- equipment cards use the same compact engineering-schematic grammar as PRIMARY, TURBINE, GRID, REACTOR and ALARMS;
- process paths use the same grid, line weight, arrows and medium colors;
- live path values are presented in the bottom legend rather than overlapping the diagram;
- selecting equipment still highlights connected paths and updates the canonical IN / OUT / detail panel;
- OPEN SUBSYSTEM still navigates to the mapped existing workspace.

## Validation status

Superseded / not validated. Use the D.3.2 Hotfix 2 checklist.
