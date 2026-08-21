# ADR-0175 — Compose initial operational challenges from existing validated evidence owners

**Status:** Accepted  
**Date:** 2026-08-20

## Context

M10.9.6.1–M10.9.6.3 established deterministic lifecycle, external-demand evidence and score arithmetic. M10.9.6.4 must create concrete exercises without letting the challenge layer become a second implementation of plant physics, protection logic, scenario faults or operator control.

The project already has validated M7.2/M7.5/M7.6 checklist evaluators and M8.4 fault lifecycle evidence. Re-authoring their physical thresholds inside challenge code would create semantic drift and duplicate ownership.

## Decision

Create six versioned initial `OperationalChallengePackDefinition` entries that compose existing `ScenarioDefinition` objectives, `ChallengeDefinition`, exact scoring policies and authored score-evidence provenance bindings.

Use a read-only `StandardOperationalChallengeConditionEvaluator` that delegates normal-operation evidence to the existing M7.2/M7.5/M7.6 checklist evaluators and reads committed M8.4 fault state plus accepted operator-action history. It receives no command dispatcher, runtime engine or plant-control authority.

Freeze the initial catalog:

1. pre-start circulation preparation;
2. synchronization and initial loading;
3. bounded 5→10→5 MWe demand-following;
4. post-load-change 10 MWe stabilization;
5. controlled normal shutdown;
6. generator-trip/load-rejection response and stabilization.

Only the planned 5→10→5 demand-following exercise exposes the next demand change. Post-load-change stabilization exposes only its current 10 MWe target; synchronization owns no external-demand profile. Demand remains observational and never writes generator requested load.

For normal-operation challenges, challenge-specific unexpected-trip conditions are allowed where the exercise contract makes that classification defensible. For the generator-trip/load-rejection challenge, the generator trip is required evidence rather than failure. No global trip=failure rule is introduced.

Do not author hard failure deadlines in the initial pack. Concrete timing windows remain observational until M10.9.6.5 runtime qualification.

## Consequences

M10.9.6.4 adds exercise composition and evidence provenance but no new plant behavior. Missing physical phenomena or richer fault scenarios remain outside M10.9.6 and in the post-M11 backlog.

M10.9.6.5 must validate replay/checkpoint/determinism and score projection using these exact pack identities rather than replacing them with ad hoc test-only exercises.
