# ADR-0214 — Use single-factor exact-v9-equivalent shadow composition before long materiality recheck

## Status

Accepted for R3 planning only.

## Context

R1 staged mode 2 without activation and R2 independently qualified it against IF97 and frozen topology. The next risk is composition: the new closure may be physically qualified in isolation yet interact differently with the exact-v9 runtime/controller/ownership path.

Directly editing exact-v9 would destroy the historical identity before composition behavior is qualified. Jumping directly to the P1B long replay would also mix composition validity with long-horizon materiality.

## Decision

R3 will use a test-only, single-factor shadow composition.

A test-local exact-v9-equivalent builder must first prove that, with historical mode 1, it is deterministically identical to canonical exact-v9 for 128 running steps. Only after that proof may the same composition be instantiated with mode 2 as the sole changed factor.

The mode-2 shadow then runs the established 120 s exact-v9 health workload and must satisfy the existing health, conservation, controller, hydraulic-ownership and deterministic-repeat contracts. Historical exact-v9 itself remains unchanged.

## Consequences

- R3 isolates composition risk without production mutation;
- failure of the mode-1 baseline-equivalence proof invalidates the shadow and blocks R3;
- success demonstrates bounded exact-v9-equivalent composition readiness, not activation;
- R4 remains the first P1B-equivalent 5→6 MWe long materiality recheck;
- returned R3 evidence must be adjudicated before R4 planning.
