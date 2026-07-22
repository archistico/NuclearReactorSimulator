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

At the original validated M6.3/M7 v1 boundary, the M2.8 iodine/xenon model was not promoted into the automatic-operation snapshot, so xenon correctly displayed as `Unavailable` rather than being reconstructed in Application/UI.

M9.3 introduces an opt-in canonical promotion path for new versioned xenon-enabled runtime configurations: the M5 reactor/primary snapshot carries the immutable committed M2.8 poison diagnostic and `ControlRoomSnapshotProjector` exposes its xenon reactivity. Legacy exact-version configurations that do not own M2.8 poison state still display `Unavailable`. The UI never integrates iodine/xenon state and never infers xenon from power, time, rods or scenario metadata.

## Non-goals

M6.3 does not add:

- primary-circuit process mnemonics are provided by the validated M6.4 baseline;
- turbine/generator/electrical production panels (M6.5);
- trends, full annunciator matrix or event timeline (M6.6);
- runtime command execution/session orchestration (M6.7/M7 boundaries);
- new neutronics, xenon, rod-worth or protection physics.
