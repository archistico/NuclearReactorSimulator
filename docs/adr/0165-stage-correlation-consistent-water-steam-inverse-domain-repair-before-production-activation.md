# ADR 0165 — Stage correlation-consistent water/steam inverse-domain repair before production activation

## Status

Accepted and validated. Repair candidate validated in Hotfix 10; staged requalification completed in Hotfix 11–14; authoritative exact-v4 production activation validated in Hotfix 16.2.

## Context

Hotfix 9.1 validated two internal defects in the historical `SimplifiedWaterSteamThermodynamicModel` inverse map.

The vapor defect is structural: saturated vapor uses the correlated `v_g(T)=1/rho_g(T)` boundary while superheated vapor uses the ideal-gas-style `p=R T/v` relation against `Psat(T)`. The two boundaries do not coincide. The validated topology census contains 194 no-root samples, 130 overlap/multiple-root samples and 44 missing-onset samples, and both exact desktop @2 and @3 reach that no-root family at `exhaust` during the frozen load raise/lower journey.

The liquid defect is a search-topology error: the saturated-liquid density correlation has its physical maximum near 4 °C, so valid fixed-volume saturation intervals can be disconnected from the triple point. The historical boundary-aware fallback assumes triple-point-connected monotonic validity and misses 83/83 sampled roots from 4.01–8.16 °C even though independent local brackets prove those roots exist.

A direct global replacement would invalidate the H.12-H.30 evidence before the repaired closure has demonstrated that it removes the defects without creating a new operational failure.

## Decision

Introduce `WaterSteamThermodynamicClosureMode` with two modes:

- `HistoricalCorrelationTopology` remains the parameterless/default mode and remains used by all registered/current runtime identities;
- `CorrelationConsistentInverseDomain` is an opt-in repair candidate used only by focused evidence composition in Hotfix 10.

The repair candidate keeps the existing saturation pressure, saturated-liquid density, saturated-vapor density and saturated internal-energy correlations unchanged.

For superheated vapor, replace the incompatible ideal boundary with a shifted-specific-volume pressure relation. At each supported saturation temperature, the shift is the difference between `R T/Psat(T)` and the correlated `v_g(T)`. Consequently the repaired superheated relation passes through exactly the same `v_g(T), Psat(T)` boundary as saturated vapor. Above the 640 K saturation ceiling, retain the 640 K shift so the declared saturation domain is not expanded implicitly.

For saturated inversion, locate the saturated-liquid density maximum numerically and solve the cold liquid, warm liquid and vapor specific-volume boundaries independently. Intersect those boundaries to obtain the complete valid saturation-temperature interval for the fixed conserved specific volume, rather than requiring validity at the triple point.

Fail-closed behavior remains mandatory when neither repaired branch contains a mathematical root inside the declared simplified domain.

## Consequences

Hotfix 10 does not activate repaired thermodynamics in production and does not overwrite exact @2/@3 replay identities. Existing historical evidence therefore remains provenance rather than being silently reinterpreted through changed physics.

The repair candidate must first prove:

- resolution of the three observed historical vapor no-root states;
- one-sided root ownership immediately below and above representative vapor seam points;
- resolution of the complete 231-point low-temperature census;
- completion of the frozen 7000-step load raise/lower journey under both explicit and corrected-commit hydraulics.

If that evidence passes, a later activation/requalification candidate must rerun H.12-H.30 and all scheduled long gates before the repaired closure can become authoritative.

No `exhaust` special case, conserved-inventory clamp, condenser retune, acceptance-tolerance widening or fail-closed weakening is permitted as a substitute for the thermodynamic repair.

## Validated outcome

The planned repair path succeeded without an `exhaust` special case or tolerance widening. Hotfix 10 resolved 3/3 observed historical no-root probes, 7/7 seam-below and 7/7 seam-above ownership probes, and 231/231 low-temperature census points; both 7000-step explicit/corrected load journeys completed. Stages 1–4 then requalified corrected ownership, 30,000-interval cross-profile behavior, replay/checkpoint/protection, H.27 off-design behavior and H.28-style relative cost/soak. Exact desktop `@4` now carries the repaired closure while exact `@2/@3` preserve historical semantics.
