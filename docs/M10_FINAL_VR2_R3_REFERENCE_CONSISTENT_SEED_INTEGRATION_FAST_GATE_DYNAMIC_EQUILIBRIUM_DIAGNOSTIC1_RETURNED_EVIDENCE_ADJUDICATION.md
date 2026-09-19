# M10 Final VR2 — R3 Reference-Consistent Seed Integration Fast-Gate Dynamic Equilibrium Diagnostic 1 — Returned Evidence Adjudication

## Status
**PASS — DIAGNOSTIC EVIDENCE COMPLETE**

Engineering classification: **PRECONDITIONING-INDUCED PRIMARY-HYDRAULIC OPERATING-POINT DIVERGENCE**.

This refines, but does not rewrite, the diagnostic runner's emitted classification `POST-SEED-DYNAMIC-EQUILIBRIUM-DIVERGENCE`.

## What the returned evidence proves

The raw reference-consistent conserved-inventory vector is not the failing contract. Candidate Construction 1 already established 12/12 raw node resolution and 12/12 raw phase agreement with very small target residuals. Implementation 1 subsequently builds and passes the ordinary Release suite.

The divergence becomes visible across the canonical 20 ms / two-step seed preconditioning and is already present **before the first normal Running step**:

- post-preconditioning `hotwell` pressure delta: `646.306785908546 Pa`;
- post-preconditioning `drum` pressure delta: `538.727745505981 Pa`;
- post-preconditioning `suction` pressure delta: `-272.129222641699 Pa`;
- `suction` phase differs after preconditioning: canonical mode 1 = `SubcooledLiquid`, candidate mode 2 = `SaturatedMixture`;
- at Running step 1, the largest hydraulic-head delta is `return` = `-1077.23071756773 Pa`;
- step-1 primary-pump flow delta = `-0.0304048876640337 kg/s`;
- step-1 governor delta = `0.000895064710395133`.

The primary hydraulic displacement therefore precedes the material governor response. The controller is reacting to a plant-state displacement; the evidence does not support treating the governor as the initiating owner.

## First-100-step result

- canonical mode 1 envelope violations: `0/100`;
- candidate mode 2 envelope violations: `86/100`;
- first primary-flow violation: step `15`;
- first governor violation: step `17`;
- step-100 primary-flow delta: `-0.714476290249237 kg/s`;
- step-100 governor delta: `0.163909676937656`;
- step-100 speed-error delta: `0.128437139027028`;
- step-100 speed-integral delta: `0.00101960824346037`;
- non-finite states: none observed;
- trips: none observed;
- breaker openings: none observed;
- rollbacks: none observed.

By step 100 the hydraulic-head displacement has grown materially:

- return head delta: `-13319.5769587904 Pa`;
- main-circulation-pump head delta: `-7205.79323059414 Pa`;
- channel head delta: `-3370.04629446473 Pa`.

## What is not yet proven

This diagnostic does **not** establish the final causal mechanism. In particular it does not yet distinguish between:

1. a phase-boundary/branch-sensitivity difference during seed preconditioning, especially around `suction`;
2. a closure-tangent difference under the same first preconditioning perturbation;
3. a preconditioning-path assumption that is exact-v9/mode-1-specific and therefore not equilibrium-preserving for mode 2.

No production repair may be selected from Diagnostic 1 alone.

## Authority

Diagnostic 1 authorizes only a **test-only two-seed-step preconditioning divergence diagnostic** that records the raw state, preconditioning step 1 and preconditioning step 2 separately for canonical mode 1 and candidate mode 2.

Not authorized:

- seed retuning;
- threshold/envelope change;
- C4 resolver or payload mutation;
- canonical exact-v9 mutation;
- production repair;
- R3 Short Requalification 3;
- R4 Planning 1.

R3 remains **RED** and R4 remains **BLOCKED**.

## Frozen returned evidence

The six returned artifacts are frozen under:

`eng/frozen-evidence/ordinary/M10FinalVR2_R3_SeedIntegration_FastGateDynamicEquilibriumDiagnostic1_ReturnedArtifacts`

with SHA-256 values recorded in the project restart checkpoint.
