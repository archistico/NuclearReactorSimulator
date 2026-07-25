# ADR 0110 — Current-v2 reference plant is 10 MWe with bidirectional grid coupling

## Status

Proposed design / M10.9.4.1-E.2. Not implemented in the validated D.4 source.

## Context

E.1 selected the reduced-scale educational interpretation rather than a dimensionally representative 1 GWe unit. E.1 accepted a 10 MWe target because the existing 1,000 kg·m², 3,000 rpm rotor stores 49.348 MJ and therefore has an inertia constant of about 4.935 s at 10 MWe, while the validated sustained operating point is 5 MWe.

The active pre-migration current-v2 model still uses a 1,000 MW generator nameplate, a 150 rpm full-load droop rise and a generation-only clamp in the infinite-bus coupling. At 5 MWe this happened to produce only 0.75 rpm of droop displacement because 5 MWe was 0.5% of the old nameplate. Changing only the nameplate would have made 5 MWe a 50% point and inflated the same droop setting to 75 rpm.

The current generation-only clamp also prevents the grid from applying motoring torque when a connected rotor fell below synchronous speed.

## Decision

When E.2 is implemented, the two current-v2 sustained reference profiles will follow this proposed contract:

1. Generator maximum electrical power is 10 MWe.
2. The normal 5 MWe sustained point is 50% of nameplate.
3. Rotor inertia remains 1,000 kg·m² at 3,000 rpm, giving `H ≈ 4.934802 s` at 10 MWe.
4. Governor full-load speed-reference rise is 1.5 rpm. This preserves the previously validated 0.75 rpm displacement at the 5 MWe normal point and deliberately avoids retuning governor behavior during the scale migration.
5. `SynchronousGridPowerFlowMode` is versioned. The default is `GenerationOnly`; current-v2 sustained profiles opt into `Bidirectional`.
6. Bidirectional coupling permits signed shaft exchange:
   - positive shaft exchange = generator loading / electrical export;
   - negative shaft exchange = grid motoring / electrical import.
7. Generating shaft power is limited by `P_e,max / efficiency`; motoring shaft delivery is limited by `P_e,max * efficiency`, so the electrical nameplate remains ±10 MWe.
8. Bidirectional coupling converts commanded signed power to torque using current electrical speed near synchronous operation rather than always using rated speed. A 10% rated-speed floor prevents a singular torque demand before E.3 loss-of-synchronism protection exists.
9. Signed electrical power is positive for export and negative for import. Conversion loss is positive in both directions.
10. The active 0.5 MW maximum synchronizing correction and 2 MW/Hz frequency damping require an explicit E.2 retain-or-retune decision. They are not ratio-scaled automatically; their dynamic suitability is a validation item.
11. Current-v2 HMI electrical-output ranges are signed -10..+10 MWe. `LOAD RAISE/LOWER` remains ±5 MWe per accepted command and clamps requested load to 0..10 MWe.
12. Legacy/default definitions preserve the historical 1,000 MW default and generation-only coupling semantics.

## Consequences

- Reverse-power and motoring states become physically representable and deterministic, but E.2 does not yet add dedicated reverse-power, underfrequency or loss-of-synchronism protection. Those remain E.3.
- The 5 MWe sustained trajectory should remain near-bumpless because its droop displacement is intentionally preserved.
- Negative electrical output must be interpreted as grid import, not as invalid generation.
- Performance ratios that only make sense during export are not reported as generation efficiency during motoring.
- Any future change to coupling stiffness must be justified by dynamic evidence, not by simple proportional scaling.
