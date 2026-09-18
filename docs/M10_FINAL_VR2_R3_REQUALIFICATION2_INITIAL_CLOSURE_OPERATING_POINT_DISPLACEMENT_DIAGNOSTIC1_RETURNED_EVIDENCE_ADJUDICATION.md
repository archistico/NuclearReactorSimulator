# M10 Final VR2 — R3 Requalification 2 Initial Closure / Operating-Point Displacement Diagnostic 1 — Returned Evidence Adjudication

## Status
**PASS — DIAGNOSTIC EVIDENCE COMPLETE**

Returned classification: `SEED-INVENTORY-DIVERGENCE`.

Important refinement: the runtime exposed as logical STEP 0 has already executed two deterministic seed-preconditioning solver steps. Therefore this classification proves that mode 1 and mode 2 have diverged by the end of preconditioning; it does not prove that the raw authored exact-v9 inventories were constructed differently.

Additional returned evidence:
- post-preconditioning factory thermodynamics differ on 12/12 nodes;
- resolving the same mode-1 post-preconditioning inventories under mode 1 vs mode 2 differs on 12/12 nodes;
- maximum same-inventory pressure displacement is about 1.458 MPa;
- maximum initial hydraulic-head displacement is about 0.743 MPa;
- mode 1 stays in envelope for 100/100 steps;
- mode 2 is out of envelope for 100/100 steps;
- rollback count is still zero for both after 100 steps.

This authorizes only Diagnostic 2 to inspect the raw authored seed before preconditioning.
