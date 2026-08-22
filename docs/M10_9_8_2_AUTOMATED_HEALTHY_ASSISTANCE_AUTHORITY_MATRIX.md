# M10.9.8.2 Hotfix 1 — Automated Healthy Assistance × Authority Matrix

**REV5 interactive-list stability:** REV4 retains the legacy-Windows-PowerShell SHA-256 compatibility repair. REV5 additionally closes residual refresh flicker observed in F4 `DEPENDENCY CHAIN — SELECT A STEP` and audits collection-backed surfaces declared both in XAML and in code. The four selectable `ListBox` bindings are explicitly inventoried, the five programmatic `ControlRoomSelector`/`ComboBox` instances no longer reset their options on unrelated state refresh, equivalent dependency/checkpoint lists preserve collection and selection identity, and unchanged MISSION timeline rows are not replaced on scalar-only mission refreshes.

## Status

**CANDIDATE / NOT VALIDATED.** The original M10.9.8.2 candidate did not compile because `M10982HealthyAssistanceAuthorityMatrixTests` referenced `SupervisoryObjectiveRequest` without importing its Application automation namespace. During the same validation cycle, live HMI use exposed two pre-existing F4 command-console defects and a historical mission-runtime mismatch. Hotfix 1 closes these together so the integrated matrix is exercised on the production runtime that M10.9.8 is intended to validate.

## Historical mission identity is preserved

`bounded-demand-following-5-10-5@1` is not edited or reinterpreted. It remains bound to `power-manoeuvring-normal-shutdown` / `stable-low-load-parallel-operation@1` for exact replay/archive compatibility. Live evidence showed that historical seed can reach the old unsupported `control-out` water/steam state region around logical step 610–615.

Hotfix 1 adds **`bounded-demand-following-5-10-5@2`**. Challenge conditions, logical-time window, external demand `bounded-demand-5-10-5@1`, score policy `demand-following@1` and score-evidence bindings are preserved; only the scenario binding moves to the already-qualified production identity:

`integrated-normal-operations-training-i5-repaired-v4-production | integrated-operations-desktop-stable@4 | CorrelationConsistentInverseDomain | FourNodeBranchContinuityCorrectedCommitOptIn | 10 ms`

A dedicated regression runs that mission binding through 1,000 continuous logical steps, beyond the reported historical failure region. The accepted M10.9.8.1 matrix JSON remains frozen and unchanged; `eng/m1098-integrated-human-automation-hmi-matrix-v2.json` is the versioned execution revision for HAA-01..HAA-09 and the bounded-demand continuation rows INT-17..INT-19.

## Healthy 3×3 matrix

All nine HAA rows execute the same representative sequence under `Hidden | ChecklistOnly | Guided × Manual | Assisted | SupervisoryAutomatic`. Supervisory rows explicitly request `HoldCurrentOperatingPoint`. Each row requires requested authority = effective authority with Normal health, no canonical trip, four accepted actions, full-replay final-fingerprint equivalence **and checkpoint-prefix → live-continuation equivalence**. Assistance-only changes must remain physically neutral within each authority mode.

The HAA gate executes an **active bounded-demand control-axis phase**. The inherited `Window(4_000, 8_000)` values are target-completion offsets measured from `ActivatedLogicalStep`; they are observational timing metadata and do **not** delay challenge activation. On the healthy exact-v4 baseline, `demand:stable-low-load-start` is satisfied, the tracker activates canonically and `bounded-demand-5-10-5@1` evidence is available. The HAA test therefore requires active external-demand evidence, preserves the +4000/+8000 target-window offsets, and keeps `GRID DEMAND / REQUESTED LOAD / ACTUAL OUTPUT` as separate owner fields under M10.9.6.2. The focused gate reruns both M10.9.6.1 lifecycle timing and M10.9.6.2 demand projection owners. The 1,000-step production mission regression separately crosses the reported STEP 610–615 failure region.

## F4 COMMANDS robustness

The contextual command list no longer replaces its `ItemsSource` on every presentation refresh. The ViewModel keeps the same collection/selection objects while entry identity, command and availability remain equivalent; dynamic `CurrentState`/blocking detail is read from the newest snapshot without rebuilding the list. This removes the 20 Hz hover/selection churn seen while RUN is active.

ENTER is no longer an Avalonia `KeyBinding` attached to the `ListBox`. `CommandCatalog_KeyDown` dispatches only `Key.Enter` and sets `Handled=true`, preventing propagation. Canonical expected command rejections (`InvalidOperationException`, `ArgumentException` including `ArgumentOutOfRangeException`, `KeyNotFoundException`, arithmetic failures) are converted to operator-visible `BLOCKED BY RUNTIME/SCENARIO`; programming/unknown failures remain unhandled by design.

### Interactive list refresh audit

The App has 29 collection-backed control instances in this audit: 24 XAML `ItemsSource` controls (four selectable `ListBox` and twenty `ItemsControl`) plus five `ControlRoomSelector` instances backed by one programmatic `ComboBox` implementation. The four selectable lists are `Workspaces`, `CommandEntries`, `SelectedCommandDependencySteps` and `SessionCheckpoints`; the target selectors are `PUMP TARGET`, `ADMISSION TRAIN`, `GENERATOR TARGET`, `ROD TARGET` and `ALARM TARGET`.

REV5 stops `ControlRoomSelector.UpdateVisuals()` from reassigning `ComboBox.ItemsSource` on unrelated state/selection visual refreshes: the parsed option sequence is cached and replaced only on semantic option change. Nineteen `ItemsControl` instances are read-only: eighteen are intentionally dynamic plant/alarm/history telemetry surfaces, while MISSION `ScoreDimensions` is semantically stabilized when its canonical sequence is unchanged. The only interactive `ItemsControl` is the MISSION timeline because rows may expose drill-down buttons. REV5 suppresses replacement notifications for unchanged timeline, score-dimension and recent-event sequences even when scalar mission fields such as logical step advance.

See `M10_9_8_2_REV5_INTERACTIVE_LIST_STABILITY_AUDIT.md`.

## Validation

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
