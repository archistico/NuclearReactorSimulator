# M10.9.4.1-D.2 — Turbine Admission Authority Evidence

## Purpose

This checkpoint measures the hydraulic authority already present in the current-v2 turbine-admission path **before** changing stage resistance, valve sizing, governor gains or the turbine flow law.

D.2 is audit-first. It deliberately does not choose a Stodola/ellipse law, an effective-area law or a resistance retune until the deterministic evidence has been run locally.

## Current-v2 canonical resistance budget

The sustained current-v2 seed currently owns:

- steam-drum internal steam-source resistance: `100 Pa·s²/kg²`;
- main-steam line resistance: `1,000 Pa·s²/kg²`;
- stop-valve base resistance: `1,000 Pa·s²/kg²`;
- control-valve base resistance: `1,000 Pa·s²/kg²`;
- admission-valve base resistance: `1,000 Pa·s²/kg²`;
- turbine-stage expansion resistance: `21,400 Pa·s²/kg²`;
- control-valve characteristic: linear;
- sustained desktop control-valve seed position: `28%`.

For a linear valve the current valve solver gives an effective hydraulic resistance proportional to `1 / position²`. The table below is therefore an **analytical resistance-budget map at equal total pressure head**, not a substitute for the dynamic plant journey.

| Control valve | Effective control-valve R | Total idealized series R | Flow capacity vs full-open | Additional capacity available to full-open | Control-valve share of total R |
|---:|---:|---:|---:|---:|---:|
| 10% | 100,000.0 | 124,500.0 | 45.26% | +120.96% | 80.32% |
| 20% | 25,000.0 | 49,500.0 | 71.77% | +39.33% | 50.51% |
| **28% current seed** | **12,755.1** | **37,255.1** | **82.73%** | **+20.87%** | **34.24%** |
| 40% | 6,250.0 | 30,750.0 | 91.06% | +9.81% | 20.33% |
| 46% historical audit point | 4,725.9 | 29,225.9 | 93.41% | +7.06% | 16.17% |
| 60% | 2,777.8 | 27,277.8 | 96.69% | +3.43% | 10.18% |
| 80% | 1,562.5 | 26,062.5 | 98.91% | +1.10% | 6.00% |
| 100% | 1,000.0 | 25,500.0 | 100.00% | — | 3.92% |

## Interpretation

The earlier external audit correctly identified a structural risk when reasoning from the older ~46% operating bias: above that region, the large fixed stage resistance leaves limited additional hydraulic authority.

The current consolidated seed is materially different: the control valve is now seeded at **28%**, where its effective resistance still represents about **34% of the idealized total resistance budget**. Under the equal-head analytical approximation, opening from 28% to 100% could increase flow capacity by about **20.9%**, not merely the single-digit figure estimated from the older 46% bias.

However, authority still compresses strongly at large openings:

- by 60% open the control valve is only about 10% of the total idealized resistance;
- by 80% open, going fully open adds only about 1.1% theoretical flow capacity;
- at full open, the stage and other fixed paths dominate almost the entire resistance budget.

Therefore the correct D.2 conclusion is **not** “the governor has no authority” and also **not** “the problem is solved.” The current seed has material authority around its operating point, but upper-range authority remains structurally compressed.

## Runtime evidence gate

`TurbineAdmissionAuthorityAuditTests` adds a small deterministic +10 rpm / -10 rpm governor-reference perturbation around the sustained 5 MWe point and records:

- effective control-valve position;
- turbine-inlet pressure;
- raw/commanded stage mass flow;
- D.1 effective vapor-admitted mass flow;
- turbine shaft power.

Run:

```text
scripts\run-turbine-admission-authority-audit.cmd
```

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
