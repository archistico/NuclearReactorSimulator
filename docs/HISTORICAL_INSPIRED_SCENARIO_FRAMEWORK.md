# Historical-Inspired Scenario Framework

## Purpose

M9.5 adds a versioned provenance and model-fidelity boundary for **historical-inspired** training content.

The framework exists to prevent three different things from being blended into one unqualified narrative:

1. **documented historical facts** supported by declared sources;
2. **educational approximations** chosen because the simulator intentionally uses a reduced model;
3. **simulator-specific assumptions** needed to construct a deterministic exercise where the historical record or current model does not uniquely determine a value or sequence.

M9.5 does **not** make the simulator a historical reconstruction engine. It makes historical claims explicit, reviewable and fail-closed before historical-inspired content is loaded.

## Ownership

M9.5 remains in the Application/Infrastructure scenario boundary.

It does not create a new physical owner and does not modify M2/M3/M4/M5 physics. Historical metadata cannot write reactor state, force outcomes or bypass typed commands/fault seams.

```text
historical sources / author statements
        ↓
HistoricalScenarioContextDefinition
        ├─ sources
        ├─ classified claims
        ├─ required validated model capabilities
        └─ deliberate non-claims
        ↓
HistoricalScenarioFidelityReviewer
        ↓
approved only when every declared capability is available
        ↓
ScenarioSessionFactory
        ↓
existing exact-version initial condition + canonical runtime/fault/command owners
```

## Scenario schema v3

`ScenarioDefinition` now has an optional `HistoricalContext`.

`JsonScenarioDefinitionSerializer` schema v3 persists this metadata. Legacy schema v0/v1/v2 documents migrate with `HistoricalContext == null`; migration never invents historical provenance or reclassifies an existing scenario as historical-inspired.

This preserves the existing exact-version scenario/initial-condition semantics while allowing new content to opt in explicitly.

## Provenance contracts

### `HistoricalSourceReference`

Carries stable source metadata supplied by the scenario author:

- `SourceId`;
- citation text;
- optional locator such as section/page/table identifier.

The runtime does not fetch external content and does not silently update citations.

### `HistoricalScenarioClaimDefinition`

Every historical statement is assigned one explicit `HistoricalScenarioClaimKind`:

- `DocumentedFact` — requires at least one declared source reference;
- `EducationalApproximation` — requires an explicit rationale describing the reduced-model approximation;
- `SimulatorSpecificAssumption` — requires an explicit rationale describing why the simulator-specific assumption is needed.

A claim may reference only source IDs declared by the same historical context. Unknown source references fail at construction/deserialization.

### `HistoricalScenarioContextDefinition`

Carries:

- historical subject/scope;
- explicit fidelity statement;
- declared sources;
- classified claims;
- required model-capability IDs;
- deliberate non-claims.

A historical context must contain at least one source-backed `DocumentedFact`, at least one explicit required model capability and at least one deliberate non-claim. This prevents vacuous fidelity approval and forces each historical-inspired scenario to state both the historical basis it actually has and what it does **not** claim, such as exact chronology, exact plant configuration, quantitative accident reconstruction or licensing-grade fidelity.

## Model-fidelity gate

`HistoricalScenarioFidelityReviewer` compares only explicit scenario requirements against an explicit set of validated capability IDs.

The current M9.5 catalog uses stable granular IDs for capabilities validated through M9.4, including:

- deterministic full-plant runtime and global point kinetics;
- integrated primary thermohydraulics;
- integrated steam/turbine/feedwater secondary cycle;
- generator/grid synchronization;
- measured instrumentation and canonical automatic-control/protection/alarm seams;
- exact versioned initial conditions and training guidance/evaluation;
- deterministic fault injection, including the explicitly bounded educational leak/LOCA and electrical-loss/SBO-class frameworks;
- recorder/checkpoint/full replay and post-incident analysis;
- canonical iodine/xenon integration;
- quasi-spatial core feedback refinement.

A scenario should require the narrowest set that its historical/educational claims actually depend on. Broad runtime availability must not be used as a substitute for declaring a more specific fidelity dependency.

Approval means only:

> every capability explicitly required by the scenario is available in the active validated model set.

Approval does **not** mean:

- the historical record is complete;
- the cited source is automatically authoritative;
- the simulator quantitatively reproduces the event;
- chronology proves causality;
- the exercise is calibrated against measured historical data.

Those claims require separate evidence and, for quantitative calibration, M9.6.

## Fail-closed session loading

`ScenarioSessionFactory` reviews every scenario carrying `HistoricalContext` before resolving/creating the runtime.

If a required capability is missing, loading throws `HistoricalScenarioFidelityException` before the initial-condition runtime is created.

Ordinary non-historical scenarios are unchanged.

## No scripted historical outcome

M9.5 metadata can describe provenance and fidelity limits only.

A historical-inspired scenario still uses the same canonical seams as every other scenario:

- exact versioned initial conditions;
- typed operator actions;
- deterministic fault definitions;
- M2/M3/M4/M5 canonical physics and control;
- M9.1 recorder/replay;
- M9.2 evidence-based analysis.

The framework cannot prescribe a target pressure, power excursion, trip time, xenon curve, damage trajectory or final outcome merely because a historical source describes one.

If the current validated physics cannot produce a phenomenon, the scenario must declare that limitation rather than script the result.

## Deliberate M9.5 limits

M9.5 does not add:

- a built-in reconstruction of Chernobyl or any other named historical event;
- automatic web/source retrieval;
- source ranking or historiographic adjudication;
- historical parameter calibration;
- hidden scenario-specific physics;
- scripted target trajectories;
- a claim of plant-specific licensing-grade fidelity.

M9.6 remains responsible for calibration/reference validation infrastructure. Historical-inspired content may be added only when its required capability/fidelity envelope is explicit and satisfied.
