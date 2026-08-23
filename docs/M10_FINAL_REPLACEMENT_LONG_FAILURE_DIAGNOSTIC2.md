# M10 Final Replacement-Long Failure Diagnostic 2

## Status

**RETURNED / EXECUTION PASS — evidence-only authority / coordinated-manoeuvre discrimination.** Replacement-Long Execution 1 remains authoritative **RED** evidence. Diagnostic 1 has now returned PASS and identified `generator-loss-of-synchronism` as the first protection function that completes its pickup/latch chain: trigger begins at step 587 and the 0.5 s pickup latches at step 636 / 6.36 s. Underfrequency begins earlier but does not complete its 1 s pickup before the generator trip.

No protection threshold, exact-v9 state, runtime physics, mission @3 definition or replacement-long workload is changed here.

## Why Diagnostic 2 is needed

The frozen replacement workload used a deliberately simple operator policy: when the external demand first reaches 10 MWe, dispatch one `GeneratorLoadRaise`; when it later returns to 5 MWe, dispatch one `GeneratorLoadLower`.

That policy is weaker than the already validated M7.6 procedure. `POWER_MANOEUVRING_NORMAL_SHUTDOWN.md` explicitly requires a load raise to be coordinated with rod withdrawal/HOLD and turbine governing, with stabilization after each change.

Diagnostic 1 additionally showed why the distinction matters on exact-v9. The 5 MWe request increment immediately raises electrical loading to almost 10 MWe while turbine shaft power remains near 5.6 MW and reactor thermal power remains at ~32.97 MWth. The governor drives admission open, but the rotor decelerates, frequency falls and phase slip accumulates until loss-of-synchronism trips.

There is also an authority-semantic question. The failed long and Diagnostic 1 run `SupervisoryAutomatic` with `HoldCurrentOperatingPoint`. In that mode `SupervisoryOperationCoordinator` rewrites the reactor-power and turbine-speed loops back to automatic setpoints before each physical step. An operator rod command can therefore be neutralized before it reaches the plant, which would make the M7.6 coordination instruction impossible under the frozen validation policy even though the command seam itself is valid.

## Probe matrix

Diagnostic 2 therefore runs eight independent 12 s exact-v9 probes. Every probe issues the same `GeneratorLoadRaise` before logical step 500. It compares:

- the exact frozen `SupervisoryAutomatic + HoldCurrentOperatingPoint + load-only` reference;
- the same supervisory path with a rod-withdraw/HOLD pulse spanning the load raise, specifically to test whether supervisory ownership suppresses it;
- an `Assisted` load-only control;
- five bounded `Assisted` rod-withdraw/HOLD timings around the same load raise.

`Assisted` is intentional: it preserves the existing local automatic turbine governor while allowing an operator command to take direct ownership of the selected rod controller. Manual takeover is not used because it would place every local controller, including turbine governing, in manual mode.

The test records every 10 ms sample for requested/actual electrical power, reactor thermal power, generator mechanical input, rotor speed, frequency, phase difference, average rod withdrawal and trip state. Each probe also records first trip, first latched protection owner and final controller/authority state.

## Decision rule

The diagnostic execution itself is an evidence gate, not a hidden acceptance retune.

Compare the supervisory rod probe directly with the frozen load-only reference rather than assuming suppression. If its command produces no material change in rod/plant trajectory and it reproduces the frozen loss-of-synchronism trip path, while at least one Assisted coordinated probe materially delays or avoids the trip, the replacement workload/authority policy is under-specified relative to M7.6. If a bounded Assisted probe also reaches a late stable 10 MWe window with breaker closed and no trip, that path can become evidence for a **separate** revised replacement operator policy. Such a change still requires a new baseline/workload freeze before any second long.

If no Assisted coordinated probe improves the protection margin, do not weaken protection and do not rewrite the workload. Continue with a production transient/control-granularity diagnostic to determine whether a valid 5 MWe step is too coarse for the exact-v9 generator/turbine dynamics or another production runtime owner is deficient.

## Validation

Run:

```bat
scripts\run-m10-final-replacement-long-failure-diagnostic2.cmd
```

Return the complete:

```text
artifacts\m10-final-replacement-long-failure-diagnostic2
```

before changing replacement workload, authority policy, protection semantics, exact-v9 runtime, mission pack or freezing a second replacement-long baseline.
