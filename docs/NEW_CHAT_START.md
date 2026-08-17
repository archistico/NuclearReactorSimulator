# Nuclear Reactor Simulator — authoritative new-chat start

## Current checkpoint

- **Validated baseline:** `M10.9.4.1-H.17 Hotfix 6 — Canonical Determinism Fingerprints`.
- **Working candidate:** `M10.9.4.1-H.18 Hotfix 1 — IReadOnlyList Count Compile Fix over Turbine-Inlet Continuity Extension & Residual-Floor Split Diagnosis`.
- **Production numerical path:** current-v2 remains `ExplicitCommittedState` at **10 ms**.
- **Phase G:** complete.
- **Phase H:** open; no H.3–H.18 shadow solver/policy is authoritative production behavior.
- **Phase I:** deferred until Phase H closes.

## What H.17 proved

H.17 Hotfix 6 passed build, the complete ordinary suite and the focused long-horizon/cross-profile diagnostic audit.

Validated H.17 evidence:

- 4 profiles: `steady-long`, validated 5→0→5 MWe `load-pulse`, `cooling-pulse`, `combined-load-cooling`;
- 30,000 explicit reference intervals;
- 3,046 P060/F040 trigger intervals;
- 92 deterministic trigger episodes;
- 473 qualified representatives;
- H.16 control remains 15/15 with interval 723 recovered;
- H.17 three-node policy (`steam|stop-out|header`) converges **228/473**;
- **245/473** representatives exhaust line search;
- deterministic repeat, closure/ownership, committed-selection transparency and hold/release challenges remain green;
- all 90,000 committed target phase checks remain production-transparent;
- the all-node inverse scan discovers new untargeted `turbine-inlet` disagreement.

The validated H.17 artifacts split the 245 failures into:

1. **120 failures with `turbine-inlet` candidate-vs-explicit phase mismatch** (usually candidate `SuperheatedVapor`, explicit `SaturatedMixture`);
2. **125 failures without `turbine-inlet` phase mismatch**.

The second class has materially larger flow residuals and is not explained by the fourth-node mismatch alone.

## What H.18 does

H.18 does **not** rerun the 3,046-trigger H.4 census and does not change production.

It freezes the validated H.17 473-representative evidence in:

```text
tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/Evidence/
H17_Hotfix6_FrozenQualifiedRepresentativeEvidence.csv
```

It reconstructs the same four explicit reference trajectories, then runs unchanged H.9 + unchanged bounded 2%/5 K hysteresis at:

```text
steam | stop-out | header | turbine-inlet
```

on:

- all **245 H.17 failures**;
- **16 deterministic H.17 success controls**.

Total H.18 nonlinear samples: **261**.

H.18 measures separately:

- recovery of the 120 `turbine-inlet` mismatch failures;
- recovery of the 125 non-mismatch failures;
- regression of success controls;
- committed `turbine-inlet` transparency.

Every H.18 failure that remains is diagnosed for:

- mapped-minus-applied mass/energy residual ranking by node;
- first/penultimate/final accepted-iterate merit and minimum relaxation;
- all-node candidate-vs-explicit inverse-map branch/phase disagreement;
- any new untargeted late saturated-root shadow node.

## H.18 validation

Run from the repository root:

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-turbine-inlet-continuity-residual-floor-split-audit.cmd
```

Expected focused artifacts:

```text
artifacts\h18-turbine-inlet-continuity-residual-floor-split\
```

The diagnostic audit may pass even when `four-node-extension-qualifies=False`; a negative qualification is valid evidence. `residual-floor-split-diagnostic-passes=True` is the structural H.18 audit criterion.

## Interpretation after H.18

- If `recovered-turbine-inlet-mismatch=120/120`, success controls remain green and committed selection is transparent, the fourth-node mechanism is confirmed.
- If failures remain **without** any new untargeted branch disagreement, the next milestone must investigate fixed-point residual floor / solution existence rather than adding targets or solver complexity.
- If new untargeted branch-disagreement nodes appear, localize those nodes first.
- If all 245 failures recover, return to a bounded four-node long-horizon qualification before any activation candidate.

## Hard constraints

Do **not**:

- activate H.9 or bounded hysteresis in production;
- modify `SimplifiedWaterSteamThermodynamicModel.Resolve()` branch order;
- generalize the target set beyond H.18 evidence;
- retune 2%/5 K hysteresis limits;
- retune P060/F040 or H.9 tolerances;
- change physical coefficients or the 10 ms production timestep;
- hide flow with filtering/clamping;
- commit shadow candidate states.

Read `docs/PROJECT_HANDOFF.md` and `docs/M10_9_4_1_H18_TURBINE_INLET_CONTINUITY_RESIDUAL_FLOOR_SPLIT_DIAGNOSIS.md` before changing code.

## Before actually changing chat

This Hotfix 1 package is created **before** the user's local H.18 validation result exists. The only H.18 change versus the original candidate is the focused-audit compile fix `.Length` -> `.Count` on two `IReadOnlyList<string>` diagnostics. Therefore H.17 Hotfix 6 is the only baseline that can be marked validated inside this ZIP.

After running the H.18 gate:

- if build, ordinary tests and focused H.18 Hotfix 1 audit all pass, state explicitly in the first message of the new chat that **H.18 Hotfix 1 is validated** and include the H.18 summary; the new chat may then promote H.18 over the package-time H.17 baseline;
- if any H.18 gate fails, H.17 Hotfix 6 remains the baseline and the failure output is the continuation evidence.

Do not silently infer H.18 validation from the existence of this candidate ZIP.
