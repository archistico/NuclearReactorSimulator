# M10 Final VR2 — R3 Seed Integration Suction Energy-Transport Causal-Seam Diagnostic 3

Date: 2026-09-19
Status: TEST-ONLY DIAGNOSTIC CANDIDATE

## Purpose

Diagnostic 2 localized the first mode-1/mode-2 phase divergence to `suction` after the first 10 ms seed-preconditioning step. Diagnostic 3 determines whether that phase flip is causally preceded by a real conserved-energy displacement and, if so, whether the displacement closes against the existing production advective-energy terms.

This is a causal attribution diagnostic, not a repair. Production `src/`, seed values, C4 data, acceptance envelopes and exact-v9 remain frozen.

## Pre-execution analytical observation

The frozen Diagnostic 2 CSVs already show a strong accounting identity:

- candidate `suction` mass is unchanged from raw to seed-step1;
- candidate `suction` internal energy decreases by about `52.163 kJ` in `10 ms`, approximately `-5.21627 MW`;
- canonical mode 1 has essentially zero energy change over the same step;
- the mode-2 raw `suction` conserved inventory carries about `52.163 kJ/kg` more selected advected specific energy than the liquid recirculation source generated from the common steam-drum forward saturation properties;
- at about `100 kg/s`, that specific-energy difference predicts the observed `-5.21627 MW` sink to numerical roundoff.

This observation is not promoted to a root-cause decision until the runtime diagnostic records the exact production snapshots and resolver path.

## Controlled experiment

The explicit Application.Tests diagnostic reconstructs the exact one-step preconditioned runtime for:

1. canonical correlation-consistent mode 1;
2. the reference-consistent mode-2 candidate.

It emits three raw evidence files:

- `01-step1-suction-energy-balance.csv`: raw/post `suction` conserved inventories, MCP mass flow, drum liquid recirculation flow, selected specific energies, observed/predicted mass and energy rates and residuals;
- `02-mode2-suction-inverse-path.csv`: mode-2 resolver path at raw and seed-step1 `suction` states;
- `03-forward-saturation-provider-seam.csv`: mode-1/mode-2 forward saturation-property comparison at the drum temperature and the actual drum liquid advected energy.

The adjudicator then writes:

- `04-diagnostic-summary.txt`;
- `05-pre-repair-review.txt`.

## Evidence conditions

The gate is diagnostic-complete only if all of the following hold:

- mode-1 and mode-2 suction mass balances close within `1E-9 kg/s` bookkeeping residual;
- mode-1 observed suction energy rate remains within `0.01 W` of zero;
- mode-2 observed suction energy rate lies between `-5.217 MW` and `-5.215 MW`;
- the production source/sink reconstruction closes the mode-2 energy rate within `0.001 W`;
- the mode-2 raw resolver state is `SubcooledLiquid` and seed-step1 is `SaturatedMixture`;
- the resolver path is recorded at raw and seed-step1 so the branch transition is evidence, not an assumption;
- the public forward saturation provider is bitwise identical between closure modes at the sampled drum temperature.

These numbers are identity/localization guards for this diagnostic only. They are not new model acceptance tolerances.

## Interpretation classes

If the complete identity above is proven, the adjudicator classifies the evidence as:

`MODE2-FORWARD-INVERSE-ADVECTED-ENERGY-CLOSURE-GAP`

This means the first step's suction energy displacement is exactly attributable to a transport seam between a forward-derived drum liquid source and the mode-2 inverse-domain conserved inventory. It still does **not** select a production repair. Repair ownership and invariant-preserving options require a separate planning/adjudication gate after the returned evidence is reviewed.

If the identity does not close, Diagnostic 3 fails closed and no repair planning is authorized from this hypothesis.

## Execution

From repository root in PowerShell:

```powershell
.\scripts\run-m10-final-vr2-r3-seed-integration-suction-energy-transport-causal-seam-diagnostic3.cmd
```

Return the complete directory:

`artifacts\m10-final-physical-reference-vr2-r3-seed-integration-suction-energy-transport-causal-seam-diagnostic3`

## Authority after local execution

Even a local PASS means only `PASS-DIAGNOSTIC-EVIDENCE-COMPLETE`. Production repair, seed retuning, threshold change, C4/payload mutation, exact-v9 change, R3 Requalification 3 and R4 Planning 1 remain false until returned evidence is independently adjudicated.
