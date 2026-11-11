# M10 Final VR2 — R3 Requalification 2 Initial Closure / Operating-Point Displacement Diagnostic 1

## Purpose

Localize the returned Requalification 2 RED without another 120 s run.

The repaired mode 2 is now executable and deterministic, but the exact-v9 operating point is displaced from the first second. This diagnostic distinguishes three mechanisms:

1. seed inventory divergence between mode 1 and mode 2;
2. different thermodynamic closure mapping for identical conserved inventories;
3. post-seed integration/control divergence despite identical initial mapping.

## Method

Test-only, no production changes.

The diagnostic compares:
- all 12 initial fluid-node conserved inventories;
- factory-resolved thermodynamic states;
- direct mode-1 vs mode-2 closure resolution on the exact same mode-1 inventories;
- eight key pre-step hydraulic pressure heads;
- the first 100 running steps (1 simulated second) side-by-side for electrical output, primary flow, drum level, governor output, rollback count and snapshot fingerprints.

## Classification

The test writes one of:
- `SEED-INVENTORY-DIVERGENCE`;
- `IDENTICAL-INVENTORY-CLOSURE-MAPPING-DISPLACEMENT`;
- `POST-SEED-INTEGRATION-DIVERGENCE`;
- `UNRESOLVED-DIAGNOSTIC1`.

A completed diagnostic does not authorize a repair. Returned evidence must be reviewed before any next planning decision.
