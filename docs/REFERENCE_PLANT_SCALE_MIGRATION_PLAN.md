# Reference Plant Scale Migration Plan

## Status

**M10.9.4.1-E.2 — PLANNED / NOT IMPLEMENTED**

M10.9.4.1-D.4 is the validated source baseline. E.1 accepted a reduced-scale 10 MWe educational target, but the runtime still uses the pre-E 1,000 MW nameplate, 150 rpm droop normalization and correction-only/generation-only grid coupling. The 2/2 reference-scale audit passed because it freezes that current contract and verifies that Phase E remains deferred.

## Accepted target

| Quantity | E.1 target / rule |
|---|---:|
| Generator nameplate | 10 MWe |
| Normal sustained point | 5 MWe |
| Requested-load envelope | 0–10 MWe |
| Rotor rated speed | 3,000 rpm |
| Rotor moment of inertia | 1,000 kg·m² retained unless evidence requires revision |
| Stored rotor energy | 49.348 MJ |
| Target inertia constant | 4.934802 s |
| Generator efficiency | 98% retained |
| LOAD RAISE / LOWER increment | 5 MWe retained unless a separate UX decision changes it |

## Current source before migration

- generator nameplate: 1,000 MW;
- 5 MWe request: 0.5% load;
- governor full-load rise: 150 rpm;
- droop displacement at 5 MWe: 0.75 rpm;
- maximum synchronizing correction: 0.5 MW;
- frequency damping: 2 MW/Hz;
- no versioned bidirectional power-flow mode;
- electromagnetic power and rotor-load path remain non-negative;
- no internal signed generator/grid torque seam;
- electrical presentation remains non-negative.

## Required E.2 implementation

### 1. Nameplate and inertia

- set only the current-v2 sustained reference generator to 10 MWe;
- preserve historical/default profiles;
- retain 1,000 kg·m² and 3,000 rpm unless focused evidence justifies a change;
- prove inertia constant and controlled acceleration/deceleration response.

### 2. Governor normalization

The existing 150 rpm full-load rise cannot be copied onto a 10 MWe denominator. A candidate value of 1.5 rpm preserves the already validated 0.75 rpm displacement at 5 MWe, but it remains an E.2 decision that must be encoded and tested.

### 3. Bidirectional generator/grid coupling

Introduce a versioned mode with:

- `GenerationOnly` as the compatibility default;
- `Bidirectional` enabled only by current-v2 sustained profiles;
- positive electromagnetic torque opposing the turbine during generation;
- negative electromagnetic torque assisting the rotor during motoring;
- bounded signed shaft/electrical exchange;
- positive conversion losses in both directions;
- a generator/grid-owned internal signed torque seam while the public/manual rotor-input contract remains non-negative.

### 4. Synchronizing magnitudes

The current 0.5 MW correction and 2 MW/Hz damping values must be either retained deliberately or retuned from dynamic evidence. They must not be ratio-scaled automatically.

### 5. HMI and operator semantics

- signed current-v2 electrical ranges, provisionally -10..+10 MWe;
- clear separation of requested load, actual signed exchange and nameplate;
- 5 MWe represented as 50% load;
- LOAD commands clamped to 0..10 MWe.

### 6. Compatibility and replay

- historical/v1 definitions and replay identities unchanged;
- checkpoint/replay equivalence for new signed states;
- no hidden runtime migration of old sessions.

## Required E.2 regressions

- explicit 10 MWe current-v2 ownership and preserved legacy defaults;
- 5 MWe equals 50% load;
- request clamping at 10 MWe;
- generation and motoring unit tests;
- restoring behavior for positive and negative slip;
- positive losses in both directions;
- signed torque reaches the rotor only through the internal grid seam;
- HMI ranges derive from the active definition;
- 60-second journeys, complete operational-envelope audit and replay/checkpoint gates remain green.

## Protection sequencing

Reverse-power, supervised-underfrequency and loss-of-synchronism protection remain **E.3**. They must be based on observed E.2 signed trajectories, not added before those states exist in the canonical model.
