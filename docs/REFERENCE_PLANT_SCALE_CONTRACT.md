# Reference Plant Scale Contract

## Status

**E.1 DECISION ACCEPTED — 10 MWe TARGET; E.2 NOT YET IMPLEMENTED**

M10.9.4.1-D.4 is the validated runtime baseline. The active current-v2 source still uses the pre-E generator/grid contract. E.1 records the intended reduced-scale educational direction; it does not authorize isolated runtime edits and it does not mean that the 10 MWe migration is already live.

## Active validated runtime contract

- generator nameplate: 1,000 MW;
- normal sustained request: 5 MWe, equal to 0.5% of nameplate;
- rated speed / rotor inertia: 3,000 rpm / 1,000 kg·m²;
- stored rotor energy at rated speed: approximately 49.348 MJ;
- inertia constant against the configured nameplate: approximately 0.049348 s;
- full-load governor reference rise: 150 rpm, giving 0.75 rpm at 5 MWe;
- grid coupling: correction-only / generation-only behavior;
- maximum synchronizing correction: 0.5 MW;
- frequency damping at one hertz slip: 2 MW;
- electrical output and HMI ranges remain non-negative under the current contract.

These values are frozen by `ReferencePlantScaleAuditTests` and `ReferencePlantScaleMigrationTests`. On 2026-07-25 the dedicated script passed 2/2 tests and confirmed that the active source remains pre-E.

## Accepted E.1 target

E.1 selects **Option B — reduced-scale educational unit** as the future current-v2 direction:

- nominal generator scale: 10 MWe;
- normal sustained point: 5 MWe, intended to become 50% load;
- rated speed: 3,000 rpm;
- rotor inertia: 1,000 kg·m² retained unless focused evidence proves otherwise;
- resulting target inertia constant: approximately 4.934802 s;
- requested-load envelope: 0–10 MWe for the current training profile.

The following remain E.2 design choices rather than active behavior:

- 1.5 rpm full-load governor rise to preserve the existing 0.75 rpm displacement at 5 MWe;
- versioned bidirectional generator/grid coupling;
- signed electrical output and internal signed rotor torque;
- -10..+10 MWe presentation ranges;
- positive conversion-loss accounting in both power directions.

## Why isolated edits are prohibited

The current source combines values associated with different apparent scales: a 1,000 MW generator nameplate, a 1,000 kg·m² rotor, a 5 MWe operating point and low-megawatt turbine/condenser capacities. Nameplate, inertia, droop, coupling, protections, HMI ranges and reference trajectories interact. Changing only `MaximumElectricalPower` would alter several validated mechanisms at once and is explicitly prohibited.

## E.2 coordinated migration gate

E.2 must be implemented as one versioned candidate covering:

1. current-v2 10 MWe nameplate ownership;
2. retained or deliberately revised rotor inertia;
3. governor normalization and bumpless 5 MWe behavior;
4. generation-only default plus current-v2 bidirectional opt-in;
5. signed power and torque with positive losses in both directions;
6. HMI range and operator-load semantics;
7. replay/checkpoint compatibility;
8. dedicated generation, motoring, synchronization and long-running evidence.

Reverse-power, supervised-underfrequency and loss-of-synchronism protection remain E.3 and must not begin until E.2 trajectories are proven.
