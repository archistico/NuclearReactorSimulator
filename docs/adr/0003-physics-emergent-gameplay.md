# ADR 0003 — Physics-emergent gameplay

- Status: Accepted
- Date: 2026-07-20

## Context

The project is intended to teach system behaviour. Arbitrary scripted failure outcomes would make operator actions appear meaningful without representing plant causality.

## Decision

Operational consequences must arise from simulated state, physical feedback, automatic controls, protection logic and operator actions. Scenario scripting may establish initial conditions, faults and objectives, but must not replace the physical causal chain.

## Consequences

- incidents must be explainable from recorded plant state;
- scenario logic remains separate from physical model logic;
- post-incident analysis can identify actual causal sequences.
