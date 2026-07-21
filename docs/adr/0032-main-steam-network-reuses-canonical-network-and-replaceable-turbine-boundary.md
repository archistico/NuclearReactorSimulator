# ADR 0032 — Main steam transport reuses the canonical plant network and terminates at a replaceable turbine boundary

## Status

Accepted for M4.1 baseline candidate.

## Context

M3.7 introduced a temporary external steam-export sink at each steam drum so the primary circuit could operate before the turbine island existed. M3.8 then established one integrated primary-circuit step and exactly one conserved-inventory integration boundary.

M4.1 must add main-steam lines, headers and admission valves without duplicating hydraulic state, exporting steam twice, or creating a second integrator.

## Decision

1. M4.1 main-steam lines and stop/control/admission valves are existing canonical `PlantDefinition` pipes/valves.
2. `MainSteamNetworkDefinition` is a semantic composition/validation layer over that topology.
3. Every M3 steam-export seam maps to exactly one main-steam line.
4. While M4.1 owns downstream steam transport, legacy M3 steam-export input is required to be zero.
5. M4.1 terminates at a temporary `TurbineAdmissionBoundaryDefinition` sourcing a canonical turbine-inlet fluid node.
6. The terminal boundary contributes signed external mass/energy source terms using committed-state specific internal energy.
7. `IntegratedPrimaryCircuitSolver` exposes a backward-compatible higher-phase supplemental-source-term overload so M4.1 contributions are combined before the same single `PlantNetworkOrchestrator` integration.
8. M4.2 replaces the temporary terminal sink with turbine expansion without changing upstream topology.

## Consequences

- no parallel steam hydraulic graph;
- no double integration of line/valve transport;
- no double steam removal at the M3/M4 seam;
- valve fail-safe and characteristic behavior continue to use validated M1 primitives;
- mass/energy exchange at the temporary turbine boundary remains explicit in global audits;
- M4.2 has a stable canonical inlet seam for expansion and shaft-power modeling.
