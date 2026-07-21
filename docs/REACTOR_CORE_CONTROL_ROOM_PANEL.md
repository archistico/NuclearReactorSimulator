# Reactor/Core Control-Room Panel

M6.3 composes the validated M6.1 shell and M6.2 component library into the first domain-specific operator workspace.

## Presentation boundary

Avalonia binds only to `ReactorCorePanelSnapshot`, `ReactorCoreZonePresentationSnapshot`, `ReactorRodPresentationSnapshot` and other Application-layer presentation records. The App project still has no direct Simulation project reference and no view/view-model traverses `FullPlantSnapshot`, `PlantState` or reactor physics types.

`ControlRoomSnapshotProjector` is the boundary that translates the validated M5.7 immutable automatic-operation snapshot into UI-safe presentation data.

## Measured instruments versus model diagnostics

M6.3 deliberately distinguishes two classes of displayed data:

- **measured instruments**: reactor thermal power is selected from the M5.1 measured-signal frame by semantic source id, preserving signal validity/quality and becoming `Unavailable` when no valid channel exists;
- **model diagnostics**: reactor period, total/rod/non-rod reactivity, rod positions and aggregated core-zone diagnostics are projected in Application and explicitly labelled as diagnostics. They are informational and are never used as hidden UI-side protection/control inputs.

No warning/trip physical thresholds are calculated in Avalonia. Instrument warning state may reflect already supplied signal quality/range semantics; trip/interlock state comes from M5.5 protection presentation.

## Reactor/core content

The workspace contains:

- measured reactor thermal power;
- reactor period and reactivity diagnostics;
- committed rod-reactivity and explicit non-rod-reactivity diagnostics;
- average rod withdrawal and canonical per-rod state;
- coarse aggregated core-zone tiles with fission power, zone power fraction, fuel/coolant temperature and void diagnostics;
- reactor SCRAM and rod-withdrawal-interlock status;
- canonical rod target selection and insert/hold/withdraw command intents;
- SCRAM and protection-reset command intents.

## Operator command boundary

M6.3 extends `ControlRoomCommandKind` with reactor-focused intents. A targeted rod command carries the canonical target id, explicit rod-versus-group target kind and operator intent. Avalonia never changes rod state, reactivity, kinetics or protection state directly.

The current shell dispatcher still records commands only. Until a runtime coordinator is connected, runtime-dependent reactor controls are `Unavailable` and therefore disabled fail-closed.

## Xenon diagnostic honesty

The M2.8 iodine/xenon model is validated, but its state is not currently promoted into the M5.7 automatic-operation snapshot envelope. M6.3 therefore displays xenon reactivity as `Unavailable` with an explicit explanation instead of reconstructing or inventing a value in the UI/application layer.

A later runtime-envelope milestone may promote the validated xenon state through an explicit immutable presentation seam. That change must not be implemented as hidden UI physics.

## Non-goals

M6.3 does not add:

- primary-circuit process mnemonics are provided by the validated M6.4 baseline;
- turbine/generator/electrical production panels (M6.5);
- trends, full annunciator matrix or event timeline (M6.6);
- runtime command execution/session orchestration (M6.7/M7 boundaries);
- new neutronics, xenon, rod-worth or protection physics.
