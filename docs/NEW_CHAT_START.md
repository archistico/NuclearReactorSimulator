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
- M7 gate: COMPLETE / VALIDATED;
- M8.1 deterministic fault scheduling/lifecycle: VALIDATED;
- last explicitly locally validated baseline: M8.2 — Hydraulic Component Faults hotfix 2;
- current implementation candidate: M8.3 — Instrumentation & Control Faults;
- M8.3 must not be marked validated until I explicitly confirm local build and complete tests pass;
- after M8.3 validation, continue with M8.4 — Turbine/Generator/Feedwater/Condenser Transients.

M8.1–M8.3 boundary:
- faults are explicit immutable scenario data; no hidden randomness;
- activation/deactivation occurs only at committed logical-step boundaries by exact step or named committed-snapshot condition;
- missing fault-type applicators or condition evaluators fail session loading closed;
- M8.1 owns validated scheduling/lifecycle; M8.2 validated hydraulic component constraints/leaks; M8.3 adds only typed M5.1 sensor and M5.2–M5.4 command-path fault overlays; turbine/generator/feedwater/condenser transient packs remain M8.4+;
- fault lifecycle state is snapshot/replay-visible but is not a second physical state owner.

Use the latest complete source ZIP/tree I provide as the working package. If its source/doc status conflicts with this checkpoint, stop advancement, reconcile the discrepancy, and keep the last explicitly validated milestone as the baseline.

For every milestone: make the smallest architecture-consistent change, add/update tests and docs, deliver a complete ZIP, and wait for my local build/test validation before marking it validated.
```

## What to upload or make available

Prefer the latest **complete** project ZIP, not a partial patch. For the checkpoint represented by this repository, that is the complete M8.3 candidate package or a later locally validated hotfix derived from it.

## What the new conversation should not assume

- M8.1 is validated; do not regress its deterministic scheduling/lifecycle semantics.
- M8.2 hotfix 2 is explicitly validated. Do not regress its hydraulic or App-test behavior.
- Do not assume M8.3 is validated unless explicit user confirmation exists.
- M8.3 sensor/control fault semantics do not imply turbine/generator/feedwater/condenser transient ownership.
- Do not infer missing fault handlers or condition evaluators; fault-enabled scenario loading fails closed.
- Do not infer missing model state in the UI or fault layer.
- Do not use wall-clock time, random numbers, UI refresh cadence or publication stride to change deterministic simulation/fault behavior or event ordering.
- Do not bypass `MeasuredSignalFrame` where instrumentation ownership applies.

## First action after restart

If M8.3 has not yet been explicitly validated, inspect the local validation result first. Only after successful validation should documentation be advanced to `M8.3 VALIDATED` and implementation begin on M8.4 Turbine/Generator/Feedwater/Condenser Transients.
