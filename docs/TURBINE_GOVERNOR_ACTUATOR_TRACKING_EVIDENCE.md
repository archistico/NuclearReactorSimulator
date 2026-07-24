# M10.9.4.1-D.3 — Turbine Governor / Actuator Tracking Evidence

## Purpose

D.3 decides whether the current-v2 turbine speed loop actually needs actuator-position tracking anti-windup. The controller already prevents integral windup when its own output saturates at 0% or 100%, but the physical control-valve actuator is rate limited to 0.5 fraction/s, equivalent to 2 seconds for full travel. The controller does not currently use committed valve position as an anti-windup tracking signal.

That is a structural possibility of windup, not proof of a material operational problem. D.3 therefore changes no production law.

## Deterministic stimulus

From the bumpless current-v2 pre-synchronization seed, with the generator breaker open and the control valve near 28%:

1. enter `RUN` without an unloaded settling interval;
2. issue five accepted `SPEED RAISE` commands (+50 rpm reference total);
3. observe 3 simulated seconds;
4. issue five accepted `SPEED LOWER` commands to restore the original reference;
5. observe 4 simulated seconds.

Advancing the breaker-open seed for 5 seconds before the stimulus is an unloaded coast-down, not settling: it closes
both controller output and valve to 0%, leaving no actuator lag for the audit to measure.

The audit records every canonical 10 ms simulation step. A 0.5 fraction/s actuator moves 5 percentage points
in 100 ms, so 100 ms sampling can skip the complete material-lag window used by the decision gate.

- speed-controller output;
- committed control-valve position;
- absolute command/position gap;
- controller integral term;
- speed error.

## Decision gates

A valid D.3 stimulus must create at least **5 percentage points** of controller-command versus committed-valve-position separation. Otherwise the rate-limit condition has not been exercised.

Tracking anti-windup is considered materially justified only if the controller integral moves by **2 or more controller-output percentage points** while that >=5-point lag exists.

- **below 2 points:** close D.3 evidence-only; do not add tracking complexity;
- **2 points or more:** the explicit gate fails and D.3.1 may introduce actuator-position tracking/back-calculation with dedicated regression tests.

The threshold is deliberately a decision threshold for this simulator, not a universal turbine-control standard. It must not be loosened merely to make the test pass.

## Non-goals

D.3 does not change:

- PID gains;
- governor droop/statism;
- actuator travel rate;
- valve or stage hydraulic resistance;
- turbine admission phase policy;
- generator/grid coupling;
- protection thresholds;
- timestep or replay behavior.

## Runner

```text
scripts\run-turbine-governor-actuator-tracking-audit.cmd
```
