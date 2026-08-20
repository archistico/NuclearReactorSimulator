# Operational challenge external energy demand

## Scope

M10.9.6.2 adds a deterministic **training/evaluation reference** for external electrical demand. It does not add physical grid dispatch, governor authority or automatic generator loading.

The semantic separation is authoritative:

```text
EXTERNAL GRID DEMAND / CHALLENGE TARGET
        !=
GENERATOR REQUESTED LOAD / OPERATOR SETPOINT
        !=
ACTUAL ELECTRICAL OUTPUT
```

A small demand/output error is therefore evidence about tracking quality only. It is never permission to mutate generator request and is not a success score by itself.

## Ownership

`ChallengeDefinition.ExternalDemandProfile` is optional. Demand is unavailable when:

- the challenge owns no profile; or
- the challenge has not yet activated.

A profile is versioned independently with `profileId@version` and is frozen by the versioned challenge definition that owns it.

## Logical-time model

All profile time is expressed as logical-step offset from `ChallengeLifecycleSnapshot.ActivatedLogicalStep`. Wall-clock time and publication cadence are absent from the contract.

The profile is a strictly ordered sequence of control points:

- logical-step offset;
- demand in MWe;
- interpolation to the next point: `HOLD` or `LINEAR`.

The final point must `HOLD` indefinitely. This single representation supports the initial primitives:

- constant target;
- step change;
- bounded ramp;
- piecewise hold/ramp sequence.

Each profile also declares minimum/maximum MWe bounds. Out-of-bound values, duplicate/non-increasing offsets and an invalid terminal interpolation fail closed.

## Future schedule visibility

`ExposeNextScheduledChange` is definition-owned. When true, the presentation/evaluation projection may expose the next authored control point as an **absolute logical step** and demand value. When false, future schedule fields remain unavailable.

This allows later challenge packs to decide explicitly whether the trainee sees the next demand change. No UI rule silently reveals it.

## Observation projection

`ScenarioChallengeExternalDemandProjector` is a pure read-only projection of:

- `ChallengeDefinition`;
- `ChallengeLifecycleSnapshot`;
- immutable `ControlRoomSnapshot`.

It publishes:

- current external demand;
- aggregate requested generator load when available;
- actual gross electrical output when available;
- external-demand minus actual-output error when actual output is available;
- optional next scheduled control point.

The projector owns no dispatcher, setpoint, torque, grid-coupling or supervisory-control seam.

## Replay and checkpoint semantics

Demand value is a pure function of exact profile identity, activation logical step and current logical step. Calling the projector at different publication cadences cannot alter the timeline. Replaying the same versioned challenge and logical trace therefore reconstructs the same demand evidence without serializing mutable demand state.

Full challenge/demand/score checkpoint integration is closed in M10.9.6.5.

## Non-scope

M10.9.6.2 does not introduce:

- scoring arithmetic;
- Mission/Performance UI;
- automatic generator load following;
- physical grid-dispatch coupling;
- supervisory automation;
- new physics, protection limits or exact-version behaviour.
