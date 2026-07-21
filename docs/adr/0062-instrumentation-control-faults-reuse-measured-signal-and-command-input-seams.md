# ADR 0062 — Instrumentation/control faults reuse measured-signal and canonical command-input seams

**Status:** Accepted / M8.3 VALIDATED

## Context

M8.1 provides deterministic fault declaration/scheduling/lifecycle and M8.2 adds hydraulic component effects. M8.3 must make sensor and control failures observable without bypassing M5.1 measured-signal ownership, M5.2–M5.4 controller/actuator command ownership or M5.5 protection/interlock arbitration.

## Decision

1. Sensor faults bind through `IInstrumentationControlFaultTarget` and are converted into the already validated M5.1 `SensorFaultInput` modes. M8.3 does not create another instrumentation solver or expose true-state values to scenario code.
2. Active sensor faults replace only the matching per-step instrumentation input for a canonical channel. The M5.1 solver remains the sole owner of bias/freeze/failed-low/failed-high/unavailable output, validity and quality semantics.
3. Controller-output freeze/fail-low/fail-high is represented as a temporary bounded `ControllerInput` override in `Manual` mode for the targeted canonical controller. The existing controller and actuator solvers still translate that command into typed physical seams.
4. Freeze captures the committed controller output at the exact activation boundary. Fault clearance removes the override and resumes the latest persistent operator/controller input; it does not restore an old plant state.
5. Actuator-command freeze/fail-low/fail-high targets a canonical `ActuatorDefinition`, but is implemented through its command-side controller input because M5.2 actuators are pure typed command translators. This is allowed only when exactly one actuator is bound to that controller; shared-controller ambiguity fails closed rather than affecting unintended targets.
6. No M8.3 fault writes pump speed, valve position, rod position, reactivity, turbine output, electrical output or protection latch state directly.
7. Protection/interlock logic continues to consume the committed `MeasuredSignalFrame`. Invalid/faulted measurements therefore affect M5.5 according to its existing configured fail-safe policies and one-step committed-measurement ordering.
8. Active scenario sensor/control faults are deterministic runtime overlays. Persistent M5 inputs remain the baseline, so operator changes made while a fault is active remain available when the fault clears.
9. Target IDs, conflicts and numeric parameters are validated fail closed; no randomness or wall-clock timing is introduced.

## Consequences

Instrumentation and control failures become scenario-addressable while all signal processing, controller dynamics, actuator translation and protection decisions remain with their validated owners. M8.3 can demonstrate misleading/invalid measurements and failed command paths without creating a hidden true-state bypass or a second control system. Later transient packs can combine M8.2 and M8.3 faults through the same M8.1 lifecycle.
