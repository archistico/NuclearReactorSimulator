# M10.9.4.1-H.12 — Thermodynamic Inverse Branch Selection Audit

## Purpose

H.11 localized the two persistent H.9 failures to concrete thermodynamic phase boundaries:

- interval 200, node `steam`, nominal `SaturatedMixture`, crossing on energy and mass probes;
- interval 360, node `stop-out`, nominal `SuperheatedVapor`, crossing on energy and mass probes.

The H.11 probes also showed extremely large pressure/temperature jumps under conserved-inventory perturbations of only about 2.5e-7 relative magnitude. H.12 therefore does **not** add another hydraulic corrector. It asks why the simplified water/steam inverse map `(mass, volume, internal energy) -> (pressure, temperature, phase)` changes branch under such tiny perturbations.

## Existing resolver order

H.12 preserves the production `SimplifiedWaterSteamThermodynamicModel.Resolve()` order exactly:

1. coarse saturated-mixture search;
2. direct subcooled-liquid candidate;
3. coarse superheated-vapor search;
4. boundary-aware saturated-mixture search;
5. boundary-aware superheated-vapor search.

The production method is not reordered, clamped or made history-dependent in H.12.

## Diagnostic provider

`IWaterSteamInverseBranchDiagnosticProvider` exposes shadow-only evidence for all five existing attempts. For each conserved-inventory probe it records:

- whether each branch finds a root/state;
- phase, pressure, temperature and vapor quality of each found candidate;
- which branch production ordering would select;
- whether both a saturated and a superheated root are simultaneously available;
- whether the coarse saturated search fails while the boundary-aware saturated search still succeeds;
- whether that later saturated root is shadowed by an earlier coarse-superheated result.

No candidate returned by this diagnostic provider is committed.

## Previous-state continuity test

The production resolver currently receives `previousState`. H.12 explicitly resolves every H.11 probe twice, once with a saturated-mixture previous state and once with a superheated-vapor previous state, and records whether the selected result changes. H.12 does not introduce hysteresis; it only determines whether a history/continuity tie-break already exists.

## Mechanism classification

The strongest mechanism classification is:

`overlapping-roots+coarse-saturated-detection+fixed-priority-no-hysteresis`

It requires all of the following:

- saturated and superheated roots coexist throughout the five H.11 probe points;
- coarse saturated root detection toggles across tiny mass/energy perturbations;
- at least one probe selects coarse superheated while a boundary-aware saturated root still exists;
- changing only `previousState` does not change selection.

The audit does not assume this result. If any condition is absent, the evidence remains valid and the mechanism is reported differently.

## Production isolation

H.12 changes no hydraulic solver, no trigger, no physical coefficient, no timestep and no `PlantNetworkOrchestrator` routing. Production remains `ExplicitCommittedState` at 10 ms. No active set, phase hold, hysteresis or semi-smooth formulation is enabled.

## Decision after H.12

If the overlapping-root/coarse-priority mechanism is confirmed, the next step should be a **narrow shadow-only thermodynamic branch-continuity/hysteresis experiment** targeted at `steam` and `stop-out`. It should compare continuity-preserving selection policies without changing production.

If the mechanism is not confirmed, inspect the detailed branch-candidate CSV before selecting any active-set or semi-smooth formulation.
