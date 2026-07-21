# Reusable Instrument & Control Components

M6.2 establishes the reusable visual vocabulary used by the operator-control-room workspaces.

## Architecture boundary

```text
Application presentation contracts
        ↓
ControlRoomVisualState / component semantics
        ↓
Avalonia reusable controls
        ↓
workspace composition
```

The component library is presentation-only. It does not own simulation state, controller state, protection state or deterministic time.

## Shared semantic states

Every reusable component can represent one of four presentation states:

- `Normal` — expected/healthy operating presentation;
- `Warning` — degraded or approaching-limit presentation;
- `Trip` — protection/trip-significant presentation;
- `Unavailable` — unavailable/invalid presentation.

The visual state is semantic and supplied by presentation logic. Avalonia controls do not infer plant safety state from raw physical values.

## Reusable components

M6.2 introduces:

- `ControlRoomNumericIndicator` — read-only value/unit presentation;
- `ControlRoomLinearMeter` — read-only ranged indication;
- `ControlRoomIndicatorLamp` — discrete condition/status indication;
- `ControlRoomToggleSwitch` — two-position operator command primitive;
- `ControlRoomSelector` — multi-position operator selection primitive;
- `ControlRoomPushButton` — momentary command primitive.

The Application-layer `ControlRoomComponentCatalog` documents the stable semantic component kinds and interaction modes without referencing Avalonia types.

## Interaction rules

Display-only components do not accept operator state changes.

Interactive controls follow predictable desktop interaction semantics:

- Tab participates in focus navigation;
- Space toggles a focused toggle switch;
- arrow keys operate selector choices while focused;
- Enter/Space invokes a focused pushbutton;
- primary pointer activation performs the corresponding control action;
- `Unavailable` operator controls are disabled and do not accept hidden commands.

The controls themselves never execute simulation physics. Operational bindings route commands through the existing Application command seams.

## Component gallery

The M6.2 shell includes a component gallery that demonstrates:

- all four semantic states;
- numeric indication;
- ranged metering;
- discrete lamps;
- toggle interaction;
- selector interaction;
- pushbutton command dispatch.

The gallery is a visual-validation surface, not a plant-specific panel. Reactor/core composition begins in M6.3.

## Performance and determinism

Component rendering remains constrained by the M6.1 presentation budget. UI refresh, focus handling, animation or pointer cadence must never influence simulation timestep, control ordering or physical results.

## Explicit non-goals for M6.2

- no reactor/core panel topology;
- no primary/secondary process mnemonics;
- no trend renderer;
- no full annunciator matrix;
- no direct binding to `FullPlantSnapshot` or `PlantState`;
- no UI-side threshold/protection/controller calculations.

## M6.3 adoption

M6.2 is validated. M6.3 composes the reusable indicators, lamps, selectors and pushbuttons into the first domain-specific Reactor/Core workspace. The components still render supplied semantic presentation state only; reactor thresholds, kinetics, rod worth and protection logic remain outside Avalonia.
