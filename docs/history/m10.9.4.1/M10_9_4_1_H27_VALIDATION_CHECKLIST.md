# M10.9.4.1-H.27 Validation Checklist

## Validation result

**PASSED on 2026-08-19 (Hotfix 1).** Six scenarios completed 2,080 runtime steps with 529 corrected commits, zero unsafe/fallback commits, four `corrected-qualified` and two `protected-boundary` classifications.

## Preconditions

- [ ] Baseline is user-validated H.26 Hotfix 1.
- [ ] Standard current-v2 is `ExplicitCommittedState` at 10 ms.
- [ ] H.24 remains frozen rare qualification evidence and is not rerun.
- [ ] P060/F040, H.9, 2%/5 K hysteresis, H.20, H.22 and the four-node target set are unchanged.

## Local gate

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-four-node-off-design-qualification-envelope-audit.cmd
```

## Focused requirements

- [ ] Six staged off-design scenarios complete their bounded evidence windows.
- [ ] Every scenario observes at least one P060/F040 trigger.
- [ ] `high-load-10mwe` reaches/observes the 10 MWe requested-load point.
- [ ] A canonical protection action after reaching 10 MWe is classified as `protected-boundary`; it is not by itself an H.27 failure.
- [ ] Cooling-degradation/loss cases show non-decreasing peak condenser backpressure relative to their initial point.
- [ ] Zero fallback-commit violations.
- [ ] Zero unsafe corrected commits.
- [ ] Every rollback produces no corrected commit.
- [ ] Network mass/energy closure and balance ownership remain within H.22 limits.
- [ ] Determinism control repeats exactly.
- [ ] Standard factory remains `ExplicitCommittedState`.
- [ ] `four-node-off-design-robustness-qualification-envelope-passes=True`.
- [ ] `h27-audit-passes=True`.

## Interpretation

Safe rollback is not failure. Canonical protection action in a protection-adjacent case is not failure. The result must be interpreted from `03-off-design-qualification-envelope.csv`, not from a simplistic requirement that every scenario remain corrected-owned and trip-free.

A single unsafe corrected commit is a hard failure.
