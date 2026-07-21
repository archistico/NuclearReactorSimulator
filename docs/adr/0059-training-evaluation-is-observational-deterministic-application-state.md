# ADR 0059 — Training evaluation is deterministic observational Application state

**Status:** Accepted / M7.7 validated

## Context

M7.1–M7.6 provide versioned sessions and validated operating procedures from cold shutdown through manoeuvring and normal shutdown. M7.7 needs objective scoring and procedure assistance without creating a new plant owner, depending on UI refresh cadence, or allowing training logic to force physical outcomes.

## Decision

1. Training checkpoints observe immutable `ControlRoomSnapshot` values only.
2. The Application runtime coordinator exposes a per-fixed-step observation event distinct from sparse presentation publication.
3. Operator-action history records only commands accepted by scenario gating and forwarded successfully; host run/pause/step controls are excluded.
4. Training checkpoint memory, scoring and guidance mode are non-physical Application state.
5. Guidance mode may change presentation assistance only; it cannot change scoring criteria, runtime inputs or simulation stepping.
6. Emergency actions remain fully available through validated protection/control seams. Training may score their use as a procedural deviation but cannot suppress or reinterpret their physical effect.
7. Evaluation criteria are deterministic functions of historical checkpoint achievement and accepted operator-action order.

## Consequences

The same exact initial condition and accepted command sequence yields the same training assessment independent of rendering cadence. M8 fault scenarios can later reuse this evaluation boundary while fault ownership remains explicit and separate.
