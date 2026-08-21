# ADR-0180 — Index immutable plant topology once and version challenge observation changes

## Status

Accepted — M10.9.7.2 Hotfix 2 REV1 VALIDATED on 2026-08-21. Build, ordinary tests and the measured focused gate passed; the original Hotfix 2 candidate remains superseded/not validated because of its test-fixture condition-id defect.

## Context

Pre-live review identified two avoidable 10 ms-path costs: id getters in `PlantDefinition` / `PlantState` performed linear LINQ scans with capturing predicates, and `ScenarioChallengeTracker` built two ordered observation fingerprint strings per evaluation. A naive dictionary inside every `PlantState` would remove scans but increase construction allocations because plant states are repeatedly materialized by solvers.

## Decision

1. `PlantDefinition` owns immutable id-to-canonical-index dictionaries constructed once with the topology.
2. `PlantState` reuses those definition indexes to index its own canonically ordered state lists; it owns no per-instance lookup dictionary.
3. Public canonical ordered lists and unknown-id exception semantics remain unchanged.
4. Challenge observation change tracking uses a private monotonic version incremented when an immutable observation value changes. Public lifecycle semantics remain unchanged.
5. Immutable compressible-steam critical pressure ratio is precomputed at definition construction.
6. Promotion evidence measures allocation elimination and same-process relative wall cost against reference implementations equivalent to the replaced algorithms.

## Consequences

- Hot id lookup becomes O(1) without increasing every `PlantState` with dictionary allocations.
- Definition construction carries a small one-time index cost.
- Tracker evaluation no longer allocates observation fingerprint strings solely to detect changes.
- Canonical list order, replay determinism and challenge/event semantics remain owned by existing contracts.
- This optimization does not authorize UI activation or any solver/physics change.
