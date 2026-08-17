# Nuclear Reactor Simulator — authoritative new-chat start

## Current checkpoint

- **Validated baseline:** `M10.9.4.1-H.18 Hotfix 1 — Turbine-Inlet Continuity Extension & Residual-Floor Split Diagnosis`.
- **Working candidate:** `M10.9.4.1-H.19 — Four-Node Long-Horizon & Cross-Profile Qualification`.
- **Production numerical path:** current-v2 remains `ExplicitCommittedState` at **10 ms**.
- **Phase G:** complete.
- **Phase H:** open; no H.3–H.19 shadow solver/policy is authoritative production behavior.
- **Phase I:** deferred until Phase H closes.

## What H.18 proved

H.18 Hotfix 1 passed local compilation, complete ordinary tests and the focused audit.

Validated evidence:

- frozen H.17 representatives: 473;
- H.17 failures: 245 = 120 `turbine-inlet` mismatch + 125 non-mismatch;
- H.18 success controls: 16;
- four-node targets: `steam|stop-out|header|turbine-inlet`;
- 261/261 converged;
- 0 remaining failures;
- 120/120 mismatch failures recovered;
- 125/125 non-mismatch failures recovered;
- 16/16 controls preserved;
- 14,746 `turbine-inlet` overrides;
- committed selection transparent;
- deterministic repeat true;
- no new untargeted late-shadow node;
- no new untargeted phase-mismatch node;
- `four-node-extension-qualifies=True`;
- `residual-floor-split-diagnostic-passes=True`;
- `h18-audit-passes=True`.

Production stayed explicit and no shadow state was committed.

## What H.19 does

H.19 returns the exact H.18 four-node policy to the complete H.17 long-horizon/cross-profile contract before any activation design.

It reconstructs the same four reference profiles and all 30,000 explicit intervals, reruns the unchanged P060/F040 census and requires exact reproduction of:

```text
3,046 trigger intervals
92 trigger episodes
473 qualified representatives
```

The regenerated 473 representative keys must exactly equal frozen H.17 Hotfix 6 evidence.

All 473 representatives are then evaluated with unchanged H.9 + unchanged bounded 2% / 5 K hysteresis at exactly:

```text
steam | stop-out | header | turbine-inlet
```

H.19 must separately report:

- recovered H.17 failures / 245;
- preserved H.17 successes / 228;
- recovered mismatch failures / 120;
- recovered non-mismatch failures / 125;
- overall convergence / 473;
- committed-selection transparency across 120,000 target phase-state checks;
- deterministic sentinel repeat;
- closure/ownership residuals;
- inherited hold/release challenges;
- new untargeted candidate-only late-shadow nodes;
- new untargeted candidate-vs-explicit phase-mismatch nodes.

## Validation

Run from repository root:

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-four-node-long-horizon-cross-profile-qualification-audit.cmd
```

Expected focused artifacts:

```text
artifacts\h19-four-node-long-horizon-cross-profile-qualification\
```

Positive qualification requires `four-node-long-horizon-cross-profile-shadow-qualification-passes=True` in addition to `h19-audit-passes=True`.

## Interpretation after H.19

- If H.19 reaches 473/473 with all safeguards green, the four-node shadow policy is long-horizon/cross-profile qualified. The next milestone may design a **separate bounded activation/rollback contract**, but must not silently activate it.
- If any representative fails, production remains explicit and the failure becomes the next diagnostic target.
- If a new untargeted branch-disagreement node appears, localize it before changing solver complexity, hysteresis limits or target scope.

## Hard constraints

Do **not**:

- activate H.9 or bounded hysteresis in production;
- modify `SimplifiedWaterSteamThermodynamicModel.Resolve()` branch order;
- widen the target set beyond `steam|stop-out|header|turbine-inlet`;
- retune 2% / 5 K hysteresis limits;
- retune P060/F040 or H.9 tolerances;
- change physical coefficients or the 10 ms production timestep;
- hide flow with filtering/clamping;
- commit shadow candidate states.

Read `docs/PROJECT_HANDOFF.md` and `docs/M10_9_4_1_H19_FOUR_NODE_LONG_HORIZON_CROSS_PROFILE_QUALIFICATION.md` before changing code.

## Package-time versus post-validation authority

H.18 Hotfix 1 is already user-validated and is the authoritative baseline encoded in this package. H.19 is only a candidate until the user reports the local H.19 gate result.
