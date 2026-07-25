# Reference Plant Scale Evidence

## Status

**A.3 PRE-MIGRATION EVIDENCE — SUPERSEDED FOR CURRENT-V2 BY E.1/E.2 CANDIDATE**

**Current source basis:** E.1 accepted target + E.2 bidirectional migration + signed-torque Hotfix 1.

The A.3 tables below preserve the evidence that exposed the old hybrid scale. They are historical pre-migration values, not the current-v2 runtime contract. The current candidate uses 10 MWe, a 5 MWe/50% normal point, 1.5 rpm full-load droop rise and bidirectional signed coupling; see `REFERENCE_PLANT_SCALE_MIGRATION_PLAN.md`.

## Current candidate evidence

| Quantity | Current-v2 candidate |
|---|---:|
| Generator maximum electrical power | 10 MWe |
| Normal sustained point | 5 MWe / 50% |
| Rotor inertia / rated speed | 1,000 kg·m² / 3,000 rpm |
| Inertia constant | 4.934802 s |
| Full-load governor reference rise | 1.5 rpm |
| Droop displacement at 5 MWe | 0.75 rpm |
| Grid power-flow mode | Bidirectional |
| Electrical presentation range | -10..+10 MWe |

The focused reference-scale/migration audit passed 2/2 tests on 2026-07-25. Long-running promotion evidence remains pending.

## Historical pre-migration configured values

| Quantity | Current value |
|---|---:|
| Requested electrical power | 5 MW |
| Generator maximum electrical power | 1,000 MW |
| Generator efficiency | 98% |
| Rotor moment of inertia | 1,000 kg·m² |
| Rated rotor speed | 3,000 rpm |
| Overspeed threshold | 3,300 rpm |
| Full-load governor reference rise | 150 rpm |
| Maximum synchronizing correction | 10 MW |
| Frequency damping at 1 Hz slip | 10 MW |
| Maximum synchronization frequency difference | 0.2 Hz |
| A.2 installed condenser cooling ceiling | 40 MW |
| A.2 maximum condensation flow | 20 kg/s |

## Historical derived evidence

The audit derives the following values directly from the canonical current-v2 definitions:

| Derived quantity | Result | Interpretation |
|---|---:|---|
| Rated angular speed | 314.159 rad/s | 3,000 rpm expressed in SI rotational units |
| Stored rotational energy at rated speed | 49.348 MJ | `0.5 × I × ω²` |
| Current requested load fraction | 0.005 | 5 MW is 0.5% of the configured 1,000 MW nameplate |
| Droop reference rise at 5 MW | 0.75 rpm | 150 rpm multiplied by the 0.005 load fraction |
| Maximum mechanical power from efficiency limit | 1,020.408 MW | 1,000 MW / 0.98 |
| Inertia constant at configured 1,000 MW nameplate | 0.049348 s | Stored rotor energy divided by configured rating |
| Inertia constant at a 10 MW reduced-scale rating | 4.934802 s | Same rotor referred to a candidate educational rating |
| Rotor acceleration per 1 MW torque imbalance | 30.396 rpm/s | Local constant-power approximation at rated speed |
| Time from 3,000 to 3,300 rpm at +1 MW imbalance | 9.8696 s | Ignores controller, damping and changing speed; scale evidence only |
| Time from 3,000 to 3,300 rpm at +5 MW imbalance | 1.9739 s | Same bounded approximation |
| Maximum synchronizing correction / nameplate | 1% | Small relative to configured 1,000 MW rating |
| Maximum synchronizing correction / current request | 200% | Twice the current 5 MW dispatch |
| Frequency damping at 0.2 Hz synchronization tolerance | 2 MW | 40% of current requested output |

## Historical governor authority map under the 1,000 MW normalization

| Requested load | Fraction of 1,000 MW nameplate | Droop reference rise |
|---:|---:|---:|
| 0 MW | 0% | 0 rpm |
| 5 MW | 0.5% | 0.75 rpm |
| 10 MW | 1% | 1.5 rpm |
| 100 MW | 10% | 15 rpm |
| 1,000 MW | 100% | 150 rpm |

The table proves that the implemented 5% full-load droop law is mathematically present but has negligible reference displacement at the currently requested 5 MW because the normalization denominator is 1,000 MW.

## Engineering interpretation

### Option A — retain a 1,000 MW-class reference unit

Retaining the configured nameplate would require the rest of the current-v2 secondary plant to be reconciled with that scale. In particular:

- rotor inertia would need review by roughly two orders of magnitude if a conventional multi-second inertia constant is intended;
- turbine, steam-path and condenser design capacities would need a declared low-load/full-load scaling policy;
- 5 MW would remain a 0.5% operating point, so governor droop, synchronization correction and protection supervision would need explicit very-low-load semantics;
- UI ranges and reference trajectories would need to distinguish nameplate capability from the deliberately modeled low-load envelope.

Changing only rotor inertia would not close the scale contract.

### Option B — adopt a reduced-scale educational unit

A nominal value around 10 MWe is materially closer to the current turbine/condenser/rotor configuration:

- the existing 1,000 kg·m² rotor gives an inertia constant of about 4.935 s at 10 MW;
- the current 5 MW point becomes 50% load rather than 0.5% load;
- a 150 rpm full-load reference rise becomes a 75 rpm requested-load displacement at 5 MW after rescaling, which is large enough to require deliberate governor retuning rather than becoming numerically decorative;
- the 10 MW synchronizing correction becomes comparable to the entire machine rating and would therefore also require rescaling;
- protection thresholds, coupling limits, requested-load increments, instrument ranges and replay/reference baselines would all require a coordinated migration.

Changing only `MaximumElectricalPower` to 10 MW is therefore prohibited.

## Historical provisional direction — resolved by E.1

The static evidence favored **Option B — a reduced-scale educational unit**. E.1 subsequently accepted that target and E.2 implemented the coordinated current-v2 candidate.

Before promotion, the project still requires:

1. successful validation of A.2 or direct evidence explaining any remaining condenser trip;
2. measured turbine mass-flow and shaft-power capability over the supported load range;
3. measured rotor response to controlled load imbalance;
4. a versioned migration plan covering nameplate, inertia, droop, grid coupling, protections, HMI ranges and reference trajectories;
5. explicit preservation or migration policy for legacy replay profiles.

## Automated evidence

`ReferencePlantScaleAuditTests` is explicit and carries both traits:

```text
Category=OperationalEnvelopeAudit
Category=ReferencePlantScaleAudit
```

The audit now covers the migrated current-v2 contract and preserved legacy behavior. Any further scale, inertia, droop or coupling change must update the contract, migration plan, evidence and tests in the same candidate.

Run through the complete operational audit:

```text
scripts\run-operational-envelope-audit.cmd
```

Or run only the scale evidence:

```text
dotnet test --project tests/NuclearReactorSimulator.Application.Tests/NuclearReactorSimulator.Application.Tests.csproj --no-build -- --explicit only --filter-trait "Category=ReferencePlantScaleAudit" --parallel none
```
