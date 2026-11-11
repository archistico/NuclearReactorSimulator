# M10 Final Replacement-Long Closure Plan 1 — P2R2 Decision Re-entry 2

**Status: VALIDATED — P2R2 decision `P3-R-OWNER-LOCALIZATION`; production repair remains unauthorized. Plan Amendment 3 now temporarily holds P3-R1 behind VR0–VR5 external physical-reference assessment.**

P2R2 is the branch-authority gate required by Plan Amendment 2. It reviews returned P1B together with P1/P1A and selects the next closure route. P1B itself did not authorize a branch.

## 1. Returned prerequisite evidence

P1B returned local execution PASS after the ordinary Release gate:

- 600 s exact-v9 5 MWe background reference completed;
- exact-v9 5→6 MWe probe completed to 3,600 s;
- P1A checkpoints at 900 / 1,800 / 3,600 s reproduced exactly within the frozen tolerances;
- zero trip, nonconvergence, nonfinite, rollback, line-search exhaustion, untargeted disagreement or shadow nonconvergence;
- P1B engineering label: `COUPLED-MULTI-DOMAIN`;
- five represented domains and nineteen observables remain load-specific/persistent in the late-tail diagnostic classification;
- no Tier-C physics was synthesized and no stationarity threshold was relaxed.

The P1A 3,600 s checkpoint still demonstrates **6 MWe load reachability**: output 6.000344232909117 MWe, frequency 50.000004483204116 Hz and dispatch adequacy +0.00035110283132855358 MW. This does not establish whole-operating-point stationarity.

## 2. What P1B adds beyond P1A

The 5 MWe background is effectively stationary in the represented observables. The 6 MWe late tail is not merely an electrical residual. Conserved inventories and hydraulic states remain materially dynamic while the numerical/conservation sentinels remain green.

From the frozen P1B late-window evidence, final-window slopes include:

- thermofluid stored energy: **+1.317398 MW equivalent storage rate**;
- thermal-body stored energy: **+0.127989 MW equivalent storage rate**;
- drum mass: **-0.039413 kg/s**;
- drum level: **-4.90958e-6 /s**;
- primary channel flow: **+0.034204 kg/s²**;
- primary return flow: **+0.034898 kg/s²**;
- turbine-inlet pressure: **+6.45111e-5 MPa/s**;
- shaft power: **+3.63741e-5 MW/s**;
- electrical output: **+3.56477e-5 MW/s**;
- speed-controller integral: **-1.39091e-5 controller-unit/s**;
- level-controller integral: **+2.24582e-5 controller-unit/s**;
- hotwell-controller integral: **-5.94540e-6 controller-unit/s**.

At the same time, total plant mass remains conserved and the mass/power/full-energy closure residuals remain numerically negligible. Therefore the dominant observation is **represented internal redistribution/storage**, not lost mass, nonfinite state or solver failure.

The complete 1 s evidence also shows large late-interval redistribution between 2,400 and 3,600 s:

- `outlet` node mass: 2283.995428 → 3141.048092 kg (**+857.052663 kg**);
- `feedwater-inventory` mass: 7621.349872 → 6565.187761 kg (**-1056.162110 kg**);
- `drum` mass: 3796.025260 → 3740.512474 kg (**-55.512787 kg**);
- total primary channel flow: 185.386501 → 233.953441 kg/s;
- total return flow: 184.624306 → 233.507814 kg/s;
- channel→return committed-state diagnostic difference: 0.762196 → 0.445627 kg/s;
- `PressureHeaderPressure - SuctionHeaderPressure`: -0.718247 → -1.736575 MPa;
- drum level: 0.484003 → 0.477043;
- level-controller output: approximately 98.54% → 99.26%.

The negative header-pressure difference is **not** interpreted here as proof of a pump-model defect: `MainCirculationLoopSnapshot.HeaderPressureRise` is a committed-state node-pressure difference, while canonical pump active boost/internal loss are separate owners. P3-R1 must inspect those owners directly before any repair claim.

## 3. P2R2 branch decision

Frozen machine-readable decision markers:

```text
P2R2-DECISION = P3-R-OWNER-LOCALIZATION
P3-W-AUTHORIZED = False
P3-R-OWNER-LOCALIZATION-AUTHORIZED = True
PRODUCTION-REPAIR-AUTHORIZED = False
NEXT-AUTHORIZED-IMPLEMENTATION = P3-R1-Primary-Inventory-Hydraulic-Slow-State-Owner-Localization
```

### P3-W — NOT AUTHORIZED

A workload/procedure-only repair is not supported yet. Although 6 MWe electrical reachability is demonstrated, the target state still contains material internal inventory/hydraulic/steam/controller motion after 3,600 s. A staged workload cannot be qualified around an operating point whose represented whole-plant closure is not established.

### P3-R — AUTHORIZED FOR OWNER LOCALIZATION ONLY

P1B provides sufficient evidence to enter the runtime-ownership branch because the unresolved behavior is represented inside the unchanged runtime and is not reducible to electrical request granularity alone. The evidence is multi-domain, but its causal ordering starts upstream with conserved inventory/hydraulic redistribution and propagates through steam-path/control/turbine/electrical observables.

**P2R2 does not authorize a production repair.** It authorizes only the first P3-R subgate:

> **P3-R1 — Primary Inventory / Hydraulic Slow-State Owner Localization & Contract Audit**

P3-R1 must determine which existing canonical owner(s) explain the persistent 6 MWe redistribution before any `src/` modification is proposed.

## 4. P3-R1 question

Why does the exact-v9 5→6 MWe trajectory retain material inventory redistribution and increasing primary flow after electrical/frequency reachability, and which existing runtime contract owns that behavior?

The first owner census must follow the represented chain, not a new model:

1. `PlantNetworkOrchestrator` node mass/energy derivatives and transfer terms;
2. `MainCirculationSystem` pump snapshots — effective speed, active pressure boost, internal loss, flow, hydraulic exchange and shaft demand;
3. channel and return pipe flows plus outlet-node storage identity;
4. steam-drum return / recirculation / steam / feedwater balance;
5. condensate/feedwater train actual pump mass flows, active boosts, internal losses and inventory transfer;
6. controller memory and saturation only after the physical inventory/transfer identities are established;
7. steam/turbine/grid as downstream consequence checks.

P3-R1 may add test-only diagnostics derived from existing canonical snapshots. It may **not** add a second hydraulic solve, new constitutive physics, new pump law, new feedwater law, new stationarity thresholds, protection retuning or workload changes.

## 5. Repair authorization boundary

After P3-R1 returns, a separate P3-R decision/repair gate is required.

A production repair may be proposed only if P3-R1 identifies one of the following with exact evidence:

- a conservation/ownership contradiction between canonical transfers and committed inventories;
- a pump/pipe/system-curve semantic contradiction;
- a secondary inventory/control capacity or ownership contradiction;
- an exact-v9 operating-point seed/trim inconsistency that is part of runtime semantics rather than workload procedure;
- another explicit runtime contract violation with a reproducible focused test.

If a production correction changes exact-version semantics, exact-v9 remains immutable and the corrected runtime becomes exact-v10. If P3-R1 instead proves that the runtime contracts are coherent and the remaining behavior is intentionally slow but convergent, P2R may return to P3-W with explicit readiness/stationarity evidence; that outcome is not assumed here.

## 6. Frozen authority after this candidate

Returned P2R2 audit PASS established:

- `P3-W-authorized=False`;
- `P3-R-owner-localization-authorized=True`;
- production repair authorized = `False`;
- replacement workload change authorized = `False`;
- exact-v9 change authorized = `False`;
- second replacement-long authorized = `False`.

Only **P3-R1 owner localization** is authorized by P2R2; however Plan Amendment 3 now places a planning hold before execution. VR0–VR5 must complete and VR5 must return `PROCEED-P3R1-EXACTV9` before P3-R1 runs. P4, P5 and M11 remain blocked.
