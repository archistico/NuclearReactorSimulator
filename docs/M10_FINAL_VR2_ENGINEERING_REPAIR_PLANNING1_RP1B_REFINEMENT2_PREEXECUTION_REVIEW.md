# M10 Final — VR2 Engineering Repair Planning 1 — RP1B Refinement 2 Pre-Execution Review

**Candidate:** C3/D3 vapor-side seam completion  
**Review status:** STATIC REVIEW COMPLETE — runtime build/test still requires the user environment  
**Production impact:** none; `src/` must remain byte-identical to the validated Refinement 1 Hotfix 1 baseline.

## Reviewed failure classes

The review explicitly re-checks the failure classes that previously caused avoidable RED attempts:

- Windows PowerShell 5.1 ASCII/UTF-8 source stability;
- null-safe text reads in validators;
- tolerant floating-point contract comparisons rather than exact `-ne` comparisons;
- RP1A CSV schema, row counts and compound-key uniqueness;
- xUnit2031-prone `Assert.Single(...Where(...))` patterns;
- runner environment variable, filter-method and output-name agreement;
- immutable boundary-only VR2 row handling;
- immutable 40/360/288/1280 RP1A corpus counts;
- frozen first-generation and Refinement 1 evidence identity;
- no C3/D3 identity under production `src/`;
- no new production closure mode and no exact-v9 semantic change.

## Candidate-specific review

### C3

C3 wraps C2 rather than editing it. The only pre-C2 path is a narrow saturated-vapor-boundary discriminator. It is built at initialization from a 0.02 K saturation table and never calls IF97 from `TryResolve`.

The discriminator is bounded to an energy margin of 0.05–100 J/kg from the same-specific-volume saturated-vapor curve. Positive margin is interpreted as immediate Region-2 side; a negative margin is used only after immutable C2 has failed and therefore acts as the near-vapor Region-4 fallback. This ordering is intended to prevent the 55 C2 R2-side mixture steals without perturbing already-resolved C2 core states.

### D3

D3 calls immutable D2 first. The new code can execute only when D2 returns unresolved. C3 then supplies a phase-aware seed. Mixture seeds receive a bounded local direct-IF97 scan over ±0.25 K with 0.005 K scan spacing and at most 48 bisection refinements. If no stronger direct root is found, the C3 seed remains available as a fail-closed phase-consistent fallback rather than reopening broad D1-style scans.

## Selection semantics

Refinement 2 does not perform selection. The candidate summary is stricter than Refinement 1: `rp1c_selection_eligible` additionally requires zero seam unresolved rows and zero seam phase mismatches across all 1,280 frozen probes.

No result from this gate authorizes production thermodynamics, tolerance changes, exact-v9 changes, VR3, P3-R1 or a second replacement-long baseline.

## Remaining runtime-only checks

This environment does not provide the project's .NET toolchain or Windows PowerShell 5.1, so the following remain intentionally unresolved until the local run:

- Release compilation and analyzers;
- ordinary suite;
- actual C3/D3 seam completion counts;
- actual resolve and seam timing distributions;
- deterministic-repeat evidence produced by the focused test.
