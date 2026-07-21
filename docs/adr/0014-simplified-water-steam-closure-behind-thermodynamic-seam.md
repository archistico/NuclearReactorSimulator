# ADR 0014 — Simplified water/steam closure behind the thermodynamic seam

## Status

Accepted for M1.7 baseline candidate.

## Context

M1.2 established `IFluidThermodynamicModel` so conserved mass/internal energy could remain independent from a specific equation of state. M1.3–M1.6 deliberately avoided inventing water/steam properties.

M1.7 now needs a production closure capable of turning a fluid node's mass, volume and internal energy into pressure, temperature and phase for educational reactor simulation.

A complete IAPWS-IF97 implementation would add substantial scope and validation burden before the plant model exists. A magic linear placeholder would be architecturally cheap but physically misleading.

## Decision

Introduce `SimplifiedWaterSteamThermodynamicModel` as the first production implementation of `IFluidThermodynamicModel`.

The model:

- uses the IAPWS-IF97 Region-4 saturation-pressure relation as the saturation boundary reference;
- exposes explicit subcooled-liquid, saturated-mixture and superheated-vapor phases;
- exposes saturated-mixture vapor quality;
- derives state only from fixed geometry and conserved inventory, not from wall-clock time;
- uses deterministic fixed-iteration numerical searches;
- fails fast outside its declared state envelope;
- isolates all simplified correlations behind the existing thermodynamic abstraction.

The implementation is explicitly documented as an educational approximation, not a full IF97 steam-table implementation.

## Consequences

Positive:

- fluid nodes now have a real mass/volume/energy thermodynamic closure;
- boiling/condensation can emerge from state rather than scripted events;
- phase and vapor fraction become available for future reactivity/void feedback;
- all previous hydraulic/thermal components remain unchanged;
- a future IF97/high-fidelity backend can replace the model without changing plant topology or runtime contracts.

Negative/accepted limitations:

- property accuracy is intentionally lower than full IAPWS-IF97;
- supercritical states are outside the M1.7 model;
- detailed enthalpy, entropy, transport properties and two-phase flow correlations remain future work;
- phase-equilibrium solving adds deterministic numerical root searches to each closure evaluation.
