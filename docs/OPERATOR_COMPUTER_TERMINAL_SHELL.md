# Operator Computer Terminal Shell — M10.1

## Purpose

M10.1 creates the fixed navigation/presentation shell for the future Operator Computer while preserving all existing ownership boundaries.

```text
ControlRoomSnapshot
        ↓
OperatorComputerSnapshotProjector
        ↓
immutable OperatorComputerSnapshot
        ↓
OperatorComputerViewModel
        ↓
ControlRoomComputerControl
```

The projector is intentionally narrow: it exposes only shell/runtime status already present in `ControlRoomSnapshot`. It does not reach into Simulation or scenario-private state.

## Fixed page catalog

The terminal page set is versioned by code and fixed for M10:

```text
F1 GUIDANCE
F2 INFO
F3 ALARMS
F4 COMMANDS
F5 MODES
F6 DIAGNOSTICS
F7 LOG
F8 SESSION
```

No free-form prompt/parser exists.

## Presentation-only selection

`OperatorComputerViewModel.SelectedPage` is presentation state only. It is not recorded as physical state, does not affect deterministic simulation results and survives ordinary runtime snapshot refreshes.

Global F1–F8 bindings in `MainWindow` select the Operator Computer workspace and requested page. Mouse selection remains available through the terminal menu.

## Runtime status line

The M10.1 status line may show only already-published shell status:

- run state;
- logical step;
- annunciated/unacknowledged alarm counts;
- invalid measured-signal count;
- whether any canonical trip is active.

It must not synthesize physical measurements or future page-specific diagnostics.

## Page content state

Every page is currently marked `ShellOnly`.

This is intentional. M10.1 establishes contracts and interaction structure without pretending that M10.2–M10.7 content already exists.

## MainWindow layout authority

The final M9 manual GUI pass produced a user-corrected `MainWindow.axaml` that resolved the center-content overlap/clipping behavior. That file is integrated as the authoritative layout basis before M10.1 additions.

M10.1 must not reintroduce the discarded horizontal-scroll/min-width workaround.

## Next milestone

M10.2 will populate GUIDANCE, INFO and DIAGNOSTICS by adapting existing canonical M7/M6 contracts. It must not duplicate guidance/checklist logic or invent unpromoted plant values.
