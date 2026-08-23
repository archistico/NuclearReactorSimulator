# M10 Final Replacement-Long Failure Diagnostic 4 — Load Ramp / Torque Coupling / Energy Support Discrimination

**Status:** CANDIDATE — evidence only. Replacement-Long Execution 1 remains RED; no second replacement long is authorized.

## Returned prerequisite

Diagnostic 3 completed PASS. The returned evidence establishes that the exact-v9 frozen 5→10 MWe step trips at logical step 636 with `generator-loss-of-synchronism`; breaker-closed `TurbineSpeedRaise` changes only the raw reference because droop owns the effective governor reference; 55%/65% valve preloading does not create material shaft-power margin; and historical exact-v4 reproduces the same protected failure family, slightly earlier at step 628.

This removes rod authority, direct SPEED authority, simple valve prepositioning, protection retuning and an exact-v9-only regression as supported first repairs.

## Remaining question

At the frozen command the electrical request changes by the production default 5 MWe increment essentially in one 10 ms step while available shaft power is only about 5.6 MW. Diagnostic 2 showed that short rod withdrawals increase thermal power only slightly before the trip. Diagnostic 3 showed that opening the valve without creating additional upstream energy does not solve that mismatch.

Before changing generator-load semantics or the replacement workload, the remaining discrimination is therefore between:

1. command granularity / instantaneous torque demand — a smaller or slower electrical request ramp is sufficient even at the current operating point;
2. missing slow energy support — load ramp alone is insufficient, but coordinated reactor-power support or deliberate pre-powering establishes enough shaft margin;
3. deeper generator-grid torque coupling / attainable-capacity behaviour — even materially increased thermal/shaft support cannot establish a stable protected 10 MWe window.

## Probe matrix

All probes are test-only. Production `ControlRoomRuntimeCommandPolicy.Default` remains unchanged. Test-only engine clones use the same solver/state/snapshot with diagnostic load increments where required.

| Probe | Exact | Electrical schedule | Energy support |
| --- | ---: | --- | --- |
| reference | 9 | +5 MWe at step 500 | hold current operating point |
| ramp A | 9 | +1 MWe every 1 s ×5 | hold current |
| ramp B | 9 | +0.5 MWe every 0.5 s ×10 | hold current |
| ramp C | 9 | +1 MWe every 2 s ×5 | hold current |
| supported ramp | 9 | +1 MWe every 2 s ×5 | reactor target scaled to next requested load, 1 s lead |
| pre-power | 9 | +5 MWe at step 2000 | reactor target 66 MWth from step 100 |
| historical supported | 4 | +1 MWe every 2 s ×5 | same proportional reactor support |

Every probe starts from `HoldCurrentOperatingPoint`; exploratory reactor objectives replace it only at their explicit support step.

## Evidence

The gate writes `160-load-ramp-energy-support-probe-summary.csv`, `161-load-ramp-energy-support-trajectories.csv`, `162-load-ramp-energy-support-decision-summary.txt` and `00-progress.txt`. It records requested/output power, reactor thermal power, generator mechanical input, turbine shaft power, rotor/frequency/phase, commanded/effective electromagnetic torque, protection owner/timing and captured exceptions.

Only the already-returned exact-v9 reference result is asserted: step-636 `generator-loss-of-synchronism`. Exploratory outcomes remain census evidence rather than expected answers.

## Decision rule

If a smaller/slower load-only ramp reaches a stable protected 10 MWe window, investigate command/workload granularity before production runtime repair. If load-only fails but reactor-supported or pre-powered operation succeeds, classify the frozen replacement manoeuvre as missing slow energy-support coordination. If support materially raises thermal/shaft power yet stable 10 MWe remains unattainable, escalate to generator-grid torque-coupling/attainable-capacity repair analysis. Historical exact-v4 is compared only under the same supported schedule to separate shared behaviour from version-specific steam-path capacity.

Diagnostic 4 authorizes no production change and no second replacement-long freeze.

## Validation

Run:

```bat
scripts\run-m10-final-replacement-long-failure-diagnostic4.cmd
```

Return the complete `artifacts\m10-final-replacement-long-failure-diagnostic4` folder before changing replacement workload, authority policy, generator-load semantics, protection semantics, exact-v9 runtime, mission pack, or freezing a second replacement-long baseline.
