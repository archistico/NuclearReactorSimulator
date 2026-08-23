# M10 Final Replacement-Long Failure Diagnostic 5 — Measured Readiness-Gated Staged Load / Attainable Capacity

**Status:** RETURNED / EXECUTION PASS — evidence only. Replacement-Long Execution 1 remains RED; no second replacement long is authorized.

## Returned prerequisite

Diagnostic 4 completed PASS. Its reference exact-v9 probe again reproduced `generator-loss-of-synchronism` at step 636. Smaller/slower fixed-time electrical ramps delayed the protection response but none established a stable 10 MWe window. One-second proportional reactor support did not change the result, and the nominal `66 MWth` pre-power probe still had only about 37.3 MWth at the actual 5→10 MWe step, with shaft power near 5.6 MW.

That last point matters: Diagnostic 4 proved that a **20 s commanded pre-power interval is insufficient**; it did not prove that a thermally prepared high-load state is unattainable.

## Remaining question

M7.6 requires stabilization after each load change, but neither the failed replacement-long workload nor Diagnostic 4 waited for measured plant readiness before applying the next electrical increment.

Diagnostic 5 therefore removes elapsed-time assumptions. For each staged probe it:

1. starts from the exact production/historical low-load operating point;
2. requests the reactor thermal power proportional to the **next** electrical-load stage;
3. waits until measured thermal power is within 0.25 MW of that target while the generator remains paralleled and near synchronous frequency;
4. only then applies one test-only generator-load increment;
5. requires one continuous second of protected near-target electrical output/frequency before advancing;
6. after reaching 10 MWe, requires a further five continuous seconds of protected stable operation.

This is a diagnostic scheduler only. It does not modify production command semantics or the frozen replacement workload.

## Probe matrix

| Probe | Exact | Increment | Purpose |
| --- | ---: | ---: | --- |
| frozen reference | 9 | +5 MWe | reproduce the returned step-636 owner |
| readiness staged A | 9 | +1 MWe | test attainable 10 MWe with measured thermal readiness |
| readiness staged B | 9 | +0.5 MWe | separate readiness from electrical-command granularity |
| historical control | 4 | +1 MWe | determine whether any remaining failure is shared or exact-v9-specific |

The +1/+0.5 MWe policies exist only in diagnostic engine clones. `ControlRoomRuntimeCommandPolicy.Default` remains unchanged.

## Evidence

The gate writes:

- `00-progress.txt`
- `180-readiness-gated-probe-summary.csv`
- `181-readiness-gated-stage-events.csv`
- `182-readiness-gated-trajectories.csv`
- `183-readiness-gated-decision-summary.txt`

Trajectory evidence includes requested/actual electrical power, reactor thermal power, generator mechanical input, turbine shaft power, turbine steam flow, turbine-inlet pressure, effective specific work, control-valve position, relief flow, rotor/frequency/phase, commanded/effective electromagnetic torque and protection state.

## Decision rule

If either exact-v9 readiness-gated schedule reaches and holds a protected stable 10 MWe window, the reduced-order plant has demonstrated attainable high-load capacity. The failed replacement-long path is then a workload/procedure timing plus command-granularity qualification gap; the next candidate should repair the workload/operator policy and freeze a new replacement baseline without retuning protection or generator-grid physics.

If measured readiness is reached for successive stages but a repeatable electrical stage still trips, use the recorded steam-flow/pressure/specific-work/relief and torque evidence to localize the first true capacity boundary.

If measured thermal readiness itself cannot be reached while holding the preceding stable electrical load, do not treat that as proof that the generator coupling is wrong. It instead shows that simple pre-powering is incompatible with the current reduced-order energy inventory; the next investigation should use an explicitly coupled reactor/load ramp rather than storing energy upstream.

Historical exact-v4 is interpreted only under the same readiness algorithm.

Diagnostic 5 authorizes no production change and no second replacement-long freeze.

## Validation

The focused script deliberately runs the established ordinary **Release** CI entry point first, then the explicit diagnostic:

```bat
scripts\run-m10-final-replacement-long-failure-diagnostic5.cmd
```

Return the complete `artifacts\m10-final-replacement-long-failure-diagnostic5` folder before changing replacement workload, authority policy, generator-load semantics, protection semantics, exact-v9 runtime, mission pack, or freezing a second replacement-long baseline.
