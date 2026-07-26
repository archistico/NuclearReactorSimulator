# ADR 0117 — Turbine bypass is an internal header-to-condenser transfer

## Status

Accepted for M10.9.4.1-F.3 candidate.

## Context

F.1 provides a validated compressible steam-flow capacity law and F.2 applies it to an atmospheric external relief boundary. A turbine bypass has different conservation and backpressure semantics: discharged steam remains inside the plant and enters the condenser steam-space inventory.

## Decision

The turbine bypass is owned by `CondenserSystemDefinition` and resolved by `TurbineBypassSolver` from committed state. Its destination is the steam-space node of an explicitly identified condenser, and capacity is calculated against that node's committed pressure.

The bypass stages equal and opposite header/destination mass and internal-energy source terms. External mass and power are exactly zero. It is current-v2 opt-in, one-way, stateless and automatic in F.3.

The F.2 atmospheric relief remains an independent main-steam external boundary. Phase G remains the sole owner of any migration from specific-internal-energy transport to an explicit enthalpy/flow-work convention.

## Consequences

- condenser backpressure affects bypass capacity without a fixed receiver constant;
- no synthetic receiver inventory or second integration pass is needed;
- legacy definitions remain unchanged through an optional empty collection;
- bypass inflow and committed-state condensation are explicitly sequenced across logical steps;
- manual controls, actuator state and wet-steam correlations remain deferred.
