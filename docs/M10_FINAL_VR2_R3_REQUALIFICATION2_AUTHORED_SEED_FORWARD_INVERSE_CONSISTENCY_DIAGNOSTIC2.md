# M10 Final VR2 — R3 Requalification 2 Authored-Seed Forward / Inverse Consistency Diagnostic 2

## Purpose

Determine whether the exact-v9 raw authored seed recipe is internally consistent with closure mode 2 before deterministic seed preconditioning begins.

The factory constructs authored fluid inventories from pressure/quality or temperature/compression using `GetSaturationProperties(...)`. That forward saturation-property API is common to closure modes 1 and 2. Closure mode 2 then interprets conserved `(v,u)` through the C4 reference-consistent inverse resolver.

Diagnostic 1 observed a post-preconditioning divergence but could not distinguish raw seed construction from preconditioning dynamics.

## Method

Diagnostic 2 reconstructs all 12 exact-v9 raw fluid inventories using the same authored recipe and plant node volumes, then:

- confirms mode-1 and mode-2 forward saturation properties are bitwise identical;
- resolves the same conserved raw inventory with mode 1 and mode 2;
- records pressure, temperature, phase and quality deltas;
- reconstructs eight initial hydraulic pressure heads from the two inverse interpretations;
- performs no deterministic preconditioning and no dynamic runtime steps.

## Classification

- `LEGACY-FORWARD-MODE2-INVERSE-SEED-CONSISTENCY-GAP`
- `FORWARD-PROPERTY-PROVIDER-DIVERGENCE`
- `NO-RAW-SEED-CLOSURE-DIVERGENCE`

This is diagnostic only. It does not authorize a seed repair, a new exact-version identity, threshold changes or R4.
