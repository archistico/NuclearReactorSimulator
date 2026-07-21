# First Criticality & Low-Power Operation

## Scope

M7.3 adds the first operator-controlled criticality progression on top of the validated M7.1 scenario/session framework and the validated M7.2 cold-shutdown/pre-start construction recipe.

The milestone deliberately ends at a stable educational low-power reactor condition. It does **not** open the steam path, raise steam for turbine operation, accelerate the turbine, synchronize the generator or take electrical load. Those transitions remain M7.4 and M7.5 ownership.

## Exact initial condition

M7.3 introduces the exact-version initial condition:

```text
pre-criticality-source-range / version 1
```

`FirstCriticalityInitialConditionFactory` reuses the canonical M7.2 construction path rather than duplicating the M1–M5 plant/control object graph. Relative to the M7.2 cold-shutdown recipe, the M7.3 handoff starts with:

- main circulation already established;
- rods fully inserted;
- steam stop/control/admission path closed;
- turbine stationary;
- generator breaker open;
- protection clear and instrumentation healthy;
- a tiny deterministic non-zero point-kinetics neutron population.

### Why a non-zero source-range seed is required

The validated M2 point-kinetics equations are homogeneous and currently contain no explicit external neutron-source term. An exact zero neutron population therefore remains exactly zero regardless of later rod withdrawal.

M7.3 does **not** add a hidden source solver or a second kinetics owner. Instead, the initial condition supplies a small deterministic non-zero source-range seed (`1e-8` relative neutron population) so the existing M2 kinetics can exhibit subcritical decay, approach to zero reactivity and subsequent power growth under operator-commanded rod motion.

This seed is an initial-condition modeling device, not a claim that an external startup neutron-source model has been implemented.

## Operator command boundary

M7.3 permits only the command kinds required for first-criticality training and safety:

- control-rod INSERT / HOLD / WITHDRAW;
- reactor SCRAM and protection reset;
- main-circulation pump START / STOP;
- turbine trip, generator trip and generator-breaker OPEN;
- alarm acknowledge/reset actions.

The scenario continues to reject fail-closed:

- generator-breaker CLOSE;
- turbine speed raise/lower;
- generator load raise/lower.

Rod commands still cross the existing Application command boundary and are resolved by the validated M5.3 actuator/controller seam. Scenario code never writes rod position, reactivity, neutron population or thermal power directly.

## Declarative operating sequence

The M7.3 guidance is observational and declarative:

1. verify the pre-criticality handoff;
2. withdraw the canonical rod/group target in controlled increments;
3. use HOLD as modeled total reactivity approaches zero;
4. establish first criticality with non-zero neutron population and approximately zero modeled reactivity;
5. use small INSERT/HOLD/WITHDRAW corrections to enter the educational low-power band;
6. return reactivity near zero and verify a long or effectively infinite reactor period;
7. hand off to M7.4 with main circulation established, steam admission isolated and generator disconnected.

No guidance step auto-dispatches a command and no check forces a physical state to pass.

## Observational criteria

`FirstCriticalityChecklistEvaluator` reads only immutable `ControlRoomSnapshot` presentation state.

The current educational criteria include:

- all published measured channels valid;
- no active reactor/turbine/generator trip;
- main circulation established;
- steam admission path closed;
- generator breaker open;
- rod withdrawal not inhibited by protection;
- non-zero source-range reactor power before the approach;
- near-critical negative-reactivity approach window;
- first criticality at approximately zero modeled total reactivity with non-zero power;
- educational low-power band of `0.01–5 MWth`;
- low-power stabilization with approximately zero reactivity and a reactor period whose magnitude is at least 20 s, or is undefined because the model is at effectively infinite-period critical equilibrium.

These are training/evaluation thresholds only. They do not change M2 physics, M5 control behavior or protection outcomes.

## Xenon boundary

M2.8 iodine/xenon physics remains validated, but quantitative xenon state is still not part of the M5.7 integrated automatic-operation state/snapshot envelope. M6 correctly presents xenon reactivity as `Unavailable` rather than reconstructing it from unrelated values.

M7.3 therefore includes an explicit xenon-boundary training objective: the operator must recognize that quantitative xenon reactivity is unavailable at this operational boundary. M7.3 does not fabricate a xenon value or create a scenario-local xenon integrator.

A later milestone may promote canonical iodine/xenon state through the automatic-operation envelope; until then, quantitative xenon manoeuvring must not be inferred from UI or scenario logic.

## Determinism and ownership

- logical fixed-step simulation time remains authoritative;
- the source-range seed is versioned initial-condition data;
- rod movement remains owned by M2/M5.3;
- reactivity and point kinetics remain owned by M2;
- primary circulation remains owned by M3 with M5.3 command arbitration;
- protection remains M5.5 authoritative;
- scenario guidance/evaluation remains observational only;
- Avalonia consumes presentation snapshots and emits typed intents only.

## M7.4 handoff

M7.3 completes when the educational reactor is stable at low power with:

- main circulation established;
- protection clear;
- steam admission path still isolated;
- generator breaker open;
- no turbine startup or electrical loading attempted.

M7.4 owns heat-up, steam raising and turbine startup.
