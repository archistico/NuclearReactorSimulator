# ADR 0090 — Current-v2 turbine work is pressure-, temperature- and vapor-dependent

## Status

Accepted for M10.9.4 Hotfix 23 candidate.

## Context

The historical M4.2 turbine model converts every kilogram of admitted steam into the same nominal work before efficiency, regardless of inlet pressure/temperature, exhaust backpressure or vapor content. That seam was sufficient for the first deterministic rotor/conservation milestone, but it is not a stable current operating model: a liquid inlet or a nearly equalized inlet/exhaust pressure can still command nominal torque, and an energetically weak inlet reaches an exception rather than a bounded degraded state.

The pressure-driven mass-flow correction, condenser `UA·ΔT` feedback and generator/grid stiffness are now validated. Turbine work can therefore depend on the same committed thermodynamic state without using seed tuning to compensate for missing feedback.

## Decision

Add optional `TurbineThermodynamicWorkDefinition` to each `TurbineStageGroupDefinition`.

- `null` preserves the historical fixed-`NominalSpecificWork` law for legacy/versioned compatibility.
- current-v2 uses an educational vapor-expansion estimate based on committed inlet absolute temperature, inlet/exhaust pressure ratio, heat-capacity ratio and committed inlet vapor mass fraction;
- the resulting ideal work is bounded by the stage `NominalSpecificWork` design cap;
- it is also bounded to 80% of committed inlet specific internal energy before turbine efficiency is applied;
- liquid admission, non-positive expansion pressure difference or absent vapor content therefore yields zero shaft work rather than nominal torque;
- low-energy states degrade through an explicit bounded work diagnostic instead of reaching negative exhaust energy in normal current-v2 operation.

The current-v2 educational constants are:

```text
vapor cp              = 2.1 kJ/(kg·K)
heat-capacity ratio γ = 1.3
maximum ideal extraction from inlet internal energy = 0.8
```

This is not a complete isentropic steam-table implementation. It is an explicit replaceable current-model closure that restores the essential pressure/backpressure/phase response while preserving deterministic single-owner energy accounting.

## Consequences

- At the validated sustained-generation point, pressure/temperature availability exceeds the 500 kJ/kg stage design cap, so the existing approximately 400 kJ/kg post-efficiency operating point remains materially unchanged.
- As exhaust backpressure approaches inlet pressure, available work collapses continuously.
- Vapor quality directly reduces available work; a liquid inlet produces no turbine shaft power.
- `TurbineStageGroupSnapshot` and the Application presentation expose available/extracted specific work plus a limitation flag for diagnostics and long-running evidence.
- Legacy replay fixtures remain fixed-work because `ThermodynamicWork` is null.
- Detailed enthalpy/entropy tables, wetness losses and multi-stage maps remain future replaceable fidelity work.
