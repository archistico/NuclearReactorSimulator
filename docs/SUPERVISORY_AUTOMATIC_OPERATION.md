# Supervisory Automatic Operation

M10.6 adds a deterministic supervisory layer inside the canonical M5 control domain.

## Ownership

```text
Operator objective
        ↓
Application typed authority/objective seam
        ↓
M5 SupervisoryOperationCoordinator
        ↓
existing ControllerMode + controller setpoints
        ↓
existing M5.3 / M5.4 local controllers
        ↓
canonical actuators
        ↓
M2 / M3 / M4 physics
```

The supervisor never writes a physical outcome directly.

## First bounded objectives

- Hold Reactor Power
- Hold Turbine Speed
- Hold Current Operating Point (reactor power + turbine speed captured from valid measured signals)

The terminal directly exposes the third objective because it requires no free-form numeric parser and gives a deterministic, testable first supervisory workflow. Explicit power/speed objective APIs exist for structured future UI/orchestration.

## Measurement discipline

A required control measurement must be present, finite and `SignalValidity.Valid`. If it is not, requested Supervisory authority remains visible but effective authority degrades to Assisted with a reason. The supervisor never reads true reactor/plant state as a fallback for a missing measured signal.

## Protection priority

Canonical SCRAM/turbine-trip/generator-trip state suspends supervisory decisions. The supervisor does not clear protection, acknowledge alarms, force breakers or override interlocks.

## Bumpless manual takeover

When Manual is requested, each local controller is switched to manual with its committed `LastOutput` used as the manual output. This prevents the authority transition itself from introducing an artificial actuator jump.

## Deterministic replay

Accepted authority/objective intents are recorded separately from plant commands in `ScenarioAutomationIntentJournal`. M9.1 replay applies them before the next fixed step, reconstructing the same supervisory state while leaving snapshot fingerprint schema v1 unchanged.
