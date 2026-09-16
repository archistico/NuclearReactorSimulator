# M10 Final — VR2 Engineering Repair Planning 1 — RP1B Refinement 5 — Pre-Execution Review

## Scope

Static pre-execution review of the C3 cross-process wall-clock tail reproducibility candidate. This review does not claim a .NET build/test PASS; the executable confirmation remains the local runner.

## Frozen invariants checked

- production `src/` is byte-identical to the validated Performance Measurement Replanning 1 baseline;
- C3/D3 test-only implementation is byte-identical;
- returned Refinement 4 evidence remains byte-identical;
- returned Performance Measurement Replanning 1 audit is frozen as prerequisite evidence;
- C3 identity and strict `409.30666666666673 us` ceiling are unchanged;
- C4 is not created and RP1C remains unauthorized.

## Runner / process isolation review

- the runner deletes the Refinement 5 artifact root once, before measurements;
- it performs ordinary Release build before focused measurement;
- it launches five separate `dotnet test` commands, therefore five fresh test runtime processes;
- each process receives a frozen run index `1..5` through `NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_REFINEMENT5_RUN_INDEX`;
- each process writes only its own `process-01` ... `process-05` subdirectory;
- cross-process adjudication runs only after all five focused processes return PASS evidence-generation status.

## C# focused-test review

- 320 unique R1-side boundaries are loaded from the immutable RP1A seam map;
- each process uses 16 warm-up passes, 64 measured passes and rotation stride 37;
- exactly 20,480 measured calls are required per process;
- each measured call records boundary/pass identity, wall time, allocation and GC deltas;
- timing exceedance does not fail the evidence gate;
- physical resolution/phase mismatch, malformed corpus or missing artifacts still fail closed;
- no xUnit2012 `Assert.True(collection.Any(...))` pattern is present;
- no xUnit2031 `Assert.Single(...Where(...))` pattern is present.

## PowerShell 5.1 review

- validator and adjudicator source are ASCII-only;
- UTF-8 text reads are explicit/null-safe where validator source text is inspected;
- floating-point contract values use tolerance comparisons in the validator;
- production source scans admit only `.cs` outside `bin`/`obj`;
- the adjudicator parses invariant-culture floating values and writes UTF-8 without BOM;
- the same-boundary rule is applied by counting distinct process runs with at least one exceedance for each boundary.

## Cross-process adjudication contract

A C3 slow path is candidate-specific only when the same boundary exceeds the unchanged maximum ceiling in at least two independent processes. Refinement 5 may therefore return evidence with exceedances and still complete successfully.

Possible classifications remain:

```text
C3-SAME-BOUNDARY-SLOW-PATH-CONFIRMED
C3-CROSS-PROCESS-ISOLATED-WALL-CLOCK-TAIL
C3-CROSS-PROCESS-NO-EXCEEDANCE-OBSERVED
```

No classification directly authorizes C4 implementation or RP1C.
