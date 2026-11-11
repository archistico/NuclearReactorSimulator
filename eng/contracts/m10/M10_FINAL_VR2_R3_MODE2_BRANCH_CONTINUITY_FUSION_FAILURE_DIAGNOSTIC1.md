# M10 Final VR2 — R3 Mode-2 Branch-Continuity Fusion Failure Diagnostic 1

R3 execution returned `R3-SHADOW-COMPOSITION-BLOCKING` in the integrated mode-2 shadow at `turbine-inlet`.

Returned state:

- specific volume: `0.048580627845180926 m^3/kg`
- specific internal energy: `2525533.2846314958 J/kg`
- exception: `WaterSteamStateOutOfRangeException`
- top frame: `SimplifiedWaterSteamThermodynamicModel.EvaluateBranchContinuity`, line 199
- canonical exact-v9 vs mode-1 shadow baseline: `128/128` fingerprints equal

Static inspection shows that `ThermodynamicBranchContinuityModel` uses the H.28.1-E fused path whenever production and diagnostic providers are the same `SimplifiedWaterSteamThermodynamicModel`. The fused path calls `EvaluateBranchContinuity()` rather than `Resolve()`.

Mode 2 is implemented in `Resolve()` through `ReferenceConsistentTabulatedInverseResolver`; `EvaluateBranchContinuity()` still evaluates the historical/correlation inverse branches. This diagnostic replays the returned conserved state and compares:

1. the internal mode-2 resolver;
2. direct mode-2 `Resolve()`;
3. the same-instance fused branch-continuity wrapper;
4. a distinct-instance non-fused wrapper that still uses mode 2 for production resolution.

No production repair is included or authorized. A PASS only authorizes repair planning.
