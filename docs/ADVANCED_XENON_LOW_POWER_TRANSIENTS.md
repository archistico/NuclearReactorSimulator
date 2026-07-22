# Advanced Xenon & Low-Power Transients

## Purpose

M9.3 connects the validated reduced M2.8 I-135/Xe-135 model to the operational reactor-control envelope without creating a new physics owner.

The core rule is:

> scenarios may seed versioned poison history and observe its consequences, but only the M2.8 solver may evolve iodine/xenon state or produce xenon reactivity.

## Runtime ownership

`ReactorPrimaryControlSystemDefinition` may carry an `IodineXenonDefinition`. When absent, the reactor/primary runtime behaves as before M9.3 and publishes xenon as unavailable. When present, `ReactorPrimaryControlState` carries canonical `IodineXenonState` and `ReactorPrimaryControlSolver` owns the integration call into `IodineXenonSolver`.

For each fixed step:

1. committed rod worth is evaluated;
2. committed fission power and committed poison state produce the xenon reactivity used for this step;
3. xenon reactivity is added to the existing explicit external non-rod reactivity seam;
4. the resulting total reactivity advances the validated point-kinetics solver;
5. candidate neutron population determines candidate fission power;
6. those candidate power/flux values advance the M2.8 I/Xe inventories for the next committed state.

This matches the validated M2.8 committed-state staging model and introduces no same-step nonlinear iteration.

## Presentation boundary

`ReactorPrimaryControlSnapshot` carries optional committed and candidate M2.8 snapshots. `ControlRoomSnapshotProjector` reads only the committed xenon diagnostic that participated in the current-step reactivity calculation.

When no canonical poison definition is configured, `ReactorCore.XenonReactivity` remains `Unavailable`. Application and UI never infer it from current power, elapsed time, rods or any scenario metadata.

## Versioned initial conditions

M9.3 adds two new versioned initial conditions. Their I/Xe inventories are explicit state describing prior operating history, analogous to seeding rod position, neutron population, thermal condition or breaker state.

They are not scripted future trajectories.

### Post-Shutdown Xenon Restart Window v1

Seeds a post-shutdown poison-memory condition with circulation established and the turbine/grid isolated. The model itself determines whether xenon initially rises or falls as neutron population and fission power change.

### Poisoned Low-Power Operation v1

Seeds a low-power condition with non-zero iodine/xenon memory. The operator can use the existing rod/HOLD/SCRAM seams while observing canonical xenon worth and total reactivity.

## Exact-version compatibility

The existing M7 version-1 initial conditions are intentionally left xenon-disabled. Enabling a new physical state owner under an existing initial-condition version would change replay semantics without changing identity.

M9.3 therefore opts in only through new M9.3 initial-condition identities. Old scenarios retain their validated explicit-unavailable xenon boundary; new M9.3 scenarios expose canonical xenon state.

## Fidelity statement

The M2.8 model is reduced and configuration-relative. The built-in M9.3 parameters are educational plant configuration values chosen to make history-dependent poisoning observable in training timescales. They are not claimed to reproduce plant-specific RBMK isotope yields, cross sections, half-lives or historical transients.

Supported qualitative phenomena are limited to those already validated in M2.8:

- iodine buildup and decay;
- xenon production directly and from iodine decay;
- neutron-population-dependent xenon burnup;
- post-power-reduction/shutdown xenon rise where the state equations produce it;
- later xenon decay;
- signed xenon reactivity contribution.

Spatial xenon oscillations, samarium, detailed depletion and historical-event reconstruction remain outside M9.3.


## Validation status

M9.3 is **VALIDATED**. Local compilation and the complete automated suite passed after two test-only hotfixes. The hotfixes did not change production xenon physics, initial-condition/scenario semantics or replay/versioning ownership.
