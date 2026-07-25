# M10.9.4.1-D.2 — Turbine Admission Authority Evidence

## Purpose

This checkpoint measures the hydraulic authority already present in the current-v2 turbine-admission path **before** changing stage resistance, valve sizing, governor gains or the turbine flow law.

D.2 is audit-first. It deliberately does not choose a Stodola/ellipse law, an effective-area law or a resistance retune until the deterministic evidence has been run locally.

## Current-v2 canonical resistance budget

The sustained current-v2 seed currently owns:

- steam-drum internal steam-source resistance: `100 Pa·s²/kg²`;
- loaded desktop main-steam line resistance after D.3.2 Hotfix 3: `850 Pa·s²/kg²`;
- breaker-open synchronization main-steam line resistance: `1,000 Pa·s²/kg²`;
- stop-valve base resistance: `1,000 Pa·s²/kg²`;
- control-valve base resistance: `1,000 Pa·s²/kg²`;
- admission-valve base resistance: `1,000 Pa·s²/kg²`;
- turbine-stage expansion resistance: `21,400 Pa·s²/kg²`;
- control-valve characteristic: linear;
- sustained synchronization control-valve seed position: `28%`;
- loaded desktop control-valve seed position after D.3.2 Hotfix 2: `28%`.

For a linear valve the current valve solver gives an effective hydraulic resistance proportional to `1 / position²`. The table below is therefore an **analytical resistance-budget map at equal total pressure head**, not a substitute for the dynamic plant journey.

| Control valve | Effective control-valve R | Total idealized series R | Flow capacity vs full-open | Additional capacity available to full-open | Control-valve share of total R |
|---:|---:|---:|---:|---:|---:|
| 10% | 100,000.0 | 124,500.0 | 45.26% | +120.96% | 80.32% |
| 20% | 25,000.0 | 49,500.0 | 71.77% | +39.33% | 50.51% |
| **28% synchronization seed** | **12,755.1** | **37,255.1** | **82.73%** | **+20.87%** | **34.24%** |
| 30% comparison point | 11,111.1 | 35,611.1 | 84.62% | +18.17% | 31.20% |
| 40% | 6,250.0 | 30,750.0 | 91.06% | +9.81% | 20.33% |
| 46% historical audit point | 4,725.9 | 29,225.9 | 93.41% | +7.06% | 16.17% |
| 60% | 2,777.8 | 27,277.8 | 96.69% | +3.43% | 10.18% |
| 80% | 1,562.5 | 26,062.5 | 98.91% | +1.10% | 6.00% |
| 100% | 1,000.0 | 25,500.0 | 100.00% | — | 3.92% |

## Interpretation

The earlier external audit correctly identified a structural risk when reasoning from the older ~46% operating bias: above that region, the large fixed stage resistance leaves limited additional hydraulic authority.

The current-v2 profiles are materially different from the older 46% audit point. Both sustained profiles retain a **28%** initial control-valve bias, where the valve represents about **34.24%** of the idealized total resistance and retains about **20.87%** equal-head capacity headroom. The profiles still remain operationally distinct because synchronization uses PI with an open breaker while the loaded desktop uses PID with a closed breaker and a 5 MWe request.

However, authority still compresses strongly at large openings:

- by 60% open the control valve is only about 10% of the total idealized resistance;
- by 80% open, going fully open adds only about 1.1% theoretical flow capacity;
- at full open, the stage and other fixed paths dominate almost the entire resistance budget.

Therefore the correct D.2 conclusion is **not** “the governor has no authority” and also **not** “the problem is solved.” The current seed has material authority around its operating point, but upper-range authority remains structurally compressed.

## Runtime evidence gate

`TurbineAdmissionAuthorityAuditTests` adds a small deterministic +10 rpm / -10 rpm governor-reference perturbation from the breaker-open sustained synchronization seed and records:

- effective control-valve position;
- turbine-inlet pressure;
- raw/commanded stage mass flow;
- D.1 effective vapor-admitted mass flow;
- turbine shaft power.

Run:

```text
scripts\run-turbine-admission-authority-audit.cmd
```

The breaker-open seed is deliberate: while paralleled, current-v2 droop derives the effective speed-controller setpoint from requested electrical load and supersedes direct speed-reference commands. D.3 separately audits breaker-closed load-droop and controller/physical-valve tracking.

The runtime evidence must be reviewed before selecting one of these possible follow-up laws:

1. retain the current quadratic-resistance model unchanged;
2. rescale the stage/control resistance distribution;
3. move control authority to an explicit effective admission area;
4. introduce a pressure/temperature-dependent Stodola/ellipse-style stage-flow relation.

## Decision rule

No production physics is changed in D.2.

A D.2 correction is justified only if local runtime evidence shows one or more of the following:

- a +10 rpm reference change moves the control valve materially but produces negligible change in inlet pressure, stage flow and shaft power;
- normal load manoeuvring drives the valve into the high-opening region where the analytical map shows severe authority compression;
- the controller repeatedly saturates at high opening while the requested operating change remains unmet;
- a proposed replacement law improves authority without breaking 60/300-second conservation, protection and replay gates.

If the local perturbation shows adequate authority around the validated operating point, D.2 closes as evidence-only and detailed Stodola/effective-area work is deferred until a broader load envelope requires it.

## D.3.2 correction to the D.2 interpretation

The later breaker-open audit found that the former current-v2 implementation did not actually enforce the analytical series-flow assumption used by the D.2 equal-head map. When `ExpansionResistance` was present, stage flow was resolved only from turbine-inlet-to-exhaust pressure difference; the stop/control/admission valve capacities were not applied as an upstream bound. This allowed `CONTROL VALVE = 0%` to coexist with about 10.6 kg/s effective stage flow and about 4.24 MW shaft power.

D.3.2 corrects the implementation by bounding pressure-driven stage capacity with the minimum positive flow through all three admission valves. The D.2 table remains useful as historical component-sizing evidence, but it is not treated as validation of the pre-D.3.2 series-flow implementation.
## D.3.2 Hotfix 1 rejection and Hotfix 2 pressure-grade correction

After D.3.2 made the admission train physically authoritative, the loaded desktop seed no longer met its existing generation-ready floors: initial effective stage flow was `11.784841 kg/s` and the 10-second shaft-power result was `4.352905 MW`. Those regressions were not accepted by lowering the tests.

Hotfix 1 moved the loaded desktop control-valve bias from 28% to 30%, but local validation produced only `11.792118 kg/s` and still failed gross electrical output. The assumption behind that change was therefore false: the control valve was not the active local bottleneck.

Using the committed seed temperatures and the same simplified saturation model:

- `278.5 °C` header pressure is about `6.2725 MPa`;
- `277.0 °C` stop-out pressure is about `6.1311 MPa`;
- the fully open stop valve has only about `141.5 kPa` head and `11.89 kg/s` capacity;
- the control valve at 28% already has about `13.10 kg/s` capacity.

Hotfix 2 therefore restores the desktop bias to 28% and changes only the loaded desktop stop-out seed from `277.0 °C` to `276.7 °C`. The resulting analytical capacities are about `13.017 kg/s` through the fully open stop valve and `13.015 kg/s` through the 28% control valve. This balances the adjacent seed capacities while keeping every resistance and solver law unchanged.

The analytical values are sizing evidence only. The ordinary suite, explicit authority/governor audits, long-running journeys and operational-envelope audit remain the authoritative coupled validation.


## D.3.2 Hotfix 3 upstream main-steam capacity correction

Local Hotfix 2 execution measured `193.421 kPa` across the stop valve, proving the stop pressure grade was no longer under-driven, but effective stage flow remained `11.792 kg/s` and ten-second shaft power remained `4.350 MW`. The remaining committed seed bottleneck was the upstream main-steam line: at `1,000 Pa·s²/kg²` its approximately `143.9 kPa` seed head supports only about `12.0 kg/s`.

The loaded desktop profile therefore uses `850 Pa·s²/kg²`, yielding approximately `13.02 kg/s` under the same head and matching the adjacent stop/control capacities. The synchronization profile remains at `1,000 Pa·s²/kg²`; D.3.2 Hotfix 3 does not globally retune current-v2 or change any valve/stage resistance.
