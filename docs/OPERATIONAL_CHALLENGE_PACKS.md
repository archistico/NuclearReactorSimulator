# Operational Challenge Packs

## Scope

M10.9.6.4 composes the previously validated M10.9.6 lifecycle, external-demand and scoring contracts into six initial operational challenges. It does **not** add plant physics, new faults, new protection logic, new command authority, supervisory automation or presentation-side score arithmetic.

Every pack binds:

- one existing versioned `ScenarioDefinition` and one of its existing objectives;
- one versioned `ChallengeDefinition`;
- one read-only `IChallengeConditionEvaluator`;
- one exact M10.9.6.3 scoring policy;
- one authored evidence-source binding for every score dimension in that policy.

The pack layer therefore composes evidence; it does not become a new plant owner.

## Initial pack catalog

### `pre-start-circulation-preparation@1`

Uses `cold-shutdown-pre-start` / objective `prepare-circulation` and `general-operations@1`.

The challenge activates only from the already-validated M7.2 cold-shutdown baseline. Completion requires an accepted main-circulation-pump start action plus the validated pre-criticality handoff checks. A canonical trip during this normal preparation is an authored challenge failure.

No external-demand profile is owned.

### `synchronization-initial-loading@1`

Uses `grid-synchronization-initial-loading` / objective `stabilize-low-load` and `general-operations@1`.

The challenge reuses the M7.5 synchronization-window and stable-low-load checks. It does not own an external-demand profile: synchronization and first loading are evaluated as a normal operating procedure, avoiding an artificial 5 MWe tracking error while the breaker is still legitimately open.

A protection trip is an authored failure for this normal synchronization challenge, not a global rule for all challenges.

### `bounded-demand-following-5-10-5@1`

Uses `power-manoeuvring-normal-shutdown` / objective `manoeuvre-power` and `demand-following@1`.

The external-demand profile is:

```text
activation + 0       5 MWe HOLD
activation + 500     10 MWe HOLD
activation + 3000    5 MWe HOLD
```

This exercise deliberately exposes the next scheduled demand change so the operator can practice planned step-demand load-following. Generator requested load remains operator/control owned and is never written by the profile.

Completion requires accepted load-raise/load-lower actions and a return to stable 5 MWe operation through existing M7.6 evidence.

### `post-load-change-stabilization@1`

Uses `power-manoeuvring-normal-shutdown` / objective `observe-feedback` and `demand-following@1`.

An accepted generator-load raise activates the challenge. The challenge owns a current 10 MWe external target but does not expose a future schedule. Completion requires observable M7.6 thermal/void feedback and a stable 10 MWe band using immutable plant evidence.

The challenge does not issue the load-raise command itself.

### `controlled-normal-shutdown@1`

Uses `power-manoeuvring-normal-shutdown` / objective `normal-shutdown` and `general-operations@1`.

Completion requires the validated post-shutdown cooling condition and accepted evidence of the normal load-lower, breaker-open and rod-insert sequence. Accepted operator use of SCRAM, turbine trip or generator trip is an authored procedural failure for this **normal procedure** challenge.

This does not make those actions globally wrong or globally score-failing; they remain valid protective/emergency actions in other contexts.

### `generator-trip-load-rejection-recovery@1`

Uses the already-supported M8.4 `m84-generator-trip-load-rejection` scenario / objective `observe-load-rejection` and `general-operations@1`.

The existing generator-trip fault activates the challenge. The generator trip itself is expected evidence, not failure. Required evidence includes the canonical generator-trip latch, generator isolation and an accepted alarm acknowledgement. Completion is an isolated, acknowledged post-trip response.

No additional failure condition is authored because M10.9.6.4 has no validated basis for classifying another protection action as a failure in this fault-response exercise. Safety dominance remains available to M10.9.6.3 score evidence when later projected.

## Demand schedule visibility

M10.9.6.4 settles the earlier open decision explicitly:

- `bounded-demand-following-5-10-5@1` exposes the next scheduled demand change;
- `post-load-change-stabilization@1` exposes only its current 10 MWe target;
- synchronization and challenges without demand profiles expose no demand schedule.

Schedule visibility is challenge-definition metadata and has no control effect.

## Evidence evaluator ownership

`StandardOperationalChallengeConditionEvaluator` is read-only. It reuses:

- M7.2 `PreStartupChecklistEvaluator`;
- M7.5 `GridSynchronizationChecklistEvaluator`;
- M7.6 `PowerManoeuvringChecklistEvaluator`;
- committed M8.4 fault lifecycle evidence;
- deterministic accepted `ScenarioOperatorActionRecord` history.

The only explicit operating bands added by M10.9.6.4 are direct compositions of already validated initial-condition/gate behavior:

- 5 MWe stable band: 4.5–5.5 MWe;
- 10 MWe stable band: 9.5–10.5 MWe;
- synchronous-speed band: 2980–3020 rpm.

The evaluator has no dispatcher, runtime-engine, controller, protection mutator or wall-clock API.

## Score evidence provenance

Every pack declares exactly one evidence-source binding for every dimension required by its exact scoring policy. These bindings document provenance only. M10.9.6.4 does not calculate dimension fractions or final scores; the pure arithmetic remains `ChallengeScoreCalculator` ownership from M10.9.6.3.

Demand-tracking bindings are permitted only when the challenge owns an external-demand profile, and every challenge with a demand profile must use an exact policy containing `DemandTracking`.

## Logical-time windows

Initial target windows are observational. M10.9.6.4 deliberately authors no hard failure deadlines because runtime qualification of concrete completion timing belongs to M10.9.6.5. This avoids converting unqualified timing guesses into challenge failure semantics.

## M10.9.6.5 handoff

The closure milestone must now prove, with the concrete packs:

- replay/checkpoint reconstruction of lifecycle and demand timelines;
- deterministic score-evidence projection and final score;
- demand never mutates generator requested load;
- assistance changes do not alter physical plant state by themselves;
- protection remains authoritative and challenge-specific classification remains intact;
- checkpoint continuation matches uninterrupted challenge state.
