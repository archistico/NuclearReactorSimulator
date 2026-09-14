# Pre-M11 Todreas/Kazimi Thermal-Hydraulic Deep Review — Pass 2 (Post-Plan-Amendment-2)

## Status and disposition

**VALIDATED RESEARCH / PLANNING REVIEW — documentation-only. Local audit PASS reported 2026-08-24.** This is the mandatory post-amendment audit required by M10 Final Replacement-Long Closure Plan 1, P2R Decision Re-entry 1 / Plan Amendment 2.

**Disposition: `PASS-AS-AUTHORED` — VALIDATED, subject to this document's binding implementation constraints.**

This disposition means that Plan Amendment 2 can support preparation of P1B — **Slow-State Closure & Phenomenon-Owner Qualification** — without changing the amendment's physical scope. It does **not** execute P1B, select P3-W or P3-R, change exact-v9, relax stationarity criteria, modify the replacement workload, or authorize a second replacement-long baseline.

Returned validation evidence is now frozen in:

- `eng/frozen-evidence/ordinary/PreM11_TodreasKazimi_DeepReviewPass1_ValidatedSummary.txt`;
- `eng/frozen-evidence/ordinary/M10FinalReplacementLongClosurePlan1_P2R_PlanAmendment2_ValidatedSummary.txt`.

Those summaries establish that Deep Review Pass 1 and Plan Amendment 2 both passed locally; P1A remains execution PASS / engineering `INCONCLUSIVE`; 6 MWe load reachability is demonstrated; whole-operating-point stationarity is not demonstrated; and the only activity authorized before P1B was this Pass 2 review.

---

## 1. Review question

Plan Amendment 2 asks P1B to answer a deliberately narrower question than P1/P1A:

> When exact-v9 repeats the already demonstrated 5→6 MWe trajectory, which **represented canonical state domain** remains materially dynamic in the late tail, and is that evolution load-specific, background, coupled across domains, or not localizable with the current model?

This review audits whether the frozen P1B design can answer that question **observationally**, using the current reduced-order model as it actually exists.

The review does not ask whether the current model contains every physical mechanism that exists in an RBMK or in a higher-fidelity two-phase code. Todreas/Kazimi instead reinforce the correct discipline: use the conservation states, boundary conditions, transfer paths and constitutive fidelity actually represented, and do not manufacture missing two-fluid physics in a diagnostic layer.

---

# Part A — Source-grounded audit principles

## 2. Conserved-state ownership is the correct first diagnostic layer

### 2.1 Source finding

Volume II Appendix L derives lumped-parameter control-volume conservation equations for mass, energy and momentum and treats junctions/flow paths as the interfaces through which conserved quantities are transferred. The appendix explicitly considers a homogeneous, thermally equilibrated water/steam mixture, which is close in *model class* to the simulator's reduced HEM-like water/steam representation.

### 2.2 Consequence for P1B

P1B is correctly ordered when it observes:

```text
stored mass / stored internal energy
        ↓
canonical inflow/outflow and balance residuals
        ↓
pressure / hydraulic response
        ↓
steam-path transfer
        ↓
shaft / electrical response
```

rather than declaring the component with the largest downstream display slope to be the root owner.

This is especially important after P1A, because electrical load and reactor thermal power are nearly static while steam-path/shaft quantities still evolve slowly.

**Pass 2 finding:** `PASS`.

---

## 3. Hydraulic boundary conditions must remain observational, not duplicated

### 3.1 Source finding

Volume I Chapter 14 and Volume II Chapters 1 and 4 show that the hydraulic problem changes according to which boundary variables are prescribed. Under pressure-pressure boundaries, flow is an emergent solution; for multiple heated channels connected at common plena, shared pressure conditions constrain the channel flow split. Multiple flow roots can exist in boiling problems.

### 3.2 Consequence for P1B

P1B must not build a parallel hydraulic solver or impose simultaneous acceptance targets for quantities the runtime already solves as coupled variables.

Allowed:

- observe header/node pressure;
- observe total and branch flow;
- observe canonical pressure differences and continuity residuals;
- observe already computed pump/head/loss terms;
- compare trends across the same committed trajectory.

Forbidden:

- independently solve a textbook pressure-drop relation and call its difference from runtime flow a new physical residual;
- prescribe both a desired branch flow and desired branch pressure drop as if both were independent state targets;
- infer a missing buoyancy or two-phase loss term from a new correlation.

**Pass 2 finding:** `PASS`.

---

## 4. Root/stationarity and stability remain separate claims

### 4.1 Source finding

Volume I Section 11.8 shows that satisfying a pressure-flow intersection is not sufficient to prove local dynamic stability. The Ledinegg example contains multiple roots, including an unstable one. Density-wave behavior is a different dynamic phenomenon and its predicted boundary depends on the two-phase model used.

### 4.2 Consequence for P1B

P1B is correctly scoped to **represented stationarity / owner localization**, not perturbation-stability qualification.

It may record oscillatory behavior if the current model produces it, but it may not:

- label an observed oscillation as a real density-wave instability;
- calculate a density-wave stability margin from textbook correlations;
- add perturbations merely because the literature discusses stability.

Local perturbation recovery remains a later operating-point qualification question, principally M12.0 unless separately authorized.

**Pass 2 finding:** `PASS`.

---

## 5. Model fidelity is a boundary on claims, not a reason to upgrade M10

### 5.1 Source finding

Volume I's two-phase transport hierarchy and Volume II Chapter 12 distinguish homogeneous/mixture approaches from richer formulations with separate phase motion and additional constitutive closure. Different model levels answer different questions.

### 5.2 Consequence for P1B

Plan Amendment 2 correctly forbids synthesizing:

- liquid/vapor slip velocity;
- independent phasic temperatures;
- ONB/NVG location;
- new flow-regime identity;
- drift-flux/two-fluid interfacial terms;
- density-wave stability margins;
- new natural-circulation, CHF/dryout or pressure-drop correlations.

P1B may only report phase, vapor quality and void fraction **as represented by the current canonical model**.

**Pass 2 finding:** `PASS`.

---

## 6. Scaling literature does not justify prototype time constants

### 6.1 Source finding

Volume II Chapter 2 treats scaling as preservation of selected processes/responses with unavoidable distortions. A reduced system is not automatically a geometrically and dynamically scaled prototype. The same chapter's process/phenomena-ranking material treats time-dependent behavior in terms of represented conservation processes, not simple clock-time identity between model and plant.

### 6.2 Consequence for P1B

The frozen P1B windows:

- 600 s background reference;
- 3,600 s maximum 6 MWe replay;
- 1 s slow-state samples;
- final 1,200 s split into 4×300 s windows;

are **project experimental windows** selected from the already observed exact-v9/P1/P1A trajectory. They are not claims about a real RBMK settling time.

The 3,600 s horizon is justified because it is the already demonstrated P1A horizon and therefore lets P1B add observability without changing the physical experiment. The 600 s background reference is a bounded control used to reveal pre-existing numerical/control drift in newly exposed variables. The 4×300 s late windows let the analysis distinguish a persistent monotonic trend from a single local fluctuation without extending the run.

No source-derived numerical time constant is imported.

**Pass 2 finding:** `PASS`.

---

# Part B — Current runtime ownership audit

## 7. Tier A observable map

The following source-code audit was performed against the validated Plan Amendment 2 Hotfix 1 baseline. The table is an ownership map, not a request for new production state.

| P1B evidence need | Existing canonical/diagnostic owner | Pass 2 disposition |
| --- | --- | --- |
| Fluid node mass/internal energy and canonical thermodynamic state | `PlantState` → immutable `PlantSnapshot.FluidNodes` | canonical |
| Whole plant mass / stored thermofluid energy | `PlantNetworkAudit.FinalTotalMass`, `FinalTotalStoredEnergy`; surfaced by `IntegratedPrimaryCircuitSnapshot` | canonical |
| Mass/energy balance and closure residuals | `PlantNetworkAudit` | canonical audit |
| Feedwater inflow / steam export | `IntegratedPrimaryCircuitSnapshot.TotalFeedwaterMassFlowRate`, `TotalSteamExportMassFlowRate` | canonical derived sum |
| Drum inventory, pressure, phase/quality/void, level, return/steam/recirculation | `SteamDrumSnapshot` | canonical diagnostic |
| Drum separable-liquid and steam-source availability state | existing `SteamDrumSnapshot` diagnostics | canonical diagnostic; do not generalize to separator fidelity claims |
| Loop/header flows and continuity residuals | `MainCirculationLoopSnapshot` | canonical diagnostic |
| Per represented branch/group flow, ΔP and outlet phase/quality/void | `MainCirculationBranchSnapshot` | canonical diagnostic |
| Pump flow, hydraulic exchange and shaft demand | `MainCirculationPumpSnapshot` / loop aggregation | canonical diagnostic |
| Hydraulic numerical state | `PlantNetworkHydraulicNumericalSnapshot` | canonical numerical diagnostic |
| Reactivity, point kinetics, fission power | `ReactorPrimaryControlSnapshot` | canonical control/physics snapshot |
| Reactor thermal bodies / heat sources | `PlantSnapshot.ThermalBodies`, `HeatSources`, integrated primary/core snapshots | canonical |
| Controller setpoint/error/P-I-D/output/saturation | `ControllerDiagnosticSnapshot` | canonical diagnostic |
| Committed integral/previous-error/output memory | `ControllerChannelState` | canonical controller memory; explicitly not physical plant state |
| Actuator command state | `ControlAndActuatorSnapshot.ActuatorCommands` | canonical command observation |
| Main steam flow/admission/pressure path | `MainSteamNetworkSnapshot` plus stage/admission snapshots | canonical diagnostic |
| Turbine shaft/passive-loss/net torque/kinetic energy | `TurbineRotorSnapshot` | canonical mechanical diagnostic |
| Stage flow, inlet state, shaft power, turbine ownership residual | `TurbineStageGroupSnapshot` | canonical diagnostic |
| Generator request/frequency/phase/electromagnetic torque/output | `SynchronousGeneratorSnapshot` | canonical electrical diagnostic |
| Generator mechanical/electrical closure | `GeneratorElectricalAudit` | canonical audit |
| Reactor-to-grid coupled energy ledger | `SecondaryCycleHeatBalanceAudit` | canonical audit |

### 7.1 Exact-derivation rule

A Tier A item may be derived only when the derivation is an **exact transparent transformation** of committed canonical state, for example:

- compensated sums of canonical node inventories;
- slopes from timestamped committed samples;
- existing generator phase/frequency correction expressions evaluated from their canonical state and frozen coupling definition;
- late-window means/ranges/regression slopes.

A derivation may not introduce a new equation of state, new pressure-drop law, new phase model or new controller semantics.

### 7.2 Coupled stored energy clarification

P1B must not invent a new "whole plant total energy" equation. Where coupled reactor-to-grid stored-energy evidence is needed, use the existing `SecondaryCycleHeatBalanceAudit` ledger, which already combines thermofluid stored energy with rotor kinetic energy and exposes explicit closure residuals. Any other energy total must be labelled by its exact included owner set.

**Mandatory check 1 disposition:** `PASS`.

---

## 8. Tier C exclusion audit

No required P1B result depends on a Tier C quantity. Owner localization can be performed at the level of represented conservation inventories, flows, pressure grades, controllers, shaft and electrical balances.

If an observed pattern would *suggest* a real-world mechanism that is not represented (for example slip relaxation or a density-wave mechanism), P1B must report:

```text
UNREPRESENTED-MECHANISM — NOT IDENTIFIABLE BY CURRENT MODEL
```

rather than converting the suggestion into a physical diagnosis.

**Mandatory check 2 disposition:** `PASS`.

---

## 9. Timing and sampling audit

### 9.1 600 s background reference

Defensible as a bounded project control because it is used only to characterize background drift/noise in the newly exposed owner variables under the same exact-v9 model. It is **not** a physical settling criterion.

### 9.2 3,600 s load replay

Defensible because P1A already used and returned this exact horizon. P1B therefore changes observability rather than extending the physical experiment.

### 9.3 1 s sampling

Defensible **only as slow-state downsampling**. It is not adequate to prove absence of sub-second oscillations or fast numerical events. Therefore P1B implementation must preserve per-step or canonical-event aggregation for:

- trip/protection transitions;
- hydraulic correction/convergence/fallback/rollback sentinels where available;
- branch/phase transitions that can occur between 1 s samples;
- non-finite or harness faults.

The one-second CSV is a compact slow-tail trajectory, not the only evidence stream.

### 9.4 4×300 s late windows

Defensible as a project trend-classification decomposition of the already frozen final 1,200 s. It provides four independent contiguous views of trend direction/magnitude without claiming statistical independence or prototype time scaling.

**Mandatory check 3 disposition:** `PASS`, with the 1 s downsampling constraint binding on P1B implementation.

---

## 10. Inventory/transfer evidence versus downstream-slope proxy

The amendment is sufficient **if implementation follows this causal ordering**:

```text
A. CONSERVED INVENTORIES
   per-node M/U; plant/drum inventory changes

B. CANONICAL TRANSFERS / CLOSURE
   feedwater, steam export, separation, plant mass/energy audits

C. HYDRAULIC COMPATIBILITY
   headers, loop/branch flows, continuity, represented ΔP/head terms

D. STEAM PATH
   line/admission flow and pressure; valve state

E. CONTROL MEMORY / ACTUATOR
   error, I term, output, saturation, requested/effective/physical state

F. MECHANICAL / ELECTRICAL
   shaft/net torque/rotor energy, generator torque/output/frequency/phase
```

A late shaft or electrical slope is a downstream symptom until upstream conserved/transfer evidence is shown to be closed or background-equivalent.

**Mandatory check 4 disposition:** `PASS`.

---

## 11. Hydraulic overconstraint audit

Plan Amendment 2 requests observations, not a replacement solve. The existing runtime already owns both the boundary state and the emergent flow solution. P1B must therefore report those values and their canonical residuals without requiring them to match independently invented targets.

For parallel represented branches, branch flow spread may be reported because `MainCirculationBranchSnapshot` already exposes it. The diagnostic must not infer that the current reduced topology has the exact common-plenum physics of a BWR/RBMK; the source principle is used only to avoid contradictory boundary claims.

**Mandatory check 5 disposition:** `PASS`.

---

## 12. Controller memory versus physical actuator audit

The runtime has an unusually useful ownership distinction:

- `ControllerChannelState` explicitly declares itself committed **controller memory only**, not physical plant state;
- `ControllerDiagnosticSnapshot` exposes controller arithmetic/status;
- `ControlAndActuatorSnapshot` exposes actuator commands;
- physical valve/pump/rod state remains in the plant/physics state owners.

P1B must keep these as separate columns/domains. An integral term continuing to move while a physical actuator is static is a different diagnosis from an actuator continuing to move under a static controller output.

**Mandatory check 6 disposition:** `PASS`.

---

## 13. Physical versus numerical drift audit

Volume I warns that time-domain instability can be physical or numerical. Current runtime evidence is sufficient to avoid a naive physical claim because hydraulic numerical telemetry already exists and earlier Phase-H qualification provides numerical provenance.

P1B may classify a represented physical owner only when:

1. required state values remain finite;
2. the P1A checkpoints reproduce;
3. no protection/harness failure occurs;
4. available hydraulic numerical telemetry remains acceptable/consistent with the frozen exact-v9 mode;
5. the owner pattern is visible in committed physical/controller state, not only in a numerical correction metric.

P1B does **not** need to prove that every residual drift is physically realistic. If the evidence is sensitive to numerical telemetry or cannot be separated from its numerical floor, the correct result is `INCONCLUSIVE` (or a numerical-evidence note), not physical retuning.

**Mandatory check 7 disposition:** `PASS`.

---

## 14. Reference-relative ranking audit

Plan Amendment 2 allows a normalized owner score but does not require one. A single scalar cross-domain score would require arbitrary choices for combining kg/s, J/s, Pa/s, controller units/s and MW/s. Volume II Chapter 13's Jacobian/sensitivity treatment does not supply a physically universal normalization for this problem; it is local to defined inputs/outputs and uncertainty assumptions.

### 14.1 Binding P1B v1 rule

**No single scalar cross-domain owner score is enabled in P1B v1.**

P1B will instead report:

1. raw physical-unit late-window values, changes and slopes;
2. same-observable 5 MWe background versus 6 MWe late behavior in the same units;
3. per-domain structured evidence;
4. causal ordering from inventories/transfers toward downstream consequences.

An optional per-observable dimensionless comparison may be used only when its formula and zero guard are frozen before execution and the raw value remains adjacent. It may never become an acceptance threshold.

If more than one domain remains materially dynamic with no defensible causal ordering, use `COUPLED-MULTI-DOMAIN`. If the evidence cannot establish even that, use `INCONCLUSIVE`.

`NO-MATERIAL-LATE-DRIFT` means only that P1B did not find a represented owner standing out from background under its frozen evidence scheme; it does **not** promote 6 MWe to whole-operating-point stationarity.

**Mandatory check 8 disposition:** `PASS` with scalar cross-domain normalization disabled.

---

## 15. Literature-number/correlation import audit

No Plan Amendment 2 threshold is taken from the books. The P1A checkpoint tolerances are retained only for deterministic reproduction of project evidence. The 600/3,600/1/1,200/300 s settings are project experimental windows. No Ledinegg criterion, density-wave number, BWR flow value, natural-circulation correlation, CHF relation, two-phase pressure-drop relation or prototype RBMK coefficient is introduced.

**Mandatory check 9 disposition:** `PASS`.

---

## 16. Bounded observational scope / branch authority audit

P1B remains bounded to one 600 s no-load-change background observation and one 5→6 MWe replay no longer than the already executed 3,600 s hold. It changes no production semantic owner and cannot authorize P3.

P1B execution PASS means evidence completeness and deterministic reproduction, **not** a branch selection. P2R2 remains the sole next branch authority.

**Mandatory check 10 disposition:** `PASS`.

---

# Part C — Pass 2 decision

## 17. Ten-point disposition table

| # | Mandatory Plan Amendment 2 review question | Result | Binding note |
| ---: | --- | --- | --- |
| 1 | Tier A canonical/exactly derived? | PASS | exact derivations only; source map frozen above |
| 2 | Tier C excluded? | PASS | unavailable physics stays unavailable |
| 3 | 600/3600/1/4×300 windows defensible? | PASS | project windows only; 1 s is slow downsample |
| 4 | Inventory/transfer evidence sufficient? | PASS | causal ordering mandatory |
| 5 | Hydraulic observations avoid overconstraint? | PASS | observe canonical solution; no second solve |
| 6 | Controller memory separate from actuator/plant? | PASS | retain distinct owners/columns |
| 7 | Physical/numerical drift distinguishable enough? | PASS | canonical-step sentinels retained; ambiguity => INCONCLUSIVE |
| 8 | Ranking prevented from becoming threshold? | PASS | no scalar cross-domain owner score in P1B v1 |
| 9 | No book coefficient/correlation imported? | PASS | all timing/tolerances are project provenance |
| 10 | Bounded, observational, no P3 selection? | PASS | P2R2 remains branch authority |

## 18. Final disposition

**TODREAS/KAZIMI DEEP REVIEW PASS 2 DISPOSITION = `PASS-AS-AUTHORED`.**

No Plan Amendment 2 engineering hotfix is required.

The amendment can support P1B implementation **provided that** the implementation contract adopts the binding clarifications in this review:

1. use only canonical state/diagnostics or transparent exact derivations;
2. do not synthesize Tier C physics;
3. treat 1 s data as slow-state downsampling and retain per-step/canonical-event protection/numerical evidence;
4. use the existing conservation/energy ledgers rather than inventing a new plant balance;
5. do not construct a second hydraulic solve;
6. keep controller memory, command and physical actuator state distinct;
7. report raw physical-unit evidence first;
8. disable a single scalar cross-domain owner score for P1B v1;
9. do not promote `NO-MATERIAL-LATE-DRIFT` to a stationarity claim;
10. return all engineering evidence to P2R2; P1B itself has no branch authority.

## 19. Authorization after this review is validated

After the **Pass 2 documentation audit itself** returns PASS locally:

- P2R1 / Plan Amendment 2 remains the validated planning checkpoint;
- the literature hold is satisfied;
- the only next authorized implementation is **P1B Slow-State Closure & Phenomenon-Owner Qualification**;
- P3-W, P3-R, P4, P5, Replacement-Long Execution 2 and M11 remain unauthorized;
- after P1B returns evidence, the next decision gate is **P2R2 Decision Re-entry 2**.
