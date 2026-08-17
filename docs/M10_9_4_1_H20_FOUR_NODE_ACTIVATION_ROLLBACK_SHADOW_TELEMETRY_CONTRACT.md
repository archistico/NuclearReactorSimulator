# M10.9.4.1-H.20 — Four-Node Activation Contract, Rollback & Shadow Telemetry

## Status

**CANDIDATE**, built directly on user-validated **M10.9.4.1-H.19**.

H.19 closed the long-horizon/cross-profile qualification gap for the exact four-node shadow policy:

```text
steam | stop-out | header | turbine-inlet
```

The validated H.19 result reproduced the complete 30,000-interval/four-profile P060/F040 census at 3,046 trigger intervals, 92 episodes and the same 473 representative keys, then converged 473/473 with zero line-search exhaustion. All 245 H.17 failures recovered, all 228 H.17 successes remained convergent, committed selection stayed transparent and no new untargeted branch disagreement was found.

H.20 does **not** activate that policy. It defines the authority boundary that a later activation candidate would have to obey.

## Purpose

The failed H.5 Hotfix 1 production experiment already showed that numerical evidence must not be promoted directly into free-running authority. H.20 therefore separates three concerns before production wiring exists:

1. **eligibility** — when a four-node corrected candidate would be allowed to request authority;
2. **rollback** — deterministic fail-closed reasons that force explicit authority;
3. **telemetry** — the exact reason and proposed authority must be observable for every decision.

No new hydraulic or thermodynamic algorithm is introduced.

## Frozen H.19 evidence

H.20 checks in the user-validated H.19 focused results as evidence inputs:

```text
tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/Evidence/
H19_ValidatedQualifiedRepresentativeResults.csv
H19_ValidatedQualificationMetrics.csv
H19_ValidatedQualificationSummary.txt
```

The frozen contract requires, among other safeguards:

- 30,000 production-shadow steps;
- four profiles;
- 3,046 P060/F040 trigger intervals;
- 92 trigger episodes;
- 473 qualified representatives;
- 473/473 convergence;
- 0 line-search exhaustion;
- 245/245 H.17 failures recovered;
- 228/228 H.17 successes preserved;
- 120/120 mismatch and 125/125 non-mismatch failures recovered;
- 120,000 committed phase-state checks;
- no untargeted late-shadow node;
- no untargeted selected-phase mismatch node;
- committed selection transparent;
- release challenges green;
- maximum H.19 closure / ownership residual `0 / 0.000000239`;
- `four_node_long_horizon_cross_profile_shadow_qualification_passes=True`;
- `h19_audit_passes=True`.

The evidence is immutable input. H.20 does not regenerate or reinterpret H.19 trajectories. The ordinary and focused gates also verify canonical SHA-256 fingerprints of all three frozen H.19 files, with line endings normalized only for portability, so a later accidental edit cannot silently preserve the headline counts while changing the validated evidence.

## Shadow activation authority contract

H.20 introduces `FourNodeBranchContinuityShadowActivationSupervisor` as a **pure shadow authority proposal**. It is not consumed by `PlantNetworkOrchestrator`.

The default options are `FourNodeBranchContinuityActivationOptions.H19QualifiedShadowOnly` and freeze:

- activation arm: **disabled**;
- P060/F040 trigger values: 0.060 relative pressure / 40 kg/s flow;
- exact target nodes: `steam|stop-out|header|turbine-inlet`;
- H.9 residual guards: `1e-5` relative pressure / `1e-2 kg/s` absolute flow;
- mass-closure guard: `1e-8 kg/s`;
- energy-ownership guard: `1e-3 W`.

With the default arm disabled, every trigger remains proposed `ExplicitCommittedState`.

The focused audit also turns the arm on **inside the shadow supervisor only** to prove the decision contract. Even then, a corrected candidate is merely *eligible*; H.20 exposes no mechanism that can commit it.

## Fail-closed rollback priority

For an armed triggered observation, authority remains explicit if any guard fails. The deterministic reason priority is:

1. H.19 qualification evidence unavailable or rejected;
2. corrector non-convergence;
3. line-search exhaustion;
4. pressure residual above H.9 tolerance;
5. flow residual above H.9 tolerance;
6. mass closure above the validated guard;
7. energy ownership residual above the validated guard;
8. new untargeted branch disagreement.

Only when all guards pass may the shadow supervisor propose `CorrectedCandidate` with reason `QualifiedTriggeredCorrection`.

An untriggered observation always proposes explicit authority without rollback.

This is intentionally **per-interval and non-persistent**: H.20 introduces no latch, cooldown, dwell time, hysteretic activation state or cross-interval candidate ownership.

## Typed telemetry

Every decision records:

- deterministic sample id;
- proposed authority;
- exact reason code;
- whether rollback was required;
- whether the trigger was observed;
- whether the shadow activation arm was enabled;
- `ProductionCommitAuthorized`, which is always `false` in H.20.

The focused audit writes the complete 473-representative decision matrix and a rollback-challenge matrix.

## Rollback challenges

H.20 injects one synthetic failure for each guard, using a real qualified H.19 representative as the otherwise-valid base observation:

- missing qualification evidence;
- corrector non-convergence;
- line-search exhaustion;
- pressure-residual breach;
- flow-residual breach;
- mass-closure breach;
- energy-ownership breach;
- untargeted branch disagreement.

All eight must propose immediate `ExplicitCommittedState`, report the expected typed rollback reason and leave production commit unauthorized.

## Production isolation

H.20 does **not** change:

- `PlantNetworkOrchestrator` routing;
- `HydraulicNumericalCouplingDefinition` production selection;
- current-v2 default `ExplicitCommittedState` mode;
- the 10 ms production timestep;
- `SimplifiedWaterSteamThermodynamicModel.Resolve()`;
- H.9 algorithm or tolerances;
- P060/F040;
- `ThermodynamicBranchContinuityModel` or 2% / 5 K limits;
- four-node target set;
- physical coefficients;
- controller/protection behavior.

No H.20 corrected candidate can be committed.

## Focused qualification

A positive H.20 design qualification requires:

- frozen H.19 evidence accepted exactly;
- all 473 qualified H.19 representatives remain explicit with the default arm disabled;
- all 473 become shadow-candidate-eligible when the arm is simulated as enabled;
- zero production-commit authorization in both modes;
- all eight rollback challenges return explicit authority with the exact expected reason;
- untriggered authority remains explicit;
- exact deterministic decision fingerprint repeat;
- current desktop current-v2 factory remains `ExplicitCommittedState`;
- `activation-contract-passes=True`;
- `h20-audit-passes=True`.

## Decision after H.20

If H.20 passes, the activation/rollback **contract** is qualified, not the production integration.

A later milestone may build a separately reviewed **opt-in production integration candidate** only if it:

- wires exactly this fail-closed authority contract;
- keeps all standard current-v2 factories explicit by default until explicit user validation authorizes otherwise;
- reports every corrected commitment and rollback;
- never hides non-convergence behind silent fallback;
- reruns the full H.19 long-horizon/cross-profile qualification as a regression gate;
- adds ordinary, replay, protection, long-running and off-design production gates before any default activation is considered.

If H.20 fails, production remains explicit and the contract itself must be repaired before any production wiring.
