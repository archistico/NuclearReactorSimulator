# ADR 0072 — Historical-inspired scenarios require explicit provenance and fidelity gates

## Status

Accepted; M9.5 subsequently passed local compilation and the complete automated suite and is validated.

## Context

The simulator now has enough validated scenario, fault, replay, xenon and quasi-spatial infrastructure to support educational exercises inspired by historical operations or incidents.

Without an explicit contract, however, scenario descriptions could easily mix:

- documented historical facts;
- reduced-model educational approximations;
- simulator-specific assumptions.

That would create false precision and could make a deterministic educational scenario appear to be a validated historical reconstruction.

The project also needs a fail-closed way to prevent content from claiming dependence on physical/model capabilities that the active validated baseline does not provide.

## Decision

Historical-inspired content is opt-in through `ScenarioDefinition.HistoricalContext`.

The context must explicitly carry:

- source references;
- classified claims;
- model capability requirements;
- a fidelity statement;
- deliberate non-claims.

Claims use three distinct categories:

1. `DocumentedFact` — requires declared source evidence;
2. `EducationalApproximation` — requires explicit approximation rationale;
3. `SimulatorSpecificAssumption` — requires explicit simulator-specific rationale.

A historical scenario may reference only sources declared in its own context.

`HistoricalScenarioFidelityReviewer` compares required capability IDs with an explicit validated capability set. `ScenarioSessionFactory` performs this review before creating the runtime and fails closed if any required capability is absent.

Scenario JSON is versioned to schema v3. Older v0/v1/v2 scenario documents migrate with no historical context; migration must never invent historical claims, sources or fidelity status.

Historical metadata remains descriptive Application-layer data. It cannot write physical state, bypass canonical M2–M5 owners, create hidden scenario-specific physics or script outcomes.

## Consequences

Positive:

- historical claims become auditable rather than implicit;
- educational approximations are visible instead of being presented as facts;
- unsupported model dependencies fail before runtime creation;
- old scenarios retain their prior semantics;
- future historical content can evolve independently from physics ownership.

Costs:

- historical scenario authors must maintain source/claim metadata;
- capability IDs must be versioned/maintained as fidelity evolves;
- a passed fidelity review is deliberately narrower than historical validation and must not be presented as quantitative agreement.

## Rejected alternatives

### Put historical notes only in prose documentation

Rejected because the metadata would not travel with the versioned scenario and could not participate in deterministic load-time review.

### Treat every scenario statement as a documented fact

Rejected because reduced models inevitably require approximations and simulator-specific assumptions; hiding that distinction would create false historical precision.

### Script historical trajectories when current physics cannot reproduce them

Rejected because scenario-owned target outcomes would bypass canonical physics and destroy the project's ownership/replay guarantees.

### Fetch/verify external sources at runtime

Rejected because it would introduce network-dependent, mutable behavior into deterministic scenario loading and would not solve source-authority questions.

## Non-claim

M9.5 approval of a scenario's fidelity requirements does not mean the scenario is an exact reconstruction, a validated causal explanation or a licensing-grade safety analysis.
