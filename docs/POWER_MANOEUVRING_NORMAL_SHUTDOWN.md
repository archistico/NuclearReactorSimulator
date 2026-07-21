# Power Manoeuvring & Normal Shutdown — M7.6

M7.6 continues from the validated M7.5 low-load parallel handoff and exercises bounded on-grid load changes followed by a controlled normal shutdown.

## Exact initial condition

`stable-low-load-parallel-operation` version 1 reuses the canonical M7.2 construction path with:

- warm critical reactor state and main circulation established;
- turbine near 3000 rpm;
- generator breaker already closed as committed M4.5 state;
- canonical requested electrical load seeded at 5 MWe;
- no active trip.

This is initial-condition data, not a scenario-side physics shortcut. The first deterministic seed still passes through the validated M1–M5 solver composition.

## Power manoeuvring ownership

Electrical load changes use `GeneratorLoadRaise/Lower` in the existing bounded 5 MWe increments. Those commands modify only M4.5 `RequestedElectricalPower`.

The physical response remains split across established owners:

- M4.5 owns generator loading, electromagnetic torque and electrical output;
- M5.4 owns turbine speed governing through canonical steam admission;
- M5.3/M2 own rod motion, reactivity, point kinetics and fission power;
- M3/M4 continue to own thermofluid inventories and energy transfer.

The scenario never sets MW, RPM, torque, valve position or reactivity directly.

## Temperature, void and xenon

M7.6 observes core-zone fuel/coolant temperature and void diagnostics already published in `ControlRoomSnapshot`. These checks are observational only.

Quantitative xenon reactivity is deliberately not reconstructed: M2.8 xenon physics is validated, but the M5.7 operational envelope still does not publish that state. `XenonReactivity` therefore remains explicitly `Unavailable` in M7.6 guidance.

## Normal shutdown sequence

The ordered guidance covers:

1. verify the stable M7.5 low-load parallel handoff;
2. raise electrical load deliberately and stabilize;
3. observe temperature/void response and explicit xenon boundary;
4. reduce load and reactor power deliberately;
5. unload the generator to approximately zero;
6. open the generator breaker through the canonical M4.5 command seam;
7. continue controlled rod insertion until shutdown conditions are established;
8. maintain main circulation for post-shutdown cooling while turbine speed is reduced through the validated governing seam.

SCRAM and turbine/generator trips remain available protective actions, but routine shutdown guidance does not use them as substitutes for controlled operation.

## Boundary to M7.7

M7.6 provides the physical/procedural operating path only. Formal scenario scoring, training-objective evaluation and richer procedure-assistance semantics belong to M7.7.
