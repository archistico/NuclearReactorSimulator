# ADR-0196 — C4 must remove resolve-time allocation without changing C3 thermodynamic semantics

## Status

Proposed — REV2 pre-execution hardening

## Context

RP1B Refinement 5 reproduced the same C3 R1-side wall-clock exceedance owner in five independent processes. Every measured C3 call reported 416 bytes allocated, and each repeated boundary-3 exceedance coincided with the only Gen0 collection observed inside a candidate-call measurement window for that process.

The 416-byte per-call measurement remains valid because `GC.GetAllocatedBytesForCurrentThread()` was sampled immediately before and after `candidate.TryResolve`. The historical whole measured region, however, was not allocation-neutral: after the full-GC baseline it created the timing-sample list and allocated one heap `R1CallTimingSample` record after each call. Therefore the returned evidence proves GC correlation, but not that candidate allocation alone determined the exact Gen0 position.

Static source review identifies two allocation risks on the immutable C3/C2 R1 path: `Rp1bRefinementSaturationTable.TryBuildReachabilityBoundary` creates a temporary `List<double>` and orders it with LINQ, while `TryResolveFromTable` enumerates an `IReadOnlyList<SurrogateTemperatureRow>` with `foreach`. The exact byte contribution of either site is not independently claimed as proven.

The first Planning-1 topology proposed only an allocation-neutral mixture pre-resolver before immutable C2. The second pre-execution review found that this is structurally insufficient. Frozen Refinement-2 C3 evidence shows all 320 R1 timing rows resolve as `SubcooledLiquid`: 310 as `REGION-1-NEAR-BOUNDARY-C2` and 10 as `REGION-1-TABLE-C2`. A mixture-only pre-resolver would decline those rows, then immutable C2 would execute its allocating mixture search again before reaching the liquid result.

## Decision

C4 remains a new test-only candidate. C2/C3/D3 historical source remains immutable and SHA-256 pinned.

The frozen REV2 topology is `ALLOCATION-NEUTRAL-C3-VAPOR-PRECEDENCE-C2-MIXTURE-LIQUID-PREFIX-THEN-IMMUTABLE-C2-FALLBACK`:

1. reproduce C3's near-vapor superheated-side discriminator first;
2. reproduce the C2 mixture prefix allocation-neutrally;
3. reproduce C2 liquid-table resolution allocation-neutrally, using indexed loops rather than interface enumeration;
4. reproduce C2 near-boundary-liquid resolution allocation-neutrally;
5. only then delegate to immutable C2 for the remaining vapor/unresolved paths;
6. reproduce C3's saturated-side near-vapor fallback last if C2 remains unresolved.

Only the C2 `MIXTURE + LIQUID-TABLE + NEAR-BOUNDARY-LIQUID` prefix may be duplicated. C2 vapor-table and near-vapor logic may not be cloned. The 320-row R1 timing corpus must reach the new prefix with zero immutable-C2 fallback invocations. C4 carries non-allocating resolution-path telemetry so this is demonstrated directly. During the measured region that telemetry is a value-type enum/code; human-readable strings are materialized only after timing has finished.
Boundary 3 must not receive a special branch.

The crossing-boundary logic uses at most two local fraction slots, preserves historical liquid-then-vapor insertion order, and swaps only when the second fraction is strictly smaller. Resolve-time C4 prefix code on the R1 timing path uses indexed `for`/`while` scans only and may not use any `foreach`, LINQ, temporary collections, boxing, per-call arrays, or reference-type sample creation.

Before performance interpretation, C4 must be bit-equivalent to C3 across all frozen VR2, exact-v9, seam and hydraulic-context observations. Thermodynamic-state equivalence covers 1,679 rows (39 inverse-applicable VR2 + 360 exact-v9 + 1,280 seam); the 288 hydraulic rows are a separate derived-equivalence dataset comparing resolved status, driving-pressure bits, flow bits and sign-change status from the same bit-exact node pressures. Double fields are compared with `BitConverter.DoubleToInt64Bits`; strings use ordinal equality; nullable vapor quality requires exact presence and bit equality.

C4-specific allocation closure is zero bytes on every measured R1 candidate call. The entire measurement harness is also allocation-neutral across the measured region: timing storage is preallocated before the post-warm-up full GC, stored as value types, and the whole-region current-thread allocation delta minus the sum of candidate-call deltas must equal zero.

C4 interface metadata is frozen before implementation. `CandidateId` is `C4-ALLOCATION-NEUTRAL-VAPOR-SEAM-COMPLETE-SURROGATE`, `FamilyId` remains `C-BOUNDED-IF97-DERIVED-TABLE-SURROGATE`, `MaximumIterativeSolveIterations` remains `48`, and `UsesDirectIf97AtResolveTime` remains `false`. `InitializationReferencePointCount` reports immutable C2 fallback references plus the dense C3-style vapor table plus the separately materialized 0.5 K prefix saturation table plus C4's liquid-prefix reference points.

Semantic equivalence runs in one dedicated focused process that contributes no wall-clock evidence. Semantic and timing evidence use two distinct `[Fact(Explicit = true)]` methods selected by exact `--filter-method`; each focused invocation targets the Simulation.Tests project in `Release` with `--parallel none` and `--no-build`, while the ordinary `eng\ci-ordinary.cmd` gate runs first with all C4 opt-in/lane/run variables unset. The semantic invocation keeps lane/run unset; timing invocations require both. Wall-clock/allocation evidence then uses two lanes with five fresh runtime processes per lane, for ten timing processes and eleven focused process invocations total. Warm-up pass indices are `0..15`; measured pass indices restart at `0..63`, matching Refinement 5. Lane A reproduces stride 37; Lane B uses the exact pre-frozen affine permutations in the Planning-1 contract.

The runner alone deletes the C4 artifact root, once before evidence begins. The semantic process owns only aggregate files `02` through `04`; the runner/adjudicator owns aggregate file `01` and files `05` through `09`; each timing process may reset only its own lane/process directory. A semantic mismatch does not short-circuit the ten timing processes: the full 59-file evidence tree is still produced before classification.

The focused evidence-generation tests fail only when evidence cannot be completed or trusted. Semantic mismatch, nonzero candidate allocation, fallback use on R1, or wall-clock exceedance are valid negative engineering evidence and are adjudicated after artifact generation rather than converted into a late xUnit RED.

## Consequences

The C4 implementation is wider than a mixture-only helper but remains substantially narrower than a C2 clone. The duplicate surface is fixed before implementation and exists only because private/sealed historical C2 cannot expose an allocation-free entry point without mutating frozen evidence.

A clean wall-clock run without zero candidate allocation is insufficient. Zero allocation with semantic drift is insufficient. Zero candidate allocation is also insufficient if the timing harness itself allocates during the measured region or if any R1 timing call reaches immutable C2 fallback.

A clean candidate still does not automatically become RP1C-selected or production code. Returned C4 artifacts require separate engineering adjudication and later selection/activation gates.
