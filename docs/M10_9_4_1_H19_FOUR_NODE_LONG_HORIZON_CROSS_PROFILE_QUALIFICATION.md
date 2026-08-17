# M10.9.4.1-H.19 — Four-Node Long-Horizon & Cross-Profile Qualification

## Status

**VALIDATED — 2026-08-17.** Local compilation, complete ordinary `dotnet test` suite and the focused H.19 audit passed. H.19 is the authoritative validated baseline for H.20.

Built directly on user-validated **M10.9.4.1-H.18 Hotfix 1**.

H.18 established that unchanged H.9 plus unchanged bounded 2% / 5 K previous-phase hysteresis targeted at:

```text
steam | stop-out | header | turbine-inlet
```

converges on all 261 H.18 samples: all 245 H.17 failures plus 16 distributed H.17 success controls. H.18 recovered both the 120 turbine-inlet-mismatch failures and the 125 non-mismatch failures, with no residual failure class left in the split diagnostic.

H.19 does not activate that policy. It asks the next required question: does the exact four-node policy qualify over the full validated H.17 long-horizon/cross-profile representative contract?

## Frozen qualification contract

H.19 deliberately reuses the H.17 Hotfix 6 long-horizon design and requires the regenerated census/stratification to remain exactly:

- 30,000 explicit reference intervals;
- four profiles: `steady-long`, `load-pulse`, `cooling-pulse`, `combined-load-cooling`;
- 3,046 P060/F040 census trigger intervals;
- 92 trigger episodes;
- 473 qualified representatives.

The 473 regenerated `(profile, interval)` keys must exactly match:

```text
tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/Evidence/
H17_Hotfix6_FrozenQualifiedRepresentativeEvidence.csv
```

This prevents a sampling change from being mistaken for a numerical improvement.

## Four-node qualification experiment

Every one of the 473 frozen representatives is evaluated with:

- unchanged `JacobianHydraulicCorrectorSolver` H.9 options;
- unchanged H.13 bounded previous-phase hysteresis limits: 2% relative pressure drift and 5 K temperature drift;
- exact target set `steam|stop-out|header|turbine-inlet`;
- unchanged P060/F040 trigger contract;
- unchanged production thermodynamic resolver and physical coefficients.

The report separates the frozen H.17 classes:

- recovered H.17 failures: target 245/245;
- preserved H.17 successes: target 228/228;
- recovered turbine-inlet-mismatch failures: target 120/120;
- recovered non-mismatch failures: target 125/125.

A positive long-horizon qualification requires all 473 representatives to converge within the existing H.9 residual tolerances and accepted-iterate merit monotonicity contract.

## Committed-state transparency

H.19 reconstructs the complete four-profile reference trajectories. For every interval it counts committed phase-state checks across all four target nodes, for a total expected count of:

```text
30,000 intervals × 4 nodes = 120,000 checks
```

The bounded continuity model is observational only. Re-resolved committed selections are sampled at the established observation stride, at phase transitions and at P060/F040 trigger intervals. Positive qualification requires committed selection to remain transparent: no shadow continuity decision may alter the committed production selection.

## All-node inverse-branch scan

For every qualified representative H.19 compares candidate and explicit inverse thermodynamic branch selection at every fluid node.

The scan now rejects two untargeted branch-disagreement classes:

1. candidate-only late saturated-root shadowing;
2. candidate-vs-explicit selected-phase mismatch.

`turbine-inlet` is no longer untargeted in H.19. Any new node appearing in either untargeted set blocks positive four-node qualification and must be localized before activation design.

## Determinism and inherited release challenges

H.19 retains the H.17 deterministic sentinel contract and canonical fingerprints for:

- nonlinear four-node policy results;
- committed-selection observations;
- all-node inverse-branch scans;
- bounded hysteresis hold/release challenges.

The inherited H.16 2,000-interval control remains present, including convergence of interval 723 with a concrete `header` override.

## Production isolation

H.19 does **not** modify or activate:

- `SimplifiedWaterSteamThermodynamicModel.Resolve()`;
- `ThermodynamicBranchContinuityModel` implementation or 2% / 5 K limits;
- H.9 algorithm/options;
- P060/F040;
- physical coefficients;
- `PlantNetworkOrchestrator`;
- current-v2 10 ms `ExplicitCommittedState` production integration.

No H.19 shadow candidate state is committed.

## User validation result

The validated focused result is:

- 30,000 production-shadow intervals across four profiles;
- P060/F040 census reproduced exactly: 3,046 triggers, 92 episodes and 473 representatives;
- regenerated representative keys exactly match frozen H.17 evidence;
- 473/473 representatives converged, with zero line-search exhaustion;
- 245/245 frozen H.17 failures recovered and 228/228 H.17 successes preserved;
- 120/120 turbine-inlet-mismatch failures and 125/125 non-mismatch failures recovered;
- 32,829 branch overrides and 127,600 previous-phase holds;
- deterministic work ratio 1.547433 and exact deterministic repeat;
- 120,000 committed target phase-state checks, 24,346 committed selection observations and zero committed-selection overrides;
- 3,992 committed target phase transitions;
- 5,676 all-node inverse scans with no untargeted candidate-only late-shadow node and no untargeted candidate-vs-explicit phase-mismatch node;
- release challenges 4/4;
- maximum closure/ownership residuals 0 / 0.000000239;
- `four-node-long-horizon-cross-profile-shadow-qualification-passes=True`;
- `h19-audit-passes=True`.

Production remained 10 ms `ExplicitCommittedState`; no shadow candidate was committed and no branch-continuity policy was activated.

## Decision after H.19

If `four-node-long-horizon-cross-profile-shadow-qualification-passes=True` with 473/473 convergence, committed transparency, no untargeted late-shadow node, no untargeted phase-mismatch node and all inherited safeguards green, H.19 closes the evidence gap identified by H.18.

That still does **not** automatically authorize production activation. The next milestone must be a separately reviewed activation-design/rollback milestone that defines scope, authority, telemetry, failure behavior and deterministic rollback before any production numerical path can change.

If H.19 qualification is negative, production remains explicit and the failing representative/episode or newly discovered untargeted branch disagreement becomes the next diagnostic target.
