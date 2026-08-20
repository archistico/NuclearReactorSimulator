# ADR-0167 — Activate repaired exact-v4 production while preserving historical exact-version semantics

## Status

Accepted — authoritative production activation validated in M10.9.4.1-I.5 REV1 Hotfix 16.2; final Phase-I cumulative closure remains pending Hotfix 17/17.1.

## Context

The historical desktop exact `@3` identity was validated under `HistoricalCorrelationTopology` plus `FourNodeBranchContinuityCorrectedCommitOptIn`. I.5 later proved that the historical water/steam inverse map contains a structural saturated-vapor/superheated gap/overlap and a separate low-temperature saturated inverse-search blind spot. `CorrelationConsistentInverseDomain` repaired both defect families and passed staged topology, operational, long-horizon, replay/protection, off-design and performance/soak requalification.

Silently changing the thermodynamic closure behind exact `@3` would break the project rule that exact-version scenario/save/replay identities are immutable. The independent synchronization version family also has its own validated exact `@3` corrected identity and must not be conflated with desktop numbering.

## Decision

- `integrated-operations-desktop-stable@4` is the authoritative desktop production identity. It uses `CorrelationConsistentInverseDomain`, `FourNodeBranchContinuityCorrectedCommitOptIn` and the unchanged 10 ms fixed step.
- Desktop exact `@3` remains immutable historical H.29/H.30/I.3 replay provenance using `HistoricalCorrelationTopology` plus corrected-commit ownership.
- Desktop exact `@2` remains immutable fail-closed rollback/reference using `HistoricalCorrelationTopology` plus `ExplicitCommittedState`.
- The `pre-synchronization-grid-loading` family remains independent; its supported corrected identity remains exact `@3`.
- Historical I.3 exact-v3 300 s evidence and its 19 budgets remain immutable provenance/acceptance authority. Final repaired-v4 reference qualification compares @4 against those unchanged budgets rather than rewriting I.3 as @4 evidence.
- No physical coefficient, H.9 tolerance, P060/F040 threshold, H.20/H.22 authority rule, previous-phase hysteresis bound or fixed timestep is changed by version activation.

## Consequences

Old exact-version saves and scenarios remain reproducible rather than being migrated implicitly to new thermodynamics. Current production benefits from the repaired inverse topology, while rollback and historical evidence remain available under their original semantics. Phase I may close only after current @4 passes the scheduled-long/reference/cumulative chain; failure of a final gate must be localized rather than addressed by retuning frozen budgets or rewriting historical versions.
