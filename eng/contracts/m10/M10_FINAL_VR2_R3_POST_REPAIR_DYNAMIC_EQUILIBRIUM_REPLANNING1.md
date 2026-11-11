# M10 Final VR2 R3 - Post-Repair Dynamic-Equilibrium Replanning 1

NRS-MARKER:R3-POST-REPAIR-DYNAMIC-EQUILIBRIUM-REPLANNING1

Date: 2026-09-20

Status: **CANDIDATE / PLANNING-ONLY / NO PRODUCTION CHANGE**

## Purpose

Freeze the returned post-repair evidence after Family B closed the proven energy-transport seam, reject the historical first-100-step zero-envelope gate as an Implementation 1 discriminator, and define the shortest next diagnostic needed to reconstruct a true post-repair operating-point equilibrium.

This planning gate does not modify production, tests, thresholds, C4, the raw candidate vector, controller settings, or R3 authority.

## Prerequisite closure

NRS-MARKER:R3-POST-REPAIR-CAUSAL-CLOSURE-FROZEN

The returned post-repair Diagnostic 3 REV1 evidence is adjudicated:

`POST-REPAIR-CAUSAL-CLOSURE-CONFIRMED`

For candidate mode 2 at seed-step1:

- mass identity residual: `0 kg/s`;
- suction-vs-recirculation transport delta: `0.00014754408039152622 J/kg`;
- observed net-energy magnitude: `0.01468658447265625 W`;
- production energy-identity residual: `6.7815184593200684E-05 W`.

Family B therefore remains the selected and accepted repair for the causal energy-transport discontinuity. This planning gate does not reopen that ownership decision.

## Why the old 100-step gate cannot adjudicate Family B

NRS-MARKER:R3-HISTORICAL-FAST-GATE-CONTRADICTION

The historical exact same 100-step zero-envelope test was already RED before Family B:

- 86 envelope-violation steps;
- first violation step 15;
- primary-flow violations: 86, first at step 15;
- governor violations: 84, first at step 17.

After Family B:

- 84 envelope-violation steps;
- first violation step 17;
- primary-flow violations: 79, first at step 22;
- governor violations: 84, first at step 17;
- electrical violations: 0;
- drum-level violations: 0;
- trip steps: 0;
- breaker-open steps: 0;
- rollback steps: 0;
- non-finite steps: 0.

Therefore `zero envelope violations` was not a valid discriminator for whether Family B closed its targeted causal seam. The envelope itself remains frozen for a future qualified equilibrium candidate; only its use as a retroactive Implementation 1 success criterion is rejected.

## Two residual owners

NRS-MARKER:R3-POST-REPAIR-DYNAMIC-RESIDUAL-OWNERS

### A. Primary hydraulic operating-point residual

Pre-repair candidate primary-pump flow:

`99.96959508257872 -> 99.2855236793509 kg/s`

Delta over the first second:

`-0.6840714032278186 kg/s`

Post-repair candidate primary-pump flow:

`99.99537721912854 -> 100.41741075778903 kg/s`

Delta:

`+0.4220335386604859 kg/s`

The repair therefore reverses the sign of the primary hydraulic drift. This is consistent with a changed post-repair operating point, not with a reason to undo the transport repair.

### B. Speed-control/governor initial-state residual

Pre-repair governor:

`29.28222490151453 -> 29.445239618143546 %`

Post-repair governor:

`29.28222490151453 -> 29.445239885077935 %`

The difference between the pre/post one-second governor deltas is only:

`2.669343892591769E-07 percentage points`

This drift is effectively common-mode across the energy-transport repair and is therefore not owned by Family B. It must be analyzed as an initial controller/rotor/load equilibrium residual.

## Replanning decision

NRS-MARKER:R3-DYNAMIC-EQUILIBRIUM-REPLANNING-DECISION

Classification:

`POST-REPAIR-DYNAMIC-EQUILIBRIUM-RECONSTRUCTION-REQUIRED`

The next activity is diagnostic, not retuning:

`R3-POST-REPAIR-DYNAMIC-EQUILIBRIUM-RESIDUAL-DIAGNOSTIC1`

Diagnostic 1 shall keep repaired production byte-identical and capture, for the exact opt-in mode-2 candidate:

1. conserved mass and energy residuals for all primary fluid nodes at post-seed, step 1, 2, 5, 10, 20, 50 and 100;
2. primary pump/channel/return branch flows and hydraulic-head terms at the same checkpoints;
3. drum mass, drum energy, pressure and liquid level;
4. speed-control error, integral and output;
5. rotor speed, turbine torque/power, generator electrical power and effective load torque;
6. per-checkpoint finite/trip/breaker/rollback status.

The diagnostic must identify whether the post-repair hydraulic residual originates primarily from the conserved-inventory seed, seed-preconditioning composition, or a controller/actuator initial state. It must separately classify the common-mode governor drift.

## Candidate repair families after Diagnostic 1

No family is selected yet.

- **H - Hydraulic seed reconstruction:** reconstruct the conserved-inventory operating point under repaired Family B physics while leaving controller state unchanged.
- **C - Controller-state initialization:** initialize speed-controller integral/output and related actuator/rotor state consistently while leaving the hydraulic conserved-inventory seed unchanged.
- **J - Joint equilibrium reconstruction:** reconstruct both hydraulic conserved inventory and controller state if Diagnostic 1 proves both are independently material.

Any eventual reconstruction must preserve the current physics model, Family B transport ownership, C4 payload, exact-v9 semantics, and the frozen final 100-step envelope.

## Frozen constraints

NRS-MARKER:R3-DYNAMIC-REPLANNING-FROZEN-CONSTRAINTS

This planning gate forbids:

- transport-physics rollback;
- threshold/envelope changes;
- pump/governor gain retuning;
- steam-capacity or hydraulic-resistance retuning;
- C4 payload changes;
- default closure-mode changes;
- canonical Exact-V9 reinterpretation;
- R3 PASS;
- R4 planning.

Raw seed or controller-state changes are not authorized yet. They may only be planned after returned Diagnostic 1 evidence assigns the residual owner.

## Authority

A local PASS of this planning audit authorizes only:

`R3-POST-REPAIR-DYNAMIC-EQUILIBRIUM-RESIDUAL-DIAGNOSTIC1`

R3 remains RED. Requalification 3 remains blocked until a later equilibrium reconstruction is implemented, locally qualified, and then passes the frozen 120 s / 12,000-step qualification.
