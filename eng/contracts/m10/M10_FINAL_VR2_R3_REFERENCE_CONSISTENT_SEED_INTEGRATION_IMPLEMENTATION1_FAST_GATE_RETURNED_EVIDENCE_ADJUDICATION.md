# M10 Final VR2 — R3 Reference-Consistent Seed Integration Implementation 1 — Fast-Gate Returned Evidence Adjudication

## Supersession note after Dynamic Equilibrium Diagnostic 1

The earlier label `POST-SEED DYNAMIC OPERATING-POINT DIVERGENCE` remains valid as the observed fast-gate symptom, but Diagnostic 1 localizes the onset more tightly: material state displacement is already present after the two canonical seed-preconditioning steps and before Running step 1. The current engineering classification is therefore **PRECONDITIONING-INDUCED PRIMARY-HYDRAULIC OPERATING-POINT DIVERGENCE**.

The raw 12-node conserved-inventory candidate remains qualified evidence; do not retune it from the fast-gate RED alone.


## Status
**RED — POST-SEED DYNAMIC OPERATING-POINT DIVERGENCE**

The implementation compiles and reached the focused fast gate. Raw seed evidence is healthy: 12/12 nodes finite and 12/12 phase matches. The first 100 Running steps remain finite with no trip, breaker opening, or rollback, but 86/100 steps are outside the unchanged exact-v9 envelope.

The first primary-flow violation is step 15 (`<99.9 kg/s`); the first governor violation is step 17 (`>29.30%`). Electrical export and drum level remain inside their frozen envelope through step 100.

This evidence does not authorize threshold changes, seed retuning, C4 mutation, Requalification 3, or R4. It authorizes only a test-only dynamic-equilibrium diagnostic.
