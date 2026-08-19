# M10.9.4.1-H.11 — Thermodynamic Switching Localization & Active-Set Diagnosis

**Status:** CANDIDATE on user-validated H.10 Hotfix 1.

## Why H.11 exists

H.10 changed the diagnosis materially. Around the two persistent H.9 failures it found no hydraulic branch switching and no local hydraulic non-smoothness, but it found exactly two thermodynamic phase/envelope switches and two thermodynamic non-smooth nodes. The matching explicit endpoints showed no such switch.

H.11 therefore does **not** introduce another nonlinear solver. It asks a narrower question: **which nodes cross which thermodynamic boundary, along which conserved-inventory direction, and how strongly are those nodes involved in the hydraulic fixed-point mismatch?**

## Shadow-only localization

`ThermodynamicSwitchingLocalizationAnalyzer` receives:

- one immutable H.9 candidate `PlantState`;
- the H.10 `HydraulicMapSmoothnessReport` for that state;
- the existing thermodynamic closure and saturation-property provider.

Only H.10 nodes with `PhaseOrEnvelopeSwitchObserved=True` are probed. No unflagged node is promoted merely because H.11 happens to see a large derivative.

## Conserved probes

For each localized node H.11 evaluates five points:

```text
nominal
energy-minus
energy-plus
mass-minus
mass-plus
```

The relative inventory amplitude remains observational at `1e-6`, matching the coarse H.10 inventory probe.

Each point records:

- resolved versus out-of-range;
- coarse phase;
- mass and internal energy;
- specific volume and specific internal energy;
- pressure and temperature;
- vapor quality when defined;
- saturation reference at the resolved temperature when available;
- relative pressure distance from saturation;
- saturated-liquid and saturated-vapor internal energies;
- energy distance above the saturated-liquid boundary and below the saturated-vapor boundary.

## Boundary classification

H.11 independently classifies whether the switch is exposed by energy, mass or both.

Boundary classes are:

- `phase-boundary` — a resolved probe changes coarse phase;
- `envelope-edge` — one side of a probe becomes unsupported by the existing thermodynamic model;
- `phase+envelope` — both phenomena are present.

The analyzer also emits `hold-<nominal phase>` as a **diagnostic suggested active set**. H.11 never enforces it and never changes the thermodynamic closure.

## Local fixed-point involvement

For every localized H.9 node the focused audit independently evaluates the hydraulic map at the final H.9 candidate and compares its node balance with `H9.AppliedHydraulicBalances`:

```text
local balance residual = mapped hydraulic balance - applied hydraulic balance
```

Mass-rate and energy-rate residuals are reported. This is observational evidence about how strongly the localized node participates in the stalled hydraulic fixed point; H.9 itself remains byte-for-byte unchanged.

## Frozen evidence contract

The H.11 audit must first reproduce:

```text
500 production-shadow intervals
P060/F040 frozen trigger
7 triggered events
H.4 = 5/7
H.6 = 6/7
H.7 = 5/7
H.8 = 5/7
H.9 = 5/7
persistent H.9 failures = 2
H.10 thermodynamic switch nodes = 2
explicit-end thermodynamic switch nodes = 0
```

Only then is H.11 localization evidence emitted.

## Decision after H.11

If H.11 localizes a stable boundary class and node identity, the next experiment may be a **narrow shadow-only active-set or semi-smooth formulation** targeted only at those nodes/boundaries.

If H.11 cannot stably localize the H.10 evidence, the next investigation returns to fixed-point existence/residual-floor/basin analysis rather than adding solver complexity.

Production remains `ExplicitCommittedState` at 10 ms throughout.
