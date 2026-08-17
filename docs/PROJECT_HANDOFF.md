# Nuclear Reactor Simulator — Project Handoff

> **Authoritative validated baseline:** M10.9.4.1-H.19 — Four-Node Long-Horizon & Cross-Profile Qualification.
>
> **Working source:** M10.9.4.1-H.20 CANDIDATE — Four-Node Activation Contract, Rollback & Shadow Telemetry.
>
> **Production numerical policy:** current-v2 remains `ExplicitCommittedState` at 10 ms. No Phase H corrected candidate or branch-continuity policy is committed.

## 1. Current engineering question

H.19 established that unchanged H.9 plus unchanged bounded 2% / 5 K previous-phase hysteresis targeted at exactly:

```text
steam | stop-out | header | turbine-inlet
```

qualifies over the complete long-horizon/cross-profile representative contract.

H.20 must now define, **without production wiring**, the fail-closed authority, rollback and telemetry contract that any later opt-in activation candidate would have to obey.

## 2. Validated Phase H chain

- **H.4 VALIDATED:** P060/F040/R015 selected from bounded Picard evidence.
- **H.5 Hotfix 2 VALIDATED:** direct production activation rolled back; extended shadow gate was only 5/7.
- **H.6–H.9 VALIDATED:** increasingly capable nonlinear correctors did not remove the original failure class.
- **H.10–H.12 VALIDATED:** failures localized to inverse thermodynamic branch selection.
- **H.13 Hotfix 2 VALIDATED:** bounded 2% / 5 K continuity at `steam|stop-out` reaches 7/7.
- **H.14 Hotfix 1 VALIDATED:** broader 2,000-interval gate reaches 14/15.
- **H.15 Hotfix 1 VALIDATED:** interval 723 localizes to the same mechanism at `header`.
- **H.16 VALIDATED:** `steam|stop-out|header` reaches 15/15.
- **H.17 Hotfix 6 VALIDATED:** 30,000 intervals, 3,046 P060/F040 triggers, 92 episodes, 473 representatives; three-node policy reaches 228/473 and exposes `turbine-inlet`.
- **H.18 Hotfix 1 VALIDATED:** four-node target recovers all 245 H.17 failures plus 16/16 controls, 261/261 overall.
- **H.19 VALIDATED:** full four-profile long-horizon qualification reaches 473/473 with all safeguards green.

## 3. Authoritative H.19 result

User validation on 2026-08-17 passed local compilation, complete ordinary `dotnet test` and focused H.19 audit.

```text
profiles                                      4
production-shadow intervals              30,000
P060/F040 trigger intervals                3,046
trigger episodes                               92
qualified representatives                     473
representative keys match frozen H.17        True
H.19 converged                           473/473
line-search exhausted                           0
recovered H.17 failures                  245/245
preserved H.17 successes                 228/228
recovered turbine-inlet mismatch         120/120
recovered non-mismatch                   125/125
branch overrides                           32,829
previous-phase holds                      127,600
committed phase-state checks              120,000
committed selection observations           24,346
committed selection overrides                   0
committed target phase transitions          3,992
inverse qualified sample node scans          5,676
untargeted late-shadow nodes                  none
untargeted phase-mismatch nodes               none
release challenges                           4/4
max closure / ownership             0 / 0.000000239
qualification passes                         True
h19 audit passes                              True
```

Deterministic work ratio was 1.547433 and exact deterministic repeat passed. Production remained explicit, target set and hysteresis limits were unchanged and no shadow candidate was committed.

## 4. H.20 design

H.20 freezes the validated H.19 focused evidence in:

```text
tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/Evidence/
H19_ValidatedQualifiedRepresentativeResults.csv
H19_ValidatedQualificationMetrics.csv
H19_ValidatedQualificationSummary.txt
```

The three frozen files are also guarded by canonical SHA-256 fingerprints (newline-normalized only), so H.20 cannot silently qualify against edited H.19 evidence.

It introduces a shadow-only `FourNodeBranchContinuityShadowActivationSupervisor`.

Default contract:

```text
activation arm                              disabled
pressure trigger                            0.060
flow trigger                                40 kg/s
pressure residual guard                     1e-5
flow residual guard                         1e-2 kg/s
mass closure guard                          1e-8 kg/s
energy ownership guard                      1e-3 W
targets                                     steam|stop-out|header|turbine-inlet
production commit authorization             always false
```

The supervisor is not wired into `PlantNetworkOrchestrator`.

## 5. Fail-closed authority

For a triggered observation with the shadow arm simulated as enabled, a corrected candidate is eligible only if all guards pass.

Deterministic rollback priority is:

1. qualification evidence unavailable;
2. corrector non-convergence;
3. line-search exhaustion;
4. pressure residual breach;
5. flow residual breach;
6. mass-closure breach;
7. energy-ownership breach;
8. untargeted branch disagreement.

Every failed guard proposes immediate `ExplicitCommittedState` and emits its typed reason. There is no persistent activation latch or cross-interval candidate authority in H.20.

## 6. H.20 focused audit

The gate evaluates:

- all 473 frozen H.19 representatives with activation arm disabled: expected 473/473 explicit;
- the same 473 with the arm simulated as enabled inside the shadow supervisor: expected 473/473 candidate-eligible but **0 production commits**;
- eight rollback fault-injection challenges: expected 8/8 immediate explicit rollback with exact typed reasons;
- an untriggered observation: expected explicit without rollback;
- exact deterministic decision fingerprint repeat;
- current desktop current-v2 factory still `ExplicitCommittedState`.

Positive result requires:

```text
activation-contract-passes=True
h20-audit-passes=True
```

## 7. Validation commands

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-four-node-activation-rollback-contract-audit.cmd
```

Focused artifacts:

```text
artifacts\h20-four-node-activation-rollback-contract\
```

## 8. Files authoritative for restart

1. `docs/NEW_CHAT_START.md`
2. `docs/PROJECT_HANDOFF.md`
3. `docs/PROJECT_STATUS.md`
4. `docs/ROADMAP.md`
5. `docs/M10_9_4_1_H20_FOUR_NODE_ACTIVATION_ROLLBACK_SHADOW_TELEMETRY_CONTRACT.md`
6. `docs/M10_9_4_1_H20_VALIDATION_CHECKLIST.md`
7. `docs/adr/0146-define-fail-closed-four-node-activation-contract-before-production-wiring.md`
8. `docs/M10_9_4_1_H19_FOUR_NODE_LONG_HORIZON_CROSS_PROFILE_QUALIFICATION.md`

## 9. Hard constraints

Do not:

- activate the H.19 four-node policy in standard production during H.20;
- wire H.20 authority into `PlantNetworkOrchestrator`;
- introduce a silent fallback that hides failed correction attempts;
- introduce a persistent activation latch, cooldown or dwell rule without evidence;
- change production inverse-map order;
- retune 2% / 5 K hysteresis limits;
- retune P060/F040 or H.9 tolerances;
- change physical coefficients;
- change production 10 ms timestep;
- generalize beyond `steam|stop-out|header|turbine-inlet`;
- hide flow with filtering/clamping;
- commit shadow candidate states.

## 10. Package-time authority

This H.20 package is authored **after** the user validated H.19. Therefore H.19 is the authoritative validated baseline encoded in the package.

H.20 remains a candidate until the user reports local build, ordinary-suite and focused-audit results. A new chat must not infer H.20 validation from the existence of the ZIP.
