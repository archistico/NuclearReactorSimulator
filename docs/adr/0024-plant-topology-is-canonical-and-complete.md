# ADR 0024 — Plant topology is canonical and complete before orchestration

## Status

Accepted for M3.1.

## Context

M0–M2 validated individual physical primitives and small coupled test kernels. M3 must compose many such primitives into one plant without allowing missing endpoints, duplicate identities, partial state or caller collection order to become hidden simulation behavior.

Starting M3 with a monolithic plant update method would mix configuration validation, topology lookup and physics orchestration in one place and make deterministic network behavior harder to prove.

## Decision

Introduce immutable `PlantDefinition`, `PlantState` and `PlantSnapshot` boundaries before multi-component solving.

`PlantDefinition`:

- canonicalizes component registries by ordinal ID;
- requires globally unique topology identities, including wrapped valve/pump hydraulic paths;
- validates all hydraulic and thermal references eagerly;
- contains definitions only and executes no physics.

`PlantState`:

- contains exactly one state for every stateful definition and no orphan states;
- canonicalizes state registries independently of caller order;
- requires fluid/thermal states to use the plant's canonical definitions;
- duplicates no state for passive topology.

`PlantSnapshot` copies the committed plant state into a stable plant-level observation boundary.

M3.2 will consume these boundaries and implement the staged gather/solve/accumulate/integrate/commit pipeline from ADR 0023.

## Consequences

Positive:

- invalid topology fails before simulation starts;
- component lookup is deterministic and unambiguous;
- orchestration code can assume complete state;
- caller collection order cannot define plant behavior;
- later core/steam-drum/turbine composition can extend explicit registries instead of bypassing them.

Trade-offs:

- M3.1 adds structure without visible new physics;
- global topology-ID uniqueness is stricter than per-type uniqueness, but substantially simplifies diagnostics, recorder references and future UI addressing;
- M2 reactor-physics composition is not forced into this baseline and remains a later explicit extension around core zones.
