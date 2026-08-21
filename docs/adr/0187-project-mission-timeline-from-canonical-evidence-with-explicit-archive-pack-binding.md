# ADR-0187 — Project mission timeline from canonical evidence with explicit archive pack binding

## Status

Proposed — implemented by the M10.9.7.4 candidate; becomes Accepted only after its automatic and manual validation gates pass.

## Context

The validated M10.9.7.3 MISSION workspace presents current objective, demand, safety and score state, but its combined bounded recent-event list is not sufficient for deterministic timeline/drill-down or archive-restored mission continuity. Dense operational events can evict lifecycle context, and session archive schema v1 deliberately persists canonical replay evidence rather than challenge-specific opaque state.

The existing snapshot fingerprint algorithm also depended on an implicit serialized presentation shape without a golden compatibility anchor.

## Decision

M10.9.7.4 will:

1. freeze `sha256-control-room-snapshot-v1` with a populated exact-version golden fixture already represented in frozen production-activation evidence;
2. retain a bounded protected lifecycle spine separately from bounded recent operational evidence;
3. merge those derived surfaces into a deterministic logical-step/source-sequence timeline;
4. expose presentation-only drill-down targets to existing workspaces/pages without command authority;
5. reconstruct challenge lifecycle/demand from the canonical verified archive/checkpoint recording prefix and then continue from future live evidence;
6. require an explicit exact pack binding for archive-restored MISSION state and fail closed on scenario/initial-condition mismatch;
7. leave an unbound archive mission-unbound rather than infer a pack from `ScenarioId`;
8. leave archive schema v1 unchanged and introduce no opaque challenge-state checkpoint payload.

For desktop usability, `START RECORDED SESSION` preserves an already explicit mission-pack binding; it does not invent one for an unbound session.

## Consequences

- Protection/scoring traffic cannot erase activation/terminal mission narrative from the timeline presentation.
- Replay/checkpoint mission continuity is derived from already verified canonical evidence.
- Fingerprint-visible shape drift becomes an explicit compatibility decision.
- Timeline navigation can take the operator to useful evidence without crossing plant-command boundaries.
- Archive v1 remains unable to self-identify a mission pack when loaded into an otherwise unbound runtime; future persistent mission-pack identity, if desired, requires an explicitly versioned persistence contract under M11.2.

## Rejected alternatives

- Reuse the combined last-100 `RecentEvents` list as the full timeline owner — rejected because dense evidence can erase mission lifecycle context.
- Persist an opaque challenge-state blob in checkpoints — rejected because M10.9.6.5 already established deterministic reconstruction from canonical recording evidence.
- Infer challenge pack from `ScenarioId` — rejected because multiple packs may legitimately share scenario identity and inference would create semantics not present in the archive.
- Silently redefine fingerprint v1 after snapshot-shape changes — rejected because it breaks replay/checkpoint compatibility without an explicit algorithm migration.
