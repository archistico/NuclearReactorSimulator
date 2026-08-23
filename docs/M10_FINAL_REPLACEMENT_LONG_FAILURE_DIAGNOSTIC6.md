# M10 Final Replacement-Long Failure Diagnostic 6 — First-Stage Long Settling / Steam-Path Lag / Synchronous Recovery

**Status:** RETURNED / EXECUTION PASS — diagnostic evidence only. Replacement-Long Execution 1 remains RED; no second replacement long is authorized.

## Returned prerequisite

Diagnostic 5 completed PASS after first running the ordinary Release CI entry point. Its exact-v9 reference again reproduced `generator-loss-of-synchronism` at step 636 for the frozen 5→10 MWe step.

The readiness-gated probes did **not** trip. Instead:

- exact-v9 +1 MWe reached the measured thermal target for 6 MWe, issued the load command, and timed out after 20 s without a stable stage;
- exact-v9 +0.5 MWe completed only its first 5.5 MWe stage and then timed out at the 6 MWe stage;
- exact-v4 +1 MWe showed the same broad behavior;
- no probe reached 10 MWe.

The returned trajectory adds an important correction to the interpretation. At the instant measured reactor thermal readiness is reached, turbine shaft power, turbine flow and governor valve position remain close to the preceding 5 MWe operating point. For exact-v9 +1 MWe, the 6 MWe command is applied around 39.32 MWth while shaft power is still about 5.60 MW, turbine flow about 13.03 kg/s and control-valve position about 29.3%. The governor cannot maintain a steady mechanical surplus while electrical load is still held at 5 MWe; upstream thermal readiness is therefore not the same thing as mechanical readiness.

Diagnostic 5's 20 s `settle-timeout` must consequently not be frozen as a hard 5.5–6 MWe capacity boundary. Shaft power and turbine flow were still evolving at timeout.


## Returned Diagnostic 6 result

The local gate completed PASS after the ordinary Release entry point. All three exploratory long-settle probes completed their 180 s post-command hold without protection, but none reached the strict synchronous operating-point window. Returned tail means are:

| Probe | Frequency | Electrical output | Shaft power | Dispatch mechanical adequacy | Phase wraps |
| --- | ---: | ---: | ---: | ---: | ---: |
| exact-v9 5.5 MWe | 50.000197136812517 Hz | 5.2462291021285026 MWe | 5.8532957365031475 MW | -0.2589531044439255 MW | 5 |
| exact-v9 6 MWe | 50.000283643052136 Hz | 5.7338236172924848 MWe | 6.3508356185870802 MW | -0.27161903467884968 MW | 25 |
| exact-v4 6 MWe | 50.000248858742545 Hz | 5.653597523127539 MWe | 6.2689687961365985 MW | -0.3534851616501925 MW | 28 |

Late net rotor acceleration is approximately zero, so the fast loss-of-synchronism transient has recovered into a near-frequency-locked state. However, requested electrical output is still not reached, the strict 5 s synchronous criterion remains unmet, and the steam/shaft path is still slowly evolving. Diagnostic 6 therefore separates **frequency/rotor recovery** from **qualified operating-point convergence**; it does not prove a hard capacity boundary and does not authorize a workload or runtime repair.

The continuation is governed by [`M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN.md`](M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN.md). Diagnostic 1–6 are now frozen as one completed evidence campaign; the next authorized implementation after P0 is P1 Asymptotic First-Stage Qualification.

## Remaining question

Before authoring a coupled thermal/electrical ramp or changing generator-grid semantics, determine whether the first small load stage eventually returns to a genuinely synchronous operating point when given enough steam-path and rotor settling time.

Diagnostic 6 therefore holds only the first target stage for a long horizon:

| Probe | Exact | Test-only increment | Target | Post-command hold |
| --- | ---: | ---: | ---: | ---: |
| frozen reference | 9 | +5 MWe | 10 MWe | reference reproduction |
| long settle A | 9 | +0.5 MWe | 5.5 MWe | 180 s |
| long settle B | 9 | +1 MWe | 6 MWe | 180 s |
| historical control | 4 | +1 MWe | 6 MWe | 180 s |

The staged probes retain Diagnostic 5's measured thermal preparation. Once thermal readiness is reached, they issue exactly one test-only load increment and then **do not advance further**.

## Strict synchronous criterion

A five-second continuous window is recorded when all of the following hold:

- breaker remains closed and no protection action is active;
- generator frequency slip is within ±0.01 Hz;
- electrical output is within ±0.10 MWe of the target stage;
- net rotor acceleration power is within ±0.05 MW.

The criterion is intentionally tighter than Diagnostic 5's broad ±0.5 MWe stage acceptance. It is diagnostic only and does not redefine any production operating envelope.

## Evidence decomposition

The trajectory records every 10 ms:

- requested and actual electrical power;
- reactor thermal power;
- turbine shaft power and passive mechanical loss;
- actual electromagnetic external-load power;
- requested mechanical dispatch (`requested electrical / generator efficiency`);
- net rotor acceleration power;
- dispatch mechanical adequacy (`shaft - passive loss - requested mechanical dispatch`);
- turbine steam flow, inlet pressure, effective specific work, valve position and relief flow;
- rotor speed, signed frequency slip and signed generator/grid phase lead;
- phase synchronizing correction and frequency-damping correction power;
- commanded/effective electromagnetic torque and protection state.

The summary additionally reports the first strict synchronous window, last-30-second synchronous fraction, cumulative frequency-slip cycles and signed phase-wrap count.

## Decision rule

If exact-v9 5.5 and/or 6 MWe recovers a strict synchronous window and the last 30 s remains predominantly locked, Diagnostic 5's 20 s timeout was primarily a steam-path/rotor settling-time issue. The next candidate should then test a coupled or staged replacement workload using evidence-derived dwell, without changing protection or generator-grid coupling.

If the long hold remains unlocked and tail `dispatch mechanical adequacy` is materially negative, the first owner is steam-path / energy-transfer capacity or the thermal-target mapping, not protection.

If dispatch mechanical adequacy closes near zero while frequency continues to slip and the signed phase repeatedly wraps, the next diagnostic should isolate synchronous-grid coupling semantics/coefficients before any workload repair.

If exact-v4 materially differs from exact-v9 under the same 6 MWe long hold, localize the remaining capacity difference to exact-v9. Matching behavior remains shared-model evidence.

Diagnostic 6 authorizes no production change and no second replacement-long freeze.

## Validation

The script first executes the ordinary Release CI gate and then the focused explicit census:

```bat
scripts\run-m10-final-replacement-long-failure-diagnostic6.cmd
```

Return the complete `artifacts\m10-final-replacement-long-failure-diagnostic6` folder before changing replacement workload, authority policy, generator-load semantics, protection semantics, exact-v9 runtime, mission pack, or freezing a second replacement-long baseline.
