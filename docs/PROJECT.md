# Project — current authoritative state

## Current checkpoint - 2026-11-10 - H numerical review and offline local-model design

NRS-MARKER:R3-H-LOCAL-MODEL-DESIGN1-CURRENT

NRS-MARKER:R3-H-NUMERICAL-STOP-REVIEW1-CURRENT

This is the current checkpoint; earlier checkpoints are archived.

The bounded H search remains frozen as inconclusive. **Numerical Stop Review 1** is
locally **PASS-REVIEW-EVIDENCE-COMPLETE**. It
authenticates its complete compressed trace and reconstructs only the final local
linear model. No plant evaluation or new seed construction is performed.

Iteration 8 contains 48 valid derivative probes and 78 trial rejections, all before
the plant evaluator. The final half-step in every damping attempt crosses the same
unchanged raw suction-energy bound. The six recorded full trial directions are
bit-identical. The independent replay reproduces them with zero coordinate error;
all six normal matrices and all diagonals versus zero damping are bit-identical.
The first box-admissible halving on that direction is 15, beyond the frozen 12.
Three derivative probes change phase labels; local physical smoothness is unproven.
This localizes the final stop to the numerical direction/box handling;
it does not demonstrate physical infeasibility or an independent controller defect.

Review and next-design contract:
[H Numerical Stop Review 1](../eng/contracts/m10/M10_FINAL_VR2_R3_H_NUMERICAL_STOP_REVIEW1.md).

```powershell
.\scripts\run-r3-h-numerical-stop-review1.cmd
```

The prescribed **offline bounded scaled local-model design** is implemented as a
separate numerical comparison: six fixed damping values, column-normalized directions
and one global fraction to the existing box. Six known numerical fixtures verify
the transform/bound handling. It is locally **PASS-OFFLINE-DESIGN-EVIDENCE** and uses
only the frozen residual/Jacobian. All six scaled dampings affect all 24 diagonals;
all six capped directions are box-admissible and predict strict model descent.
Predicted maxima range from about 6.908 million to 7.172 million versus 7.226 million
at the frozen point. No physical candidate is selected; no nonlinear improvement is
claimed. The original unscaled direction is retained as a separate model comparator.

Design payloads are frozen under `eng/frozen-evidence/r3-h-local-model1`. Seven negative
contract cases reject plant/nonlinear calls, residual reweighting, altered damping,
wider bounds and baseline/input-digest drift. Production/tests remain unchanged.

Specification: [H Local-Model Design 1](../eng/contracts/m10/M10_FINAL_VR2_R3_H_LOCAL_MODEL_DESIGN1.md).

```powershell
.\scripts\run-r3-h-local-model-design1.cmd
```

Next work is review of these local-model predictions before any separately frozen
nonlinear plan. No new nonlinear search, extra plant probes, bound expansion, extra
unknowns, controller/seed edits or C/J are authorized. R3 stays RED; Requalification 3
and R4 remain blocked.

This gate proves production and all 406 test files unchanged. The inherited ordinary
PASS in the archived checkpoint remains provenance; no new ordinary-suite execution is claimed.
Eight negative audit cases reject authority/bounds expansion, replay-guard widening,
baseline/digest/marker drift, physical-search continuation and output outside artifacts.
The compact review payloads are frozen under `eng/frozen-evidence/r3-h-numerics1`;
the complete H1 input trace stays unchanged in its original frozen archive.

## Earlier checkpoints

The complete preceding state and handoff history is preserved in the
[2026-11-11 snapshot](history/archives/README.md#snapshot-docs-project-md).
Use the checkpoint above for continuation.

For the consolidated engineering context, use the [validation summary](validation/README.md).
