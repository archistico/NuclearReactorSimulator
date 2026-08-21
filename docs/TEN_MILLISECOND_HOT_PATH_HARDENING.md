# 10 ms Hot-Path Allocation & Lookup Hardening — M10.9.7.2 Hotfix 2 REV1

## Purpose

M10.9.7.2 Hotfix 1 REV1 closed Domain construction-time invariants and is the validated baseline for this work. The original Hotfix 2 runtime implementation was not validated because one newly added lifecycle regression fixture referenced unsupported condition ID `step>=99-observation`; REV1 changes that fixture to existing `step>=3-observation` and leaves the runtime optimization unchanged. Before activating the Mission/Performance workspace at the 10 ms simulation cadence, Hotfix 2 removes three measured sources of avoidable hot-path work without changing solver equations, reference-plant coefficients or challenge semantics.

## Challenge observation change tracking

`ScenarioChallengeTracker` previously constructed a canonical string fingerprint of every current observation before and after each evaluation. That required ordering, interpolation and `string.Join` twice per evaluation. The fingerprint contents included logical step, so active observations normally changed every deterministic step anyway.

Hotfix 2 replaces that implementation with a monotonic internal observation version. `Observe(...)` increments the version only when the immutable `ChallengeConditionObservation` value actually changes. `Evaluate(...)` compares the version before/after evaluation plus the existing transition-count check. The public lifecycle snapshot and `LifecycleChanged` contract do not expose the version.

The ordinary regression verifies that an accepted RUN command at the same logical step causes no spurious lifecycle event while three deterministic steps with changing observation evidence still cause exactly three lifecycle-change notifications.

## Plant topology/state id lookup

The previous `PlantDefinition` and `PlantState` getters used `Enumerable.FirstOrDefault` with a capturing predicate. Every lookup therefore combined an O(n) scan with closure/delegate allocation.

Hotfix 2 does **not** add a lookup dictionary to every `PlantState`. Plant states are materialized repeatedly by control and hydraulic solvers, so a per-instance dictionary could trade lookup cost for larger construction/allocation cost.

Instead:

- immutable `PlantDefinition` creates one `Dictionary<string,int>` index for each canonical registry at construction time;
- public `PlantDefinition.Get*` methods use those indexes while preserving the canonical ordered `IReadOnlyList<>` surfaces;
- `PlantState` reuses the owning definition's stateful indexes and directly indexes its own canonical lists;
- exact-set validation guarantees the state list and definition registry share the same canonical id order;
- unknown/blank id exception semantics remain fail-closed.

This gives O(1) hot lookup without per-state lookup dictionaries.

## Compressible steam critical ratio

`CompressibleSteamFlowDefinition` is immutable, but `CriticalDownstreamToUpstreamPressureRatio` previously recalculated the same `Math.Pow` expression on every property access. Hotfix 2 computes it once after validating `HeatCapacityRatio` and publishes the cached result through the same property.

The formula and numerical value are unchanged.

## Measurement contract

The focused audit uses same-process relative measurements after warm-up. It compares the optimized public lookup paths against a test-local implementation equivalent to the pre-Hotfix-2 linear/capturing lookup and compares the eliminated observation string fingerprint work against version-counter change tracking.

Primary promotion signals are:

- indexed `PlantDefinition` lookup has effectively zero per-call allocation after warm-up;
- indexed `PlantState` lookup has effectively zero per-call allocation after warm-up;
- reference linear/capturing lookup allocates more and is slower on a 256-node registry worst-case lookup;
- version-counter change tracking allocates zero bytes while the reference string fingerprint allocates;
- the cached critical ratio is numerically identical to the previous formula.

Wall-clock ratios are relative evidence from the same process, not universal hardware budgets. Hotfix 2 does not replace H.28/H.28.1 plant-level performance evidence.

## Explicit non-scope

Hotfix 2 does not:

- retune any hydraulic, turbine, reactor or electrical solver;
- change any reference-plant coefficient or synchronization limit;
- create per-`PlantState` dictionaries;
- change canonical collection ordering;
- change challenge lifecycle, scoring or terminal semantics;
- use generated record equality over `IReadOnlyList<>` as UI change detection;
- resolve score-dominance authoring classification;
- change the v1 100-point `FinalScore == FinalPercentage` invariant;
- activate `MISSION`, add F9 or modify F1–F8;
- introduce plant-command authority.

After validation, M10.9.7.3 may activate the Mission/Performance UI, but must use an explicit presentation change-detection mechanism rather than generated record equality.
