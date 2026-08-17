# Nuclear Reactor Simulator — Project Handoff

> **Authoritative validated baseline:** M10.9.4.1-H.17 Hotfix 6.
>
> **Working source:** M10.9.4.1-H.18 Hotfix 1 CANDIDATE — IReadOnlyList Count Compile Fix over Turbine-Inlet Continuity Extension & Residual-Floor Split Diagnosis.
>
> **Production numerical policy:** current-v2 remains `ExplicitCommittedState` at 10 ms. No Phase H shadow corrector or branch-continuity policy is committed.

## 1. Immediate goal

H.18 must separate two failure classes discovered by validated H.17 instead of assuming one global cure:

- branch-continuity failures correlated with `turbine-inlet`;
- residual failures not correlated with `turbine-inlet` phase mismatch.

The next engineering decision must be based on the H.18 split, not on another solver escalation or plant-wide hysteresis expansion.

## 2. Validated numerical-hardening chain

The important Phase H chain is:

- **H.4 VALIDATED:** P060/F040/R015 selective Picard evidence; original frozen set 5/7 primary convergence.
- **H.6 VALIDATED:** bounded Picard rescue reaches only 6/7.
- **H.7 Hotfix 1 VALIDATED:** true residual + deterministic backtracking reaches 5/7.
- **H.8 VALIDATED:** safeguarded Anderson reaches 5/7.
- **H.9 VALIDATED:** finite-difference Jacobian + damped Newton reaches 5/7; Jacobians are well-conditioned, so solver direction alone is not the original root cause.
- **H.10 VALIDATED:** no hydraulic-law non-smoothness at the two persistent original failures; thermodynamic switching exists.
- **H.11 VALIDATED:** original switches localize to `steam` interval 200 and `stop-out` interval 360.
- **H.12 VALIDATED:** overlapping saturated/superheated inverse roots + coarse saturated detector toggle + fixed branch priority + no previous-state tie-break.
- **H.13 Hotfix 2 VALIDATED:** bounded previous-phase hysteresis at `steam|stop-out` moves unchanged H.9 from 5/7 to 7/7.
- **H.14 Hotfix 1 VALIDATED:** 2,000-interval broader gate reaches 14/15; interval 723 remains.
- **H.15 Hotfix 1 VALIDATED:** interval 723 is the same inverse-map mechanism at `header`; no hydraulic switching cause.
- **H.16 VALIDATED:** unchanged bounded 2%/5 K policy at `steam|stop-out|header` reaches 15/15, recovers 723 with 68 header overrides, committed state remains transparent.
- **H.17 Hotfix 6 VALIDATED:** 30,000-interval/four-profile long-horizon diagnostic; infrastructure and audit pass, but the three-node policy does not qualify across the extended representative set.

## 3. Authoritative H.17 result

Profiles and reference horizon:

```text
steady-long             12,000 intervals
load-pulse               6,000 intervals (validated 5→0→5 MWe)
cooling-pulse            6,000 intervals (100%→75%→100%)
combined-load-cooling    6,000 intervals
TOTAL                    30,000 intervals
```

Trigger/qualification evidence:

```text
P060/F040 census trigger intervals = 3,046
trigger episodes                    = 92
qualified representatives           = 473
H.16 control                        = 15/15
H.17 converged                      = 228/473
H.17 line-search exhausted          = 245/473
```

Profile breakdown:

```text
steady-long             25/170 converged
load-pulse              93/108 converged
cooling-pulse           20/84 converged
combined-load-cooling   90/111 converged
```

Other validated safeguards:

- deterministic H.9 sentinel repeat: true;
- committed selection transparency: true;
- 90,000 committed target phase checks;
- 17,990 committed branch observations;
- 2,752 real target phase transitions;
- hold/release challenges: 4/4;
- closure/ownership preserved;
- production unchanged.

## 4. H.17 failure split

The H.17 all-node inverse scan discovered `turbine-inlet` as a new untargeted branch-disagreement node.

Across all 473 representatives:

- candidate-vs-explicit `turbine-inlet` phase mismatch occurs in 121 representatives;
- **120/121 are H.17 failures**;
- only one mismatch representative converges;
- 120 of the 245 failures therefore belong to the `turbine-inlet` mismatch class;
- 125 of the 245 failures have no `turbine-inlet` phase mismatch.

Typical mismatch:

```text
candidate turbine-inlet = SuperheatedVapor
explicit turbine-inlet  = SaturatedMixture
```

Candidate-only late saturated-root shadowing is explicitly observed at least at `steady-long` interval 7191, but the broader candidate-vs-explicit phase disagreement is much more common than the strict late-shadow marker.

Residual separation in H.17 evidence:

- mismatch failures: pressure residual around 0.20, mean flow residual about 1.0 kg/s;
- non-mismatch failures: pressure residual around 0.20, mean flow residual about 14.4 kg/s.

Therefore **adding only `turbine-inlet` cannot be assumed to solve all H.17 failures**.

## 5. H.18 design

H.18 freezes the validated H.17 representative evidence in:

```text
tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/Evidence/
H17_Hotfix6_FrozenQualifiedRepresentativeEvidence.csv
```

The file is a compact projection of the validated H.17 focused artifacts and is guarded by an ordinary regression requiring:

```text
representatives = 473
failures        = 245
successes       = 228
mismatch fails  = 120
non-mismatch    = 125
```

H.18 reconstructs the same four reference trajectories but skips the already-validated expensive H.4 trigger census. It evaluates unchanged H.9 + unchanged H.13 bounded hysteresis with target set:

```text
steam | stop-out | header | turbine-inlet
```

on all 245 H.17 failures plus 16 distributed success controls.

Total H.18 nonlinear evaluations: **261**.

### Experiment A — four-node extension

Measure:

- recovered `turbine-inlet` mismatch failures / 120;
- recovered non-mismatch failures / 125;
- preserved success controls / 16;
- concrete turbine-inlet overrides;
- committed turbine-inlet transparency.

Positive four-node qualification is deliberately separate from audit validity.

### Experiment B — residual-floor split

For every failure remaining after Experiment A record:

- node-local mapped-minus-applied mass/energy residuals and ranks;
- final pressure/flow/merit residual;
- first/penultimate/final accepted-iterate merit;
- minimum accepted relaxation;
- all-node candidate-vs-explicit inverse-map phase/branch disagreement;
- new untargeted late-shadow nodes.

If no new branch-disagreement node remains, the next step should be fixed-point residual-floor / solution-existence analysis rather than further branch-target expansion.

## 6. H.18 validation commands

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-turbine-inlet-continuity-residual-floor-split-audit.cmd
```

Focused artifacts:

```text
artifacts\h18-turbine-inlet-continuity-residual-floor-split\
```

The heartbeat is `00-progress.txt`.

## 7. Files authoritative for restart

Read these first in a new chat:

1. `docs/NEW_CHAT_START.md`
2. `docs/PROJECT_HANDOFF.md`
3. `docs/PROJECT_STATUS.md`
4. `docs/ROADMAP.md`
5. `docs/M10_9_4_1_H18_TURBINE_INLET_CONTINUITY_RESIDUAL_FLOOR_SPLIT_DIAGNOSIS.md`
6. `docs/M10_9_4_1_H18_VALIDATION_CHECKLIST.md`
7. `docs/adr/0144-split-turbine-inlet-branch-continuity-from-residual-floor-diagnosis.md`

## 8. Hard constraints

Do not:

- activate H.9 in production;
- activate branch continuity/hysteresis in production;
- change production inverse-map branch order;
- retune 2%/5 K hysteresis limits;
- retune P060/F040 or H.9 tolerances;
- change physical coefficients;
- change production 10 ms timestep;
- generalize the target set beyond evidence;
- hide flow with filtering or thermodynamic clamping;
- commit shadow candidate states.

H.18 is a split diagnostic. It must not become an activation milestone accidentally.

## 9. Package-time versus post-validation authority

This handoff is authored while H.18 Hotfix 1 is still a candidate. Hotfix 1 changes only the focused-audit use of `IReadOnlyList<string>.Length` to `.Count`; the H.18 experiment and frozen H.17 evidence are unchanged. H.17 Hotfix 6 is therefore the last validated baseline encoded in the package. If the user locally validates H.18 after receiving this ZIP, the H.18 build/test/focused summary supplied by the user supersedes the package-time validation status. A new chat must not guess that promotion; it must use the reported local H.18 gate result.
