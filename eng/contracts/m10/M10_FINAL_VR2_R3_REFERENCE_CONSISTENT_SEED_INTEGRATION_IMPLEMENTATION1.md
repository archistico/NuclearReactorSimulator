# M10 Final VR2 — R3 Reference-Consistent Seed Integration Implementation 1

## Scope

Implements the returned Planning 1 design exactly:
- generic conserved-inventory authored seed subtype;
- active-closure resolution in `ColdShutdownInitialConditionFactory`;
- separate internal reference-consistent exact-v9-equivalent candidate factory;
- frozen 12-node candidate mass/internal-energy vector;
- canonical exact-v9 method body unchanged.

## Fast gate

Before any 120 s rerun:
- direct raw-vector mode-2 resolution: 12/12 finite and phase-matched;
- candidate factory construction including 20 ms / 2 seed steps;
- first 100 Running steps at 10 ms;
- zero exact-v9 envelope violations;
- zero trips, breaker-open steps and hydraulic rollbacks;
- ordinary Release suite PASS.

A PASS is implementation evidence only. R3 remains RED until Requalification 3.
