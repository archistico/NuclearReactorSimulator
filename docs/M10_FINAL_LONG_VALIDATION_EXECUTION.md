# M10 Final Pre-M11 Long Validation — Execution Handoff

## Baseline

Stacked exclusively on **M10 Final Pre-M11 Cumulative Validation Hotfix 1 VALIDATED**.
The cumulative marker is frozen in `eng/m10-final-cumulative-validation-record.json`.

## Command

From the repository root:

```bat
scripts\run-m10-final-long-validation.cmd
```

Do not run the five explicit tests individually for promotion evidence. The orchestrator validates the frozen contract, restores/builds the long test surface, executes every leg even if an earlier leg fails, and finalizes the complete artifact set.

## Expected successful terminal markers

```text
m10-long-workload-completed=True
m10-long-simulated-seconds=14400
m10-long-logical-steps=1440000
m10-long-conservation-ceilings-pass=True
m10-long-healthy-budget-sentinels-pass=True
m10-long-numerical-coupling-safety-pass=True
m10-long-mission-pack-v2-pass=True
m10-long-degraded-recovery-pass=True
m10-long-protection-takeover-pass=True
m10-long-replay-checkpoint-sentinels-pass=True
m10-long-evidence-growth-bounded=True
m10-final-long-validation-passes=True
m10-closure-eligible=True
```

If any leg fails, M10 remains OPEN. Preserve the entire artifact directory and investigate the first physical/evidence divergence; do not widen frozen tolerances to obtain a green run.

## First acceptance execution — current evidence

The first Hotfix-1 campaign is fail-collect and was still running when the pre-M11 documentation consolidation was prepared. LR-H1 already failed after approximately 48m54s wall-clock with:

```text
WaterSteamStateOutOfRangeException
node=outlet
v=0.0026153411609661885 m^3/kg
u=1615124.4119888516 J/kg
```

The exception originated in the canonical production path (`SimplifiedWaterSteamThermodynamicModel.Resolve` through the integrated full-plant/control runtime), so it is not currently classified as a harness-only failure. The remaining legs must be allowed to finish so the complete diagnostic artifact set is preserved.

M10 is therefore already ineligible for closure on this run. The next engineering action after campaign completion is evidence classification using `M10_LR_H1_EQUILIBRIUM_DIAGNOSTIC_PLAN.md`; no thermodynamic-envelope widening, I.3 budget retuning, conservation-ceiling retuning or workload reduction is authorized merely to obtain a pass.
