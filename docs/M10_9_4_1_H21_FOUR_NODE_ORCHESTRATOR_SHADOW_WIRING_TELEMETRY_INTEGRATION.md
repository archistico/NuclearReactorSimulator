# M10.9.4.1-H.21 — Four-Node Orchestrator Shadow Wiring & Telemetry Integration

## Status

**VALIDATED — Hotfix 1, 2026-08-18.** Build, complete ordinary suite and cumulative H.19 -> H.20 -> H.21 focused gate passed locally.

H.20 validated the authority boundary independently from production wiring: frozen H.19 evidence accepted for 473 representatives, 473/473 default explicit decisions, 473/473 corrected-candidate eligibility only in armed shadow simulation, zero production commit authorization, 8/8 typed rollback challenges and exact deterministic repeat.

H.21 validated the next architectural seam: the exact H.19/H.20 mechanism executes from the real `PlantNetworkOrchestrator` without changing the committed current-v2 trajectory in the qualified 2,000-interval control window.

## Scope

H.21 introduces a separately opt-in numerical mode:

```text
FourNodeBranchContinuityShadowIntegrated
```

The standard current-v2 desktop factory remains:

```text
ExplicitCommittedState
```

The H.21 mode is audit-only and must be requested explicitly. It wires, without retuning:

- the H.19 P060/F040 predictor trigger;
- H.9 finite-difference Jacobian + damped Newton corrector;
- H.13 bounded previous-phase hysteresis at 2% pressure / 5 K;
- target nodes `steam|stop-out|header|turbine-inlet`;
- the validated H.20 fail-closed authority supervisor;
- typed per-step telemetry.

## Critical authority rule

H.21 **never commits the corrected candidate**.

For every network step:

1. the unchanged explicit predictor is evaluated;
2. P060/F040 determines whether the four-node sidecar is triggered;
3. if untriggered, H.20 reports `NotTriggered`; the historical explicit orchestration path remains authoritative;
4. if triggered, unchanged H.9 + bounded four-node continuity produces a shadow corrected candidate;
5. the H.20 supervisor evaluates convergence, line-search, residual, closure/ownership and untargeted-branch guards;
6. the resulting authority proposal is recorded;
7. `PlantNetworkOrchestrator` still applies the **historical explicit orchestration path** as its candidate state; the sidecar predictor/corrected candidate are observational only.

`CorrectedCandidateCommitted` is therefore always `false` in H.21 even if the H.20 proposal is `CorrectedCandidate`.

Terminology note: the sidecar `EvaluatePredictor(...)` result is used for trigger/correction observation, but it is **not** substituted as the returned plant state. `StepFourNodeBranchContinuityShadowIntegrated(...)` separately preserves the historical explicit pipe/valve/pump accumulation and fluid/thermal integration order for the applied state. The H.21 lockstep fingerprint gate exists to prove that this integration plumbing remains trajectory-transparent.

This is intentionally different from the failed H.5 Hotfix 1 free-running activation attempt.

## Exact trigger reuse

`HybridSemiImplicitHydraulicGateSolver` now exposes `EvaluatePredictor(...)`, which factors the existing H.4/H.19 explicit predictor and P060/F040 metrics without running the historical H.3 corrector.

The existing `Step(...)` method is rebuilt on that same predictor result, so H.19 trigger semantics are not duplicated or reimplemented.

## H.20 evidence provenance

H.21 freezes the user-validated H.20 focused artifacts:

```text
H20_ValidatedActivationContractSummary.txt
H20_ValidatedAuthorityDecisions.csv
H20_ValidatedRollbackChallenges.csv
H20_ValidatedActivationContractMetrics.csv
```

Their newline-normalized canonical SHA-256 fingerprints are ordinary-test guarded. This makes the H.20 prerequisite explicit rather than relying on milestone labels alone.

## Integrated telemetry

`PlantNetworkHydraulicNumericalSnapshot` gains optional `FourNodeBranchContinuity` telemetry. Historical explicit and H.5 hybrid modes leave it null.

For the H.21 opt-in mode, each step records:

- trigger observed;
- whether the shadow corrector was evaluated;
- H.20 proposed authority and typed reason;
- rollback flag;
- corrected-candidate eligibility;
- corrected-candidate committed flag;
- untargeted branch disagreement flag;
- branch overrides, previous-phase holds and releases;
- shadow iteration count, convergence and line-search status;
- pressure/flow residuals;
- mass closure and energy ownership residuals.

The generic `PlantNetworkHydraulicNumericalSnapshot` continues to describe the state actually returned by the orchestrator: `UsedSemiImplicitCorrection=false`, one-pass explicit authority, with sidecar details separated in H.21 telemetry.

## Focused integration qualification

H.21 runs two deterministic 2,000-interval current-v2 engines in lockstep with the explicit control:

- historical explicit runtime;
- H.21 shadow-integrated runtime;
- a second H.21 repeat runtime.

The 2,000-interval window is the already-established H.16 control window and must retain exactly 15 P060/F040 trigger intervals.

Required H.21 evidence:

- explicit vs H.21 presentation fingerprint equality: **2,000/2,000**;
- H.21 repeat fingerprint equality: **2,000/2,000**;
- P060/F040 triggers: **15**;
- shadow correction evaluated on every trigger;
- H.20 corrected-candidate eligibility: **15/15**;
- rollback: **0** on the qualified control window;
- corrected candidates committed: **0**;
- untargeted branch disagreement: **0**;
- every triggered correction remains within H.20 residual/closure/ownership guards;
- deterministic telemetry fingerprint repeat;
- standard desktop current-v2 factory remains `ExplicitCommittedState`.

## Full prerequisite regression

The H.21 focused script first reruns:

1. the complete H.19 30,000-interval/four-profile long-horizon qualification;
2. the complete H.20 activation/rollback contract audit;
3. the H.21 integrated 2,000-interval lockstep audit.

This intentionally makes H.19/H.20 prerequisite drift a blocking H.21 failure.

## Production isolation

H.21 does not change:

- standard current-v2 factory selection;
- production `SimplifiedWaterSteamThermodynamicModel.Resolve()` ordering;
- P060/F040 thresholds;
- H.9 controls or tolerances;
- 2% / 5 K hysteresis limits;
- four-node target set;
- physical coefficients;
- 10 ms timestep;
- controller or protection logic;
- replay schema.

No corrected H.21 state can become committed.

## Decision after H.21

H.21 is green: the numerical policy, fail-closed authority contract **and the real orchestrator sidecar wiring** are all validated while remaining trajectory-transparent.

H.22 therefore introduces the first separately opt-in corrected-candidate commit seam, but only behind the unchanged H.20 decision and immediate explicit fallback. That first commit candidate must add replay, protection, long-running and off-design gates before any discussion of default activation.



## Authoritative validated result — 2026-08-18

```text
intervals=2000
production-fixed-step=10.000 ms
explicit-vs-shadow-integrated-presentation-equivalent=2000/2000
shadow-integrated-repeat-equivalent=2000/2000
P060-F040-triggered=15
corrected-candidate-eligible=15/15
rollbacks=0
corrected-candidates-committed=0
untargeted-branch-disagreements=0
branch-overrides=408
previous-phase-holds=6456
hysteresis-releases=0
deterministic-repeat=True
telemetry-fingerprint=0454270F4AA63E89915FE231328807D4A6B7AD0C733441F78DC06C86A159CDC8
default-current-v2-mode=ExplicitCommittedState
standard-factory-shadow-integration-active=False
four-node-orchestrator-shadow-integration-passes=True
h21-audit-passes=True
```

The result validates trajectory-transparent sidecar wiring only. H.21 still committed zero corrected candidates. H.22 is the first separately opt-in corrected-candidate ownership candidate.
