# ADR 0088 — Current-v2 secondary actuators have explicit travel/ramp dynamics

## Status

Accepted for M10.9.4 Hotfix 21 candidate validation.

## Context

The M5.2 controller/actuator layer produces typed valve-position and pump-speed commands, but M5.4 historically copied those requested values directly into canonical `PlantState` on the next fixed step. A valve could therefore move from fully closed to fully open, or a pump from zero to rated speed, in one 10 ms step.

That behavior makes closed-loop stability depend on unrealistically instantaneous final control elements and hides a real distinction between commanded target and physical actuator state.

Protection and fault authority are separate concerns. A turbine-trip stop-valve override must not be weakened merely because normal governor/feedwater actuators acquire travel limits, and hydraulic fault overrides remain owned by the existing fault seam.

## Decision

1. `ActuatorDefinition` may optionally publish an `ActuatorTravelRate`, expressed as normalized full-scale fraction per second.
2. `TravelRate = null` preserves historical instantaneous command application for legacy/versioned definitions.
3. For rate-limited M5.4 valve/pump targets, the controller output and typed actuator command remain immediate and observable, but canonical physical position/speed moves from the committed state toward the request by at most `rate * deltaTime` per committed step.
4. Current-v2 secondary bindings use:
   - control/admission valves: `0.5 fraction/s` (2 s full stroke);
   - condensate/feedwater pumps: `0.25 fraction/s` (4 s full ramp).
5. A pump commanded to zero ramps canonical speed down before `IsRunning` becomes false; a positive request starts at the first finite ramped speed. No second private coast-down state is introduced.
6. Turbine-trip stop-valve closure remains a higher-authority protection override and is not rate-limited by normal M5.4 actuator dynamics.
7. Hydraulic fault overrides remain separate and are not reinterpreted as normal travel dynamics.

## Consequences

- Normal current-v2 control actions can no longer create full-scale physical actuator jumps in one fixed timestep.
- Commanded target and physical position/speed are intentionally distinct during transients.
- Legacy replay/model identities can retain instantaneous behavior through null travel rates without constraining current-v2 correctness.
- The next structural step can address governor/load-control mode semantics on top of finite final-control-element dynamics rather than compensating for instantaneous actuators.

## Validation

Hotfix 21 requires:

- domain tests for typed travel-rate validation and null legacy behavior;
- M5.4 tests proving bounded per-step valve/pump movement and pump coast-down semantics;
- versioned seed tests proving v1 null rates and current-v2 explicit rates;
- the complete ordinary suite;
- both explicit 60-second gameplay journeys.
