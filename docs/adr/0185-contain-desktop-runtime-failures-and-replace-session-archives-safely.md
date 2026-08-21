# ADR 0185 — Contain desktop runtime failures and replace session archives safely before timeline expansion

## Status

Accepted.

## Context

The post-M10.9.7.3 App review identified two host-level integrity gaps that are independent of Mission/Performance scoring or presentation semantics:

1. `DesktopControlRoomRuntimePump` converts `InvalidOperationException` into PAUSE + diagnostic status but can still allow expected numerical `ArithmeticException`/`OverflowException` failures to escape the Avalonia timer callback;
2. session overwrite currently truncates the selected destination before the replacement archive has been written successfully, so a disk/write failure can destroy an older valid session file.

The same review found inconsistent UI-handler failure boundaries, mixed invariant/current-culture engineering formatting and longer-term UI-thread/ViewModel responsiveness debt.

## Decision

Before M10.9.7.4 begins, create a dedicated M10.9.7.3 Hotfix 2 on the **validated** Hotfix 1 REV2 baseline.

Hotfix 2 will:

- classify and contain expected deterministic-step numerical failures at the desktop runtime-pump boundary;
- pause and report once without hiding unrelated programming defects;
- align start/reset/load/restore failure boundaries;
- show the save destination picker before full archive export;
- save through a temporary sibling + successful close/flush + atomic/local-filesystem replace/move semantics;
- preserve the previous archive on write or replacement failure;
- fail closed rather than use destructive fallback when safe replacement semantics are unavailable;
- align remaining engineering-number presentation to the current invariant HMI convention.

Hotfix 2 will **not** move Simulation to a worker thread, change physics/timestep, add simulation-speed functionality, refactor the full `MainWindowViewModel`, change archive schema or introduce streaming persistence.

M11.3 owns measurement of UI-thread/runtime/projection cost, archive serialization/LOH and possible immutable-snapshot/off-thread or streaming work. M13 owns structural ViewModel decomposition and stable-ID selection semantics.

## Consequences

- M10.9.7.4 timeline/replay work cannot begin immediately after REV2 manual validation; Hotfix 2 must validate first.
- Session overwrite becomes a fail-safe data-integrity boundary rather than a truncate-first best effort.
- Expected numerical step failure becomes operator-visible paused state rather than an unhandled desktop exception.
- Broader responsiveness/threading work remains evidence-driven and cannot be smuggled into a correctness hotfix.
