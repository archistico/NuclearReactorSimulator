# New Chat Start — Nuclear Reactor Simulator

Use this document when restarting development in a new ChatGPT conversation.

## Copy/paste bootstrap

```text
We are continuing the Nuclear Reactor Simulator project.

Treat docs/PROJECT_HANDOFF.md as the authoritative continuity document. Before changing code, read in order:
1. docs/PROJECT_HANDOFF.md
2. docs/PROJECT_STATUS.md
3. docs/ROADMAP.md
4. docs/ARCHITECTURE.md
5. the current milestone file and linked ADR/domain documentation.

Preserve all non-negotiable ownership and determinism rules from the handoff. Do not recreate physics/state owners in UI, control, protection, scenario or diagnostic layers. Compose through existing validated seams.

Current recorded checkpoint:
- last explicitly locally validated baseline: M7.5 — Grid Synchronization & Load Increase;
- current implementation candidate: M7.6 — Power Manoeuvring & Normal Shutdown;
- M7.6 must not be marked validated until I explicitly confirm local build and complete tests pass;
- after M7.6 validation, continue with M7.7 — Training Objectives, Procedure Guidance & Evaluation.

Use the latest complete source ZIP/tree I provide as the working package. If its source/doc status conflicts with this checkpoint, stop advancement, reconcile the discrepancy, and keep the last explicitly validated milestone as the baseline.

For every milestone: make the smallest architecture-consistent change, add/update tests and docs, deliver a complete ZIP, and wait for my local build/test validation before marking it validated.
```

## What to upload or make available

Prefer the latest **complete** project ZIP, not a partial patch. For the checkpoint represented by this repository, that is the complete M7.6 candidate package or a later locally validated hotfix derived from it.

## What the new conversation should not assume

- M7.5 is explicitly validated; do not mark M7.6 validated without explicit local build/test confirmation.
- M7.6 load manoeuvring changes only canonical M4.5 requested electrical power; do not mutate electrical output, torque or rotor speed directly.
- Reactor power changes remain rod → reactivity → kinetics → fission-power through M2/M5.3; normal shutdown must not write thermal power directly.
- Temperature/void checks are observational. Quantitative xenon remains unavailable at the current M5.7 operational snapshot boundary and must not be synthesized.
- Normal shutdown uses unload → breaker open → controlled rod insertion → turbine rundown → continued main circulation; trips/SCRAM remain safety actions.
- Do not infer missing model state in the UI. Unavailable measurements remain unavailable rather than synthesized.
- Do not use wall-clock time to change deterministic simulation behavior or event order.
- Do not bypass `MeasuredSignalFrame` where instrumentation ownership applies.

## First action after restart

Validate the current M7.6 candidate locally. Only after successful build and complete tests should M7.6 be marked validated and implementation advance to M7.7.
