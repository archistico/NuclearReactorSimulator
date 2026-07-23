# ADR 0075 — Operator HMI separates range semantics and performance time is logical

**Status:** Accepted  
**Date:** 2026-07-23

## Context

After validation of M10.8, manual use showed that the simulator exposed many technically correct values and controls but did not consistently communicate whether a value was operationally acceptable, how close it was to a warning/protection boundary, how connected equipment influenced it, or what downstream effects should be monitored after a command.

The approved M10.9 operator-experience refactor also adds future performance-oriented training: timed operating objectives and deterministic tracking of external electrical demand.

Without explicit architecture rules, the UI could accidentally:

- treat instrument minimum/maximum as safe operating limits;
- hardcode warning/trip thresholds in Avalonia;
- present expected dependency chains as proven causal outcomes;
- create a second predictive physics model in the UI;
- use wall-clock time for scoring, making results dependent on pause/UI cadence/hardware;
- conflate training assistance with plant-control authority.

## Decision

### 1. HMI range semantics are explicit and separate

Presentation contracts distinguish:

- instrument display scale;
- normal/warning/alarm operating bands;
- scenario/controller target band;
- setpoint;
- protection thresholds.

Avalonia renders these semantics but does not invent them or recompute protection logic.

### 2. The plant is the primary visual mental model

The long-term HMI centers whole-plant and subsystem engineering schematics with explicit equipment inputs/outputs and connected process/signal paths. The validated M10.8 operator computer remains a utility workstation for detailed guidance, diagnostics, commands, modes, logs and sessions.

### 3. Expected command influence and observed response remain distinct

A command-consequence view may explain direct effect, expected downstream dependencies, permissives/blockers and what to monitor. It must not claim those relationships are observed causal proof.

Post-command changes are shown separately as observed response evidence. No UI-side predictive physics solver is introduced.

### 4. Performance-oriented training uses logical simulation time

Future timed objectives and energy-demand tracking use deterministic logical simulation time. Wall-clock elapsed time, rendering cadence and hardware speed do not affect authoritative scores.

Practice/debug operations such as pause/step must be explicitly handled by challenge rules so they cannot create scoring exploits.

### 5. Safety dominates scoring

Future scoring may include objective time, grid-demand tracking, stability and safety/procedure quality, but unsafe operation must never be rewarded merely because it is fast or satisfies electrical demand.

Training assistance remains independent from Manual / Assisted / Supervisory Automatic plant-control authority.

## Consequences

- M10.9.1 introduces immutable scale/range metadata before advanced gauges are implemented.
- M10.9.2 gauges consume canonical metadata rather than embedded thresholds.
- M10.9.3/M10.9.4 schematics become primary situation-awareness surfaces.
- M10.9.5 must separate expected influence from observed response.
- M10.9.6/M10.9.7 challenge/scoring logic must remain deterministic and logical-time-based.
- Existing M2/M3/M4/M5/M7/M9 ownership is unchanged.
