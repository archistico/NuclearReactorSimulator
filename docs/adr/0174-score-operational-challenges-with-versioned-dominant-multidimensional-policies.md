# ADR-0174 — Score operational challenges with versioned dominant multidimensional policies

## Status

Accepted — M10.9.6.3 Hotfix 1 validated 2026-08-20
**Date:** 2026-08-20

## Context

M10.9.6.1 froze deterministic challenge lifecycle and M10.9.6.2 froze challenge-owned external-demand evidence. M10.9.6.3 must define scoring before challenge packs or presentation code can accidentally establish weights or compensation rules.

A single additive score is insufficient if excellent MW tracking or fast completion can offset an unsafe/prohibited action. Assistance and plant-control authority also must not influence score through hidden UI logic.

## Decision

Use versioned Application-layer `ChallengeScoringPolicyDefinition` objects with explicit dimensions, exact 100-point weights, grade thresholds, dominance caps and per-mode modifiers.

Freeze standard v1 policies:

- `general-operations@1`: safety 45, procedure 30, stability 20, logical time 5;
- `demand-following@1`: safety 40, procedure 25, stability 15, demand 15, logical time 5.

Freeze pass/proficiency/excellence thresholds at 60/75/90 percent. Authored critical safety failure caps the final result at 39 percent and makes it non-passing; authored critical procedure failure caps at 59 percent and makes it non-passing. Safety dominance wins if both are present.

Unavailable required evidence scores zero and makes evaluation incomplete/non-passing. Standard v1 guidance and authority modifiers are explicitly neutral at 1.00 for every defined mode. Non-neutral modifiers require a distinct versioned policy.

The calculator is pure observational arithmetic and has no command, control-authority, protection, wall-clock or Simulation ownership.

## Consequences

Challenge packs must provide documented evidence sources for every scoring dimension they enable. Demand/procedure/stability evaluators remain separate from the arithmetic policy. Protection events are not globally declared failure by scoring; challenge definitions decide their semantic role.

Weights/caps cannot be changed later without a new scoring-policy version.
