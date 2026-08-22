# M10.9.8.5 — Manual Integrated HMI Acceptance Checklist

## Purpose

This is the final **manual** gate for M10.9.8. It is stacked on **M10.9.8.4 Hotfix 1 VALIDATED** and adds no feature, plant command, fault injector or physics change.

Before starting, require all of the following to be green:

```bat
dotnet build
dotnet test
scripts\run-m1098-integrated-human-automation-hmi-audit.cmd
```

The focused preflight must report `m10985-integrated-hmi-closure-preflight-passes=True`.

> Passing this checklist closes **M10.9.8 only**. It does **not** close M10. M11 remains blocked until `scripts\run-m10-final-validation.cmd` and the separate approximately one-hour `scripts\run-m10-final-long-validation.cmd` are implemented and passed according to `M10_FINAL_PRE_M11_VALIDATION_PLAN.md`.

## Manual route

### HMI-01 — Startup, UNBOUND, keyboard and minimum window

- [ ] Start the desktop normally: MISSION shows `NO ACTIVE MISSION / UNBOUND` and does not invent challenge evidence.
- [ ] Navigate the primary workspaces by keyboard; focus remains visible.
- [ ] At approximately **1340×700**, critical safety/status/command content remains reachable; scroll is acceptable, overlap/clipping of critical content is not.

### HMI-02 — COMPUTER F1–F8, assistance and requested/effective authority

- [ ] COMPUTER exposes **F1–F8** and no F9.
- [ ] In MODES, switch `NONE / HIDDEN → CHECKLIST → GUIDED`; only guidance/presentation changes.
- [ ] Exercise `MANUAL` and `ASSISTED` authority requests and confirm requested/effective status is obvious.
- [ ] For healthy `SUPERVISORY`, use `HOLD CURRENT OPERATING POINT` before requesting supervisory authority; the result must remain protection/interlock subordinate.

### HMI-03 — Production mission @2 and demand/request/actual

Start:

```bat
dotnet run --project src\NuclearReactorSimulator.App\NuclearReactorSimulator.App.csproj -- --mission-pack=bounded-demand-following-5-10-5@2
```

- [ ] MISSION shows the exact @2 binding.
- [ ] Run beyond **STEP 1000** without the historical `control-out` envelope failure around STEP 610–615.
- [ ] `GRID DEMAND`, `REQUESTED LOAD` and `ACTUAL OUTPUT` are visually distinct.
- [ ] SAFETY / PROTECTION remains visually more important than score.

### HMI-04 — F4 COMMANDS, dependency chain, hover and ENTER

- [ ] During RUN, hover/select items in the command catalog: no repeated flicker/reset.
- [ ] Move farther down `DEPENDENCY CHAIN — SELECT A STEP`: hover and selection remain stable while logical steps advance.
- [ ] Press ENTER on representative available/blocked commands: the application remains open.
- [ ] A canonical rejection is shown as operator-visible blocked/runtime/scenario feedback, not silent success.

### HMI-05 — Target selector stability

While RUN is active, exercise the available selectors:

- [ ] PUMP target;
- [ ] ADMISSION TRAIN target;
- [ ] GENERATOR target;
- [ ] ROD target;
- [ ] ALARM target.

Options/selection must not visibly reset merely because unrelated telemetry refreshes.

### HMI-06 — F8 session/checkpoint/replay

- [ ] In F8, checkpoint list and selection remain stable during refresh.
- [ ] Save a recorded @2 session, continue, then restore it: exact mission binding remains @2.
- [ ] Restore a checkpoint: rows from later logical steps disappear.
- [ ] Continue live after restore: no duplicate MISSION timeline rows appear.

### HMI-07 — Protection, alarms and first-out

- [ ] Trigger/use the existing canonical SCRAM/protection control in a safe validation session.
- [ ] Protection indication is visually dominant and normal control remains subordinate.
- [ ] First-out/alarm context is obvious and reachable.
- [ ] ACK/RESET does not fabricate clearance of a still-active physical protection condition.

### HMI-08 — MISSION timeline and drill-down

- [ ] Timeline hover and drill-down controls remain stable while RUN advances.
- [ ] Drill down to COMMANDS/ELECTRICAL/ALARMS where available; navigation alone changes no plant state/logical step.
- [ ] Return to MISSION: context/timeline remains preserved.
- [ ] No F9 or MISSION plant-command authority appears.

### HMI-09 — Degraded/unavailable measurement truth

M10.9.8.5 does **not** add a manual-only fault injector. Forced instrumentation degradation remains covered by validated M10.9.8.3/8.4 automation.

- [ ] Wherever an existing field is naturally `UNAVAILABLE`/suspect, it is explicit and never rendered as fabricated zero.
- [ ] `MEASURED` and `MODEL` provenance remains visually distinguishable.
- [ ] Degraded/requested/effective authority wording is understandable whenever such evidence is present.

### HMI-10 — Manual takeover

- [ ] From a healthy supervisory session, select `MANUAL`.
- [ ] Takeover feedback is immediate and obvious.
- [ ] Requested/effective authority converges correctly.
- [ ] Assistance mode does not hide the takeover state or imply that stale supervisory objective still owns the plant.

### HMI-11 — Terminal mission vs continuing plant

If a terminal challenge state is reached naturally, inspect it directly. Otherwise M10.9.7.5/M10.9.8 automated evidence owns the terminal transition and this manual step verifies the independent plant presentation.

- [ ] Terminal mission status cannot hide current plant/safety state.
- [ ] Current logical time and safety/protection remain readable while the plant continues.
- [ ] Score never obscures protection.

### HMI-12 — Operator language, feedback and visual cleanliness

- [ ] No internal milestone/hotfix labels leak into the operator UI.
- [ ] Critical actions have clear accepted/blocked feedback.
- [ ] No new blank workspace, critical overlap or clipping is observed in normal/maximized/minimum practical window use.

## Validation record

```text
Date:
Build/package:
HMI-01: PASS / FAIL
HMI-02: PASS / FAIL
HMI-03: PASS / FAIL
HMI-04: PASS / FAIL
HMI-05: PASS / FAIL
HMI-06: PASS / FAIL
HMI-07: PASS / FAIL
HMI-08: PASS / FAIL
HMI-09: PASS / FAIL
HMI-10: PASS / FAIL
HMI-11: PASS / FAIL
HMI-12: PASS / FAIL
Blocking defects:
Notes:
```

If every required route is accepted, report exactly:

```text
M10.9.8.5 manual integrated HMI acceptance OK
```

That promotes M10.9.8.5 and closes M10.9.8. **M10 remains OPEN** pending the final cumulative + long pre-M11 validation.
