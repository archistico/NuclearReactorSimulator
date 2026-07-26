# Reference Plant Scale Evidence

## Status

**M10.9.4.1-E.2 HOTFIX 1 — VALIDATED EVIDENCE**

## Configured current-v2 values

| Quantity | Validated value |
|---|---:|
| Requested sustained electrical power | 5 MWe |
| Generator maximum electrical power | 10 MWe |
| Generator efficiency | 98% |
| Rotor moment of inertia | 1,000 kg·m² |
| Rated rotor speed | 3,000 rpm |
| Overspeed threshold | 3,300 rpm |
| Full-load governor reference rise | 1.5 rpm |
| Maximum synchronizing correction | 0.5 MW |
| Frequency damping at 1 Hz slip | 2 MW |
| Maximum synchronization frequency difference | 0.2 Hz |
| Coupling mode | Bidirectional |

## Derived evidence

| Derived quantity | Result | Interpretation |
|---|---:|---|
| Rated angular speed | 314.159 rad/s | 3,000 rpm in SI units |
| Stored rotational energy | 49.348 MJ | `0.5 × I × ω²` |
| Requested load fraction | 0.5 | 5 MWe is 50% of nameplate |
| Droop rise at 5 MWe | 0.75 rpm | 1.5 rpm × 0.5 |
| Maximum generating shaft power | 10.2040816 MW | 10 MWe / 0.98 |
| Maximum motoring shaft delivery | 9.8 MW | 10 MWe × 0.98 |
| Inertia constant | 4.934802 s | stored rotor energy / 10 MWe |
| Rotor acceleration per 1 MW imbalance | 30.396 rpm/s | local constant-power approximation at rated speed |
| Time from 3,000 to 3,300 rpm at +1 MW | 9.8696 s | bounded analytical scale evidence |
| Time from 3,000 to 3,300 rpm at +5 MW | 1.9739 s | same approximation |
| Maximum synchronizing correction / nameplate | 5% | 0.5 MW / 10 MWe |
| Maximum synchronizing correction / 5 MWe request | 10% | 0.5 MW / 5 MWe |
| Frequency damping at 0.2 Hz tolerance | 0.4 MW | 2 MW/Hz × 0.2 Hz |

## Governor authority map

| Requested load | Fraction of 10 MWe nameplate | Droop reference rise |
|---:|---:|---:|
| 0 MWe | 0% | 0 rpm |
| 5 MWe | 50% | 0.75 rpm |
| 10 MWe | 100% | 1.5 rpm |

The 5 MWe operating point therefore preserves the same 0.75 rpm governor displacement validated before the migration.

## Signed conversion examples

At the electrical nameplate:

- generation: approximately `+10.2040816 MW` mechanical → `+10 MWe` electrical + `0.2040816 MW` loss;
- motoring: `-10 MWe` electrical import → `-9.8 MW` shaft delivery + `0.2 MW` loss.

The common closure equation is:

```text
mechanical exchange - electrical exchange - conversion loss = 0
```

## Automated evidence

The dedicated explicit script is:

```text
scripts\run-reference-plant-scale-audit.cmd
```

The validated E.2 source contains **4 explicit scale tests**:

1. coordinated 10 MWe scale evidence;
2. current-v2 ownership with bidirectional coupling;
3. legacy 1,000 MWe / generation-only compatibility;
4. 0–10 MWe request clamp and signed HMI ranges.

Focused ordinary generator/grid evidence is available through:

```text
scripts\run-generator-grid-bidirectional-tests.cmd
```

The user confirmed local compilation and all requested ordinary, focused and long-running gates passed on 2026-07-26. E.3.1 recorded and validated the dynamic signed trajectories; the complete bundle now governs E.3.2 threshold selection.
