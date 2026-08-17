# ADR 0137 — Audit inverse thermodynamic branch selection before active-set formulation

**Status:** Accepted for M10.9.4.1-H.12 candidate.

## Context

H.9 showed that a well-conditioned finite-difference Newton corrector still fails on the same two frozen P060/F040 events as H.7/H.8. H.10 ruled out local hydraulic-law switching and instead found two thermodynamic switches. H.11 localized them to `steam` at interval 200 and `stop-out` at interval 360, both energy+mass phase boundaries. Tiny conserved-inventory perturbations caused multi-megapascal pressure jumps between saturated-mixture and superheated-vapor states.

The simplified water/steam inverse resolver contains coarse and boundary-aware root searches and evaluates superheated candidates before the late boundary-aware saturated fallback. It also receives `previousState`, but H.11 did not establish whether that state influences branch selection.

## Decision

Before implementing an active-set or semi-smooth hydraulic corrector, add a shadow-only diagnostic provider and analyzer that expose all existing inverse-map branch candidates at the H.11 probe points. Preserve `Resolve()` behavior and ordering. Measure root overlap, coarse-search detection toggles, late saturated-root shadowing and previous-state selection sensitivity.

## Consequences

A confirmed overlapping-root/fixed-priority mechanism justifies a narrow branch-continuity/hysteresis shadow experiment. A non-confirmed result requires further inverse-map investigation. Neither outcome authorizes production activation.

H.12 does not change thermodynamic equations, physical coefficients, hydraulic solvers, production routing or timestep.
