# ADR-0184 — Anchor snapshot fingerprint v1 and separate mission lifecycle retention from recent evidence

## Status

Accepted for M10.9.7.4 planning; implementation pending M10.9.7.3 promotion.

## Context

M10.9.7.3 activates a live Mission/Performance workspace over bounded recent evidence. The subsequent M10.9.7.4 timeline must also prove replay/checkpoint equivalence. Two review findings affect that work directly:

1. `ControlRoomSnapshotFingerprint` v1 is version-labelled but lacks a sufficiently populated golden fixture that forces an explicit algorithm bump when the serialized fingerprint surface changes.
2. the live `RecentEvents` last-100 combined buffer may legitimately evict sparse mission lifecycle events under a high-volume protection/scoring stream, which is unsuitable for a mission timeline narrative.

## Decision

Before/archive-equivalence work in M10.9.7.4:

- add a populated golden `sha256-control-room-snapshot-v1` fixture with exact algorithm-id and expected-hash assertions;
- treat any intentional fingerprint-surface change as a new algorithm id, never a silent redefinition of v1;
- make M10.9.7.4 timeline projection own two separately bounded presentation sources: a protected mission lifecycle spine and recent operational evidence;
- merge those sources only at deterministic presentation ordering time;
- keep the timeline derived from canonical recorder/challenge evidence rather than turning it into a second recorder.

## Consequences

M10.9.7.4 gets stronger replay compatibility and cannot lose its mission narrative merely because recent protection evidence is dense. The current M10.9.7.3 at-a-glance `RecentEvents` contract remains valid and does not need a runtime hotfix before manual validation.

M11.2 owns future multi-algorithm compatibility if fingerprint v2 is ever introduced. M11.3 owns fingerprint/recorder performance, memory growth, collection-view and recorder-failure-policy work.

## Rejected alternatives

- silently updating a v1 expected hash after a DTO/presentation change;
- making objective events unbounded forever;
- using the current combined last-100 `RecentEvents` list as the full M10.9.7.4 timeline owner;
- truncating/decimating M9.1 recording v1 without a new versioned recording contract.
