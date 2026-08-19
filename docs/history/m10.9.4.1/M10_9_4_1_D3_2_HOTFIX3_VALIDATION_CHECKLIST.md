# M10.9.4.1-D.3.2 Hotfix 3 validation checklist

## Candidate scope

Hotfix 3 is cumulative over D.3.2 Hotfix 2. It changes only the loaded desktop current-v2 main-steam-line resistance from 1,000 to 850 Pa·s²/kg². The synchronization profile remains at 1,000 Pa·s²/kg². PLANT rendering and D.3.2 admission isolation are unchanged.

## Automated gates

- [ ] `dotnet clean`
- [ ] `dotnet restore`
- [ ] `dotnet build --no-restore`
- [ ] `dotnet test --no-build`
- [ ] `scripts\run-turbine-admission-authority-audit.cmd`
- [ ] `scripts\run-turbine-governor-actuator-tracking-audit.cmd`
- [ ] `scripts\run-gameplay-long-tests.cmd`
- [ ] `scripts\run-operational-envelope-audit.cmd`

## Required evidence

- [ ] Loaded desktop main-steam-line resistance is exactly 850 Pa·s²/kg².
- [ ] Synchronization main-steam-line resistance remains exactly 1,000 Pa·s²/kg².
- [ ] Initial effective stage flow remains within 12.5–30 kg/s.
- [ ] Ten-second shaft power remains above 4.5 MW.
- [ ] Ten-second gross electrical output remains above 4 MWe.
- [ ] Closed control valve still enforces zero effective stage admission.
- [ ] Breaker-open rotor reaches a protection-clear controllable speed band and completes the ±10 rpm audit.
- [ ] PLANT remains visually uniform and interactive.

## Status

Candidate until the user confirms every required gate.
