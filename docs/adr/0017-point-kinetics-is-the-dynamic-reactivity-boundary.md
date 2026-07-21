# ADR 0017 — Point kinetics is the dynamic reactivity boundary

## Status

Accepted for M2.3 baseline candidate.

## Context

M2.1 established compositional reactivity and M2.2 established control-rod mechanics/worth. The simulator now requires a deterministic dynamic model that converts total reactivity into neutron-population evolution without prematurely coupling neutron state to thermal power or embedding RBMK-specific constants in the generic engine.

## Decision

Implement generic point-reactor kinetics behind an injected `PointKineticsParameters` set containing prompt-neutron generation time and an arbitrary canonical collection of delayed-neutron groups.

State consists of normalized neutron population plus one precursor population per delayed group. Critical-equilibrium initialization is explicit.

Integrate the standard point-kinetics ODE system with deterministic fixed-count RK4 substepping inside each already-fixed simulation timestep. The internal substep count is derived only from physical coefficients and the requested timestep and is bounded fail-fast.

Expose prompt-critical margin, dollars/cents relative to the active effective delayed-neutron fraction and signed instantaneous reactor period as diagnostics only.

Do not convert neutron population into fission thermal power in this milestone. Do not hardcode plant-specific kinetic constants.

## Consequences

- M2.1 reactivity becomes a true dynamic input rather than a diagnostic-only value.
- Delayed-neutron dynamics are explicit and independently inspectable.
- External UI pulse segmentation cannot change kinetics results.
- Plant configurations can supply different kinetic parameter sets without changing solver code.
- M2.4 can consume normalized neutron population through an explicit thermal-power coupling seam.
- Higher-fidelity spatial kinetics can later replace or complement point kinetics without contaminating the control-rod or reactivity models.
