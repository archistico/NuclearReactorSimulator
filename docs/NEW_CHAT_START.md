# New Chat Start — Nuclear Reactor Simulator

We are continuing the **Nuclear Reactor Simulator** project.

## Authoritative restart order

Read first:

1. `docs/PROJECT_HANDOFF.md`
2. `docs/PROJECT_STATUS.md`
3. `docs/ROADMAP.md`
4. `docs/ARCHITECTURE.md`
5. `docs/milestones/M10.7.1.md`
6. `docs/OPERATOR_CONTROL_STATE_SYNCHRONIZATION_USABILITY.md`
7. `docs/milestones/M10.7.md`
8. `docs/OPERATOR_COMPUTER_SESSION_CHECKPOINT_REPLAY_SAVE.md`
9. `docs/milestones/M10.6.md`
10. `docs/milestones/M10.5.md`
11. `docs/SUPERVISORY_AUTOMATIC_OPERATION.md`
12. `docs/DUAL_ASSISTANCE_CONTROL_AUTHORITY.md`
13. `docs/milestones/M10.4.md`
14. `docs/OPERATOR_COMPUTER_CONTEXTUAL_COMMAND_CONSOLE.md`
15. `docs/milestones/M10.3.md`
16. `docs/OPERATOR_COMPUTER_ALARM_LOG_INCIDENT_WORKSTATION.md`
17. `docs/milestones/M10.2.md`
18. `docs/OPERATOR_COMPUTER_INFORMATION_GUIDANCE_DIAGNOSTICS.md`
19. `docs/milestones/M10.1.md`
20. `docs/OPERATOR_COMPUTER_TERMINAL_SHELL.md`
21. `docs/OPERATOR_COMPUTER_SUPERVISORY_AUTOMATION.md`
22. ADR 0070, ADR 0074 and relevant M9 ADRs 0067–0073

## Exact current checkpoint

- M7, M8 and M9 gates: **COMPLETE / VALIDATED**.
- **M10.1–M10.7: VALIDATED**. The cumulative M10.2→M10.6 Hotfix 1 chain passed first; the user then confirmed M10.7 Hotfix 1 compiled and the complete automated suite passed.
- **Official application baseline:** `M10.7 — Session, Checkpoint, Replay & Save Workspace`.
- **Current implementation candidate:** `M10.7.1 — Operator Control-State & Synchronization Usability Hotfix`.
- Next after explicit M10.7.1 validation: `M10.8 — Integrated Operator Computer UI`.

## Validated M10.7 boundary / current M10.7.1 candidate

M10.7 is validated and adds F8 SESSION over canonical owners only:

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

## Validation action for M10.7.1

Run:

```text
dotnet clean
dotnet restore
dotnet build --no-restore
dotnet test --no-build
```

Then manually verify M10.7.1 usability:

- SCRAM / TURBINE TRIP / GENERATOR TRIP become filled `— ACTIVE` indications after latching and cannot be issued again while active;
- the same canonical `RESET PROTECTION` is discoverable near affected panels and shows `RESET AVAILABLE` or a real M5.5-derived blocking reason;
- with generator breaker closed, synchronization shows `PARALLELED` / normal rather than a stale warning;
- with breaker open, synchronization shows Δfrequency / Δphase / Δvoltage and per-dimension OK/WAIT against canonical M4.5 limits;
- Overview shows current condition, next canonical action and the cold-shutdown-to-first-electrical-output command map without dispatching automatically;
- existing M10.7 SESSION save/load/checkpoint/replay behavior still passes automated regressions.
