# ADR 0122 — The reference core will evolve to a reduced spatial 2D educational model

## Status

Accepted on 2026-08-16 as future product/architecture direction. No change to the current G.3 candidate.

## Context

The simulator already has aggregated-core zones, equivalent channel-group concepts and validated quasi-spatial feedback architecture, but the current reference plant presentation is still too aggregated to let the operator learn by seeing local power, void, temperature, xenon and rod effects across the core.

A full licensing-grade 3D neutron-transport model is outside the project's educational scope.

## Decision

1. The future reference core will use multiple 2D zones/equivalent channel groups with stable logical coordinates.
2. The UI will provide a 2D core map with selectable layers such as relative power, flow, void, fuel/coolant temperature, xenon and rod position/influence.
3. The operator will be able to select a zone/channel group and inspect local values and trends.
4. The reference plant will evolve from one representative rod toward multiple rods or rod groups with explicit zone mapping.
5. Spatial presentation must only expose fidelity that the underlying reduced model actually owns; it must not imply thousands of independently solved channels when none exist.
6. Domain/Simulation remain owners of spatial state. Avalonia only renders immutable presentation snapshots.

## Consequences

Future spatial work should extend the existing aggregated/quasi-spatial seams instead of replacing them. Tests must cover symmetry, mapping, deterministic spatial evolution and reduction to the global kinetics seam.
