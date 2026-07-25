# Reference Plant Scale Migration Plan

## Status

**M10.9.4.1-E.2 + HOTFIX 1 — IMPLEMENTED CANDIDATE / FOCUSED AUDIT GREEN; LONG PROMOTION GATES PENDING**

E.1 closed the scale-direction decision. E.2 applies that accepted target to the current-v2 sustained runtime while preserving legacy v1 semantics; Hotfix 1 carries signed motoring torque through the generator/grid-owned internal rotor seam. On 2026-07-25 the focused scale/migration audit passed 2/2 tests. The candidate still requires both 60-second journeys, the complete operational-envelope audit and manual HMI validation.

## Accepted target

The current-v2 candidate uses a **10 MWe nominal generator scale**.

| Quantity | Accepted target / rule |
|---|---:|
| Generator nameplate | 10 MWe |
| Normal sustained reference point | 5 MWe |
| Supported requested-load envelope for the current training profile | 0–10 MWe |
| Rotor rated speed | 3,000 rpm |
| Rotor moment of inertia | 1,000 kg·m² retained |
| Stored rotor energy at rated speed | 49.348 MJ |
| Inertia constant at 10 MWe | 4.934802 s |
| Generator efficiency | 98% retained |
| Operator LOAD RAISE / LOWER increment | 5 MWe retained for the present training profile |

The resulting 5 MWe sustained point is a 50% load point rather than the previous hybrid interpretation of 0.5% of a 1,000 MW nameplate.

## Why the target is accepted

The accumulated A–D evidence supports the reduced-scale interpretation:

- the existing rotor stores 49.348 MJ at 3,000 rpm, giving `H ≈ 4.935 s` against a 10 MWe rating;
- the turbine, condenser and steam-path capacities are already in a low-megawatt educational range;
- the sustained reference point is 5 MWe;
- D.1 closed the turbine phase-policy mismatch;
- D.2 showed material admission authority around the current operating bias and identified authority compression only at high opening;
- D.3 exercised 23.418 percentage points of command/position lag while integral excursion remained only 0.134 percentage points, so actuator tracking anti-windup is not required before scale migration.

Retaining a 1,000 MW nameplate would require an explicit full-scale/very-low-load scaling policy for rotor inertia, steam path, condenser, governor and protection. That interpretation is rejected for the current educational reference profile.

## What E.1 does not change

E.1 changed documentation and migration ownership only. E.2 now sets the current-v2 sustained reference generator to 10 MWe; historical/default profiles retain the 1,000 MW legacy default for replay and compatibility.

E.1 therefore does **not** silently change:

- `MaximumElectricalPower`;
- rotor inertia or rated speed;
- governor droop rise;
- synchronizing correction or frequency damping;
- generator/grid torque law;
- protection thresholds;
- HMI ranges derived from the active generator definition;
- replay/reference trajectories.

## E.2 coordinated migration requirements

E.2 must apply the scale change as one versioned physical migration, not as an isolated nameplate edit.

### 1. Nameplate and inertia

- set the current-v2 reference generator nameplate to 10 MWe;
- retain 1,000 kg·m² rotor inertia and 3,000 rpm rated speed unless a focused regression proves a change is necessary;
- prove the derived inertia constant and rotor acceleration response.

### 2. Governor normalization

The current 150 rpm full-load rise cannot be copied blindly onto the new 10 MWe normalization because it would change the 5 MWe closed-loop reference from 0.75 rpm to 75 rpm.

E.2 must choose and test a versioned current-v2 droop value using these gates:

- bumpless or deliberately bounded transition at the validated 5 MWe point;
- material but controllable load authority;
- no persistent saturation;
- D.3 integral-windup result remains valid or is re-audited if controller gains/travel are changed.

### 3. Bidirectional generator/grid coupling

E.2 must replace the present non-negative electromagnetic-power clamp with signed coupling semantics before reverse-power or loss-of-synchronism protection is added.

Required behavior:

- positive electromagnetic load opposes the turbine as generation;
- negative electromagnetic load accelerates the rotor as motoring;
- both positive and negative slip directions have restoring behavior when paralleled;
- coupling magnitude is bounded independently and cannot be inferred solely from the nameplate;
- conversion losses remain positive in both power directions.

### 4. Synchronizing/coupling magnitudes

The current 10 MW synchronizing correction and 10 MW-per-Hz damping values are pre-migration values. E.2 must retune or explicitly retain them from dynamic evidence; they must not be rescaled by ratio alone.

### 5. HMI and operator semantics

- nameplate/ranges must show the migrated 10 MWe scale;
- the normal 5 MWe point must be clearly presented as 50% load;
- LOAD RAISE/LOWER remains 5 MWe per accepted command for the current training profile unless a separate UX decision changes it;
- requested load, actual electrical power and machine rating remain visually distinct.

### 6. Protection sequencing

Reverse power, supervised underfrequency and loss-of-synchronism protection remain **E.3**, after E.2 proves that the corresponding negative-power/slip states exist physically and deterministically.

## Required E.2 regressions

- 10 MWe nameplate and 1,000 kg·m² inertia are explicit current-v2 ownership;
- 5 MWe is exactly 50% requested load;
- generator inputs reject requests above the migrated nameplate;
- signed motoring and generation are both representable;
- grid coupling restores phase/frequency error in both slip directions;
- generator conversion losses remain non-negative in both directions;
- 60-second synchronization and 300-second sustained journeys remain healthy;
- replay/checkpoint determinism remains unchanged for preserved historical profiles;
- HMI ranges and labels follow the migrated definition rather than hard-coded values.

## Compatibility rule

Historical/v1 profiles remain on their historical definitions. The scale migration is current-v2/current-hardening ownership and must not rewrite old replay physics implicitly.

## E.2 implemented candidate decisions

- current-v2 sustained nameplate: **10 MWe**; legacy/default: unchanged;
- current-v2 normal point: **5 MWe = 50%**;
- current-v2 governor full-load rise: **1.5 rpm**, chosen to preserve the already validated **0.75 rpm** droop displacement at 5 MWe rather than retuning governor behavior during the scale migration;
- current-v2 grid power-flow mode: **Bidirectional**; legacy coupling defaults to **GenerationOnly**;
- signed shaft exchange: positive = generation loading, negative = grid motoring;
- signed electrical power: positive = export, negative = import; conversion loss remains positive in both directions;
- bidirectional torque uses current electrical speed as the power-to-torque reference near synchronous operation, with a bounded 10% rated-speed floor until E.3 loss-of-synchronism protection exists;
- shaft-side clamps correspond to ±10 MWe electrical nameplate with 98% conversion efficiency;
- synchronizing correction 10 MW and frequency damping 10 MW/Hz are explicitly retained, not automatically ratio-scaled;
- current-v2 HMI electrical scale: **-10..+10 MWe**; LOAD ± remains **5 MWe** and request clamps at **10 MWe**.

These choices are candidate until E.2 local validation passes.
