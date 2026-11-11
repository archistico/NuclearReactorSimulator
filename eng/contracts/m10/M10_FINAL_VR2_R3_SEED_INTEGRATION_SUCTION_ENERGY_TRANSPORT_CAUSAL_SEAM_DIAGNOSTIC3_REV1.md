# M10 Final VR2 — R3 Seed Integration Suction Energy-Transport Causal-Seam Diagnostic 3 REV1

Date: 2026-09-19  
Status: **IMPLEMENTED / NOT YET EXECUTED — PREEXECUTION AMENDMENT 1 + VALIDATOR CONTRACT HYGIENE HOTFIX 1 APPLIED**

This document implements the test-only successor authorized by `M10_FINAL_VR2_R3_DIAGNOSTIC3_DEEP_REVIEW_REV1_PLANNING1.md`. Its preexecution harness/guard refinement is recorded in `M10_FINAL_VR2_R3_DIAGNOSTIC3_REV1_PREEXECUTION_AUDIT_PLANNING_AMENDMENT1.md`. Validator document-contract hygiene is recorded in `M10_FINAL_VR2_R3_DIAGNOSTIC3_REV1_VALIDATOR_CONTRACT_HYGIENE_HOTFIX1.md`.

<!-- NRS-MARKER:DIAG3-REV1-IMPLEMENTED -->

## Purpose

Diagnostic 3 REV1 preserves the reviewed raw -> seed-step1 10 ms experiment and strengthens only the evidence path. It does not change production code, the seed, thresholds, the C4 payload, canonical exact-v9 behavior, or any runtime default.

The diagnostic separates two evidence owners:

1. `Application.Tests` captures production runtime bookkeeping and transport decomposition.
2. `Simulation.Tests` consumes only the minimal runtime input artifact and computes an independent IAPWS-IF97 counterfactual with the existing test-only `IapwsIf97Reference`.

The independent comparator does not call the production water/steam model and is not referenced by `src/`.

## Recorded predecessor review

The pre-execution deep review is frozen as:

`PASS-WITH-PREEXECUTION-REVISION`

The review already confirmed:

- deterministic first-step temporal attribution;
- suction-node source/sink topology completeness;
- reconstructible mass and energy bookkeeping;
- the architectural ownership seam between the steam-drum internal/default forward model and the selected mode-2 inverse closure;
- authority containment.

REV1 does not rewrite the reviewed Diagnostic 3 candidate. The reviewed test, document, validator, adjudicator and runner remain provenance.

## Runtime evidence path

The explicit `Application.Tests` method:

`M10FinalVr2R3SeedIntegrationSuctionEnergyTransportCausalSeamDiagnostic3Rev1Tests.SeedStep1_SuctionEnergyTransport_EmitsRuntimeEvidenceForIndependentCounterfactual`

reconstructs exactly the same frozen one-step scenario and writes:

1. `01-step1-suction-energy-balance.csv`
2. `02-mode2-suction-inverse-path.csv`
3. `03-forward-transport-decomposition.csv`
4. `04-reference-counterfactual-input.csv`

### `01-step1-suction-energy-balance.csv`

Records mode-1 and mode-2:

- frozen raw mass/energy;
- frozen Diagnostic-2 step1 mass/energy;
- runtime step1 mass/energy;
- runtime-vs-frozen checkpoint delta;
- MCP flow;
- requested and actual liquid recirculation;
- inventory limiting;
- observed and source/sink-predicted mass rate;
- observed and source/sink-predicted energy rate;
- raw suction selected transport energy;
- drum liquid selected transport energy;
- their specific-energy difference;
- resulting step1 phase and quality.

### `02-mode2-suction-inverse-path.csv`

REV1 does **not** claim to instrument a private runtime call. It reconstructs the production mode-2 inverse resolver path from the exact frozen conserved inventories and labels the evidence:

`RECONSTRUCTED-FROM-EXACT-FROZEN-CONSERVED-INVENTORY`

### `03-forward-transport-decomposition.csv`

REV1 explicitly decomposes the production steam-drum liquid transport term:

`h_selected = u + p/rho`

for the current `SpecificEnthalpy` transport mode.

It records:

- drum raw temperature/pressure checkpoint identity;
- public forward saturation properties for mode 1 and mode 2;
- true IEEE-754 bit equality using `BitConverter.DoubleToInt64Bits`;
- production liquid density;
- production liquid internal energy;
- production specific flow work;
- independently recomputed `p/rho`;
- production specific enthalpy;
- independently recomputed `u + p/rho`;
- selected advected specific energy;
- liquid energy rate and its recomputed value.

This closes the bookkeeping chain from forward properties to the actual source term without changing production.

### `04-reference-counterfactual-input.csv`

This is intentionally minimal. It exposes only the data required by the independent test-only comparator:

- drum temperature and pressure;
- mode-2 raw suction transport energy;
- production drum-liquid transport energy;
- production drum-liquid internal energy and density;
- actual recirculation and MCP flows;
- actual observed candidate suction energy rate.

<!-- NRS-MARKER:DIAG3-REV1-EVIDENCE-CONTRACT -->

## Independent IAPWS-IF97 counterfactual

The explicit `Simulation.Tests` method:

`M10FinalVr2R3SeedIntegrationSuctionEnergyTransportCausalSeamDiagnostic3Rev1ReferenceCounterfactualTests.RawDrumPoint_IndependentIf97Reference_EmitsTransportCounterfactual`

runs only after the Application runtime capture.

It reads `04-reference-counterfactual-input.csv`, records its SHA-256, and uses only the existing test-only `IapwsIf97Reference` implementation to compute:

- Region-4 saturation pressure at the captured drum temperature;
- Region-1 liquid specific volume, density and internal energy at the captured drum temperature/pressure;
- independent liquid flow work `p*v`;
- independent liquid transport energy `u + p*v`;
- IF97-vs-production drum transport gap;
- IF97-vs-mode2 suction transport gap;
- counterfactual source rate using the actual recirculation flow;
- counterfactual sink rate using the actual MCP flow;
- counterfactual net suction energy rate.

The result is written to:

`05-if97-reference-counterfactual.csv`

The counterfactual is independent: it does not substitute the suction transport energy as the recirculation source and therefore cannot force a zero result by construction.

## Frozen guards

These are diagnostic identity/localization guards, not model acceptance-envelope changes.

Runtime:

- mode-1 `|observed energy rate| <= 0.01 W`;
- mode-2 observed energy rate in `[-5.217 MW, -5.215 MW]`;
- production energy identity residual `<= 0.001 W`;
- mass identity residual `<= 1E-9 kg/s`;
- production transport gap in `[52.100, 52.250] kJ/kg`;
- raw mode-2 suction resolves `SubcooledLiquid`;
- step1 mode-2 suction resolves `SaturatedMixture`;
- mode-1/mode-2 public forward saturation properties are truly bit-identical;
- explicit `p/rho`, `u+p/rho` and liquid-energy-rate identities must close to bookkeeping precision.

Independent reference:

- `|Pdrum - Psat_IF97| <= 1 Pa`;
- `|href_IF97 - hsuction_mode2| <= 0.001 J/kg`;
- counterfactual net rate must remain inside the **derived component budget** defined by Planning Amendment 1;
- counterfactual rate algebra identity residual `<= 1E-6 W`;
- `href_IF97 - hproduction_drum` must preserve the positive `52.100–52.250 kJ/kg` localized gap.

The superseded fixed `0.01 W` counterfactual-net guard is retained only in amendment provenance and is not used for adjudication.

## Frozen adjudication classes

Exactly one of the following may be written by the REV1 adjudicator:

- `CAUSAL-CLOSURE-CONFIRMED`
- `TRANSPORT-SEAM-LOCALIZED-COUNTERFACTUAL-NOT-CLOSED`
- `DIAGNOSTIC-IDENTITY-RED`
- `REFERENCE-COUNTERFACTUAL-RED`

`CAUSAL-CLOSURE-CONFIRMED` means the independent test-only reference closes the ownership hypothesis strongly enough for a later planning gate. It does **not** select a production repair owner.

For every class:

`repair-owner=UNSELECTED`

## Returned artifacts

A complete execution returns exactly:

1. `01-step1-suction-energy-balance.csv`
2. `02-mode2-suction-inverse-path.csv`
3. `03-forward-transport-decomposition.csv`
4. `04-reference-counterfactual-input.csv`
5. `05-if97-reference-counterfactual.csv`
6. `06-diagnostic-summary.txt`
7. `07-pre-repair-review.txt`

The adjudicator writes `06` and `07` after both focused tests complete.

## CI and authority

`GitHub Ordinary CI Deterministic Serialization Hotfix 1` is locally PASS. Hosted GitHub confirmation is still a separate track.

<!-- NRS-MARKER:DIAG3-REV1-HOSTED-CI-HOLD -->

Test-only REV1 execution and returned-evidence adjudication may proceed while hosted `ordinary-ci` confirmation is pending.

Production repair planning or implementation may **not** begin until hosted `ordinary-ci` is GREEN.

Even with `CAUSAL-CLOSURE-CONFIRMED`, REV1 does not authorize:

- production repair;
- repair planning;
- seed retuning;
- threshold/envelope changes;
- C4 payload changes;
- canonical exact-v9 changes;
- R3 PASS;
- R3 Requalification 3;
- R4 Planning 1.

<!-- NRS-MARKER:DIAG3-REV1-NEXT-AUTHORITY -->

After implementation audit, the only next activity is:

`EXECUTE-DIAGNOSTIC3-REV1-TEST-ONLY`

After execution, return all `01`–`07` artifacts for independent adjudication. Only a later returned-evidence gate may combine `CAUSAL-CLOSURE-CONFIRMED` with hosted `ordinary-ci=GREEN` and decide whether `R3 Energy-Transport Ownership Repair Planning 1` is authorized.
