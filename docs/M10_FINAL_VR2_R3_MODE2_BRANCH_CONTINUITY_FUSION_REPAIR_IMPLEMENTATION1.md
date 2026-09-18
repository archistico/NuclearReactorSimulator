# M10 Final VR2 — R3 Mode-2 Branch-Continuity Fusion Repair Implementation 1

## Scope
Implements the Planning 1 selected repair `MODE2-FUSION-ELIGIBILITY-GUARD`.

Production change is intentionally limited to:

1. `SimplifiedWaterSteamThermodynamicModel`: expose an internal legacy-fusion eligibility fact, true for modes 0/1 and false for mode 2.
2. `ThermodynamicBranchContinuityModel`: add that eligibility to the existing H.28.1-E same-instance fused condition.

No thermodynamic equation, C4 resolver, payload, enum/default, H.13 decision policy or exact-v9 wiring is changed.

## Required evidence
The implementation gate must prove the returned `turbine-inlet` failure state now resolves through the repaired same-instance wrapper and equals direct mode 2 and the distinct-instance non-fused result exactly. It must also prove modes 0/1 remain fusion-eligible and optimized/non-fused equivalent, then pass the full ordinary Release suite.

A PASS qualifies only this repair implementation. It does not close R3; the 120 s / 12,000-step R3 requalification remains mandatory before R4 planning.
