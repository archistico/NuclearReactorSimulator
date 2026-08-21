# Domain Definition Invariant Closure — M10.9.7.2 Hotfix 1 REV1

## Purpose

M10.9.7.2 REV1 validated the Mission/Performance workstation placement decision without activating UI. Before the 10 ms live wiring work, a static review of `NuclearReactorSimulator.Domain` identified several construction-time invariant gaps. This hotfix closes only those gaps; it does not retune solver equations or introduce new plant behavior.

## Closed invariants

### Synchronous-generator synchronization windows

`SynchronousGeneratorDefinition` now requires:

- maximum frequency difference greater than zero;
- maximum phase difference strictly greater than zero and strictly less than 180 degrees;
- maximum voltage difference greater than zero and smaller than generator rated line voltage.

`GeneratorGridSystemDefinition` additionally requires each generator synchronization frequency and voltage window to be smaller than the nominal grid frequency and nominal grid line voltage respectively. These are non-degenerate relative bounds, not new operating setpoints. Existing reference-plant values remain unchanged at 0.2 Hz, 10 degrees and 10 kV.

### Defaultable positive quantity structs

C# permits `default(T)` for value types without invoking their validating private constructors. Therefore a definition that consumes a quantity whose semantic contract is strictly positive must re-check that invariant at the definition boundary.

This hotfix applies that rule to:

- `SteamDrumSteamSourceDefinition.HydraulicResistance`;
- `IodineXenonDefinition.IodineDecayConstant`;
- `IodineXenonDefinition.XenonDecayConstant`;
- optional `TurbineStageGroupDefinition.ExpansionResistance` when specified.

The quantity types themselves are not converted to reference types in this hotfix. That would be a wider public Domain migration and is unnecessary to close the observed failure paths.

### Canonical plant-state identity

`PlantState` documentation and diagnostics already require fluid-node and thermal-body states to use the plant's canonical definitions. The check now enforces reference identity with `ReferenceEquals`, rather than record value equality. A separately allocated but structurally equal definition is therefore rejected.

### Control-rod actuator target enum

`ActuatorDefinition.ControlRod` now rejects undefined `ControlRodCommandTargetKind` values at construction time, matching the fail-closed enum behavior already used by command-side consumers.

## Explicit non-scope

This hotfix does not:

- change reference-plant synchronization setpoints;
- add synchronization automation or breaker authority;
- change relief/bypass hysteresis;
- change PID algorithm semantics;
- change pressure quantity arithmetic;
- optimize Domain lookup registries;
- optimize `ObservationFingerprint()`;
- change turbine, hydraulic, iodine/xenon or electrical solver equations;
- activate the `MISSION` workspace;
- change scoring, challenge, protection or command authority.

The measured hot-path allocation/lookup work remains a separate pre-live follow-up after this hotfix validates.

## Validation

Promotion requires:

```bat
dotnet build
dotnet test
scripts\run-m10972-domain-definition-invariant-closure-audit.cmd
```

The focused gate exercises every newly closed construction boundary plus the grid-relative synchronization envelope and writes the M10.9.7.2 Hotfix 1 artifact summary.


## REV1 descriptor contract alignment

The first Hotfix 1 candidate was not validated because the ordinary Application test suite still carried the previous M10.9.7.2 REV1 descriptor expectation. Hotfix 1 REV1 leaves the Domain invariant implementation unchanged, aligns `ApplicationDescriptorTests` to the Hotfix 1 REV1 metadata, and includes that regression in the focused gate.
