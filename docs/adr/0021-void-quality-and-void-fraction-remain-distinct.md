# ADR 0021 — Vapor quality and void fraction remain distinct

## Status

Accepted for M2.7.

## Context

M1.7 exposes saturated-mixture `VaporQuality`, which is a mass fraction. Neutronic void feedback is driven by the volumetric displacement of liquid/moderator by vapor, so directly reusing quality as void fraction would mix different physical dimensions and can materially distort feedback.

## Decision

1. Introduce a strongly validated `VoidFraction` value type distinct from `VaporQuality`.
2. Introduce a signed `VoidFractionDifference` for reference-relative feedback arithmetic.
3. Resolve water/steam void fraction in a dedicated `WaterSteamVoidFractionSolver`.
4. For saturated mixtures, use the homogeneous-equilibrium volume relation based on vapor quality and the M1.7 saturation liquid/vapor densities.
5. Map subcooled liquid to zero void and superheated vapor to full void; unspecified phase fails fast.
6. Keep the neutronic mapping separate in `VoidFeedbackSolver` using a configurable signed `VoidReactivityCoefficient` and explicit reference void fraction.
7. Evaluate only committed thermodynamic state at each fixed-step boundary, preserving the explicit one-step multiphysics staging established by M2.6.
8. Hardcode neither the sign nor magnitude of plant-specific void feedback in the generic engine.

## Consequences

- Thermohydraulic and neutron-physics meanings stay explicit.
- A future higher-fidelity void/slip model can replace the void resolver without changing reactivity composition.
- A future nonlinear/tabulated RBMK-specific void-worth model can replace the linear feedback law without changing fluid closure.
- No hidden same-step algebraic iteration is introduced.
