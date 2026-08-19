# M10.9.4.1-D.3.2 validation checklist

## Scope

D.3.2 is cumulative over D.1, D.2, D.2 Hotfix 1 and D.3.1. It closes the discovered pressure-driven-stage bypass of the physical admission train, corrects the synchronization-controller contract test, and restores a uniform PLANT engineering-schematic visual language.

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

Candidate. Do not mark D.3.2 validated until the user confirms all required gates and the PLANT visual check.
