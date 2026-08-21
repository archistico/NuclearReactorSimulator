# Desktop Host Failure & Session Save Integrity Review

## Purpose

This document records the disposition of the post-M10.9.7.3 static review of `NuclearReactorSimulator.App`. It is a planning/architecture record only. It does not change the current M10.9.7.3 Hotfix 1 REV2 runtime candidate or its already-green automated gates.

Current-state authority remains `PROJECT.md`.

## Confirmed immediate risks

### 1. Deterministic-step failures can escape the Avalonia timer boundary

`DesktopControlRoomRuntimePump.Tick()` currently catches `InvalidOperationException`, pauses the coordinator and reports a failure. That correctly covers several reduced-model failures, but Simulation can also raise `ArithmeticException`/`OverflowException` from expected numerical fail-closed guards in flow/integration code. If those exceptions escape the `DispatcherTimer.Tick` callback, they can terminate the UI process instead of converting a recoverable plant-step failure into a paused/diagnosed desktop state.

The required behavior is:

```text
expected deterministic-step failure
    -> stop requesting further running batches
    -> request PAUSE through the existing coordinator/runtime boundary
    -> publish one clear operator-visible diagnostic
    -> do not create a repeated exception storm on subsequent UI ticks
    -> do not invent/commit a partial plant step
```

The implementation must classify only failures that are genuinely expected at the desktop host boundary. It must not become a blanket `catch (Exception)` that hides programming defects.

### 2. Existing session archives are vulnerable during overwrite

The current desktop save path opens the selected file, truncates it to zero length and then writes the newly serialized archive. A write failure after truncation can therefore destroy the previously valid archive.

For a save/overwrite feature, this is a data-integrity defect. The required desktop-filesystem behavior is:

```text
choose destination
    -> capture/export complete archive content
    -> write unique temporary sibling file
    -> flush and close temporary file successfully
    -> atomically replace/move into destination where the local filesystem supports it
    -> preserve the previous destination until replacement succeeds
    -> best-effort cleanup of the temporary file after any failure
```

If a storage provider cannot expose semantics sufficient for safe replacement, the App must fail closed with a truthful status rather than silently falling back to destructive truncate-first overwrite.

The picker must be shown **before** expensive archive export/serialization so a cancelled save performs no unnecessary full-session serialization and the click receives immediate UI feedback.

## Confirmed command-handler boundary inconsistencies

The desktop session handlers currently do not share one consistent failure policy:

- start-recorded-session has no equivalent protective boundary;
- reset/recreate session can surface construction/definition failures directly to Avalonia;
- load and restore eventually exercise overlapping archive/reconstruction logic but their catch contracts have diverged;
- save has a narrower failure contract than load/restore and currently owns destructive file-write details in code-behind.

M10.9.7.3 Hotfix 2 must centralize the **policy**, even if individual commands retain separate handlers. Equivalent failure categories should produce equivalent operator-visible behavior and must not crash the desktop process.

## Numeric presentation consistency

The current Application presentation contract formats engineering values with invariant decimal syntax, while a small number of App-layer gauge scale labels and COMPUTER setpoint strings use `CurrentCulture` implicitly. On an `it-IT` host the same instrument can therefore mix `1234.5` and `1234,5`.

For the current technical HMI contract, M10.9.7.3 Hotfix 2 should align those remaining App strings to invariant technical formatting. A future full localization policy may deliberately choose local numeric culture, but it must then apply coherently across values, scales, trends and computer text rather than mixing policies inside one instrument.

## Confirmed but deferred App/runtime work

### UI-thread coupling

The desktop host deliberately requests bounded fixed-step batches from a `DispatcherTimer`; it does **not** feed elapsed wall-clock time to the generic Simulation runtime. Therefore the generic fixed-timestep catch-up spiral is not reached by the current host.

However, Simulation work, projections and ViewModel publication currently run synchronously on the Avalonia UI thread. The resulting risk is responsiveness coupling rather than concurrent overlapping ticks:

```text
slow deterministic batch / projection
    -> UI thread occupied
    -> rendering/input/timer callback delayed
    -> simulated progress slows relative to wall-clock cadence
```

M11.3 must measure this before any threading change. A background worker may be introduced only with an explicit single-owner runtime contract and immutable snapshot handoff back to the UI. Moving current mutable/session work to `Task.Run` opportunistically is forbidden.

### MainWindowViewModel notification fan-out

`MainWindowViewModel.OnSnapshotChanged()` currently publishes a large, mostly unconditional `PropertyChanged` surface and owns many independent workflows. This is a measurable presentation-cost/refactor candidate, not an immediate correctness hotfix.

M11.3 owns before/after measurement of projection and notification cost. M13 owns structural decomposition into child/workflow/subsystem ViewModels so each presentation owner updates only its relevant surface.

### Stable selection identity

Several selection guards clamp an invalid selected index to another existing element. The current reference topology is effectively fixed, so this is not an active incident, but silently retargeting a future operator command is the wrong safety behavior if topology/list membership becomes dynamic.

M13 must move command-bearing selections toward stable canonical IDs:

```text
selected EquipmentId still exists -> retain selection
selected EquipmentId disappears  -> clear selection / disable command
                                -> require explicit operator reselection
```

Automatic retargeting to an adjacent element is forbidden for command-bearing selections.

### Simulation speed / pacing

The current desktop host uses a deliberate fixed number of deterministic steps per timer callback and does not expose generic Simulation `SimulationSpeed` as a desktop pacing control. M11.3 may measure/document the current pacing contract. Adding a user-facing simulation-speed feature is a product decision, not release-performance cleanup, and remains deferred unless separately approved.

## Planned immediate work: M10.9.7.3 Hotfix 2

Hotfix 2 is **not** to be stacked on the current REV2 candidate while manual HMI validation is pending. Sequence:

1. complete `M10_9_7_3_MANUAL_VALIDATION_CHECKLIST.md` on Hotfix 1 REV2;
2. promote Hotfix 1 REV2 only if manual evidence is green;
3. build Hotfix 2 exclusively on that validated REV2 baseline;
4. rerun build, ordinary suite, focused desktop-host/session-integrity gate and the affected manual save/failure checks;
5. only after Hotfix 2 validation may M10.9.7.4 begin.

### Hotfix 2 scope

1. classify/contain expected deterministic-step numerical failures at the desktop runtime-pump boundary;
2. pause/report once instead of terminating Avalonia or producing a timer exception storm;
3. regression coverage for `ArithmeticException`, including `OverflowException` inheritance where applicable;
4. protect start-recorded-session and reset/recreate-session UI boundaries;
5. align restore/load failure classification where they traverse equivalent archive/reconstruction contracts;
6. move destination selection before archive export;
7. extract filesystem save/replace behavior from click-handler code;
8. write to a temporary sibling and replace/move only after successful complete write/flush/close;
9. preserve the previous archive on injected write/flush/replace failure;
10. clean temporary artifacts best-effort and report truthful failure state;
11. fail closed where a provider cannot supply the required safe-replace semantics instead of destructive fallback;
12. align the remaining App engineering-number strings to the current invariant HMI formatting policy.

### Hotfix 2 non-scope

- no worker-thread Simulation migration;
- no change to fixed 10 ms physics or solver cadence;
- no new simulation-speed feature;
- no `MainWindowViewModel` decomposition;
- no reduction of the 114-notification surface without measurement;
- no stable-ID selection refactor yet;
- no archive schema change;
- no stream-based persistence API migration;
- no MISSION scoring/challenge/protection/plant-command authority change.

### Planned evidence

Focused gate stem:

`run-m10973-desktop-host-session-integrity-audit.cmd`

The gate should prove at least:

- expected numerical failure -> PAUSED + one diagnostic + no unhandled UI exception;
- unknown/programming exception is not silently swallowed;
- start/reset/load/restore handlers use the intended common failure classification;
- picker cancellation does not export/serialize an archive;
- overwrite success produces the new valid archive;
- injected write failure preserves the original destination byte-for-byte;
- injected replace failure preserves the original destination byte-for-byte;
- temporary file cleanup is attempted;
- unsupported atomic-provider path fails closed with truthful status;
- numeric scale/setpoint formatting follows the same invariant technical convention as canonical HMI values;
- MISSION remains presentation-only and F1-F8/no-F9 contracts remain green.

## Deferred owners

| Finding | Owner |
| --- | --- |
| UI-thread Simulation/projection responsiveness budget | M11.3 |
| PropertyChanged/projection fan-out measurement | M11.3 |
| immutable capture + optional off-thread archive serialization | M11.3, only after ownership is explicit |
| streaming/chunking/LOH persistence work | M11.3 |
| user-facing simulation-speed/pacing feature | deferred product decision; not implicit M11 optimization |
| `MainWindowViewModel` decomposition | M13 |
| stable-ID selection / no silent command retargeting | M13 |
| full localization/culture policy beyond current invariant technical HMI | M13 or later presentation-localization work |

## Decision summary

The App review does not reopen M10.9.7.3 Mission/Performance semantics. It does identify two pre-7.4 host-integrity blockers: expected numerical failures must not terminate the desktop host, and overwriting a session archive must not destroy the prior valid file before a new archive is safely committed. Those are isolated in M10.9.7.3 Hotfix 2 after REV2 manual validation; the larger reactivity/modularity work remains measured/deferred.
