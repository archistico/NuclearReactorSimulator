# Operational challenge multidimensional scoring

## Scope

M10.9.6.3 freezes deterministic Application-layer challenge evaluation arithmetic. Scoring consumes authored observational evidence only. It has no command dispatcher, no control-authority ownership, no protection ownership, no wall-clock dependency and no plant-physics mutation path.

The five scoring dimensions are distinct:

- **SAFETY / PROTECTION DISCIPLINE**;
- **PROCEDURE / REQUIRED ACTIONS**;
- **STABILITY / OPERATING QUALITY**;
- **DEMAND TRACKING** when the selected policy uses external-demand evidence;
- **LOGICAL TIME / COMPLETION EFFICIENCY** when the selected policy uses a time objective.

A dimension evaluator supplies a normalized `0..1` performance fraction only when its evidence is available, plus a stable evidence-source ID and human-readable evidence summary. Missing required evidence scores zero, marks the evaluation incomplete and prevents a passing result.

## Frozen standard v1 policies

Two standard exact policy identities are frozen:

| Policy | Safety | Procedure | Stability | Demand | Logical time |
| --- | ---: | ---: | ---: | ---: | ---: |
| `general-operations@1` | 45 | 30 | 20 | — | 5 |
| `demand-following@1` | 40 | 25 | 15 | 15 | 5 |

Every policy totals exactly 100 points. Challenge definitions reference the exact scoring-policy identity through `ChallengeAssistancePolicy.ScoringPolicyId`; resolution fails closed on mismatch.

## Grade scale

- `< 60` — `NEEDS IMPROVEMENT`;
- `60 .. <75` — `SATISFACTORY`;
- `75 .. <90` — `PROFICIENT`;
- `>= 90` — `EXCELLENT`.

An incomplete evidence set produces `INCOMPLETE EVIDENCE` regardless of the numeric subtotal.

## Dominance rules

Safety and procedure are dominant and cannot be compensated by demand tracking, stability or speed.

- authored **critical safety failure**: final score is capped at **39%**, result is non-passing and grade is `UNSAFE`;
- authored **critical procedure failure**: final score is capped at **59%**, result is non-passing and grade is `PROCEDURE FAILURE`;
- if both are present, safety dominance wins.

These flags come from challenge-owned evidence. A generator/reactor trip or protection action is **not globally classified** as failure by the scoring engine. A challenge pack may classify an event as failure, protected completion or non-terminal evidence according to its own authored contract.

## Assistance and plant-control authority

Guidance mode and plant-control authority are separate inputs. Any score modifier must be declared explicitly in the versioned scoring policy.

The standard v1 policies intentionally use neutral `1.00` multipliers for every `TrainingGuidanceMode` and every `PlantControlAuthorityMode`. Therefore assistance and authority do not create a hidden score penalty in the standard policies. A future non-neutral policy must declare all mode multipliers explicitly and version its identity.

## Determinism and ownership

`ChallengeScoreCalculator` is pure score arithmetic over:

- a versioned `ChallengeDefinition`;
- a versioned `ChallengeScoringPolicyDefinition`;
- explicit guidance and plant-authority modes;
- one authored `ChallengeScoreDimensionEvidence` item for every policy dimension.

No `DateTime`, `DateTimeOffset`, `TimeSpan`, UI refresh cadence, command dispatcher or control-authority dispatcher participates in score arithmetic. Re-evaluating the same exact inputs yields the same result.

## Non-scope

M10.9.6.3 does not add:

- challenge packs;
- challenge/scoring UI;
- automatic demand following;
- new faults or protection semantics;
- physical/control retuning;
- presentation-side score arithmetic.

Initial challenge packs and their concrete evidence evaluators are M10.9.6.4 work.
