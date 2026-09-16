# M10 Final — VR2 Engineering Repair Planning 1 — RP1B Refinement 3 Pre-Execution Review

**Review status:** STATIC REVIEW COMPLETE — build/runtime still require the local .NET/Windows PowerShell gate.  
**Scope:** Refinement 3 harness, contract, validator, runner, frozen evidence and package boundaries.  
**Production scope:** none.

## Frozen identity checks

The candidate was constructed directly from the RP1B Refinement 2 C3/D3 package. Before packaging Refinement 3:

```text
src/                                             byte-identical
Rp1bRefinement2ShadowThermodynamicCandidates.cs  byte-identical
Refinement 2 focused test                         byte-identical
Refinement 2 JSON contract                        byte-identical
Refinement 2 validator                            byte-identical
Refinement 2 runner                               byte-identical
returned Refinement 2 artifacts                   byte-identical 9/9
```

C3 therefore remains the exact version that produced the returned Refinement-2 evidence. No C4 identity exists.

## C# review

The new focused test was reviewed for the failure classes previously encountered in VR2/RP1B:

- no direct access to `internal` Application members;
- no `Assert.Single(...Where(...))` xUnit2031 pattern;
- nullable use is explicit for the optional timing-sample list;
- CSV schemas are exact-string checked before parsing;
- node and seam counts are asserted before timing;
- C3 physical resolution/phase behavior is rechecked before any timing interpretation;
- timing loops are finite and bounded;
- tuple keys use the full `(probe_id, logical_step, node_id)` identity;
- braces, parentheses and brackets are structurally balanced in a lexical scan;
- no new thermodynamic candidate implementation is present in the focused test.

The test intentionally does not assert that C3 meets the performance ceiling. It asserts only evidence completeness/finite timing so a RED/green engineering result is written to artifacts instead of destroying the attribution evidence.

## Performance semantics review

The frozen ceilings remain:

```text
median       94.8 us
p95          158.80666666666667 us
max          409.30666666666673 us
allocation   2816 B
```

Refinement 3 makes the full worst-case predicate explicit:

```text
screen median <= median ceiling
screen p95 <= p95 ceiling
screen exact-v9 max <= max ceiling
seam max <= same max ceiling
targeted-repeat max <= same max ceiling
median allocation <= allocation ceiling
```

Targeted repeat labels are diagnostic only in meaning; the targeted calls themselves are additional strict measurements and cannot relax an observed ceiling violation.

## Validator review

The validator was checked for the PowerShell failure classes already observed in this project:

- ASCII-only source for Windows PowerShell 5.1;
- null-safe UTF-8 reads via `System.IO.File.ReadAllText`;
- all floating values use finite bounded comparisons through `Require-NearDouble`;
- numeric `-ne` is used only for integer counts/protocol constants;
- returned Refinement-2 9/9 artifact existence and row counts are checked;
- C3/D3 returned timing values and physical completion markers are checked;
- RP1A timing corpus counts and compound-key uniqueness are checked;
- contract output order and complete authority-false set are checked;
- source scan forbids C3/Refinement3/C4 identities from production `src/`;
- exact-v9 identity and existing closure mode markers remain required.

## Package review boundary

Refinement 3 may add only:

- returned Refinement-2 frozen evidence and engineering summary;
- Refinement-3 focused performance-attribution test;
- Refinement-3 contract, validator and runner;
- Refinement-3 documentation/provenance updates.

It must not add `bin/`, `obj/`, generated `artifacts/` or production thermodynamic changes.

## Residual limitation

This environment does not provide .NET 10 or Windows PowerShell, so compilation, xUnit analyzer execution and actual machine-local timing cannot be certified statically. Those remain the purpose of the local `[2/3]` and `[3/3]` gate.

## Attempt 1 returned preflight RED and Hotfix 1 review

The first local Refinement-3 run did not reach ordinary build or the focused timing test. The static validator reported an RP1B Refinement-3 identity leak in:

```text
src/NuclearReactorSimulator.App/bin/Release/net10.0/runtimes/osx/native/libSkiaSharp.dylib
```

This path is generated native build output, not production C# source. The original review incorrectly validated the source scan only against the clean packaged tree. In a real working tree, prior builds leave `src/**/bin` and `src/**/obj` populated. On Windows PowerShell 5.1 the original `Get-ChildItem -LiteralPath 'src' -Recurse -File -Include *.cs` form did not reliably restrict the returned descendants to C# source, so binary content could reach `Read-Utf8Text` and accidentally satisfy a short forbidden marker.

Hotfix 1 changes only the validator/provenance path. The production identity scan now:

```text
enumerates descendants of src/
keeps only Extension == .cs
excludes any /bin/ or /obj/ path segment
rejects an empty production-source set
rechecks that every selected item is .cs and outside bin/obj before reading text
```

C3, the focused test, timing protocol, thresholds, contract and runner are unchanged. Attempt 1 therefore produces no performance evidence and no engineering attribution.

## Hotfix 2 build review

Attempt 2 passed the generated-output-safe static preflight and entered the ordinary Release build. The build stopped on a single test-only `CS1061`: `TailRepeat` exposes `TargetP95Microseconds`, while line 119 referenced `P95Microseconds`. Hotfix 2 changes only that member access. A complete scan of `StateTiming`, `TailRepeat` and `SeamSideTiming` member usage found no second name drift, and the xUnit2031 pre-check remains clean. No C3 timing evidence was produced by Attempt 2.
