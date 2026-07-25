# M10.9.4.1-D.3.2 Hotfix 2 validation checklist

## Scope

D.3.2 Hotfix 2 is cumulative over D.1, D.2, D.2 Hotfix 1, D.3.1 and D.3.2. It rejects the ineffective Hotfix 1 bias-only hypothesis, restores the loaded desktop control-valve bias to 28% and rebalances only the loaded desktop header-to-stop-out pressure grade (`277.0 °C → 276.7 °C`). D.3.2 admission-train isolation and the uniform PLANT renderer remain unchanged.

## Required automated validation

Run:

```powershell
dotnet clean
dotnet restore
dotnet build --no-restore
dotnet test --no-build
scriptsun-turbine-admission-authority-audit.cmd
scriptsun-turbine-governor-actuator-tracking-audit.cmd
scriptsun-gameplay-long-tests.cmd
scriptsun-operational-envelope-audit.cmd
```

Expected contracts:

- synchronization speed controller remains PI with P=0.5, I=0.02 s^-1 and D=0;
- loaded desktop speed controller remains PID with P=1.0, I=0.02 s^-1 and D=0.2 s;
- both sustained current-v2 profiles start from a 28% control-valve bias;
- loaded desktop stop-valve pressure difference is approximately 150–190 kPa after deterministic seeding;
- stop and control valve capacities are both at least 12.5 kg/s and remain within 0.5 kg/s of one another;
- desktop initial effective stage flow remains within 12.5–30 kg/s;
- desktop 10-second shaft power remains above 4.5 MW and gross electrical output above 4 MWe;
- a closed current-v2 control valve resolves zero pressure-driven stage flow;
- an open admission train retains positive pressure-driven stage capacity;
- breaker-open steam isolation produces zero effective stage flow and zero shaft power before passive-loss deceleration;
- the rotor enters the protection-clear governor-controllable band and the +10/-10 rpm audit completes;
- ordinary, long-running and operational-envelope gates remain green.

## Required manual PLANT validation

Open the PLANT workspace and verify:

- equipment cards use the same engineering-schematic grammar as PRIMARY, TURBINE, GRID, REACTOR and ALARMS;
- process paths, grid, arrows, colors and bottom legend remain unchanged from D.3.2;
- selection still highlights connected paths and updates the IN / OUT / detail panel;
- OPEN SUBSYSTEM still navigates to the mapped workspace.

## Validation status

Candidate. Do not mark D.3.2 Hotfix 2 validated until the user confirms all required gates and the PLANT visual check.
