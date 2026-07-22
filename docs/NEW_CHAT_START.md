# New Chat Start — Nuclear Reactor Simulator

We are continuing the **Nuclear Reactor Simulator** project.

## Authoritative restart order

Read first:

1. `docs/PROJECT_HANDOFF.md`
2. `docs/PROJECT_STATUS.md`
3. `docs/ROADMAP.md`
4. `docs/ARCHITECTURE.md`
5. `docs/milestones/M10.7.md`
6. `docs/OPERATOR_COMPUTER_SESSION_CHECKPOINT_REPLAY_SAVE.md`
7. `docs/milestones/M10.6.md`
8. `docs/milestones/M10.5.md`
9. `docs/SUPERVISORY_AUTOMATIC_OPERATION.md`
10. `docs/DUAL_ASSISTANCE_CONTROL_AUTHORITY.md`
11. `docs/milestones/M10.4.md`
12. `docs/OPERATOR_COMPUTER_CONTEXTUAL_COMMAND_CONSOLE.md`
13. `docs/milestones/M10.3.md`
14. `docs/OPERATOR_COMPUTER_ALARM_LOG_INCIDENT_WORKSTATION.md`
15. `docs/milestones/M10.2.md`
16. `docs/OPERATOR_COMPUTER_INFORMATION_GUIDANCE_DIAGNOSTICS.md`
17. `docs/milestones/M10.1.md`
18. `docs/OPERATOR_COMPUTER_TERMINAL_SHELL.md`
19. `docs/OPERATOR_COMPUTER_SUPERVISORY_AUTOMATION.md`
20. ADR 0070, ADR 0074 and relevant M9 ADRs 0067–0073

## Exact current checkpoint

- M7, M8 and M9 gates: **COMPLETE / VALIDATED**.
- **M10.1–M10.6: VALIDATED**. The user confirmed the cumulative M10.2→M10.6 Hotfix 1 package compiled and the complete automated suite passed.
- **Official application baseline:** `M10.6 — Supervisory Automatic Operation`.
- **Current implementation candidate:** `M10.7 — Session, Checkpoint, Replay & Save Workspace`.
- Next after explicit M10.7 validation: `M10.8 — Integrated Operator Computer UI`.

## Current M10.7 candidate boundary

M10.7 adds F8 SESSION over canonical owners only:

- explicit opt-in M9.1 recording rather than hidden recorder overhead on normal desktop startup;
- replay-backed checkpoint create/list/restore;
- compact versioned `ScenarioSessionArchive` JSON persistence;
- exact-version load and full replay verification through `ScenarioFullReplayRunner`;
- persistence of operator actions plus separate M10.5/M10.6 automation intents;
- resumed recording after verified archive/checkpoint restore;
- training tracker reconstruction by attaching it before deterministic replay.

No opaque solver-state dump, second checkpoint owner, second fault trace or UI-owned restore logic is introduced.

## Non-negotiable architecture rules

Preserve:

- deterministic fixed timestep independent of wall clock/UI cadence;
- immutable committed/candidate semantics;
- each conserved inventory integrated exactly once by its canonical owner;
- M2 reactor physics, M3 primary thermohydraulics, M4 secondary/electrical and M5 instrumentation/control/protection/alarm ownership;
- UI consumes presentation contracts and dispatches typed intents only; no UI physics;
- unavailable values remain unavailable; no fabricated zero or true-state fallback;
- guidance/checklist criteria remain owned by their M7 scenario contracts/evaluators;
- runtime permissives/interlocks remain authoritative even when DIAGNOSTICS says READY;
- training assistance and plant control authority remain independent axes;
- real supervisory automation remains M5-owned;
- no free-form/NLP terminal command prompt.

## Validation action for M10.7

Run:

```text
dotnet clean
dotnet restore
dotnet build --no-restore
dotnet test --no-build
```

Then manually verify F8 SESSION:

- ordinary startup shows recorder `INACTIVE`;
- `START RECORDED SESSION` restarts at STEP 0 paused with recorder active;
- run some steps, pause, create a checkpoint and verify replay;
- save `.nrs-session.json`, load it, confirm restored logical step/state and continued recording;
- restore the selected checkpoint and confirm exact checkpoint step/fingerprint reconstruction;
- continue after restore and create/save another checkpoint/archive;
- verify M10.2–M10.6 pages/commands/modes remain operational and MainWindow layout remains unchanged.
