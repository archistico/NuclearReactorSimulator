# M10 Final VR2 — R3 Two-Seed-Step Preconditioning Divergence Diagnostic 2 — Returned-Evidence Adjudication

Date: 2026-09-19

## Decision

Diagnostic 2, including Adjudicator Hotfix 1, is accepted as complete diagnostic evidence. The Hotfix changed only the PowerShell adjudication path; the dynamic evidence files `01`–`06` were not regenerated. The complete returned evidence set `01`–`09` is frozen and SHA-256 locked.

R3 remains **RED**. This decision does not authorize a production repair, seed retuning, acceptance-envelope change, C4/payload change, canonical exact-v9 change, R3 Requalification 3 or R4 Planning 1.

## Localized first divergence

The raw checkpoint contains 12/12 phase-aligned nodes. The first phase mismatch occurs after the first 10 ms deterministic preconditioning step on `suction`:

- mode 1: `SubcooledLiquid`;
- candidate mode 2: `SaturatedMixture`;
- candidate quality: `1.5376981274668928E-07`.

The same mismatch persists at seed-step2. During seed-step1 the maximum absolute pressure delta grows to `338.69053787482881 Pa`, the maximum hydraulic-head delta to `370.76260225754231 Pa`, while the maximum flow delta is still only `4.5798939183328E-05 kg/s`. Governor, speed-error and integral deltas remain zero at this first step.

The evidence therefore localizes the initiating seam to the first preconditioning step in the primary hydraulic / thermodynamic path, not to a subsequent governor response.

## Causal question left open by Diagnostic 2

Diagnostic 2 does not establish why `suction` crosses the mode-2 phase boundary. Three materially different mechanisms remain possible without a dedicated balance closure:

1. a true change in conserved suction mass/energy caused by source/sink transport;
2. a closure-path branch change with essentially unchanged conserved inventory;
3. an interaction between a common forward saturation-property source and the mode-2 reference-consistent inverse-domain interpretation.

The next authorized gate is therefore test-only `R3-SEED-INTEGRATION-SUCTION-ENERGY-TRANSPORT-CAUSAL-SEAM-DIAGNOSTIC3`.

## Authority

Diagnostic 3 may reconstruct exactly one deterministic seed step, close the `suction` mass/energy balance, record the mode-2 inverse resolver path and verify whether the steam-drum forward saturation provider is common across modes. It may not change production `src/`, seed values, thermodynamic tables, transport conventions, tolerances or exact-v9.
