# ADR 0114 — Evidence-derived electrical protection uses supervised delayed M5.5 functions

## Status

Accepted and validated in M10.9.4.1-E.3.2 Hotfix 3 on 2026-07-26

**Date:** 2026-07-26

## Context

E.2 validated signed bidirectional generator/grid exchange. E.3.1 then recorded normal load manoeuvring, sustained reverse power after prime-mover loss, breaker-open coastdown and phase-offset trajectories. The evidence separates persistent hazardous trajectories from normal current-v2 transients, but the original M5.5 trip function had neither pickup delay nor measured eligibility supervision.

Implementing the new protections in Application or directly in the electrical solver would duplicate protection ownership and weaken replay determinism.

## Decision

Extend the canonical M5.5 protection function with two optional, backward-compatible contracts:

- a committed logical-time pickup delay;
- one measured supervision condition.

Zero delay and no supervision remain the defaults. Both current-v2 sustained profiles opt into three generator-trip functions:

- reverse power: -0.30 MWe, reset -0.10 MWe, 2.0 s delay;
- underfrequency: 48.8 Hz, reset 49.5 Hz, 1.0 s delay;
- loss of synchronism: 1.5 Hz absolute slip, reset 0.5 Hz, 0.5 s delay.

All three require a measured closed generator breaker. Supervision becoming inactive clears an incomplete pickup timer and makes the function reset-safe. A completed trip remains latched until the existing canonical reset is accepted.

## Consequences

- Historical protection definitions remain immediate and unsupervised.
- The disconnected coastdown cannot create an underfrequency or slip trip.
- The normal one-sample negative-power excursion cannot create a reverse-power trip.
- Pickup timing is deterministic simulation time, not wall-clock time.
- Generator trip continues to open the canonical breaker through the existing protection arbitration path.
- HMI frequency and signed-power scales can publish the new thresholds without owning them.
- Raw wrapped phase angle is not used as the loss-of-synchronism discriminator in the current reduced-order model.
