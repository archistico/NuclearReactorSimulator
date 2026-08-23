# M10 Final Replacement-Long Failure Diagnostic 3 — Paralleled Governor / Mechanical Preload / Historical Version Discrimination

**Status:** CANDIDATE — evidence only. Replacement-Long Execution 1 remains RED; no second replacement long is authorized.

## Returned prerequisite

Replacement-Long Failure Diagnostic 2 completed PASS. Its returned artifact fixes these observations on the authoritative exact-v9 5→10 MWe path:

- the frozen `SupervisoryAutomatic + HoldCurrentOperatingPoint` reference again trips at logical step 636 with `generator-loss-of-synchronism` first latched;
- a supervisory rod-withdraw pulse produces zero physical rod motion and exactly reproduces the frozen trip;
- under `Assisted`, 2.5 / 5 / 10 percentage-point rod withdrawals do physically execute and increase reactor thermal power;
- nevertheless every Assisted rod/load probe still trips at the same logical step 636 with the same loss-of-synchronism owner;
- none survives two seconds after the load raise and none reaches a late stable 10 MWe window.

Therefore rod authority/coordination is not the missing margin. Protection retuning is not justified.

## Why another diagnostic is required before runtime repair

M7.6 describes generator-load manoeuvring as coordinated with both reactor power and turbine governing. Diagnostic 2 closed only the reactor/rod branch. Existing current-v2 turbine audits also document an important breaker-closed rule: when the generator is paralleled, the droop adapter derives the effective governor speed reference from requested electrical load and supersedes direct `TurbineSpeedRaise/Lower` setpoint authority.

Diagnostic 1 additionally showed the transient energy mismatch directly. At the 5→10 MWe command the electrical side applies roughly 10.2 MW of mechanical-equivalent electromagnetic loading while turbine shaft power is only about 5.6 MW. The governor saturates and the physical control valve opens at its finite 0.5 fraction/s travel rate, but shaft power does not rise before loss-of-synchronism completes its pickup.

Before changing generator-load semantics or exact-v9, this gate must distinguish three possibilities:

1. the existing breaker-closed turbine-speed command is ineffective by design and the workload lacks mechanical-power prepositioning;
2. physical valve preloading still cannot create enough transient shaft margin, pointing toward generator-load-order/control-granularity semantics;
3. historical exact-v4 behaves materially better than exact-v9, pointing instead toward an exact-v9 steam-path transient-capacity regression.

## Probe matrix

All probes use the existing 10 ms runtime and the same step-500 `GeneratorLoadRaise`. No production file is changed.

| Probe | Exact | Authority | Additional diagnostic action |
| --- | ---: | --- | --- |
| `exact-v9-frozen-supervisory-load-only` | 9 | SupervisoryAutomatic | frozen load-only reference |
| `exact-v9-assisted-load-only` | 9 | Assisted | D2 control |
| `exact-v9-assisted-speed-raise-1x-preload` | 9 | Assisted | one +10 rpm raw speed-reference command at step 400 |
| `exact-v9-assisted-speed-raise-5x-preload` | 9 | Assisted | five +10 rpm raw speed-reference commands at step 400 |
| `exact-v9-assisted-manual-valve-100-at-load` | 9 | Assisted | manual control-valve demand 100% at load step |
| `exact-v9-assisted-manual-valve-55-preload` | 9 | Assisted | manual control valve to 55% from step 400 |
| `exact-v9-assisted-manual-valve-65-preload` | 9 | Assisted | manual control valve to 65% from step 400 |
| `exact-v4-frozen-supervisory-load-only` | 4 | SupervisoryAutomatic | historical exact-version discrimination control |

The manual-valve probes are diagnostic perturbations only. They do not redefine M7.6 procedure or authorize a replacement workload.

## Evidence recorded

The gate writes:

- `150-turbine-governing-preload-version-probe-summary.csv` — one-row-per-probe owner/timing, raw/effective governor references, preload shaft/valve state, load-step shaft/electromagnetic loading, frequency margin and captured exception status;
- `151-turbine-governing-preload-version-trajectories.csv` — complete 10 ms trajectories;
- `152-turbine-governing-preload-version-decision-summary.txt` — branch classification inputs and decision rule;
- `00-progress.txt` — probe progress for partial-failure recovery.

Only two physics outcomes are asserted because they are already returned facts: the frozen exact-v9 and Assisted load-only controls must reproduce step-636 loss-of-synchronism. Other probe outcomes are census evidence, not hypotheses disguised as test expectations. Any diagnostic exception in a non-reference probe is captured into the artifact instead of discarding the rest of the matrix.

## Decision rule

1. If `TurbineSpeedRaise` changes the raw controller reference but not the effective breaker-closed governor setpoint, and trip timing remains unchanged, the direct SPEED seam is confirmed ineffective for paralleled load coordination.
2. If bounded physical valve preloading materially delays/avoids the trip, the frozen replacement operator policy is missing mechanical-power prepositioning. The next candidate must separately define and validate that operator/workload policy before a new baseline freeze. Do not retune protection.
3. If manual-at-load reproduces the automatic trajectory and preload also fails, compare exact-v4 with exact-v9:
   - matching failure → shared generator-load-order/control-granularity semantics; next work is a dedicated request-ramp/torque-coupling diagnostic before any runtime repair;
   - materially healthier exact-v4 → exact-v9-specific steam-path transient-capacity diagnosis.
4. Diagnostic 3 itself never authorizes a production change or second replacement long.

## Validation

Run:

```bat
scripts\run-m10-final-replacement-long-failure-diagnostic3.cmd
```

Return the complete:

```text
artifacts\m10-final-replacement-long-failure-diagnostic3
```

before changing replacement workload, authority policy, generator-load semantics, protection semantics, exact-v9 runtime, mission pack, or freezing a second replacement-long baseline.
