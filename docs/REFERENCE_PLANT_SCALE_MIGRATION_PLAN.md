# Reference Plant Scale Migration Plan

## Status

**M10.9.4.1-E.2 Hotfix 1 — VALIDATED**

The user confirmed compilation and all requested ordinary, focused and long-running gates passed on 2026-07-26. E.2 Hotfix 1 is the validated parent for E.3.1.

## Implemented candidate

### Current-v2 scale ownership

Only these version-2 sustained profiles opt into the new contract:

- `DesktopSustainedGenerationInitialConditionFactory`;
- `GridSynchronizationSustainedInitialConditionFactory`.

They now declare:

- 10 MWe generator nameplate;
- 1.5 rpm full-load governor rise;
- bidirectional grid coupling;
- unchanged 1,000 kg·m² rotor and 3,000 rpm rated speed;
- unchanged 5 MWe normal sustained request;
- unchanged 0.5 MW phase correction and 2 MW/Hz damping.

### Compatibility

The operational-seed factory defaults remain:

- 1,000 MWe nameplate;
- `GenerationOnly` coupling mode;
- null coupling when no coupling calibration is supplied.

Historical/v1 profiles therefore keep their prior definitions. The new public parameters are appended to the factory signature.

### Signed generator/grid solver

- bidirectional coupling can produce negative electromagnetic torque for a slow connected rotor;
- the public/manual rotor input remains non-negative;
- an internal generator/grid-owned factory carries signed torque into the rotor solver;
- generation is clamped by `Pmax / efficiency`;
- motoring is clamped by `Pmax × efficiency`;
- signed conversion uses positive losses in both directions;
- bidirectional power-to-torque conversion uses current speed with a 10% rated-speed floor;
- generation-only and null-coupling behavior preserve the historical rated-speed path.

### HMI and commands

- current-v2 individual and gross electrical ranges are `-10..+10 MWe`;
- labels distinguish grid exchange from one-way output;
- positive values mean export and negative values mean import;
- LOAD commands remain non-negative requests and clamp to the active 10 MWe nameplate.

## Implemented regressions

- default and explicit coupling-mode validation;
- generation-only slow-rotor compatibility;
- slow-rotor motoring with negative torque/power and positive loss;
- electrical-nameplate clamp during motoring;
- public negative manual torque remains rejected;
- current-v2 10 MWe / 1.5 rpm / bidirectional ownership;
- legacy 1,000 MWe / non-negative HMI compatibility;
- current-v2 0–10 MWe load-command clamp;
- signed HMI range derived from the active definition.

## Promotion gate — PASSED

1. `scripts\run-generator-grid-bidirectional-tests.cmd`;
2. complete ordinary suite;
3. turbine admission authority 3/3;
4. governor/actuator tracking 2/2;
5. gameplay long journeys 2/2;
6. operational-envelope audit 9/9;
7. reference-plant scale audit 4/4;
8. manual GENERATOR-station review of export/import labels and signed ranges.

Expected discovery used during E.2 candidate review, assuming the validated D.4.1 count was unchanged:

- ordinary passed: **952**;
- explicit skipped by ordinary run: **19**;
- total discovered: **971**.

The explicit scripts execute 20 cases because the scale-evidence test is shared between the operational-envelope and scale categories; this corresponds to **19 unique explicit tests**.

## E.3 continuation

E.3.1 recorded and validated reverse-power, supervised-underfrequency and phase-slip trajectories over the validated E.2 runtime. E.3.2 is now the working candidate after review of those generated reports.
