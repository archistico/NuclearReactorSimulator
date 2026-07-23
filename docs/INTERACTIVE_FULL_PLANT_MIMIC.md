# Interactive Full-Plant Mimic

M10.9.3 establishes the PLANT workspace as the primary whole-plant situation-awareness surface.

## Purpose

The operator should be able to answer quickly:

- where energy and mass are moving;
- what enters and leaves each major system;
- which major equipment is healthy, warning, tripped or unavailable;
- which subsystem to open next for detailed action.

## Data path

```text
canonical M2/M3/M4/M5 state
        ↓ existing validated projection
ControlRoomSnapshot
        ↓ M10.9.3 presentation composition
ControlRoomPlantMimicSnapshot
        ↓ render/select only
ControlRoomPlantMimicControl
```

No reverse control path exists through the mimic itself. Selection and drill-down are navigation semantics only. Equipment cards are focusable: pointer selection and keyboard focus both select presentation context, while `OPEN SUBSYSTEM` remains a separate navigation command.

## Presentation contracts

`ControlRoomPlantMimicSnapshot` contains:

- equipment elements;
- directed connections.

An equipment element carries identity, kind, normalized layout bounds, state, status, key values, explicit input/output text, context and destination workspace.

A connection carries endpoints, medium/energy class, state, display evidence, route geometry and label position.

Normalized layout is part of Application presentation semantics so Avalonia does not reconstruct process topology from IDs or hidden conventions.

## High-level versus subsystem detail

The whole-plant mimic deliberately aggregates the plant into eight recognizable macro groups. This avoids repeating the dense card/list problem that motivated the M10.9 refactor.

M10.9.4 owns detailed engineering schematics for:

- reactor/core dependencies;
- primary/steam drums;
- turbine/secondary;
- generator/grid;
- instrumentation/control/protection.

## Color semantics

Process medium color and operating severity are independent axes.

A steam line remains visually identifiable as steam even when a connected component enters warning/trip state. Warning/trip emphasis is added through state borders/accents; it does not redefine the physical medium.

## Selection semantics

Selecting equipment:

- highlights the equipment;
- emphasizes directly connected paths;
- updates selected-equipment key values, IN/OUT ports and context;
- enables deterministic navigation to an existing detailed workspace.

Selection never dispatches a plant command.

## Design references

The user-provided SVGs under `docs/reference/hmi/` remain visual/design references. They do not define authoritative plant topology or runtime values.
