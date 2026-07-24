# Reference Plant Scale Evidence

## Status

**M10.9.4.1-A.3 — HISTORICAL SCALE EVIDENCE; SUPERSEDED FOR CURRENT-V2 BY E.2**

**Source basis:** M10.9.4.1-A.2 Hotfix 1 candidate.

This document preserves the pre-E.2 A.3 scale evidence that exposed the former hybrid electromechanical configuration. It is historical evidence, not the current-v2 runtime contract. E.1 subsequently accepted the reduced-scale educational identity and E.2 migrates the current-v2 sustained profiles to a 10 MWe nameplate while preserving legacy/default profiles.

## Historical pre-E.2 configured values

| Quantity | Historical A.3 value |
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

## Derived evidence

The original A.3 audit derived the following values from the then-current current-v2 definitions:

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

## Historical governor authority map under the pre-E.2 normalization

| Requested load | Fraction of 1,000 MW nameplate | Droop reference rise |
|---:|---:|---:|
| 0 MW | 0% | 0 rpm |
| 5 MW | 0.5% | 0.75 rpm |
| 10 MW | 1% | 1.5 rpm |
| 100 MW | 10% | 15 rpm |
| 1,000 MW | 100% | 150 rpm |

The table demonstrated why the pre-E.2 1,000 MW normalization made the 5 MWe point only 0.5% load. E.2 no longer uses that normalization for the current-v2 sustained profiles.

## Engineering interpretation

### Historical alternative — retain a 1,000 MW-class reference unit

Retaining the configured nameplate would require the rest of the current-v2 secondary plant to be reconciled with that scale. In particular:

- rotor inertia would need review by roughly two orders of magnitude if a conventional multi-second inertia constant is intended;
- turbine, steam-path and condenser design capacities would need a declared low-load/full-load scaling policy;
- 5 MW would remain a 0.5% operating point, so governor droop, synchronization correction and protection supervision would need explicit very-low-load semantics;
- UI ranges and reference trajectories would need to distinguish nameplate capability from the deliberately modeled low-load envelope.

Changing only rotor inertia would not close the scale contract.

### Accepted direction — adopt a reduced-scale educational unit

A nominal value around 10 MWe is materially closer to the current turbine/condenser/rotor configuration:

- the existing 1,000 kg·m² rotor gives an inertia constant of about 4.935 s at 10 MW;
- the current 5 MW point becomes 50% load rather than 0.5% load;
- a 150 rpm full-load reference rise becomes a 75 rpm requested-load displacement at 5 MW after rescaling, which is large enough to require deliberate governor retuning rather than becoming numerically decorative;
- the 10 MW synchronizing correction becomes comparable to the entire machine rating and would therefore also require rescaling;
- protection thresholds, coupling limits, requested-load increments, instrument ranges and replay/reference baselines would all require a coordinated migration.

Changing only `MaximumElectricalPower` to 10 MW is therefore prohibited.

## Accepted direction and E.2 current-v2 contract

The user accepted the **reduced-scale educational unit** as the project identity. E.2 therefore migrates the current-v2 sustained profiles to:

- 10 MWe generator nameplate;
- 5 MWe normal reference point = 50% load;
- unchanged 1,000 kg·m² rotor at 3,000 rpm, giving `H ≈ 4.934802 s`;
- 1.5 rpm full-load governor reference rise, preserving the previously validated 0.75 rpm displacement at 5 MWe;
- signed bidirectional grid coupling for generation and motoring;
- signed -10..+10 MWe electrical HMI ranges;
- legacy/default definitions preserved at their historical scale and generation-only semantics.

The 10 MW synchronizing-correction and 10 MW/Hz damping magnitudes are intentionally retained pending dynamic validation rather than automatically ratio-scaled. E.3 protection extensions remain blocked until E.2 is locally green.

## Automated evidence

`ReferencePlantScaleAuditTests` is explicit and carries both traits:

```text
Category=OperationalEnvelopeAudit
Category=ReferencePlantScaleAudit
```

The audit now preserves the historical A.3 evidence and asserts the accepted E.2 current-v2 10 MWe contract. Legacy/default scale evidence remains explicit so replay-compatible profiles are not silently migrated.

Run through the complete operational audit:

```text
scripts\run-operational-envelope-audit.cmd
```

Or run only the scale evidence:

```text
dotnet test --project tests/NuclearReactorSimulator.Application.Tests/NuclearReactorSimulator.Application.Tests.csproj --no-build -- --explicit only --filter-trait "Category=ReferencePlantScaleAudit" --parallel none
```
