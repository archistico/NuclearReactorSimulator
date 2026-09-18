# ADR-0213 — Requalify production mode 2 against independent IF97 and frozen topology before exact-v9 composition

## Status

Accepted for R2 planning only.

## Context

R1 staged the selected C4 behavior in production as explicit opt-in mode 2 and returned qualified implementation evidence. R1 demonstrated production-vs-C4 equivalence, not an independent post-implementation physical requalification. The repair route requires R2 before any exact-v9-equivalent composition work.

## Decision

R2 is a test/reference-only qualification gate. It will explicitly instantiate production mode 2 and compare it against the independent IAPWS-IF97 helper over the frozen RP1A VR2 matrix, exact-v9 committed-state corpus and 320-boundary seam map.

C4/shadow outputs are prerequisite provenance, not the R2 acceptance oracle. R2 preserves the existing `1e-8` reference self-check, `25%` VR2 blocking ceiling and `10%` Planning 1 pressure target. It also preserves the already qualified seam-continuity maxima as non-regression ceilings.

R2 may not modify production, change defaults, compose mode 2 into exact-v9, create a new exact identity or execute the later long materiality gate.

## Consequences

- a green R2 demonstrates focused reference/topology qualification of the staged production mode;
- returned R2 evidence must be adjudicated before R3 planning;
- R3, not R2, owns short exact-v9-equivalent shadow/composition requalification;
- R4 retains the P1B-equivalent long materiality recheck;
- no activation authority is implied by R2 planning or execution.
