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

Preserve all non-negotiable ownership and determinism rules from the handoff. Do not recreate physics/state owners in UI, control, protection, scenario, fault or diagnostic layers. Compose through existing validated seams.

Current recorded checkpoint:
- last explicitly locally validated baseline: M7.7 — Training Objectives, Procedure Guidance & Evaluation;
- M7 gate: COMPLETE / VALIDATED;
- last explicitly locally validated baseline: M8.1 — Deterministic Fault-Injection Framework hotfix 1;
- current implementation candidate: M8.2 — Hydraulic Component Faults hotfix 1;
- M8.2 must not be marked validated until I explicitly confirm local build and complete tests pass;
- after M8.2 validation, continue with M8.3 — Instrumentation & Control Faults.

M8.1/M8.2 boundary:
- faults are explicit immutable scenario data; no hidden randomness;
- activation/deactivation occurs only at committed logical-step boundaries by exact step or named committed-snapshot condition;
- missing fault-type applicators or condition evaluators fail session loading closed;
- M8.1 owns validated scheduling/lifecycle; M8.2 adds only typed hydraulic component constraints and selected audited leaks through canonical seams; instrumentation/control/transient effects remain M8.3+;
- fault lifecycle state is snapshot/replay-visible but is not a second physical state owner.

Use the latest complete source ZIP/tree I provide as the working package. If its source/doc status conflicts with this checkpoint, stop advancement, reconcile the discrepancy, and keep the last explicitly validated milestone as the baseline.

For every milestone: make the smallest architecture-consistent change, add/update tests and docs, deliver a complete ZIP, and wait for my local build/test validation before marking it validated.
```

## What to upload or make available

Prefer the latest **complete** project ZIP, not a partial patch. For the checkpoint represented by this repository, that is the complete M8.2 hotfix-1 candidate package or a later locally validated hotfix derived from it.

## What the new conversation should not assume

- M8.1 is validated; do not regress its deterministic scheduling/lifecycle semantics.
- Do not assume M8.2 is validated unless explicit user confirmation exists.
- M8.2 hydraulic fault semantics do not imply sensor/control/turbine/electrical fault semantics.
- Do not infer missing fault handlers or condition evaluators; fault-enabled scenario loading fails closed.
- Do not infer missing model state in the UI or fault layer.
- Do not use wall-clock time, random numbers, UI refresh cadence or publication stride to change deterministic simulation/fault behavior or event ordering.
- Do not bypass `MeasuredSignalFrame` where instrumentation ownership applies.

## First action after restart

If M8.2 has not yet been explicitly validated, inspect the local validation result first. Only after successful validation should documentation be advanced to `M8.2 VALIDATED` and implementation begin on M8.3 Instrumentation & Control Faults.
