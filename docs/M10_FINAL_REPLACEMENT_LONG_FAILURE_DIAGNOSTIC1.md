# M10 Final Replacement-Long Failure Diagnostic 1

**Status:** RETURNED / EXECUTION PASS — diagnostic evidence only. Replacement-Long Execution 1 remains RED.

The first authorized exact-v9 replacement campaign executed all five frozen legs in 35.2527 workstation minutes, so the redesigned wall budget is validated. The campaign remains RED because RL-M1 and RL-R1 both entered protection after the same 5 -> 10 MWe generator-load raise.

The failure is narrower than the aggregate summary initially suggests. RL-H1 passed 900 s with exact-v9 operating-point, conservation and numerical-coupling sentinels. RL-D1 and RL-P1 passed. MISSION live-projection scalability passed with late/early ratio 0.969616. RL-R1 produced identical authored, full-replay and checkpoint-continuation fingerprints, exact recording equivalence and bounded archive growth. Its only failing criterion was the same trip seen by RL-M1.

RL-M1 reached the 10 MWe demand at logical step 500, dispatched one `GeneratorLoadRaise`, and the challenge terminated `Failed` at step 637. RL-R1 independently reproduced protection after its load raise, leaving 5,365 of 6,000 authored steps with trip active. Therefore replay/checkpoint determinism is not the blocker; the common transient is.

The frozen replacement workload used only generator-load raise/lower commands. Existing M7.6 guidance is stronger: an on-grid load increase must coordinate generator loading with deliberate rod withdrawal/HOLD and turbine governing, stabilizing after each change. This makes an under-specified validation operator policy plausible, but it is not yet accepted as the diagnosis because the exact relay owner was not captured by Execution 1.

Diagnostic 1 changes no runtime source, protection threshold, exact-v9 seed, challenge pack or replacement workload. It repeats only the first 10 simulated seconds, dispatches the same 5 MWe load raise at step 500, and records every protection function on every 10 ms step together with reactor/turbine/generator/governor state. The returned evidence must identify first trigger, pickup and latch step for each relevant protection function before any repair or workload rewrite is authored.

The failed replacement long remains authoritative RED evidence. M10 remains OPEN. A second replacement campaign may not run until the resulting decision is implemented and a new baseline/workload freeze explicitly authorizes it.
