# ADR 0087 — Current-v2 secondary protections are measured, latching and operationally supervised

## Status

Accepted for M10.9.4 Hotfix 20 candidate.

## Context

The reference protection system historically contained only a very-high steam-drum pressure reactor scram. Consequently `AnyTripActive == false` was too weak a health criterion for turbine/generator/condenser operation. The simulation already owns measured M5.1 channels, latching M5.5 protection functions and turbine/generator trip arbitration.

## Decision

The current-v2 sustained-generation and synchronization profiles opt into three additional measured latching protections:

- turbine overspeed: high at 3300 rpm, reset-safe at 3150 rpm, turbine + generator trip;
- condenser high backpressure: high at 30 kPa absolute, reset-safe at 20 kPa, turbine + generator trip;
- generator overfrequency: high at 53 Hz, reset-safe at 51.5 Hz, generator trip.

Legacy/default profiles retain the historical minimal protection definition. Protection consumes measured instrumentation only and remains owned by M5.5; UI/HMI code receives presentation snapshots only.

Generator underfrequency is intentionally not added yet because the current protection primitive cannot supervise the function with generator-breaker/load state. A disconnected machine must not latch underfrequency merely because it is not synchronized. Underfrequency will be added only with explicit operational supervision.

## Consequences

`AnyTripActive` becomes materially more informative in current-v2 operation. Overspeed and condenser backpressure fail closed through turbine/generator trip arbitration, and overfrequency opens the generator path. Actuator travel rates and governor/load-control mode changes remain separate follow-on work.
