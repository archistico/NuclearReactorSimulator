# M10 Final VR2 — R3 Mode-2 Branch-Continuity Fusion Repair Planning 1

## Frozen root cause
`H28.1-E-FUSED-PATH-NOT-MODE2-AWARE`

## Selected repair
**MODE2-FUSION-ELIGIBILITY-GUARD**

Future implementation may change exactly:
- `src/NuclearReactorSimulator.Simulation/Physics/Fluids/SimplifiedWaterSteamThermodynamicModel.cs`
- `src/NuclearReactorSimulator.Simulation/Physics/Fluids/ThermodynamicBranchContinuityModel.cs`

Required design:
1. add an internal legacy-fusion eligibility/capability fact to `SimplifiedWaterSteamThermodynamicModel`, true for modes 0/1 and false for mode 2;
2. add that guard to the existing H.28.1-E same-instance condition;
3. therefore route mode 2 through the existing non-fused path (`_productionModel.Resolve(...)` plus diagnostic provider);
4. preserve H.13 continuity/hysteresis logic.

Forbidden: changes to `EvaluateBranchContinuity()`, the mode-2 resolver, C4 payload, closure enum/default, canonical exact-v9 wiring, thresholds or tolerances.

Repair Implementation 1 must prove the returned failure state resolves in same-instance mode 2 and equals both direct mode 2 and the distinct-instance non-fused path; modes 0/1 must remain fusion-eligible; ordinary Release must pass.

Planning PASS may authorize only `R3-MODE2-BRANCH-CONTINUITY-FUSION-REPAIR-IMPLEMENTATION1`. An implementation PASS may authorize only rebased `R3-SHORT-EXACT-V9-EQUIVALENT-SHADOW-COMPOSITION-REQUALIFICATION2`. R4 remains blocked until that 120 s R3 gate passes.
