# M10 Final — VR2 Materiality Diagnostic 1 Returned-Evidence Adjudication

**Status:** RETURNED PASS — INTERPRETATION CLOSED  
**Source evidence:** Materiality Diagnostic 1 Attempt 5 complete seven-file artifact set  
**Authority boundary:** evidence adjudication only. No production repair, thermodynamic tolerance change, exact-v9 change, VR3, P3-R1 or second replacement-long execution is authorized.

## Purpose

Attempt 5 completed the full 600 s background plus exact-v9 5→6 MWe P1B trajectory to 3,600 s and wrote all seven planned artifacts. The generated summary reports `execution-pass=True` and `classification=HYDRAULIC-MATERIALITY-CONFIRMED`. The xUnit process then returned RED because a final self-consistency assertion required the committed canonical flow to equal the instantaneous quadratic hydraulic map to `1e-9 kg/s`.

That assertion is incompatible with the numerical contract of the exact-v9 hydraulic coupling. Exact-v9 uses `HydraulicNumericalCouplingDefinition.H22FourNodeBranchContinuityCorrectedCommitOptIn`, whose authoritative `CorrectorAbsoluteFlowToleranceKilogramsPerSecond` is `1e-2`. H.22 therefore permits a converged committed fixed-point iterate to differ from its instantaneous map by up to `0.01 kg/s`. Attempt 5 measured `0.009952798974779853 kg/s`, which is below that production numerical ceiling.

The adjudication does not rewrite the failed `1e-9` assertion. It freezes the original RED and the returned artifacts, then asks whether the engineering classification remains valid after accounting conservatively for the full H.22 fixed-point uncertainty.

## Frozen returned evidence

The adjudication requires the exact Attempt-5 evidence set under `eng/frozen-evidence/ordinary/M10FinalPhysicalReferenceVR2MaterialityDiagnostic1_Attempt5_Artifacts/`:

- `01-contract-and-provenance.txt`;
- `02-p1b-checkpoint-reproduction.csv`;
- `03-node-if97-inverse-map.csv`;
- `04-hydraulic-path-counterfactual.csv`;
- `05-late-window-materiality.csv`;
- `06-materiality-summary.txt`;
- `07-sentinels.txt`.

Required returned facts are 3/3 P1B checkpoint reproduction, 360 node rows with zero unresolved rows, 288 hydraulic-path rows with zero unresolved rows, zero trip/nonconvergence/nonfinite/rollback sentinels, deterministic repeat, eight CONFIRMED windows and eight NOT-EXCLUDED windows.

## Fixed-point adjudication rule

The original materiality thresholds are not changed. For each returned late window:

```text
conservative_shift = max(0, returned_mean_abs_counterfactual_shift - 0.01 kg/s)
conservative_impact_ratio = conservative_shift / frozen_phenomenon_scale
```

The bands remain:

```text
CONFIRMED      impact ratio >= 1.0, or driving-pressure sign change
NOT-EXCLUDED   0.1 <= impact ratio < 1.0
NOT-DEMONSTRATED impact ratio < 0.1
```

Persistence remains at least three of four windows on the same path.

The returned data have large margin relative to the `0.01 kg/s` numerical bound. `CHANNEL` remains CONFIRMED in all four late windows and changes driving-pressure sign; `FEEDWATER-PUMP` remains CONFIRMED in all four windows. `MCP` and `RETURN` remain NOT-EXCLUDED in all four windows. The smallest returned RETURN impact ratio is about `0.11627`; after subtracting the full H.22 bound it remains about `0.11616`, above the frozen `0.1` threshold.

## Phase-boundary evidence discovered on the actual path

The actual exact-v9 inventories reveal a stronger closure issue than the fixed VR2 matrix alone showed. For all 72 sampled `pressure` states, production labels the state `SubcooledLiquid`, while independent IF97 inversion of the same `(v,u)` resolves Region 4 `SaturatedMixture`. The same production-subcooled / IF97-mixture reinterpretation occurs in 55 of 72 `suction` samples.

This means later repair planning must investigate the inverse thermodynamic closure and phase-boundary ownership together. A repair that merely retunes a constant compressed-liquid pressure coefficient would not be justified by the returned evidence.

## Execution

The adjudication intentionally does **not** replay the 3,600 s trajectory. It performs:

1. static frozen-evidence and contract audit;
2. the complete ordinary Release gate;
3. a focused C# adjudication of the returned artifact set against the authoritative H.22 fixed-point residual contract.

PowerShell command:

```powershell
.\scripts\run-m10-final-vr2-replanning-materiality-diagnostic1-adjudication.cmd
```

Expected output folder:

```text
artifacts/m10-final-physical-reference-vr2-materiality-diagnostic1-adjudication
```

A PASS closes only the interpretation of Materiality Diagnostic 1. It does not authorize production changes or VR3. The returned adjudication artifact must be reviewed before a separate repair-planning decision.

## Returned adjudication result

The returned adjudication artifact closes the diagnostic-evidence interpretation with:

```text
engineering-classification=HYDRAULIC-MATERIALITY-CONFIRMED
production-repair-authorized=False
thermodynamic-tolerance-change-authorized=False
exact-v9-change-authorized=False
vr3-authorized=False
p3-r1-authorized=False
second-replacement-long-authorized=False
```

The next gate is planning-only: [`M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1.md`](M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1.md).
