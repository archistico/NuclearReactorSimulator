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
- last explicitly locally validated baseline: M7.2 — Cold Shutdown & Pre-Startup (hotfix 1);
- M6 gate: COMPLETE / VALIDATED;
- current implementation candidate: M7.3 — First Criticality & Low-Power Operation;
- M7.3 must not be marked validated until I explicitly confirm local build and complete tests pass;
- after M7.3 validation, continue with M7.4 — Heat-Up, Steam Raising & Turbine Startup.

Use the latest complete source ZIP/tree I provide as the working package. If its source/doc status conflicts with this checkpoint, stop advancement, reconcile the discrepancy, and keep the last explicitly validated milestone as the baseline.

For every milestone: make the smallest architecture-consistent change, add/update tests and docs, deliver a complete ZIP, and wait for my local build/test validation before marking it validated.
```

## What to upload or make available

Prefer the latest **complete** project ZIP, not a partial patch. For the checkpoint represented by this repository, that is the complete M7.3 candidate package or a later locally validated hotfix derived from it.

## What the new conversation should not assume

- M7.1 and M7.2 are explicitly validated; M6 remains complete.
- M7.3 uses exact `pre-criticality-source-range` v1 initial-condition data and controlled rod permissions. Do not reinterpret its tiny non-zero neutron seed as an external-source solver or broaden permissions into steam/turbine/grid startup.
- Do not infer missing model state in the UI. For example, a capability that is not promoted into the operational snapshot remains unavailable rather than synthesized.
- Do not use wall-clock time to change deterministic simulation behavior or event order.
- Do not bypass `MeasuredSignalFrame` where instrumentation ownership applies.

## First action after restart

Validate the current M7.3 candidate locally. Only after successful build and complete tests should M7.3 be marked validated and implementation advance to M7.4.
