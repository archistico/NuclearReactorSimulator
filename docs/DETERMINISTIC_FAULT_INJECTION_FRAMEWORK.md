# Deterministic Fault-Injection Framework

M8.1 introduces the scenario-level orchestration boundary for faults. It deliberately does **not** implement hydraulic, instrumentation, control, turbine or electrical failure physics itself.

## Scenario fault declaration

Each `ScenarioFaultDefinition` contains:

- `FaultId` — stable unique identity inside the scenario;
- `FaultTypeId` — exact handler key owned by a later typed fault family;
- `TargetId` — canonical target identifier interpreted by that typed applicator;
- deterministic string `Parameters`;
- one activation trigger;
- optional deactivation trigger.

Faults are persisted as part of scenario schema v2. Legacy v0/v1 scenarios migrate with an empty fault set.

## Trigger semantics

Two trigger forms exist:

1. **Logical step** — fires at the exact committed logical-step boundary. The transition is applied before that physical step executes.
2. **Plant condition** — references a named `IScenarioFaultConditionEvaluator` and evaluates only the committed `ControlRoomSnapshot` at a step boundary.

No trigger uses wall clock, timers, random values, UI refresh cadence or candidate true state.

## Runtime composition

`ScenarioSessionFactory` resolves the exact initial-condition version first. If the scenario declares faults, it then:

1. binds every distinct `FaultTypeId` to an explicit runtime-bound `IScenarioFaultApplicator`;
2. validates every named condition evaluator;
3. wraps the canonical runtime in `ScenarioFaultRuntimeEngine`;
4. starts the normal `ControlRoomRuntimeCoordinator` in `Paused` state.

Missing handlers fail loading closed.

The wrapper owns only scheduling and lifecycle. A typed applicator may modify only the validated input/command seam belonging to its subsystem milestone. It must never create a parallel integrator or write derived physical outputs directly.

## Lifecycle and snapshots

A declared fault follows one deterministic lifecycle:

```text
Pending → Active → Cleared
```

The control-room snapshot exposes for every declared fault:

- identity/type/target;
- lifecycle;
- activation logical step;
- clear logical step;
- last monotonic transition sequence.

This state is observational/presentation-safe. It does not replace authoritative physical state.

## Replay

M7.1 replay remains the only command replay boundary. Reloading the same exact scenario reconstructs its fault schedule, while the existing logical-step operator command trace reconstructs operator actions. Therefore the same:

```text
initial-condition id/version
+ scenario fault declarations
+ registered deterministic condition/applicator semantics
+ operator command trace
+ final logical step
```

produces the same fault lifecycle history/state without a separate wall-clock fault script.

## Ownership boundary for later M8 milestones

- M8.2: pump, valve, restriction/blockage and selected hydraulic/leak applicators;
- M8.3: sensor and control/actuator diagnostic applicators over canonical M5.1–M5.5 seams (current candidate);
- M8.4: turbine/generator/feedwater/condenser transient scenarios;
- M8.5/M8.6: larger educational leak/LOCA and electrical-loss/SBO-class scenarios.

All concrete effects must compose through existing canonical M1–M5 owners.
