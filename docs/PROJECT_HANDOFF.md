# Project Handoff — Nuclear Reactor Simulator

This document is the **authoritative continuity checkpoint** for restarting the project in a new conversation.

## 1. Current truth

### Validated baseline

The last explicitly locally validated application milestone is:

**M10.7 — Session, Checkpoint, Replay & Save Workspace — VALIDATED**

The user confirmed M10.7 Hotfix 1 compiled successfully and the complete automated suite passed. The earlier cumulative M10.2→M10.6 Hotfix 1 gate had already validated M10.2, M10.3, M10.4, the incorporated M10.5 prerequisite and M10.6 in sequence.

The underlying M9 phase gate remains **COMPLETE / VALIDATED** with the user-corrected `MainWindow.axaml` as the authoritative validated layout baseline.

```text
M9 gate — COMPLETE / VALIDATED
        ↓
M10.1 — Operator Computer Contracts & Terminal Shell — VALIDATED
        ↓
M10.2 — Unified Information, Guidance & Diagnostics — VALIDATED
        ↓
M10.3 — Alarm, Log & Incident Workstation — VALIDATED
        ↓
M10.4 — Contextual Command Console — VALIDATED
        ↓
M10.5 — Dual Assistance & Control-Authority Model — VALIDATED
        ↓
M10.6 — Supervisory Automatic Operation — VALIDATED
        ↓
M10.7 — Session, Checkpoint, Replay & Save Workspace — VALIDATED / OFFICIAL BASELINE
        ↓
M10.7.1 — Operator Control-State & Synchronization Usability Hotfix — IMPLEMENTATION CANDIDATE
```

### Validated M10.7 boundary / current M10.7.1 candidate

M10.7 is validated and activates F8 SESSION by packaging existing M7/M9 owners rather than introducing a second state owner:

- normal desktop startup keeps M9.1 full recording **inactive** to avoid hidden every-fixed-step fingerprint/frame overhead;
- `START RECORDED SESSION` reloads the exact versioned desktop initial condition at STEP 0 with a recorder attached;
- checkpoints remain `ScenarioCheckpoint` replay-backed anchors;
- `ScenarioSessionArchive` schema v1 stores exact scenario definition, compact per-step fingerprint/event evidence, operator actions, M10.5/M10.6 automation intents, recorder events and checkpoints;
- JSON load always reconstructs through `ScenarioFullReplayRunner` and fails closed on fingerprint/event/checkpoint divergence;
- selected-checkpoint restore replays only the exact archived prefix and verifies the checkpoint fingerprint;
- after verified load/restore, `ScenarioRecorder` resumes from the verified prefix so the session can continue one deterministic trace;
- the built-in training tracker is attached before replay so training checkpoint/scoring state is reconstructed rather than reset at the final snapshot.

Routine desktop/full-plant endurance tests were reduced in M10.7 from 6,000 to 1,000 steps / 10 simulated seconds. The historical M9.7 60-second validation evidence remains authoritative, and direct `drum`/`exhaust` thermodynamic boundary regressions remain mandatory.

See `docs/milestones/M10.7.md`, `docs/OPERATOR_COMPUTER_SESSION_CHECKPOINT_REPLAY_SAVE.md`, ADR 0067, ADR 0070 and ADR 0074.


M10.7.1 is the current usability-hotfix candidate before M10.8. It preserves all M10.7 replay/session ownership while:

- separating latched protection visual state from one-shot command availability;
- exposing the same canonical `ProtectionReset` near reactor/turbine/electrical trip indications with M5.5-derived reset readiness/blockers;
- presenting synchronization as a pre-close breaker check only and `PARALLELED` after closure;
- adding current-condition, next-action and cold-shutdown-to-first-output guidance composed from validated M7 procedures without automatic dispatch.

See `docs/milestones/M10.7.1.md` and `docs/OPERATOR_CONTROL_STATE_SYNCHRONIZATION_USABILITY.md`.

---

## 2. Technology and solution structure

- C# / .NET 10
- Avalonia desktop UI
- xUnit v3
- warnings-as-errors
- deterministic educational full-plant RBMK-like simulator

Primary projects:

- `NuclearReactorSimulator.Domain`
- `NuclearReactorSimulator.Simulation`
- `NuclearReactorSimulator.Application`
- `NuclearReactorSimulator.Infrastructure`
- `NuclearReactorSimulator.App`
- matching test projects

The project is educational. It does not claim licensing-grade reactor-safety/LOCA/containment/ECCS/severe-accident fidelity.

---

## 3. Non-negotiable ownership rules

### Core runtime

1. Simulation uses a deterministic fixed timestep.
2. Wall-clock time, UI refresh cadence, publication stride and random timing must not change physical results.
3. Components read one committed state per step; candidate state is validated before commit.
4. Conserved fluid/thermal inventories are integrated exactly once by canonical plant-network orchestration.
5. Snapshot/state objects exposed across boundaries remain immutable in semantics.

### Physical ownership

- **M2:** reactivity, control rods, neutron kinetics, fission power, decay heat, feedbacks, iodine/xenon physics.
- **M3:** primary-circuit thermo-hydraulic topology and conserved inventories.
- **M4:** main steam, turbine, condenser/hotwell, condensate/feedwater, generator/grid and secondary-cycle energy path.
- **M5:** instrumentation, measured signals, automatic control, interlocks/trips/SCRAM, alarms.

No later layer may recreate these owners.

### Application/UI

- Avalonia/UI contains no physics.
- UI consumes `ControlRoomSnapshot`/presentation contracts only.
- UI dispatches typed application commands.
- App must not become a hidden Simulation owner.
- Missing data remains explicitly unavailable; never fabricate values for presentation convenience.

### Instrumentation/control/protection

- consumers requiring instrumentation use measured signals only;
- no silent true-state fallback;
- protection priority is above normal control;
- alarm acknowledge/reset does not reset physical protection;
- invalid required measurements follow validated fail-safe behavior.

### Scenario/fault/training

- scenario/fault layers schedule/orchestrate existing canonical seams;
- no scenario-specific physics owner;
- no scripted target pressures, levels, speeds, powers, breaker outcomes or recovery trajectories;
- fault schedules use deterministic logical-step/committed-condition semantics;
- unknown fault handlers/targets fail closed;
- fault lifecycle is replay/snapshot-visible but is not authoritative physical state.

---

## 4. Validated milestone history relevant to continuation

Earlier M0–M6 foundations/control-room stack are validated. Key later phase state:

### M7 — COMPLETE / VALIDATED

- M7.1 Versioned Initial Conditions & Scenario Framework
- M7.2 Cold Shutdown & Pre-Startup
- M7.3 First Criticality & Low-Power Operation
- M7.4 Heat-Up, Steam Raising & Turbine Startup
- M7.5 Grid Synchronization & Load Increase
- M7.6 Power Manoeuvring & Normal Shutdown
- M7.7 Training Objectives, Procedure Guidance & Evaluation

Key rule: exact initial-condition identity/version; no implicit latest when reconstructing sessions/replay.

### M8 — COMPLETE / VALIDATED

- M8.1 Deterministic Fault-Injection Framework
- M8.2 Hydraulic Component Faults hotfix 2
- M8.3 Instrumentation & Control Faults
- M8.4 Turbine / Generator / Feedwater / Condenser Transients hotfix 2
- M8.5 Educational Leak/LOCA-Class Scenarios hotfix 2
- M8.6 Electrical Loss & Station Blackout-Class Scenarios
- M8.7 Safety-Response Scenario Pack hotfix 2

Important limits:

- M8.5 pressure-driven break is a bounded educational conservative mass/energy source-term model, not licensing-grade LOCA critical-flow physics;
- M8.6 SBO-class composition does not imply detailed AC/DC buses, diesels, batteries, switchgear or ECCS electrical systems;
- scenario clearing never silently resets canonical protection latches or historical physical state;
- fault layers never directly write derived pressure/temperature/phase/rotor/generator outcomes.

---

## 5. M9.1 — VALIDATED

**Recorder, Checkpoints & Full Replay**

### Delivered contracts

- `ScenarioRecorder`
- immutable `ScenarioRecording`
- every-fixed-step `ScenarioRecordingFrame`
- monotonic `ScenarioRecordingEvent` stream
- accepted typed operator-action journal integration
- versioned `ControlRoomSnapshot` fingerprint
- `ScenarioCheckpoint` schema v1
- `ScenarioFullReplayRunner`
- replay divergence fail-closed semantics
- JSON checkpoint persistence

### Critical semantics

- recorder observes every deterministic fixed step independent of UI publication stride;
- accepted operator action at journal step `N` is replay-applied at fixed step `N + 1` in accepted sequence order;
- Run/Pause host mode is normalized out of replay identity, but committed logical state remains fingerprinted;
- checkpoint is a **replay-backed anchor**, not an opaque physical-state dump;
- seek reconstructs exact initial condition + scenario + action prefix and verifies fingerprint;
- M8 fault lifecycle is reconstructed from exact scenario data; M9.1 does not store a second fault command trace;
- full replay verifies every frame and deterministic event stream fail closed.

ADR: `docs/adr/0067-checkpoints-are-versioned-replay-anchors-not-opaque-state-dumps.md`.

---

## 6. M9.2 — VALIDATED

**Post-Incident Analysis**

### Delivered contracts

- `ScenarioPostIncidentAnalyzer`
- `PostIncidentAnalysisOptions`
- exact or deterministic incident-anchor selection
- logical-step pre/post windows
- `PostIncidentAnalysisTimelineEntry`
- synchronized fixed-step `PostIncidentTrendSample`
- compact start/anchor/end `PostIncidentStateSummary`
- `PostIncidentResponseMetrics`
- `PostIncidentAnalysisReport` schema v1
- `JsonPostIncidentAnalysisSerializer`

### Anchor policy

Without explicit recorder event sequence, deterministic selection prefers:

1. first fault transition;
2. otherwise first protection transition;
3. otherwise first alarm;
4. otherwise first operator action.

This is a **selection policy only**, not a physical causal claim.

### Evidence/timeline rules

- event order follows M9.1 recorder monotonic sequence;
- same-step event ordering remains explicit;
- relative logical-step offsets are derived from the selected anchor;
- typed operator commands remain typed evidence;
- analysis is immutable/observational and must not mutate runtime or replay state.

### Synchronized trend values

M9.2 only uses values already promoted through `ControlRoomSnapshot`, including where available:

- reactor thermal power;
- total primary mass;
- feedwater flow;
- steam export/steam flow;
- turbine shaft power;
- condenser heat rejection;
- gross electrical output;
- invalid-signal/alarm/fault counts;
- SCRAM/turbine-trip/generator-trip states.

Unavailable presentation values remain `null`; analysis must never synthesize them.

### Response metrics

Observed logical-step metrics include:

- first alarm latency;
- first protection activation latency;
- first operator-action latency;
- first fault-clear latency;
- peak invalid measured signals;
- peak annunciated/unacknowledged alarms;
- peak active fault count.

`null` latency means **not observed in the selected window**, not “never occurred”.

### Checkpoint linkage

M9.2 may reference the nearest preceding M9.1 checkpoint. It does not own restoration. Reconstruction remains `ScenarioFullReplayRunner.SeekAndVerify` ownership.

### Deliberate omission

M9.1 recordings do not currently include all private M3/M4/M5 conservation/audit objects. M9.2 therefore does **not** reach into Simulation to fabricate retrospective conservation diagnostics. Such diagnostics require an explicit future versioned presentation/recording contract.

ADR: `docs/adr/0068-post-incident-analysis-is-evidence-based-not-causal-inference.md`.

---

## 7. M9.3 — VALIDATED / CURRENT FUNCTIONAL BASELINE

**Advanced Xenon & Low-Power Transients**

M9.3 closes the historical operational-observability seam for validated M2.8 iodine/xenon physics without creating a second poison model:

```text
versioned M9.3 poison state/configuration
        ↓
M5 reactor/primary state envelope
        ↓
canonical M2.8 IodineXenonSolver
        ↓ committed xenon worth through explicit non-rod seam
M2 rods → total reactivity → point kinetics → fission power
        ↓
next canonical I/Xe state
        ↓ immutable committed diagnostic
ControlRoomSnapshot → scenario/training observation
```

Existing M7 v1 exact-version initial conditions remain xenon-disabled so prior replay/checkpoint identity semantics are not silently changed. New exact-version M9.3 seeds provide post-shutdown xenon restart and poisoned low-power operation. Scenario/Application/UI layers do not integrate poison inventories or script xenon/power/recovery trajectories.

Local compilation and the complete automated suite passed after two test-only hotfixes; M9.3 was then the official validated baseline and was subsequently superseded by validated M9.4.

ADR: `docs/adr/0069-m93-xenon-promotion-is-opt-in-through-versioned-runtime-state.md`.

---

## 8. Immediate continuation

Current implementation candidate:

**M9.7 — Advanced Fidelity Integration Gate — VALIDATED / M9 GATE COMPLETE**

M9.6 local compilation and the complete automated suite passed after one test-compilation-only hotfix. M9.7 hotfix 2 then compiled and passed the complete automated suite after the first manual-GUI findings were addressed. A subsequent manual pass found remaining bidirectional center-workspace clipping, no whole-session reset action, and a continuous-run block around logical step 3111. M9.7 hotfix 5 addresses those findings with a rebuilt scroll extent, composition-root session reset and a 6,000-step / 60-second endurance gate while preserving validated M7 v1 identities through the separate versioned desktop seed. The M9.7 gate introduces no new physical capability; hotfix 4 corrected saturated-mixture boundary bracketing and hotfix 5 adds the symmetric superheated-onset boundary search after revalidation exposed another valid low-pressure state missed by the coarse scan. The gate closes the phase with explicit cross-feature evidence:

- M9.3 xenon + M9.4 quasi-spatial feedback composed simultaneously through one global point-kinetics/non-rod seam with no double counting;
- a real xenon scenario traversing M9.1 recorder/checkpoint/full replay, M9.2 post-incident analysis and M9.6 immutable snapshot metric projection, requiring identical original/replay fingerprints and xenon evidence;
- M9.5 validated-capability declarations checked alongside M9.6 reference provenance so an internal green baseline never becomes an implicit historical-calibration claim;
- real-runtime App/ViewModel integration for Reactor workspace, xenon availability, legacy `Unavailable` semantics and RUN/PAUSE/SINGLE STEP synchronization.

Validation result: local compilation and all 760 automated tests passed, including the 6,000-step / 60-second endurance regressions. The user then supplied and manually validated the final corrected `MainWindow.axaml`; M9.7 and the M9 gate are complete.

See `docs/milestones/M9.7.md`, `docs/M9_ADVANCED_FIDELITY_INTEGRATION_GATE.md`, `docs/M9_FINAL_MANUAL_VALIDATION_CHECKLIST.md`, plus the M9.1–M9.6 milestone/domain documents.

M10.1–M10.7 are validated: the cumulative M10.6 Hotfix 1 gate validated M10.2–M10.6, and the user subsequently confirmed M10.7 Hotfix 1 compiled and the complete automated suite passed. M10.7 is the official application baseline; M10.7.1 is the current usability-hotfix candidate before M10.8. Final release hardening remains M11 after M10.

---

## 9. Approved M10 — Operator Computer, Supervisory Automation & Human-Machine Integration

Approved sequence:

1. M10.1 Operator Computer Contracts & Terminal Shell — VALIDATED
2. M10.2 Unified Information, Guidance & Diagnostics — VALIDATED
3. M10.3 Alarm, Log & Incident Workstation — VALIDATED
4. M10.4 Contextual Command Console — VALIDATED
5. M10.5 Dual Assistance & Control-Authority Model — VALIDATED
6. M10.6 Supervisory Automatic Operation — VALIDATED
7. M10.7 Session, Checkpoint, Replay & Save Workspace — VALIDATED / official baseline
7.1. M10.7.1 Operator Control-State & Synchronization Usability Hotfix — current implementation candidate
8. M10.8 Integrated Operator Computer UI
9. M10.9 Integrated Human-Automation Validation Gate

Critical M10 rules:

- operator computer = thin Application aggregation + App presentation;
- training assistance and physical control authority are orthogonal;
- supervisory automation = M5-owned deterministic control coordination;
- no direct physical-result assignment and no App/UI physics/control algorithm;
- measured-signal discipline, protection priority, degraded/fail-closed semantics and bumpless manual takeover;
- fixed menu/pages and keyboard-first operation; no free-form text/NLP/LLM control;
- M9.1 remains recorder/checkpoint/replay authority; persistent session archives, if added, are replay-backed packaging only.

See ADR 0070 for the durable ownership decision.

---

## 10. Authoritative documentation order

For continuation, read:

1. `docs/PROJECT_HANDOFF.md`
2. `docs/PROJECT_STATUS.md`
3. `docs/ROADMAP.md`
4. `docs/ARCHITECTURE.md`
5. `docs/NEW_CHAT_START.md`
6. `docs/milestones/M10.2.md`
7. `docs/OPERATOR_COMPUTER_INFORMATION_GUIDANCE_DIAGNOSTICS.md`
8. `docs/milestones/M10.1.md` and `docs/OPERATOR_COMPUTER_TERMINAL_SHELL.md`
9. `docs/OPERATOR_COMPUTER_SUPERVISORY_AUTOMATION.md`, `docs/milestones/M10.md` and ADR 0070
10. `docs/milestones/M9.7.md` and `docs/M9_ADVANCED_FIDELITY_INTEGRATION_GATE.md`
11. M9 ADRs 0067–0073 as relevant

If documentation conflicts, this handoff plus explicit local validation results take precedence; then fix the conflicting documentation before milestone advancement.

---

## 11. Delivery discipline

- deliver complete ZIPs, not isolated patch snippets;
- preserve warnings-as-errors;
- add deterministic/conservation/invariant tests appropriate to each change;
- update roadmap/status/handoff/ADR when durable architecture changes;
- do not mark a newly changed package validated without explicit user confirmation of local build + complete tests.
