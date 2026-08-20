# Grid Synchronization & Load Increase — M7.5

M7.5 continues from the validated M7.4 turbine-startup handoff and owns the first deliberate connection of the modeled generator to the infinite-bus grid plus initial low-load pickup.

## Exact initial conditions

`pre-synchronization-grid-loading@1` is the historical M7.5 identity and reuses the canonical M7.2 object-graph construction path. It seeds the already-rolled turbine at 3000 rpm with generator/grid electrical phase initially matched, breaker open, zero requested electrical power, low reactor power and main circulation established.

`@2` is the later generation-ready explicit profile. It remains loadable as an exact compatibility/reference identity and is never reinterpreted. I.5 long-running evidence showed that its `ExplicitCommittedState` hydraulics do not reliably sustain the modern 60 s low-load journey.

`@3` is the supported sustained-synchronization profile. It preserves the complete @2 physical seed, steam-path coefficients, PI governor, grid coupling, protections and 10 ms step, changing only hydraulic numerical ownership to `FourNodeBranchContinuityCorrectedCommitOptIn`. The qualified journey treats 10 s after load pickup as bounded stabilization and requires strict sustained operation from 20–60 s.

None of these recipes closes the breaker or fabricates a synchronized snapshot. The first deterministic seed step is still solved through M1–M5 owners; the published M4.5 `SynchronizationConditionsSatisfied` result remains authoritative.

## Synchronization ownership

The scenario may permit `GeneratorBreakerClose`, but permission is not a permissive. The command is a one-step request and M4.5 alone evaluates frequency difference, phase difference and terminal/grid voltage difference. A close request outside the canonical window is rejected by the existing generator solver.

The desktop continues to fail closed by disabling CLOSE BREAKER when the published synchronization permissive is false.

## Initial loading

M7.5 completes the previously dormant generator-load UI intent. `GeneratorLoadRaise/Lower` changes only the canonical M4.5 `RequestedElectricalPower` input in bounded 5 MWe increments. It does not write rotor torque, electrical output or turbine power directly.

After breaker closure M4.5 converts the requested electrical power to electromagnetic rotor loading through the validated generator efficiency/audit path. M5.4 turbine-speed governing remains responsible for normal admission response to rotor-speed error. Reactor power remains controlled through the validated M2/M5.3 rod-reactivity-kinetics chain.

This separation gives the operator an explicit coordination task: raise reactor thermal power deliberately while taking electrical load and keep the turbine close to synchronous speed.

## Guidance boundary

The ordered guidance covers:

1. pre-synchronization handoff verification;
2. synchronization-window verification and fine speed trim;
3. deliberate breaker closure;
4. unloaded parallel verification;
5. first electrical-load pickup;
6. reactor/turbine/electrical power coordination;
7. stable low-load handoff to M7.6.

All checks are observational over `ControlRoomSnapshot`. They never change plant state, clear trips, force synchronization or modify load.

M7.6 owns broader power manoeuvring, xenon/temperature/void response during load changes, controlled normal shutdown and post-shutdown cooling.
