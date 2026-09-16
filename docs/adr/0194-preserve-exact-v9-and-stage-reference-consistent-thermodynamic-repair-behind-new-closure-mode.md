# ADR-0194 — Preserve exact-v9 and stage physical-reference thermodynamic repair behind a new closure mode

## Status

Accepted after the returned VR2 Engineering Repair Planning 1 static audit completed `PASS-AS-AUTHORED`.

## Context

VR2 independently compared the authoritative `CorrelationConsistentInverseDomain` water/steam closure with IAPWS-IF97 and returned `MODEL-DISCREPANCY-BLOCKING`. The later exact-v9/P1B materiality diagnostic and returned-evidence adjudication classified the discrepancy `HYDRAULIC-MATERIALITY-CONFIRMED`.

The returned path evidence also exposes a phase-boundary mismatch: all 72 sampled `pressure` inventories are production `SubcooledLiquid` but resolve from the same `(v,u)` as IF97 Region-4 mixture, with the same reinterpretation in 55 of 72 `suction` samples.

Earlier ADR 0165–0167 already established that repaired thermodynamic semantics must be staged, requalified and activated under a new exact identity rather than silently rewriting historical exact versions.

## Decision

If a later repair is authorized:

- do not change the semantics of `WaterSteamThermodynamicClosureMode.CorrelationConsistentInverseDomain` in place;
- do not reinterpret `integrated-operations-desktop-stable@9` through new thermodynamic physics;
- introduce a new opt-in thermodynamic closure mode for the selected repair;
- keep that mode unregistered/unselected by authoritative production until focused reference, topology, deterministic, performance and operational requalification pass;
- introduce a new desktop exact-version identity only after repair qualification and a separate activation decision;
- retain exact-v9 and earlier exact versions as reproducible provenance under their original closure semantics;
- keep the independent IF97 implementation as a reference authority unless a separately reviewed production implementation is selected and qualified.

## Consequences

A successful repair will require a versioned requalification chain rather than a local coefficient edit behind exact-v9. Existing saves, recordings, fingerprints and historical evidence remain interpretable under the physics that produced them.

This ADR does not choose the repair mathematics and does not authorize a production source change. Repair-family selection remains owned by VR2 Engineering Repair Planning 1 RP1A–RP1C.
