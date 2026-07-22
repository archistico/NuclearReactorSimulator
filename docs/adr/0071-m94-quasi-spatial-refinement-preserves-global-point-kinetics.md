# ADR 0071 — M9.4 Quasi-Spatial Refinement Preserves Global Point Kinetics

## Status

Accepted. M9.4 was subsequently locally validated after one test-compilation-only namespace hotfix; production ownership/physics semantics were unchanged.

## Context

M3.3 deliberately established configurable aggregated core zones while keeping the validated M2 point-kinetics model global. The zone state already carries normalized power shares and references canonical plant fuel/coolant domains, but the validated baseline does not automatically evolve power shape from local feedback.

M9.4 needs better spatial/quasi-spatial fidelity without introducing an unvalidated multi-node neutron solver, duplicate plant inventories or silent changes to existing exact-version scenarios/replays.

## Decision

1. Global `PointKineticsSolver` remains the sole neutron-kinetics owner.
2. M9.4 introduces optional `QuasiSpatialCoreFeedbackDefinition` over the existing canonical `AggregatedCoreDefinition`.
3. Existing validated linear fuel-temperature, coolant-temperature and void feedback equations are evaluated per zone from committed canonical plant state.
4. One current-power-share-weighted scalar feedback is composed through the existing non-rod-reactivity seam before global point kinetics; callers must not double-count those same configured fuel/coolant/void terms in the external scalar input.
5. Local feedback values may separately drive normalized `AggregatedCoreState` power-share redistribution.
6. Zone coupling is explicit symmetric configuration and affects only the shape-driving signal; coordinates do not imply coupling.
7. Candidate shape is produced by deterministic target normalization plus explicit relaxation and applies on the next committed step.
8. No local neutron population, delayed-neutron state, xenon inventory, mass or energy inventory is introduced.
9. The feature is opt-in. Configurations without an M9.4 definition retain the previous path and do not have their `IntegratedPrimaryCircuitInputs.CoreState` silently replaced.
10. Higher-resolution aggregation remains configurable, but a full-plant profile must provide matching canonical topology/channel-group ownership rather than fabricating duplicate physical inventories.

## Consequences

- Spatial feedback weighting becomes physically more expressive while preserving validated global kinetics.
- Local hot/cold/void conditions can influence both one global reactivity contribution and future power shape without becoming independent reactors.
- M3.3/M3.4 conservation and total-power closure remain authoritative.
- Existing versioned M7/M8/M9.3 configurations are not automatically changed.
- M9.5/M9.6 may later introduce explicit versioned higher-resolution/calibrated profiles without redesigning the ownership seam.
- True spatial kinetics, spatial xenon and historical plant-specific calibration remain separate future fidelity decisions.
