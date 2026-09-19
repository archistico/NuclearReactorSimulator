# M10 Final VR2 — R3 Diagnostic 3 Deep Review & REV1 Planning 1

Date: 2026-09-19  
Status: **PRE-EXECUTION REVIEW RECORDED / REV1 PLANNING AUTHORIZED**

## Purpose

This document records the deep pre-execution review of `R3 Seed Integration Suction Energy-Transport Causal-Seam Diagnostic 3` and freezes the detailed successor plan before any Diagnostic 3 evidence is executed or promoted.

The reviewed Diagnostic 3 candidate is **not rejected**: its runtime topology, temporal attribution, balance reconstruction, provenance and authority containment are sound. The review nevertheless identified two pre-execution revisions that must be completed before authoritative evidence is generated:

1. the causal wording currently promotes a localized seam to a `causal-owner` more strongly than the existing experiment directly proves;
2. `ROADMAP.md` contains an older repository-hygiene sequence that conflicts with the newer rule allowing test-only physics evidence collection while hosted CI confirmation is pending.

The review also identified three evidence-strengthening changes that should be incorporated because the diagnostic has not yet executed: complete `u + p/rho` transport decomposition, a true IEEE-754 bit comparison, and an independent IAPWS-IF97 counterfactual transport calculation.

No production repair is authorized by this plan.

<!-- NRS-MARKER:DIAG3-REV1-REVIEW-DISPOSITION -->

## Recorded deep-review disposition

Overall disposition:

`PASS-WITH-PREEXECUTION-REVISION`

| Review area | Disposition | Recorded evaluation |
| --- | --- | --- |
| Production baseline integrity | PASS | `src/` remains frozen; Diagnostic 3 is test-only. |
| Historical-test integrity | PASS | Historical tests are unchanged by the reviewed candidate. |
| Diagnostic 2 provenance | PASS | Returned `01`–`09` evidence is SHA-256 frozen and is the only predecessor evidence. |
| Seed-step temporal attribution | PASS | MCP and drum source terms are evaluated from the same committed pre-step state used by the network integrator. |
| Suction topology completeness | PASS | At the first seed step, drum liquid recirculation and MCP suction are the relevant source/sink terms for the localized balance. |
| Mass-balance reconstruction | PASS | Candidate suction mass remains balanced to bookkeeping precision. |
| Energy-balance reconstruction | PASS | The observed approximately `-5.21627 MW` candidate rate is reconstructible from production source/sink terms. |
| Forward/inverse architectural seam | CONFIRMED | Steam-drum separation owns an internal default `SimplifiedWaterSteamThermodynamicModel`, while the network integrator receives the selected closure model. |
| PowerShell adjudicator robustness | PASS | CSV imports are array-wrapped; the prior Diagnostic 2 scalar/null `.Count` defect is not reproduced. |
| Authority containment | PASS | Repair, retuning, C4/exact-v9 changes, R3 Requalification 3 and R4 remain false. |
| Causal wording | NEEDS REVISION | `causal-owner` must not be promoted before an independent counterfactual closes the ownership hypothesis. |
| ROADMAP consistency | NEEDS REVISION | The older “do not resume physics until hosted CI green” sequence is superseded for test-only evidence collection, but production repair remains held. |
| Forward transport decomposition | STRENGTHEN | Explicitly prove `h_selected = u + p/rho` for the drum liquid path used by production. |
| Provider equality semantics | STRENGTHEN | Replace numerical `.Equals` wording with actual IEEE-754 bit comparisons or rename the claim. REV1 will use true bit comparison. |
| Resolver-path wording | STRENGTHEN | State that REV1 reconstructs the production mode-2 inverse path from the exact conserved inventories; it does not instrument an internal runtime call. |

## Frozen physical observation before REV1

The returned Diagnostic 2 evidence supports the following observation without yet selecting a repair:

- candidate `suction` mass is unchanged over the first 10 ms seed step;
- candidate `suction` conserved internal energy falls by approximately `52.163 kJ`, or approximately `-5.21627 MW`;
- mode 1 remains essentially energy-neutral over the same step;
- the source/sink accounting closes from the difference between the raw suction selected transport energy and the drum-liquid recirculation selected transport energy at approximately `100 kg/s`;
- the first phase divergence then appears at `seed-step1 / suction`, where mode 2 becomes `SaturatedMixture` with very small quality while mode 1 remains `SubcooledLiquid`;
- governor/controller displacement is not present at that first checkpoint.

The review additionally confirms the architectural seam:

```text
SteamDrumSeparationSolver
    -> internally constructed default SimplifiedWaterSteamThermodynamicModel
    -> drum liquid forward properties / transport energy

PlantNetworkOrchestrator / FluidNodeIntegrator
    -> selected IFluidThermodynamicModel
    -> mode 2 ReferenceConsistentTabulatedInverseDomain for conserved inventory
```

This is a localized ownership seam, not yet a selected repair owner.

## Independent analytical counterfactual to reproduce in REV1

REV1 must not manufacture a zero-energy counterfactual by simply substituting the suction energy into the recirculation source. Instead it must use the already-qualified, **test-only** `IapwsIf97Reference` implementation in `Simulation.Tests` as an independent thermodynamic comparator.

At the frozen raw drum point near `T = 553.15 K`, `P = 6.416459281680372 MPa`, the existing test-only IF97 equations analytically imply approximately:

- Region-4 saturation pressure: `6.416459281680371 MPa`;
- Region-1 liquid specific volume: `0.001332845339067766 m3/kg`;
- Region-1 liquid density: `750.274597275804 kg/m3`;
- Region-1 liquid internal energy: `1,228,118.85285655 J/kg`;
- independent liquid transport energy `u + p*v`: `1,236,671.00070346 J/kg`.

The frozen mode-2 suction conserved inventory carries approximately the same reference-consistent internal energy and a selected transport energy of approximately `1,236,671.00070132 J/kg`. The resulting independent difference is only about `2.14E-6 J/kg`, corresponding to roughly `2.14E-4 W` at `100 kg/s` before the tiny measured mass-flow mismatch is included.

These numbers are **pre-execution analytical expectations**, not returned evidence. REV1 must reproduce them through the existing C# test-only IF97 helper and emit them as evidence before the ownership hypothesis may be called causally closed.

## Diagnostic 3 REV1 — implementation scope

REV1 is a new test-only diagnostic identity. The reviewed Diagnostic 3 files remain provenance and are not rewritten as if they had executed successfully.

### A. Application runtime evidence

A new explicit `Application.Tests` REV1 diagnostic will reconstruct exactly the same raw -> seed-step1 10 ms scenario and emit runtime evidence for:

1. mode-1 and mode-2 suction mass/energy balances;
2. MCP suction flow and steam-drum recirculation flow from the committed-step snapshot;
3. suction raw selected transport energy;
4. drum liquid internal energy, density, pressure, specific flow work and selected advected transport energy;
5. explicit identities:
   - `specific_flow_work = p / rho`;
   - `selected_transport_energy = u + p / rho` for the enthalpy transport mode used by this path;
   - `liquid_energy_rate = selected_transport_energy * recirculation_flow`;
6. reconstruction of the mode-2 inverse resolver path from the exact frozen raw and seed-step1 conserved inventories;
7. true IEEE-754 bit comparison for the common public forward saturation properties between closure modes.

The runtime test must not reference the test-only IF97 helper.

### B. Independent Simulation reference counterfactual

A separate explicit `Simulation.Tests` REV1 test will run **after** the Application runtime capture. It will read only the minimal runtime input artifact required for the independent calculation and use the existing `IapwsIf97Reference` helper.

At the recorded drum temperature and pressure it will calculate:

1. IF97 Region-4 saturation pressure;
2. IF97 Region-1 liquid `v`, `rho` and `u` at the same sampled thermodynamic point;
3. independent liquid transport energy `h_ref = u_ref + p * v_ref`;
4. the production drum-liquid transport-energy delta relative to IF97;
5. the mode-2 raw suction transport-energy delta relative to IF97;
6. a counterfactual suction source rate using the independent IF97 liquid transport energy and the **actual captured recirculation flow**;
7. the counterfactual net suction energy rate using the **actual captured MCP flow**.

This test must not change production or generate C4 data. `IapwsIf97Reference` remains test-only.

### C. Causal language

REV1 adjudication must distinguish three concepts:

- `localized-seam`: supported directly by runtime evidence;
- `causal-closure`: permitted only if the independent IF97 counterfactual closes;
- `repair-owner`: remains `UNSELECTED` until a later repair-planning gate.

The reviewed `causal-owner=...` field is therefore retired from the future REV1 output.

## REV1 planned evidence set

The planned returned directory remains dedicated to Diagnostic 3 REV1 and must contain a complete ordered set. The implementation may refine exact column names, but not the evidence responsibilities:

1. `01-step1-suction-energy-balance.csv` — runtime mode-1/mode-2 mass and energy identity;
2. `02-mode2-suction-inverse-path.csv` — reconstructed mode-2 raw and step1 inverse paths;
3. `03-forward-transport-decomposition.csv` — production drum `u`, `rho`, `p/rho`, `u+p/rho`, rates and true bit-equality evidence;
4. `04-reference-counterfactual-input.csv` — minimal SHA-stable runtime inputs consumed by the independent reference test;
5. `05-if97-reference-counterfactual.csv` — Region-4/Region-1 independent result and actual-vs-counterfactual energy-rate comparison;
6. `06-diagnostic-summary.txt` — adjudicated diagnostic classification;
7. `07-pre-repair-review.txt` — authority and next-step record.

No `06`/`07` artifact may pre-exist before execution.

## Frozen REV1 identity/localization guards

These are diagnostic guards, not model-acceptance envelope changes.

The REV1 implementation must preserve the already-reviewed guards:

- runtime step = `10 ms`;
- one deterministic seed-preconditioning step;
- mode-1 suction observed energy rate absolute value `<= 0.01 W`;
- candidate observed energy rate within `[-5.217 MW, -5.215 MW]` before any repair;
- candidate production source/sink reconstruction residual absolute value `<= 0.001 W`;
- suction mass bookkeeping residual absolute value `<= 1E-9 kg/s`;
- mode-2 raw reconstructed phase = `SubcooledLiquid`;
- mode-2 seed-step1 reconstructed phase = `SaturatedMixture`;
- forward saturation property comparison must be true **bitwise** equality across closure modes at the sampled point.

REV1 adds the following independent counterfactual guards, frozen before execution:

- absolute difference between actual drum pressure and independent IF97 Region-4 saturation pressure at the frozen point `<= 1 Pa`;
- absolute difference between the independent IF97 liquid transport energy and mode-2 raw suction selected transport energy `<= 0.001 J/kg`;
- absolute independent counterfactual net suction energy rate `<= 0.01 W`;
- production-vs-IF97 liquid transport-energy gap must explain the same sign and approximately `52.1–52.25 kJ/kg` magnitude already localized by returned Diagnostic 2 evidence.

The independent counterfactual is therefore strong enough to falsify the ownership hypothesis: if it does not close, the diagnostic must not relabel the failure as a repair plan.

## REV1 adjudication classes

### `CAUSAL-CLOSURE-CONFIRMED`

Allowed only when all runtime identities and the independent IF97 counterfactual pass. Interpretation:

- the first-step energy discontinuity is real and source/sink-accounted;
- the production drum-liquid transport state differs materially from the independent reference-consistent liquid state;
- the independent reference state aligns with the mode-2 suction inventory strongly enough to remove the approximately `5.216 MW` discontinuity in the test-only counterfactual;
- the seam is causally closed for planning purposes;
- **repair owner remains unselected**.

### `TRANSPORT-SEAM-LOCALIZED-COUNTERFACTUAL-NOT-CLOSED`

Runtime accounting closes but the independent IF97 counterfactual does not. No repair planning may select the forward-provider ownership hypothesis. A new diagnostic/replanning gate is required.

### `DIAGNOSTIC-IDENTITY-RED`

Mass/energy identity, temporal/topological assumptions or frozen checkpoint semantics fail. Stop; do not use the ownership hypothesis.

### `REFERENCE-COUNTERFACTUAL-RED`

The independent IF97 helper/input contract or counterfactual reference calculation fails. Stop; do not promote causal ownership.

## Hosted CI track

`GitHub Ordinary CI Deterministic Serialization Hotfix 1` is locally PASS. Hosted GitHub confirmation remains pending.

The two tracks are deliberately separate:

- <!-- NRS-MARKER:DIAG3-REV1-HOSTED-CI-REPAIR-HOLD -->

Diagnostic 3 REV1 **test-only evidence collection and returned adjudication may proceed** while hosted confirmation is pending;
- **production repair planning/implementation may not begin** until hosted `ordinary-ci` is GREEN on the deterministic candidate;
- a hosted CI RED must be repaired as repository-hygiene work without weakening test coverage or changing physics to make CI green.

This supersedes the older roadmap sentence that prohibited all physics-branch evidence work while hosted CI was red/pending. It does **not** weaken the production-repair hold.

<!-- NRS-MARKER:DIAG3-REV1-NEXT-AUTHORITY -->

## Detailed successor sequence

### Gate 1 — Diagnostic 3 REV1 implementation audit

Implement only the test/validator/runner/adjudicator changes described above. Requirements:

- no `src/` changes;
- no historical-test semantic changes;
- reviewed Diagnostic 3 remains frozen provenance;
- new REV1 Application and Simulation reference tests are explicit/opt-in;
- no direct IF97 dependency may enter production;
- no C4 payload regeneration;
- no seed, threshold, exact-v9 or runtime-default change.

### Gate 2 — Diagnostic 3 REV1 execution

Run in controlled order:

1. static/provenance audit;
2. build `Application.Tests` and `Simulation.Tests` with warnings-as-errors inherited from repository policy;
3. Application runtime capture;
4. independent Simulation IF97 counterfactual;
5. evidence adjudication;
6. return complete `01`–`07` artifacts.

A local execution PASS is still evidence only.

### Gate 3 — returned-evidence independent adjudication

Re-hash and review `01`–`07`. Confirm that the result is one of the frozen classes above. No class may be invented after seeing the data without a new planning amendment.

### Gate 4 — hosted ordinary-ci confirmation

In parallel with Gates 1–3, confirm the deterministic ordinary candidate on GitHub. The production-repair branch opens only when:

```text
Diagnostic 3 REV1 returned/adjudicated = CAUSAL-CLOSURE-CONFIRMED
AND
hosted ordinary-ci = GREEN
```

### Gate 5 — R3 Energy-Transport Ownership Repair Planning 1

Only after both Gate-4 prerequisites are true. The planning gate must compare, at minimum, these non-selected design families:

- **Family A — model/provider ownership injection:** make steam-drum separation consume the selected thermodynamic ownership instead of constructing an independent default model;
- **Family B — explicit closure-aware transport-property contract:** separate conserved-inventory resolution from transport-property ownership through a dedicated contract;
- **Family C — bounded mode-2 transport bridge:** localize reference-consistent transport ownership at the integrated-primary/drum seam while preserving historical modes unchanged.

No family is preferred or selected by this document.

Repair Planning 1 must evaluate each family against these invariants:

1. modes 0/1 and canonical exact-v9 historical behavior remain unchanged;
2. no raw-seed retuning;
3. no threshold/envelope relaxation;
4. no C4 payload mutation;
5. no direct IF97 production-runtime dependency;
6. exact mass/energy conservation bookkeeping is preserved;
7. deterministic replay/checkpoint behavior is preserved;
8. no new per-step allocation/performance regression is introduced on the hot path;
9. ownership is singular and explicit rather than duplicated between solvers;
10. the smallest justified production surface is used.

Planning output must include an ownership map, option matrix, selected implementation seam with rationale, revalidation impact and a pre-implementation stop review.

### Gate 6 — bounded repair implementation and fast gate

Only an explicitly selected Repair Planning 1 result may authorize a production change. The repair candidate must then pass:

- build + complete ordinary suite;
- focused transport/conservation/reference tests;
- unchanged exact-v9/current-evidence audits;
- a bounded seed-integration fast gate using the already-frozen R3 criteria rather than a new tolerance;
- explicit confirmation that the artificial approximately `-5.216 MW` first-step seam is removed without introducing a new source/sink imbalance.

The fast gate must not use phase-label equality alone as the acceptance criterion; conserved inventory, transport energy, hydraulic state and existing dynamic envelopes remain authoritative.

### Gate 7 — R3 Short Requalification 3

Authorized only by an explicit fast-gate PASS. Re-run the short R3 qualification with unchanged reference criteria. Returned evidence must be independently adjudicated.

### Gate 8 — R4 Planning 1

Authorized only after returned/adjudicated R3 PASS. Until then R4 remains blocked.

## Stop rules

Stop immediately and do not advance to repair planning if any of the following occurs:

- REV1 runtime energy identity fails;
- IF97 counterfactual does not close;
- the reference calculation requires a production IF97 dependency;
- hosted ordinary CI remains RED when a production repair would otherwise begin;
- a proposed repair requires seed retuning, tolerance relaxation or C4 payload mutation to pass;
- modes 0/1 or canonical exact-v9 behavior changes outside the explicitly authorized seam;
- a repair requires multiple simultaneous ownership changes that prevent causal attribution.

## Authority after this planning record

Authorized next activity:

`IMPLEMENT-DIAGNOSTIC3-REV1-TEST-ONLY`

Still unauthorized:

- production repair;
- seed retuning;
- transport convention change in production;
- threshold/envelope change;
- C4 resolver/payload change;
- canonical exact-v9 change;
- R3 Short Requalification 3;
- R4 Planning 1;
- VR3;
- P3-R1;
- Replacement-Long Baseline 2.

