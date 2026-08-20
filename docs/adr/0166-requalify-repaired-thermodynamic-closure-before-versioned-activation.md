# ADR-0166 — Requalify the repaired thermodynamic closure before versioned activation

## Status

Accepted and validated through M10.9.4.1-I.5 REV1 Hotfix 16.2.

## Context

Hotfix 10 validated an opt-in `CorrelationConsistentInverseDomain` repair for two internal inverse-map defect families in the historical simplified water/steam model. The repair intentionally removes the saturated-vapor/superheated gap/overlap topology that H.12-H.19 previously diagnosed and compensated through branch-continuity policy. It also makes low-temperature saturated inversion interval-aware.

The repaired closure completes the frozen 7000-step operational load journey under both explicit and corrected hydraulics, but this proves topology and reachability only. It does not prove that the historical H.20-H.30 corrected-commit authority chain remains safe, deterministic and necessary under the changed thermodynamic topology. Mechanically rerunning old H.12-H.19 acceptance counts would also be invalid because some old counts describe the defect that has now intentionally been removed.

## Decision

Use staged requalification before creating any new registered exact-version identity.

Stage 1 applies the already validated H.29 1024-interval control pattern to the Hotfix 10 repaired-closure evidence seam under both explicit and corrected hydraulics. It must preserve no-trip operation, conservation, deterministic repeat and fail-closed corrected-commit safety. Trigger, commit, branch-override, previous-phase-hold and hysteresis-release counts are recorded as classification evidence rather than frozen historical floors.

If Stage 1 is green, its observed activity determines the next long-horizon/cross-profile H.17-H.19/H.24 repair requalification. A later activation candidate may introduce a new exact repaired desktop identity only after replay/protection/off-design/performance and scheduled-long gates are green. Historical exact desktop `@2` and `@3` identities are never reinterpreted.

## Consequences

- No production selector or registered exact-version identity changes in Hotfix 11.
- No historical H.12-H.19 overlap count is treated as a required property of the repaired model.
- H.20/H.22 fail-closed safety remains mandatory wherever corrected commits are exercised.
- Branch-continuity/hysteresis machinery may be retained, narrowed or retired only from repaired-closure evidence, never by assumption.
- Cumulative Phase-I closure remains blocked until the full repaired requalification matrix is green.

## Validated outcome

Stage 1 passed the 1024-interval control with fail-closed corrected ownership and deterministic conservation. Stage 2 passed 30,000 intervals across four H.19/H.24 profiles with 58/58 trigger/commit, zero rollback/fallback/unsafe/disagreement and deterministic repeat; branch overrides fell to zero while previous-phase hysteresis remained materially exercised. Stage 3 passed replay/checkpoint/reverse-power protection and the six-scenario H.27 off-design matrix. Stage 4 passed the original H.28 relative ceilings and a 1536-step soak. Exact `@4` was then registered, readiness-tested and activated as authoritative production without reinterpreting exact `@2/@3`.
