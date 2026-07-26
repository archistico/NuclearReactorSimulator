# ADR 0110 — Current-v2 reference plant is 10 MWe with bidirectional grid coupling

## Status

Accepted and validated in M10.9.4.1-E.2 Hotfix 1 on 2026-07-26.

## Context

E.1 selected a reduced-scale educational plant rather than a dimensionally representative 1 GWe unit. The existing 1,000 kg·m², 3,000 rpm rotor stores approximately 49.348 MJ, giving an inertia constant of about 4.935 s at 10 MWe. The validated sustained operating point remains 5 MWe.

The validated parent used a historical 1,000 MWe nameplate and 150 rpm full-load governor rise. At 5 MWe those values happened to produce a 0.75 rpm reference displacement because the requested load was only 0.5% of nameplate. A nameplate-only change would therefore have created an unintended 75 rpm displacement. The generation-only coupling also could not apply motoring torque when a connected rotor was below synchronous speed.

## Decision

The two current-v2 sustained reference profiles use this coordinated contract:

1. Generator maximum electrical power is 10 MWe.
2. The normal 5 MWe point is 50% of nameplate.
3. Rotor inertia remains 1,000 kg·m² at 3,000 rpm, giving `H ≈ 4.934802 s`.
4. Full-load governor speed-reference rise is 1.5 rpm, preserving the validated 0.75 rpm displacement at 5 MWe.
5. `SynchronousGridPowerFlowMode` defaults to `GenerationOnly`; current-v2 sustained profiles opt into `Bidirectional`.
6. Positive shaft/electrical exchange represents generation/export; negative exchange represents grid motoring/import.
7. Generating shaft absorption is limited by `P_e,max / efficiency`; motoring shaft delivery is limited by `P_e,max × efficiency`, preserving an electrical range of ±10 MWe.
8. Bidirectional power-to-torque conversion uses current rotor speed with a 10% rated-speed floor. Null and generation-only paths retain historical rated-speed conversion.
9. Conversion loss remains positive in both directions.
10. The existing 0.5 MW maximum synchronizing correction and 2 MW/Hz frequency damping are retained deliberately until trajectory evidence justifies a change.
11. Current-v2 HMI electrical ranges are -10..+10 MWe. LOAD RAISE/LOWER remains 5 MWe per accepted command and clamps requested load to 0..10 MWe.
12. Historical/default definitions retain the 1,000 MWe default, null/GenerationOnly coupling and non-negative presentation range.

## Consequences

- Motoring and reverse-power states become deterministic representable states.
- The 5 MWe trajectory should remain near-bumpless because its governor displacement is preserved.
- Negative electrical exchange means grid import, not invalid generation.
- E.2 does not add reverse-power, supervised-underfrequency or loss-of-synchronism protection; E.3.1 records trajectories and E.3.2 remains evidence-gated.
- Coupling stiffness changes require measured trajectory evidence rather than proportional scaling.
