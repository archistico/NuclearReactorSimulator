# Mission / Performance deterministic timeline

M10.9.7.4 extends the validated read-only `MISSION` workspace with a bounded deterministic timeline and presentation-only drill-down. It does not create a second recorder, a second scoring owner or any plant-command authority.

## Evidence ownership

The timeline is projected only from canonical owners already validated before M10.9.7.4:

- challenge lifecycle transitions from `ScenarioChallengeTracker`;
- external-demand evidence from the M10.9.6 demand projector;
- operator actions, alarms, protection transitions and fault transitions from the canonical `ScenarioRecording` event stream when a recorder is present;
- score evidence/results from the M10.9.6 scoring owner.

Ordering is deterministic: logical step first, canonical source sequence second, then stable category/source/summary tie-breakers when an evidence family has no canonical sequence. Wall clock is not an ordering or scoring input.

## Two retention surfaces

M10.9.7.3 `RecentEvents` remains the compact at-a-glance presentation contract and is not reinterpreted.

M10.9.7.4 adds two separately bounded sources:

1. **Lifecycle spine** — at most 32 lifecycle transitions. When the full transition history is larger, the first activation boundary and latest terminal completion/failure/cancellation boundary are protected before newest lifecycle evidence fills the remaining capacity.
2. **Recent operational evidence** — at most 100 projected demand changes, operator actions, alarms/protection/fault evidence and current scoring context.

Dense operational evidence therefore cannot evict the activation/terminal narrative of the mission.

The public `Timeline` is the deterministic merge of those two surfaces. Retention is a presentation policy only; the canonical recorder remains unchanged.

## Drill-down contract

Timeline rows may carry an immutable `MissionPerformanceDrillDownTarget`. A target can select an already-existing main workspace and, only for COMPUTER, an already-existing page.

Current mappings include:

- demand change → `ELECTRICAL`;
- operator action → `COMPUTER / COMMANDS`;
- alarm/protection → `ALARMS / EVENTS`;
- fault evidence → `COMPUTER / DIAGNOSTICS`.

A drill-down changes presentation selection only. It owns no `ControlRoomCommandDispatcher`, protection authority or supervisory authority and cannot mutate plant state. Evidence without a target remains readable as evidence-only.

## Fingerprint-v1 compatibility anchor

Before adding archive-restored mission presentation, M10.9.7.4 freezes the existing fingerprint contract with a populated exact-version golden fixture:

- algorithm id: `sha256-control-room-snapshot-v1`;
- exact fixture: the retained H29 exact-version production-activation candidate after 128 deterministic steps;
- expected fingerprint: `63643e5506a6b99f8106950ecb25a5243e9755b3bc96bf2a60e96c219216f362`.

The fixture is structurally populated across reactor-core, primary-circuit, turbine/secondary and electrical presentation surfaces, but not every topology-dependent subcollection is non-empty. In particular, H29 has no valve endpoint in the projector's primary-node set, so `PrimaryCircuit.Valves` is intentionally empty; stop/control/admission are represented on the turbine/secondary side. That emptiness is itself covered by the frozen serialized v1 payload/golden hash and must not be "fixed" by changing production projection semantics.

That value already exists in frozen H22/H28/H29 evidence. A future fingerprint-visible shape/semantic change must either preserve the golden result or introduce an explicitly versioned new algorithm. Updating the golden value alone is not an acceptable compatibility fix.

## Archive and checkpoint restoration

Archive schema v1 is unchanged. It does **not** gain an implicit mission-pack field in M10.9.7.4.

A restored MISSION therefore requires an **explicit exact operational challenge pack binding** supplied by the caller. The binding must match both the archived scenario id and exact initial-condition reference; mismatch fails closed. No challenge pack is inferred from `ScenarioId`.

When an exact pack is supplied:

1. `ScenarioFullReplayRunner` first verifies/reconstructs the canonical archive or checkpoint prefix;
2. challenge lifecycle and demand evidence are reconstructed from that verified recording prefix using the M10.9.6 replay contract;
3. a continuation evidence source attaches to the already reconstructed live session only after snapshot fingerprint and operator-action history agree;
4. future deterministic steps/actions continue the same tracker without an opaque challenge-state checkpoint blob.

An archive loaded from an unbound session remains mission-unbound.

For the desktop manual flow, `START RECORDED SESSION` preserves the current exact mission pack when the current runtime is already explicitly bound. In an unbound runtime it preserves the historical unbound recorded-session behavior.

## Non-scope

M10.9.7.4 does not change:

- archive/checkpoint schema version;
- fingerprint algorithm implementation;
- challenge definitions or scoring arithmetic;
- protection or plant-command authority;
- Simulation physics or timestep;
- COMPUTER F1–F8 or the no-F9 contract;
- M10.9.7.3 `RecentEvents` semantics.

Fingerprint v2/multi-algorithm compatibility remains M11.2. Recorder memory/streaming/performance and notification-cost work remains M11.3.
