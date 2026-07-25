# Reference Plant Scale Evidence

## Status

**A.3 CURRENT PRE-E RUNTIME EVIDENCE — E.1 TARGET ACCEPTED, E.2 DEFERRED**

The active M10.9.4.1-D.4 source still uses the hybrid pre-migration scale. The tables below are therefore current runtime evidence, not merely historical data. E.1 accepts a future 10 MWe educational target, but no E.2 nameplate, droop, bidirectional-coupling or signed-HMI migration is present in the validated source.

## Active configured values

| Quantity | Active current-v2 value |
|---|---:|
| Requested electrical power | 5 MWe |
| Generator maximum electrical power | 1,000 MW |
| Generator efficiency | 98% |
| Rotor moment of inertia | 1,000 kg·m² |
| Rated rotor speed | 3,000 rpm |
| Overspeed threshold | 3,300 rpm |
| Full-load governor reference rise | 150 rpm |
| Maximum synchronizing correction | 0.5 MW |
| Frequency damping at 1 Hz slip | 2 MW |
| Maximum synchronization frequency difference | 0.2 Hz |

## Derived evidence

| Derived quantity | Result | Interpretation |
|---|---:|---|
| Rated angular speed | 314.159 rad/s | 3,000 rpm in SI units |
| Stored rotational energy | 49.348 MJ | `0.5 × I × ω²` |
| Current requested load fraction | 0.005 | 5 MWe is 0.5% of the configured nameplate |
| Droop rise at 5 MWe | 0.75 rpm | 150 rpm × 0.005 |
| Maximum mechanical power from efficiency limit | 1,020.408 MW | 1,000 MW / 0.98 |
| Inertia constant at configured nameplate | 0.049348 s | stored rotor energy / 1,000 MW |
| Inertia constant at accepted 10 MWe target | 4.934802 s | same rotor referred to 10 MWe |
| Rotor acceleration per 1 MW imbalance | 30.396 rpm/s | local constant-power approximation at rated speed |
| Time from 3,000 to 3,300 rpm at +1 MW | 9.8696 s | bounded analytical scale evidence |
| Time from 3,000 to 3,300 rpm at +5 MW | 1.9739 s | same approximation |
| Maximum synchronizing correction / nameplate | 0.05% | 0.5 MW / 1,000 MW |
| Maximum synchronizing correction / 5 MWe request | 10% | 0.5 MW / 5 MW |
| Frequency damping at 0.2 Hz tolerance | 0.4 MW | 2 MW/Hz × 0.2 Hz |

## Governor authority map under current normalization

| Requested load | Fraction of 1,000 MW nameplate | Droop reference rise |
|---:|---:|---:|
| 0 MW | 0% | 0 rpm |
| 5 MW | 0.5% | 0.75 rpm |
| 10 MW | 1% | 1.5 rpm |
| 100 MW | 10% | 15 rpm |
| 1,000 MW | 100% | 150 rpm |

The law is mathematically active, but the 5 MWe operating point receives only 0.75 rpm of displacement because the denominator remains 1,000 MW.

## E.1 interpretation

Static evidence favors a reduced-scale educational unit because the existing rotor yields a conventional multi-second inertia constant near a 10 MWe reference and the turbine/condenser path is already low-megawatt in scale. E.1 therefore accepts 10 MWe as the migration target.

That decision does not make the following values active:

- 10 MWe runtime nameplate;
- 1.5 rpm current-v2 full-load droop;
- bidirectional coupling;
- signed electrical output;
- -10..+10 MWe HMI ranges.

They remain coordinated E.2 work.

## Automated evidence

The dedicated script is:

```text
scriptsun-reference-plant-scale-audit.cmd
```

On 2026-07-25 it passed **2/2** explicit tests:

- `ReferencePlantScaleAuditTests.CurrentV2_ReferencePlantScaleEvidence_IsExplicitAndReproducible` freezes the active 1,000 MW / 150 rpm / 0.5 MW / 2 MW-per-hertz contract and its derived consequences;
- `ReferencePlantScaleMigrationTests.CurrentV2_GridCouplingRetainsThePresentCorrectionOnlyContractPendingPhaseE` explicitly proves that bidirectional migration remains deferred.

Any E.2 implementation must update code, contract, evidence, ADRs and tests in the same candidate.
