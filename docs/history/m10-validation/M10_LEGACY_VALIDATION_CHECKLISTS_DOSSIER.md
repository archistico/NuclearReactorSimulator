# M10 LEGACY VALIDATION CHECKLISTS DOSSIER
> Historical consolidation dossier. The source documents below were completed/superseded and had no executable references at consolidation time. Their content is retained here for provenance while the individual top-level files are removed.
## Source manifest
| Original top-level file | Lines | SHA-256 (normalized LF UTF-8) |
| --- | ---: | --- |
| `M10_8_MANUAL_VALIDATION_CHECKLIST.md` | 66 | `B3824EF37521534297801D534F2C6A9F1B504B8278B6D7A3BAF277BDEF83F27F` |
| `M10_9_4_FINAL_MANUAL_VALIDATION_CHECKLIST.md` | 52 | `F794B37BAF8514584E791BE7698A39F25945A50D740683AA8E6190E066924F84` |
| `M10_9_5_3_MANUAL_VALIDATION_CHECKLIST.md` | 21 | `AF4A4B4C47FF1CFF994ACBDE58E9717D646B9EC3DDBB4B9706FD83E748CB9C09` |
| `M10_9_7_3_HOTFIX1_REV2_DOCS3_ALIGNMENT.md` | 38 | `1C476DBC02EF8B6E8B406180D620E67325EDF123E59622C5A3A732DFA6A5683E` |
| `M10_9_7_3_HOTFIX1_REV2_DOCS4_ALIGNMENT.md` | 38 | `8E68AE1314AF7DD6176CB31DF07F871BC8ACDEA7E320C7FE55BAE3F47E7EEE69` |
| `M10_9_7_3_MANUAL_VALIDATION_CHECKLIST.md` | 72 | `C94187DD7C994852EB79B64C3213E825AE8122DD6A5AB69A5E3DD87641779590` |
| `M10_9_8_1_VALIDATION_MATRIX.md` | 118 | `8D98651DED85F10119CF476033CE60ABB6BABF64B4BEB103CC148C4BE20B4E34` |
| `M10_9_8_2_AUTOMATED_HEALTHY_ASSISTANCE_AUTHORITY_MATRIX.md` | 55 | `6C49BB31978F80A9534659C3E431FCAC861128F1C664D30E29E37EA044CC8C3E` |
| `M10_9_8_2_HOTFIX1_MANUAL_SMOKE_CHECKLIST.md` | 19 | `15C02C8BC8E56283272D4ACD3DA7A6CF149B41FFC327C99580A60F09C62CEEC9` |
| `M10_9_8_3_DEGRADED_FAULT_PROTECTION_TAKEOVER_MATRIX.md` | 64 | `C43B25FB867CD206CFB51E3E9AEB6BB6A15E02CC717554FBE957AB469995FC75` |
| `M10_9_8_4_REPLAY_CHECKPOINT_SAME_SEED_INTEGRITY.md` | 70 | `A23EDB281F08CC0FC2A0C6ED3C7965CD75D8A8F425C62F122B762B94F0C11386` |

## Retained source snapshots

---

## Source snapshot — `M10_8_MANUAL_VALIDATION_CHECKLIST.md`

### M10.8 Manual Validation Checklist — Integrated Operator Computer UI

**VALIDATION RESULT: PASSED / M10.8 VALIDATED.** The user confirmed local compilation and the complete automated suite passed; M10.8 is now the baseline beneath M10.9.1.

Use this checklist after a clean build and complete automated suite.

#### A. Global terminal navigation

- [ ] From Overview, press F1: Computer opens on GUIDANCE.
- [ ] Repeat F2–F8: each key opens the correct page.
- [ ] Exactly one page-selection indicator is visible at a time.
- [ ] Mouse clicking each page button selects the same page as the corresponding F-key.
- [ ] Merely changing pages does not advance the logical step or change plant state.

#### B. Fixed workstation shell

- [ ] Header/menu remain visible while scrolling a long page.
- [ ] Runtime, logical step, alarms, signal health and protection summary remain visible while scrolling.
- [ ] Footer status/keyboard-help remains visible while scrolling.
- [ ] Active-page title/state are readable and not clipped.

#### C. Keyboard-only operation

Without using the mouse after opening the Computer workspace:

- [ ] F1–F8 selects every page.
- [ ] Tab / Shift+Tab reaches all interactive controls on COMMANDS, MODES and SESSION.
- [ ] COMMANDS list supports Up/Down selection.
- [ ] Enter dispatches only the selected available typed command.
- [ ] Blocked/unavailable commands remain fail-closed.
- [ ] SESSION buttons can be focused/activated by keyboard; native file dialogs remain operable by keyboard.

#### D. COMMANDS layout

- [ ] Catalog rows remain readable at the minimum supported window size.
- [ ] No horizontal overlap/clipping requires a hidden scroll hack.
- [ ] Selected command detail and availability/block reason remain readable.
- [ ] Executing a command still uses canonical runtime validation.

#### E. SESSION layout

- [ ] START RECORDED SESSION remains available.
- [ ] CREATE CHECKPOINT, VERIFY REPLAY, SAVE ARCHIVE, LOAD ARCHIVE and RESTORE SELECTED remain reachable.
- [ ] Checkpoint list is readable without forcing an excessively wide center workspace.
- [ ] Save/load/restore semantics remain replay-backed and verified.

#### F. M10.7.1 regression checks

- [ ] SCRAM / TURBINE TRIP / GENERATOR TRIP show persistent ACTIVE state only when latched.
- [ ] RESET PROTECTION shows AVAILABLE or a real canonical blocking reason.
- [ ] Breaker closed shows PARALLELED rather than stale SYNC WARNING.
- [ ] Breaker open shows Δf / Δphase / ΔV synchronization details.
- [ ] INSERT/HOLD/WITHDRAW show committed rod motion state.
- [ ] START/RUN/STOP show committed pump state.
- [ ] CLOSE/OPEN BREAKER show committed breaker position.
- [ ] SPEED± and LOAD± remain momentary and show explicit accepted/blocked last-action feedback rather than staying latched.

#### G. Acceptance

M10.8 may be marked VALIDATED only when:

1. clean restore/build succeeds with warnings-as-errors;
2. complete automated suite passes;
3. this manual checklist passes without material overlap/clipping/keyboard-navigation regressions.

After validation, update authoritative docs to `M10.8 VALIDATED` and continue in a new chat with M10.9.

---

## Source snapshot — `M10_9_4_FINAL_MANUAL_VALIDATION_CHECKLIST.md`

### M10.9.4 Final Manual HMI / Engineering-Schematic Validation Checklist

**Status:** PASSED — user-confirmed on 2026-07-24. M10.9.4 is the official validated baseline.

#### Purpose

Hotfix 23 has passed compilation, the complete ordinary suite and both explicit 60-second gameplay journeys. This checklist is the final evidence required to promote M10.9.4 from automated-green checkpoint to validated milestone.

Record each item as PASS / FAIL with a short note or screenshot reference where useful.

#### Environment

- [x] Clean Hotfix 23 build is being tested.
- [x] Default desktop scenario loads without startup error.
- [x] Minimum supported window size and normal maximized layout are both checked.

#### Engineering schematics

- [x] Reactor workspace shows the rod → reactivity → neutron/power → thermal/void feedback chain clearly.
- [x] Primary workspace shows drum → suction → MCP → header → channels → return plus steam export/feedwater.
- [x] Turbine workspace shows main steam → stop/control/admission valves → turbine → shaft → exhaust → condenser/feedwater.
- [x] Grid workspace shows shaft → generator → breaker → grid and separate synchronization/protection signal paths.
- [x] Instrumentation/protection workspace uses signal-flow grammar distinct from piping and makes protection priority unambiguous.

#### Readability and semantics

- [x] No schematic overlaps, clipping or unreadable labels at minimum size.
- [x] Every process path has unambiguous direction and endpoints.
- [x] Equipment cards expose meaningful IN/OUT information.
- [x] Amber SHAFT is clearly understood as mechanical-energy medium, not warning severity.
- [x] Measured / Model Diagnostic / Unavailable provenance remains visible and is not silently merged.

#### Generator power-path diagnostics

- [x] Breaker open: 0 MWe is explained as expected and the next synchronization/close action is clear.
- [x] Breaker closed with zero requested load: LOAD RAISE is identified as the missing action.
- [x] Requested load with insufficient shaft support: diagnostics direct attention to steam/admission/turbine/protection rather than suggesting more load.
- [x] Requested MWe, actual MWe, turbine shaft power, generator mechanical input, speed/frequency, breaker and trip state are distinguishable.

#### Regression smoke check

- [x] Existing gauges, whole-plant mimic and subsystem selection still work.
- [x] Operator Computer F1–F8 navigation and keyboard operation still work.
- [x] RUN / PAUSE / SINGLE STEP remain synchronized.
- [x] Alarm acknowledgement/reset and protection reset remain separate canonical actions.
- [x] Checkpoint/replay/session functionality remains accessible and coherent.

#### Sign-off

- [x] **M10.9.4 manual acceptance passed.**

Promotion completed: the authoritative project documents record M10.9.4 as validated. The later M10.9.4.1-A audit has since executed and found a ~70-second trip; that follow-on result does not reopen this completed manual HMI acceptance.

---

## Source snapshot — `M10_9_5_3_MANUAL_VALIDATION_CHECKLIST.md`

### M10.9.5.3 — Manual HMI validation checklist

**Status:** completed as part of the validated M10.9.5.3 Hotfix 2 baseline on 2026-08-20.

Use the validated M10.9.5.2 baseline plus the M10.9.5.3 candidate. Do not change physics/configuration merely to make the presentation look better.

- [ ] Open **F4 COMMANDS** with the desktop at the minimum supported window size and confirm the page remains usable through normal scrolling.
- [ ] Select commands using keyboard **TAB + UP/DOWN** and confirm selection alone never dispatches a command.
- [ ] For at least one reactor command, verify **DIRECT EFFECT**, **EXPECTED INFLUENCE** and **WHAT TO MONITOR** are visibly distinct and understandable.
- [ ] Repeat for one primary/pump command, one turbine command and one generator/breaker command.
- [ ] Select a blocked/unavailable command and confirm the blocker remains visible while the consequence/dependency evidence is still inspectable.
- [ ] Select different dependency-chain steps and confirm only the presentation/schematic focus changes; plant state and command status do not change.
- [ ] Select a dependency step backed by a canonical mimic element and confirm that exact element is highlighted.
- [ ] Select a dependency step backed by a canonical mimic connection and confirm the text explicitly identifies the connection plus its proxy highlight.
- [ ] Select a command-target or published-state step with no graphical reference and confirm the mimic highlight clears instead of moving to an unrelated element.
- [ ] Confirm the compact whole-plant mimic uses the same canonical equipment/path vocabulary as the main plant overview.
- [ ] Confirm unavailable/non-mimic dependency evidence is labelled rather than fabricated as a schematic target.
- [ ] Press **ENTER** or **EXECUTE [ENTER]** only on an intentionally selected available command and confirm the existing M10.4 dispatch feedback remains intact.
- [ ] Confirm there is still no free-form text command input.

Promotion to M10.9.5.3 VALIDATED requires build, complete ordinary tests, focused audit and this checklist to be green.

---

## Source snapshot — `M10_9_7_3_HOTFIX1_REV2_DOCS3_ALIGNMENT.md`

### M10.9.7.3 Hotfix 1 REV2 Docs3 — Desktop Host / Session Integrity Roadmap Alignment

#### Scope

Documentation-only alignment over M10.9.7.3 Hotfix 1 REV2 Docs2. No runtime, test, validation-script, `eng/` or CI change belongs to Docs3.

#### Current runtime validation state

- build: already green for Hotfix 1 REV2;
- complete ordinary suite: already green;
- `scripts/run-m10973-mission-performance-live-workspace-audit.cmd`: already green;
- `docs/M10_9_7_3_MANUAL_VALIDATION_CHECKLIST.md`: still pending;
- therefore Hotfix 1 REV2 is still CANDIDATE, not yet promoted.

#### New accepted planning decisions

1. After REV2 manual validation/promotion, build **M10.9.7.3 Hotfix 2 — Desktop Host Failure & Session Save Integrity** exclusively on REV2 VALIDATED.
2. Hotfix 2 must contain expected numerical step failures at the desktop pump boundary without blanket exception swallowing.
3. Hotfix 2 must align start/reset/load/restore host failure policy.
4. Save must select destination before full export and use non-destructive temporary-write + safe replace/move semantics for supported local desktop storage.
5. Failure during write/replace must preserve the previous archive.
6. Remaining mixed App engineering-number formatting should align with the current invariant technical HMI convention.
7. M10.9.7.4 cannot begin until Hotfix 2 is validated.
8. M11.3 owns measured UI-thread/projection/notification/export responsiveness and any worker/off-thread ownership design.
9. M13 now owns stable canonical-ID selection/no silent command retargeting and staged `MainWindowViewModel` decomposition.

#### Explicit non-scope

Docs3 authorizes no change to Simulation physics, fixed timestep, challenge/scoring/protection authority, archive schema, streaming API, worker-thread ownership or user-facing simulation speed.

#### References

- `DESKTOP_HOST_FAILURE_AND_SESSION_SAVE_INTEGRITY_REVIEW.md`
- `adr/0185-contain-desktop-runtime-failures-and-replace-session-archives-safely.md`
- `milestones/M10.9.7.md`
- `milestones/M11.md`
- `milestones/M13.md`
- `FORWARD_EXECUTION_PLAN_M10_9_7_TO_M15.md`

---

## Source snapshot — `M10_9_7_3_HOTFIX1_REV2_DOCS4_ALIGNMENT.md`

### M10.9.7.3 Hotfix 1 REV2 Docs4 — Documentation Architecture / Indexing / Limitations Alignment

#### Scope

Documentation-only alignment over the unchanged M10.9.7.3 Hotfix 1 REV2 runtime/test/script candidate. Automated build/ordinary/focused evidence already reported for REV2 remains applicable because Docs4 changes no source, test, script, build, CI or evidence contract.

#### What Docs4 changes

- reorganizes `ARCHITECTURE.md` by stable layer/subsystem ownership and removes stale current-checkpoint narration;
- preserves the former milestone-led architecture additions in `history/ARCHITECTURE_MILESTONE_LEDGER.md`;
- restores `ROADMAP.md` to future-only content by removing completed-milestone status duplication;
- adds exhaustive `TOP_LEVEL_DOCUMENT_INDEX.md` while keeping `README.md` curated;
- adds `adr/README.md` with normalized navigation status/area and standardizes ADR status headings;
- adds `DOCUMENTATION_ARCHITECTURE_AND_INDEXING.md` and ADR-0186;
- expands `KNOWN_MODEL_LIMITATIONS.md` with the stateless relief/bypass no-blowdown/reseat limitation and moves exact regression precision back to machine evidence;
- explicitly documents that physical control-rod travel rate is already stateful/deterministic and is not an instantaneous-motion limitation;
- fixes the live `milestones/M10.9.4.md` root-CHANGELOG/history references;
- assigns automated documentation-index/status/link/source-comment drift checks to M11.5.

#### Important review correction

The App/Domain review inference that control rods move instantaneously is not supported by the current physical model. `ControlRodDefinition.TravelRate` and `ControlRodMotionSolver` own deterministic rod travel. The null generic `ActuatorDefinition.ControlRod` travel-rate field means only that controller-side actuator ramping is not duplicated above the physical rod owner.

#### Validation state

Docs4 does not promote M10.9.7.3 Hotfix 1 REV2. The only remaining promotion requirement remains the manual HMI checklist recorded in `PROJECT.md`.

#### Static documentation audit

- Markdown files checked: 585;
- live top-level Markdown files indexed: 124 / 124;
- ADR files indexed: 186 / 186 (`0001`–`0186`, contiguous);
- ADR files with `## Status`: 186 / 186;
- relative Markdown links missing: 0;
- `ARCHITECTURE.md` milestone-led headings: 0;
- `ARCHITECTURE.md` current-checkpoint section: absent;
- concrete `VALIDATED / CLOSED` status blocks in `ROADMAP.md`: 0;
- `src/`, `tests/`, `scripts/`, `eng/`, `.github/`: byte-identical to Docs3.

---

## Source snapshot — `M10_9_7_3_MANUAL_VALIDATION_CHECKLIST.md`

### M10.9.7.3 Hotfix 1 REV2 manual HMI validation checklist

**Result: PASSED / VALIDATED locally on 2026-08-21.** This checklist is retained as acceptance evidence for the promoted REV2 baseline.

Run this review only after build, ordinary tests and `scripts\run-m10973-mission-performance-live-workspace-audit.cmd` are green.

M10.9.7.3 Hotfix 1 REV2 activates presentation only. Do not interpret this gate as challenge selection, score ownership, protection ownership or plant-command authority.

#### A. Normal desktop startup — unbound mission state

Launch normally:

```bat
dotnet run --project src\NuclearReactorSimulator.App\NuclearReactorSimulator.App.csproj
```

Confirm:

- the left workspace rail contains exactly one `MISSION` entry titled `Mission & Performance`;
- opening MISSION shows `NO ACTIVE MISSION` / `UNBOUND`, not a fabricated challenge inferred from the desktop scenario;
- unavailable mission/demand/score values are shown as unavailable rather than zero;
- COMPUTER still exposes exactly F1–F8 and no F9;
- COMPUTER `OPEN MISSION` selects MISSION and does not operate the plant;
- returning among PLANT/REACTOR/PRIMARY/TURBINE/GRID/ALARMS/COMPUTER/MISSION remains practical.

#### B. Explicit active mission startup

Close the app and launch one exact authored pack explicitly:

```bat
dotnet run --project src\NuclearReactorSimulator.App\NuclearReactorSimulator.App.csproj -- --mission-pack=bounded-demand-following-5-10-5@1
```

Confirm:

- startup opens a real M10.9.6 mission rather than inferring from scenario identity;
- objective title/description are the scenario objective metadata, not challenge-title aliases;
- lifecycle and logical-step information is readable;
- `GRID DEMAND`, `REQUESTED LOAD` and `ACTUAL OUTPUT` are three visibly separate values;
- `GRID DEMAND` is presented as a training/reference quantity, never as a generator command;
- score/classification and score dimensions are readable without relying on color alone;
- `SAFETY / PROTECTION SIGNIFICANCE` is visually at least as prominent as the score block;
- recent evidence is readable, bounded and ordered consistently by logical evidence;
- assistance and control-authority evidence are observational only.

#### C. Live update and navigation behavior

Using the normal canonical controls for the loaded challenge:

- run a few deterministic steps and confirm MISSION updates without obvious 100 Hz UI churn;
- issue at least one applicable generator-load action and confirm requested/actual evidence can diverge without being merged;
- use F1 through F8 and confirm every key still opens the historical COMPUTER page associated with that key;
- from COMPUTER choose `OPEN MISSION` and confirm the plant state does not change as a result of navigation;
- verify no F9 behavior exists.

#### D. Minimum-window/readability review

At the supported minimum window size (`1340 × 700`):

- objective/lifecycle remains discoverable;
- safety/protection significance remains discoverable;
- the three demand/request/output values remain distinguishable;
- secondary score/evidence detail may scroll, but the surface does not become ambiguous or overlap controls;
- text is not dependent on color alone for meaning.

#### E. Scope confirmation

Confirm there is no UI control in MISSION that directly issues plant commands, changes scoring rules, changes challenge definitions, resets protection or changes physical state.

Archive-restored mission binding/timeline reconstruction is intentionally **not** an M10.9.7.3 acceptance criterion; it is owned by M10.9.7.4.

If automated gates and all applicable checks above are green, promote M10.9.7.3 Hotfix 1 REV2 to VALIDATED. Do **not** begin M10.9.7.4 yet: the accepted post-7.3 App review requires M10.9.7.3 Hotfix 2 — Desktop Host Failure & Session Save Integrity — to be built exclusively on that validated REV2 baseline and validated first.

---

## Source snapshot — `M10_9_8_1_VALIDATION_MATRIX.md`

### M10.9.8.1 REV1 — Integrated Human / Automation / HMI validation matrix freeze

#### Status

**REV1 Docs1 VALIDATED — accepted 2026-08-22 after build, complete ordinary suite, focused matrix audit and explicit user acceptance.**

M10.9.8.1 is a contract/evidence-planning slice only. REV1 deliberately keeps the entire compiled surface unchanged from the validated baseline: all files under `src/` and `tests/` are byte-identical to M10.9.7.5 Hotfix 1 VALIDATED. It adds no plant behavior, XAML/runtime semantics, Simulation physics, authority rule, challenge/scoring rule, protection rule, archive schema, fingerprint algorithm, workstation command authority or production scenario registration.

The machine-readable source of truth is:

`eng/m1098-integrated-human-automation-hmi-matrix.json`

The matrix is frozen before execution work begins. Its schema is validated externally by `eng/validate-m10981-integrated-validation-matrix.ps1`; the focused gate also reuses already-validated M5/Application owner tests instead of adding new compiled M10.9.8.1 tests. A failing future row must be classified to its existing owner; M10.9.8 is not authorization to retune Phase H/I numerics or invent new physics/HMI features.

#### Baseline

The only baseline is **M10.9.7.5 Hotfix 1 VALIDATED / M10.9.7 CLOSED**.

The authoritative desktop production identity remains:

`integrated-normal-operations-training-i5-repaired-v4-production | integrated-operations-desktop-stable@4 | CorrelationConsistentInverseDomain | FourNodeBranchContinuityCorrectedCommitOptIn | 10 ms`

Historical exact-version identities remain immutable.

#### Fixed axes

Training assistance:

- `Hidden`
- `ChecklistOnly`
- `Guided`

Plant control authority:

- `Manual`
- `Assisted`
- `SupervisoryAutomatic`

The healthy matrix therefore contains exactly **9 rows**. All nine use the same exact bounded-demand challenge/trajectory and command schedule so assistance and authority are the intended independent variables.

#### Cross-cutting invariants

1. Assistance-only changes must not change plant physics or control authority.
2. Protection always overrides normal control.
3. Requested and effective authority remain separately visible.
4. Supervisory degradation is fail-closed and evidence-based.
5. Expected command influence remains distinct from observed response.
6. External demand, requested generator load and actual electrical output remain distinct.
7. Scoring remains observational.
8. Unavailable/suspect measurements stay unavailable/suspect; no true-state substitution.
9. MISSION has no plant-command authority.
10. Replay/checkpoint reconstructs equivalent operator-visible state without opaque workstation dumps.
11. Keyboard-only critical operation remains viable.

The JSON matrix assigns an explicit investigation owner to every invariant and every row.

#### Frozen rows

| Row | Family | Exact scenario / validation composition | Exact profile | Assistance | Requested → expected effective authority |
| --- | --- | --- | --- | --- | --- |
| HAA-01 | healthy bounded load | `bounded-demand-following-5-10-5@1` | `stable-low-load-parallel-operation@1` | Hidden | Manual → Manual |
| HAA-02 | healthy bounded load | `bounded-demand-following-5-10-5@1` | `stable-low-load-parallel-operation@1` | Hidden | Assisted → Assisted |
| HAA-03 | healthy bounded load | `bounded-demand-following-5-10-5@1` | `stable-low-load-parallel-operation@1` | Hidden | SupervisoryAutomatic → SupervisoryAutomatic |
| HAA-04 | healthy bounded load | `bounded-demand-following-5-10-5@1` | `stable-low-load-parallel-operation@1` | ChecklistOnly | Manual → Manual |
| HAA-05 | healthy bounded load | `bounded-demand-following-5-10-5@1` | `stable-low-load-parallel-operation@1` | ChecklistOnly | Assisted → Assisted |
| HAA-06 | healthy bounded load | `bounded-demand-following-5-10-5@1` | `stable-low-load-parallel-operation@1` | ChecklistOnly | SupervisoryAutomatic → SupervisoryAutomatic |
| HAA-07 | healthy bounded load | `bounded-demand-following-5-10-5@1` | `stable-low-load-parallel-operation@1` | Guided | Manual → Manual |
| HAA-08 | healthy bounded load | `bounded-demand-following-5-10-5@1` | `stable-low-load-parallel-operation@1` | Guided | Assisted → Assisted |
| HAA-09 | healthy bounded load | `bounded-demand-following-5-10-5@1` | `stable-low-load-parallel-operation@1` | Guided | SupervisoryAutomatic → SupervisoryAutomatic |
| INT-10 | synchronization/loading | `grid-synchronization-initial-loading` | `pre-synchronization-grid-loading@1` | Guided | Manual → Manual |
| INT-11 | blocked permissive/interlock | `grid-synchronization-initial-loading` | `pre-synchronization-grid-loading@1` | ChecklistOnly | Manual → Manual |
| INT-12 | degraded required supervisory measurement | validation-only `m1098-supervisory-required-measurement-unavailable@1` | `integrated-operations-desktop-stable@4` | Guided | SupervisoryAutomatic → Assisted |
| INT-13 | canonical protection trip | `m87-protection-fail-safe-response` | `stable-low-load-parallel-operation@1` | Guided | SupervisoryAutomatic → Assisted |
| INT-14 | equipment fault | `hydraulic-component-fault-demonstration` | `stable-low-load-parallel-operation@1` | ChecklistOnly | Assisted → Assisted |
| INT-15 | instrumentation fault | `instrumentation-control-fault-demonstration` | `stable-low-load-parallel-operation@1` | Guided | Assisted → Assisted |
| INT-16 | manual takeover | `integrated-normal-operations-training-i5-repaired-v4-production` | `integrated-operations-desktop-stable@4` | Hidden | SupervisoryAutomatic → Manual |
| INT-17 | challenge/demand-following | `bounded-demand-following-5-10-5@1` | `stable-low-load-parallel-operation@1` | Guided | Assisted → Assisted |
| INT-18 | checkpoint/replay continuation | `bounded-demand-following-5-10-5@1` | `stable-low-load-parallel-operation@1` | ChecklistOnly | SupervisoryAutomatic → SupervisoryAutomatic |
| INT-19 | terminal mission with plant continuing | `bounded-demand-following-5-10-5@1` | `stable-low-load-parallel-operation@1` | Hidden | Manual → Manual |

##### Why INT-12 is validation-only

The existing product catalog does not contain a dedicated scenario that invalidates exactly one measurement required by an active `HoldOperatingPoint` supervisory objective while keeping the rest of the row controlled. M10.9.8.1 therefore freezes a **validation-only composition ID**, `m1098-supervisory-required-measurement-unavailable@1`.

M10.9.8.3 may realize that row only by composing the already-existing authoritative exact-v4 profile, measured-signal/fault seam and M5 supervisory coordinator in test/audit code. It must not register a new production scenario or fault type merely to satisfy the validation matrix.

#### Execution ownership

M10.9.8.2 owns the nine healthy HAA rows. M10.9.8.3 owns degraded/fault/protection/takeover rows. M10.9.8.4 owns full replay/checkpoint integrity for representative healthy and degraded rows. M10.9.8.5 owns the end-to-end manual HMI acceptance and M10 closure.

If a row fails, classify before changing code:

- presentation/usability → App/HMI presentation owner;
- recorder/replay/checkpoint → M9.1/M10.7 owner;
- challenge/demand/scoring → M10.9.6 owner;
- plant control authority/takeover/degradation → M5 owner;
- protection → M5 protection owner;
- existing fault behavior → M8 fault framework plus physical owner;
- newly discovered physical-model limitation → post-M11 engineering backlog unless it invalidates supported operation;
- Phase H/I numerics → reopen only with direct evidence against an already validated numerical contract.

#### Seed contract

The selected rows use deterministic non-RNG owners. `deterministicSeed` is therefore `null` in matrix schema v1. This is explicit, not omitted evidence. A seeded row may be added only through a deliberate versioned matrix revision.

#### M10.9.8.1 exit gate

Run:

```bat
dotnet build
dotnet test
scripts\run-m10981-integrated-validation-matrix-audit.cmd
```

Then review `docs\M10_9_8_1_MATRIX_ACCEPTANCE_CHECKLIST.md`.

This exit gate is satisfied. M10.9.8.2 may execute only the accepted frozen HAA-01..HAA-09 contract; changes to the matrix require an explicit versioned revision rather than silent repair.

---

## Source snapshot — `M10_9_8_2_AUTOMATED_HEALTHY_ASSISTANCE_AUTHORITY_MATRIX.md`

### M10.9.8.2 Hotfix 1 — Automated Healthy Assistance × Authority Matrix

**REV5 interactive-list stability:** REV4 retains the legacy-Windows-PowerShell SHA-256 compatibility repair. REV5 additionally closes residual refresh flicker observed in F4 `DEPENDENCY CHAIN — SELECT A STEP` and audits collection-backed surfaces declared both in XAML and in code. The four selectable `ListBox` bindings are explicitly inventoried, the five programmatic `ControlRoomSelector`/`ComboBox` instances no longer reset their options on unrelated state refresh, equivalent dependency/checkpoint lists preserve collection and selection identity, and unchanged MISSION timeline rows are not replaced on scalar-only mission refreshes.

#### Status

**CANDIDATE / NOT VALIDATED.** The original M10.9.8.2 candidate did not compile because `M10982HealthyAssistanceAuthorityMatrixTests` referenced `SupervisoryObjectiveRequest` without importing its Application automation namespace. During the same validation cycle, live HMI use exposed two pre-existing F4 command-console defects and a historical mission-runtime mismatch. Hotfix 1 closes these together so the integrated matrix is exercised on the production runtime that M10.9.8 is intended to validate.

#### Historical mission identity is preserved

`bounded-demand-following-5-10-5@1` is not edited or reinterpreted. It remains bound to `power-manoeuvring-normal-shutdown` / `stable-low-load-parallel-operation@1` for exact replay/archive compatibility. Live evidence showed that historical seed can reach the old unsupported `control-out` water/steam state region around logical step 610–615.

Hotfix 1 adds **`bounded-demand-following-5-10-5@2`**. Challenge conditions, logical-time window, external demand `bounded-demand-5-10-5@1`, score policy `demand-following@1` and score-evidence bindings are preserved; only the scenario binding moves to the already-qualified production identity:

`integrated-normal-operations-training-i5-repaired-v4-production | integrated-operations-desktop-stable@4 | CorrelationConsistentInverseDomain | FourNodeBranchContinuityCorrectedCommitOptIn | 10 ms`

A dedicated regression runs that mission binding through 1,000 continuous logical steps, beyond the reported historical failure region. The accepted M10.9.8.1 matrix JSON remains frozen and unchanged; `eng/m1098-integrated-human-automation-hmi-matrix-v2.json` is the versioned execution revision for HAA-01..HAA-09 and the bounded-demand continuation rows INT-17..INT-19.

#### Healthy 3×3 matrix

All nine HAA rows execute the same representative sequence under `Hidden | ChecklistOnly | Guided × Manual | Assisted | SupervisoryAutomatic`. Supervisory rows explicitly request `HoldCurrentOperatingPoint`. Each row requires requested authority = effective authority with Normal health, no canonical trip, four accepted actions, full-replay final-fingerprint equivalence **and checkpoint-prefix → live-continuation equivalence**. Assistance-only changes must remain physically neutral within each authority mode.

The HAA gate executes an **active bounded-demand control-axis phase**. The inherited `Window(4_000, 8_000)` values are target-completion offsets measured from `ActivatedLogicalStep`; they are observational timing metadata and do **not** delay challenge activation. On the healthy exact-v4 baseline, `demand:stable-low-load-start` is satisfied, the tracker activates canonically and `bounded-demand-5-10-5@1` evidence is available. The HAA test therefore requires active external-demand evidence, preserves the +4000/+8000 target-window offsets, and keeps `GRID DEMAND / REQUESTED LOAD / ACTUAL OUTPUT` as separate owner fields under M10.9.6.2. The focused gate reruns both M10.9.6.1 lifecycle timing and M10.9.6.2 demand projection owners. The 1,000-step production mission regression separately crosses the reported STEP 610–615 failure region.

#### F4 COMMANDS robustness

The contextual command list no longer replaces its `ItemsSource` on every presentation refresh. The ViewModel keeps the same collection/selection objects while entry identity, command and availability remain equivalent; dynamic `CurrentState`/blocking detail is read from the newest snapshot without rebuilding the list. This removes the 20 Hz hover/selection churn seen while RUN is active.

ENTER is no longer an Avalonia `KeyBinding` attached to the `ListBox`. `CommandCatalog_KeyDown` dispatches only `Key.Enter` and sets `Handled=true`, preventing propagation. Canonical expected command rejections (`InvalidOperationException`, `ArgumentException` including `ArgumentOutOfRangeException`, `KeyNotFoundException`, arithmetic failures) are converted to operator-visible `BLOCKED BY RUNTIME/SCENARIO`; programming/unknown failures remain unhandled by design.

##### Interactive list refresh audit

The App has 29 collection-backed control instances in this audit: 24 XAML `ItemsSource` controls (four selectable `ListBox` and twenty `ItemsControl`) plus five `ControlRoomSelector` instances backed by one programmatic `ComboBox` implementation. The four selectable lists are `Workspaces`, `CommandEntries`, `SelectedCommandDependencySteps` and `SessionCheckpoints`; the target selectors are `PUMP TARGET`, `ADMISSION TRAIN`, `GENERATOR TARGET`, `ROD TARGET` and `ALARM TARGET`.

REV5 stops `ControlRoomSelector.UpdateVisuals()` from reassigning `ComboBox.ItemsSource` on unrelated state/selection visual refreshes: the parsed option sequence is cached and replaced only on semantic option change. Nineteen `ItemsControl` instances are read-only: eighteen are intentionally dynamic plant/alarm/history telemetry surfaces, while MISSION `ScoreDimensions` is semantically stabilized when its canonical sequence is unchanged. The only interactive `ItemsControl` is the MISSION timeline because rows may expose drill-down buttons. REV5 suppresses replacement notifications for unchanged timeline, score-dimension and recent-event sequences even when scalar mission fields such as logical step advance.

See `M10_9_8_2_REV5_INTERACTIVE_LIST_STABILITY_AUDIT.md`.

#### Validation

Run:

```bat
dotnet build
dotnet test
scripts\run-m10982-healthy-assistance-authority-matrix-audit.cmd
```

Then smoke-check manually:

```bat
dotnet run --project src\NuclearReactorSimulator.App\NuclearReactorSimulator.App.csproj -- --mission-pack=bounded-demand-following-5-10-5@2
```

Let RUN pass STEP 1000 without a `control-out` envelope failure. In F4 COMMANDS, keep the pointer over different command entries and over rows near the bottom of `DEPENDENCY CHAIN — SELECT A STEP` while RUN advances and verify no continuous flicker or selection reset. If F8 checkpoints exist, keep a non-first checkpoint selected during refresh. Exercise the available plant target selectors (`PUMP TARGET`, `ADMISSION TRAIN`, `GENERATOR TARGET`, `ROD TARGET`, `ALARM TARGET`) while state refreshes and verify their dropdowns do not repeatedly rebuild or move selection. In MISSION, hover a timeline drill-down button while logical steps advance without new timeline evidence. Finally select an AVAILABLE F4 command and press ENTER, confirming the application stays open and the console reports dispatch/rejection status.

---

## Source snapshot — `M10_9_8_2_HOTFIX1_MANUAL_SMOKE_CHECKLIST.md`

### M10.9.8.2 Hotfix 1 REV5 — Manual smoke checklist

This is a narrow promotion gate for the live mission/F4/list-stability defects reported during M10.9.8.2 validation. It does not replace the broader M10.9.8.5 end-to-end HMI acceptance.

- [ ] Build, complete ordinary suite and `scripts\run-m10982-healthy-assistance-authority-matrix-audit.cmd` are green.
- [ ] Start `--mission-pack=bounded-demand-following-5-10-5@2`; confirm MISSION shows the exact @2 pack and the session runs past STEP 1000 without the historical `control-out` water/steam envelope failure around STEP 610–615.
- [ ] Open COMPUTER → F4 COMMANDS while RUN is active. Keep the mouse over several command-catalog rows for at least 10 seconds; there is no continuous hover/selection flicker caused by snapshot refresh.
- [ ] In `DEPENDENCY CHAIN — SELECT A STEP`, select a row near the bottom and keep the mouse stationary over several rows for at least 10 seconds while RUN advances; the list and selected step remain visually stable unless the selected command itself changes.
- [ ] If F8 SESSION contains checkpoints, select a non-first checkpoint and leave it selected while normal snapshot/session refresh occurs; selection does not jump or flicker unless the checkpoint collection actually changes.
- [ ] Exercise each available target selector (`PUMP TARGET`, `ADMISSION TRAIN`, `GENERATOR TARGET`, `ROD TARGET`, `ALARM TARGET`) while RUN/state refresh continues; opening/hovering the dropdown does not cause repeated option-list rebuild, collapse or selection jump when the options themselves have not changed.
- [ ] In MISSION, when a timeline row exposes a drill-down button, hover the button while logical steps advance without new timeline evidence; the button/container does not continuously disappear/reappear.
- [ ] Select an AVAILABLE F4 command and press ENTER. The application remains open and COMMANDS reports `DISPATCHED` or an explicit `BLOCKED BY RUNTIME/SCENARIO` result.
- [ ] Repeat ENTER on a command that is currently blocked/unavailable; the app remains open and no plant state is mutated by the presentation layer.
- [ ] Mouse click on `EXECUTE [ENTER]` still uses the same canonical command boundary.
- [ ] F1–F8 navigation remains intact; no F9 is introduced.

Promotion acceptance text:

`M10.9.8.2 Hotfix 1 REV5 manual mission/F4/list-stability smoke validation OK`

---

## Source snapshot — `M10_9_8_3_DEGRADED_FAULT_PROTECTION_TAKEOVER_MATRIX.md`

### M10.9.8.3 — Degraded Measurement / Fault / Protection / Takeover Matrix

#### Status

**CANDIDATE / NOT VALIDATED.** Stacked exclusively on **M10.9.8.2 Hotfix 1 REV5 VALIDATED**. This milestone adds automated integration/evidence only: no production runtime source, Simulation physics/coefficient, challenge/scoring/protection owner, archive schema, fingerprint algorithm, plant-command authority, production scenario registration or new fault type.

#### Purpose

M10.9.8.3 realizes the degraded/fault/protection/takeover portion of the frozen M10.9.8 validation plan. The execution contract is `eng/m10983-degraded-fault-protection-takeover-matrix.json` and contains exactly eleven rows `DFP-01..DFP-11`:

1. invalid required supervisory measurement;
2. suspect/unavailable measurement operator truth;
3. protection active before a normal command;
4. protection trip during automated/assisted operation;
5. component fault;
6. instrumentation fault;
7. command rejected by a real permissive/interlock;
8. requested SupervisoryAutomatic degrading to effective Assisted;
9. operator manual takeover;
10. supported recovery after a declared degradation clears;
11. challenge active while degraded/protection truth is present.

#### Validation-only composition

DFP-01/08/10/11 use a test-local exact-v4 scenario/pack. It reuses the authoritative `integrated-operations-desktop-stable@4` runtime, the existing M8.3 `instrumentation.sensor-unavailable` fault seam, and the existing bounded-demand challenge/scoring/evaluator contracts. The local fault makes the canonical `power` measured signal unavailable at logical step 2 and clears it at step 5. Nothing is registered into the production scenario/challenge catalog.

The evidence must show:

- requested `SupervisoryAutomatic` remains the operator request;
- effective authority becomes `Assisted` with `Degraded` health while the required measurement is invalid;
- no true-state fallback is fabricated;
- MISSION publishes the same requested/effective/degradation truth and remains observational;
- external demand remains challenge-owned and available while the non-protection measurement degradation is active;
- after the declared fault clears, recovery occurs only through the canonical M5 supervisory coordinator and the valid measured frame;
- canonical protection reset remains separately owned by `ProtectionSystemSolverTests`: reset is accepted only after the safe-threshold and permissive conditions are satisfied.

#### Protection precedence

A separate exact-v4 production-bound challenge row requests `SupervisoryAutomatic`, then invokes the canonical reactor SCRAM action. A later normal rod-withdraw command cannot clear or bypass protection. The expected authority state is requested `SupervisoryAutomatic`, effective `Assisted`, health `SuspendedByProtection`; the bounded-demand challenge may transition to `Failed` because unexpected trip is an authored challenge failure condition, but neither challenge nor assistance owns the trip.

#### Fault and permissive owners

M8.2 hydraulic and M8.3 instrumentation tests are rerun directly. The M4.5 generator close-check owner is rerun at Simulation level to prove an unsynchronized breaker close is rejected by the real canonical permissive and leaves no electrical-load torque. M10.9.5.4 observed-response evidence is rerun so a rejection remains feedback rather than a fictional plant-effect delta.

#### Replay ownership boundary

M10.9.8.3 deliberately does **not** claim replay/checkpoint equivalence for every degraded row. That matrix is M10.9.8.4 ownership. This milestone freezes deterministic, owner-correct degraded state and authority transitions that M10.9.8.4 must replay.

#### Validation

Run:

```bat
dotnet build
dotnet test
scripts\run-m10983-degraded-fault-protection-takeover-audit.cmd
```

Promotion requires both artifact markers:

- `m10983-integration-composition-passes=True`
- `m10983-degraded-fault-protection-takeover-passes=True`

No separate manual HMI gate is introduced here; the end-to-end degraded/fault/protection HMI acceptance remains M10.9.8.5 ownership.

---

## Source snapshot — `M10_9_8_4_REPLAY_CHECKPOINT_SAME_SEED_INTEGRITY.md`

### M10.9.8.4 — Replay / Checkpoint / Same-Seed Integrity

#### Scope

M10.9.8.4 is an automated integration/evidence milestone stacked on M10.9.8.3 VALIDATED. It does not add production runtime behavior, Simulation physics, new fault/protection/scoring ownership, archive schema fields, fingerprint algorithms or plant-command authority.

Its purpose is to prove that representative M10 integrated states reconstruct identically through the canonical M9.1/M10.7 recorder/replay/checkpoint seams and through the M10.9.6.5 challenge replay projector.


#### Hotfix 1 — protection/authority observation boundary

The original M10.9.8.4 candidate was not validated: the protection row checked `SuspendedByProtection` immediately after the step that committed the reactor SCRAM. The canonical authority owner observes committed protection on the following deterministic tick. Hotfix 1 therefore captures the protection checkpoint only after that next tick, matching the already validated M5 authority integration and M10.9.8.3 protection-precedence contracts. No production runtime or protection semantics are changed.

#### Same-seed meaning

The simulator has no runtime pseudo-random state that needs a new serialized seed. For this milestone, same-seed means:

- the same exact versioned scenario and initial-condition identity;
- the same accepted operator-action trace;
- the same accepted M5 automation-intent trace;
- fresh independently loaded sessions.

Those inputs must produce the same ordered recorder-frame fingerprints, events, operator actions, automation intents, checkpoints and final challenge replay fingerprint.

No RNG field, opaque physical state blob or opaque challenge checkpoint state is introduced.

#### Integrity rows

`eng/m10984-replay-checkpoint-same-seed-integrity-matrix.json` freezes four representative state classes:

- **RCI-01** — healthy bounded-demand SupervisoryAutomatic operation;
- **RCI-02** — required measurement unavailable, requested SupervisoryAutomatic degraded to effective Assisted, followed by deterministic recovery;
- **RCI-03** — canonical reactor SCRAM with SupervisoryAutomatic suspended by protection;
- **RCI-04** — manual takeover from healthy SupervisoryAutomatic operation with stale supervisory objective cleared.

For every row the gate requires:

1. fresh same-seed repeat equivalence;
2. full replay equivalence through `ScenarioFullReplayRunner`;
3. replay-backed checkpoint prefix restoration followed by the identical live continuation;
4. equivalent M10.9.6.5 challenge replay projection fingerprint.

#### Ownership preserved

- every-step plant replay fingerprint: M9.1/M10.7 recorder/replay owner;
- archive/checkpoint schema: M10.7 schema v1;
- authority/objective intent replay: M5 session seam plus M10.7 recorder;
- lifecycle/demand/score reconstruction: M10.9.6.5;
- degraded/fault/protection/takeover semantics: M10.9.8.3 owners;
- operator-visible manual acceptance: M10.9.8.5.

M10.9.8.4 does not reinterpret any of these contracts.

#### Validation

Run:

```bat
dotnet build
dotnet test
scripts\run-m10984-replay-checkpoint-same-seed-integrity-audit.cmd
```

Promotion requires:

```text
m10984-replay-checkpoint-same-seed-integrity-passes=True
```

There is no separate manual gate for M10.9.8.4. Manual integrated HMI/keyboard/session acceptance remains M10.9.8.5.
