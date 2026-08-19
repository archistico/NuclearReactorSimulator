# M10.9.4.1-I.3 Hotfix 4 — Explicit-vs-Corrected Branch Discontinuity Comparison

## Status

CANDIDATE lineage document. I.2 remains the authoritative validated baseline. I.3 remains unvalidated. **The original admission-only decision rule below is superseded by Hotfix 4 Classifier Fix 1**, after the completed 10 ms run showed 8 reverse stop-valve drop steps plus 330 reverse admission-valve drop steps. See `M10_9_4_1_I3_HOTFIX4_CLASSIFIER_FIX1_TARGETED_TRAIN_REVERSE_FLOW.md`.

## Frozen red evidence

The completed 300 s exact-v2 I.3 diagnostic reported five isolated generation-health violations at 55, 66, 72, 79 and 88 s. In each sampled violation canonical turbine shaft power and stage flow are zero, admission-valve flow is negative, turbine-inlet pressure spikes, the turbine-inlet phase remains `SuperheatedVapor`, no trip occurs and global conservation remains closed.

The summary plus `06-generation-health-violations.csv` and `07-shaft-drop-episodes.csv` are copied into the test Evidence directory and protected by canonical SHA-256 fingerprints.

## Hotfix 4 experiment

Hotfix 4 runs the exact v2 explicit reference and exact v3 corrected candidate over the same first 100 simulated seconds at 10 ms resolution (10,000 steps per mode). It records:

- `steam`, `header`, `stop-out`, `control-out`, `turbine-inlet` pressures;
- stop/control/admission valve mass flow;
- turbine stage effective flow and canonical shaft power;
- generator request/output and rotor speed;
- four-node trigger/eligibility/commit/rollback counters for v3.

The comparison is diagnostic-only. It does not change H.30 policy or production selection.

## Original decision rule — superseded

The first Hotfix 4 candidate required every explicit drop to coincide specifically with reverse **admission** flow. The completed 10 ms evidence showed that this was too narrow: all 338 explicit drops coincide with reverse flow somewhere in the targeted stop/control/admission train, but 8 of those are stop-valve reversals and 330 are admission-valve reversals.

Classifier Fix 1 therefore requires one-for-one coincidence between explicit generation drops and targeted-train reverse flow, plus zero drops/reverse flow in v3 and the existing zero rollback/fallback/unsafe/untargeted-disagreement conditions.

If this corrected classification passes, do not weaken the I.3 shaft-health floor. Re-open the H.30 production-policy decision before freezing I.3 reference tolerance budgets.
