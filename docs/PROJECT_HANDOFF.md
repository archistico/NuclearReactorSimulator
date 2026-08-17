# Nuclear Reactor Simulator — Project Handoff

> **Authoritative validated baseline:** M10.9.4.1-H.18 Hotfix 1.
>
> **Working source:** M10.9.4.1-H.19 CANDIDATE — Four-Node Long-Horizon & Cross-Profile Qualification.
>
> **Production numerical policy:** current-v2 remains `ExplicitCommittedState` at 10 ms. No Phase H shadow corrector or branch-continuity policy is committed.

## 1. Current engineering question

H.18 validated the four-node shadow target extension on every frozen H.17 failure plus controls. H.19 must now determine whether the exact same four-node policy qualifies on the **complete H.17 long-horizon/cross-profile representative contract** before any activation design is considered.

No solver, trigger, hysteresis limit, physical coefficient, branch order or production integration path is being changed.

## 2. Validated Phase H chain

- **H.4 VALIDATED:** P060/F040/R015 selected from bounded Picard evidence.
- **H.5 Hotfix 2 VALIDATED:** production activation rolled back; extended shadow gate shows only 5/7 convergence.
- **H.6 VALIDATED:** bounded Picard rescue reaches 6/7.
- **H.7 Hotfix 1 VALIDATED:** true residual + deterministic backtracking reaches 5/7.
- **H.8 VALIDATED:** safeguarded Anderson reaches 5/7.
- **H.9 VALIDATED:** finite-difference Jacobian + damped Newton reaches 5/7; solver direction is not the root cause.
- **H.10–H.12 VALIDATED:** failures localize to inverse thermodynamic branch selection, not hydraulic-law switching.
- **H.13 Hotfix 2 VALIDATED:** bounded 2% / 5 K previous-phase continuity at `steam|stop-out` reaches 7/7.
- **H.14 Hotfix 1 VALIDATED:** 2,000-interval gate reaches 14/15; interval 723 remains.
- **H.15 Hotfix 1 VALIDATED:** interval 723 localizes to the same mechanism at `header`.
- **H.16 VALIDATED:** `steam|stop-out|header` reaches 15/15 and recovers interval 723.
- **H.17 Hotfix 6 VALIDATED:** 30,000-interval/four-profile diagnostic produces 3,046 triggers, 92 episodes and 473 representatives; three-node policy converges 228/473 and fails 245/473; `turbine-inlet` is discovered as a new disagreement node.
- **H.18 Hotfix 1 VALIDATED:** four-node target set `steam|stop-out|header|turbine-inlet` converges 261/261 over all 245 H.17 failures plus 16 success controls.

## 3. Authoritative H.18 result

Local build, complete ordinary `dotnet test` and focused H.18 audit passed on 2026-08-17.

```text
frozen H.17 representatives                 473
H.17 failures                               245
  turbine-inlet mismatch failures           120
  non-mismatch failures                     125
H.18 success controls                        16
H.18 evaluated samples                      261
H.18 converged                              261/261
remaining failures                            0
recovered mismatch failures                 120/120
recovered non-mismatch failures             125/125
preserved success controls                   16/16
turbine-inlet overrides                  14,746
four-node-extension-qualifies              True
```

Additional safeguards:

- committed `turbine-inlet` observations: 4,111;
- committed `turbine-inlet` phase transitions: 1,240;
- committed selection transparent: true;
- deterministic sentinels: 24;
- deterministic repeat: true;
- new untargeted late-shadow nodes: none;
- new untargeted phase-mismatch nodes: none;
- `residual-floor-split-diagnostic-passes=True`;
- `h18-audit-passes=True`;
- production remained explicit and unchanged.

The H.17 hypothesis of a separate residual-floor class was therefore not sustained on the H.18 selected set: the fourth target recovered both the mismatch and non-mismatch failure classes.

## 4. H.19 design

H.19 reuses the H.17 long-horizon qualification machinery but changes only the shadow target set to the H.18-validated four-node set.

Reference horizon:

```text
steady-long             12,000 intervals
load-pulse               6,000 intervals (validated 5→0→5 MWe)
cooling-pulse            6,000 intervals (100%→75%→100%)
combined-load-cooling    6,000 intervals
TOTAL                    30,000 intervals
```

H.19 requires the regenerated evidence contract to remain exactly:

```text
P060/F040 census trigger intervals = 3,046
trigger episodes                    = 92
qualified representatives           = 473
```

The 473 regenerated `(profile, interval)` keys must exactly match:

```text
tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/Evidence/
H17_Hotfix6_FrozenQualifiedRepresentativeEvidence.csv
```

Every representative is then evaluated with unchanged H.9 + unchanged 2% / 5 K bounded hysteresis at exactly:

```text
steam | stop-out | header | turbine-inlet
```

H.19 reports:

- total convergence / 473;
- recovered frozen H.17 failures / 245;
- preserved frozen H.17 successes / 228;
- recovered mismatch failures / 120;
- recovered non-mismatch failures / 125;
- deterministic work and exact repeat;
- closure/ownership residuals;
- 120,000 committed target phase-state checks across the complete horizon;
- committed-selection transparency;
- all-node candidate-vs-explicit inverse-branch scan;
- untargeted candidate-only late-shadow nodes;
- untargeted candidate-vs-explicit phase-mismatch nodes;
- inherited hold/release challenge result.

Positive qualification requires all of those safeguards to remain green and `four-node-long-horizon-cross-profile-shadow-qualification-passes=True`.

## 5. H.19 validation commands

From repository root:

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-four-node-long-horizon-cross-profile-qualification-audit.cmd
```

Focused artifacts:

```text
artifacts\h19-four-node-long-horizon-cross-profile-qualification\
```

Heartbeat:

```text
00-progress.txt
```

A negative qualification flag is valid evidence if the structural focused audit itself completes. Production stays explicit either way.

## 6. Files authoritative for restart

Read these first:

1. `docs/NEW_CHAT_START.md`
2. `docs/PROJECT_HANDOFF.md`
3. `docs/PROJECT_STATUS.md`
4. `docs/ROADMAP.md`
5. `docs/M10_9_4_1_H19_FOUR_NODE_LONG_HORIZON_CROSS_PROFILE_QUALIFICATION.md`
6. `docs/M10_9_4_1_H19_VALIDATION_CHECKLIST.md`
7. `docs/adr/0145-qualify-four-node-continuity-before-any-activation-design.md`
8. `docs/M10_9_4_1_H18_TURBINE_INLET_CONTINUITY_RESIDUAL_FLOOR_SPLIT_DIAGNOSIS.md`

## 7. Hard constraints

Do not:

- activate H.9 in production;
- activate bounded branch continuity/hysteresis in production;
- change production inverse-map branch order;
- retune 2% / 5 K hysteresis limits;
- retune P060/F040 or H.9 tolerances;
- change physical coefficients;
- change the production 10 ms timestep;
- generalize the target set beyond `steam|stop-out|header|turbine-inlet`;
- hide flow with filtering or thermodynamic clamping;
- commit shadow candidate states.

H.19 is a qualification milestone, not an activation milestone.

## 8. Package-time authority

This H.19 package is authored **after** the user validated H.18 Hotfix 1. Therefore H.18 Hotfix 1 is the authoritative validated baseline encoded in this package.

H.19 remains a candidate until the user reports local build, ordinary-suite and focused-audit results. A new chat must not infer H.19 validation from the existence of this ZIP.
