# M10 Final — VR2 Engineering Repair Planning 1 — RP1C C4 Exact-v9 Wall-Clock Tail Attribution Planning 1

**Status:** CANDIDATE — PLANNING ONLY  
**Prerequisite:** Full-Domain Performance Confirmation 1 returned evidence independently adjudicated PASS as complete evidence with classification `C4-FULL-DOMAIN-PERFORMANCE-NOT-CONFIRMED`.  
**Authority:** planning only. This document does not implement the attribution gate, select C4, mutate C4/C2/C3, change any performance threshold, change exact-v9, execute VR3/P3-R1 or authorize a second replacement-long baseline.

## 1. Why this planning gate exists

Full-Domain Performance Confirmation 1 closed the semantic, allocation and seam questions but left a narrow wall-clock question open.

Frozen returned evidence contains:

```text
5 fresh processes
217,600 measured calls total
115,200 exact-v9 calls
102,400 seam calls
0 unresolved calls
0 harness allocation
0 measured-region GC collections
0 seam strict-max exceedances
2 exact-v9 strict-max exceedances
```

The unchanged maximum is:

```text
409.30666666666673 us
```

The two strict exact-v9 misses are:

```text
run 4 / pass 18 / C2-MIXTURE-PREFIX / 1027.1 us / allocated 0 B
run 5 / pass 18 / C2-MIXTURE-PREFIX /  796.1 us / allocated 0 B
```

Looking below the strict threshold, all five exact-v9 calls above `100 us` occur at measured passes:

```text
15, 17, 17, 18, 18
```

and all five use `C2-MIXTURE-PREFIX`.

This pattern is evidence for a bounded runtime/tiering attribution hypothesis. It is not proof that tiered compilation, Dynamic PGO, ReadyToRun, OS scheduling or any other runtime mechanism caused the tails.

## 2. What is already closed and must not be reopened

The following are frozen:

- C4 semantic equivalence and physical qualification;
- C4 R1 allocation/tail closure;
- the 360-row exact-v9 corpus;
- all 1,280 seam probes and their returned green result;
- C4 source identity;
- C2/C3 historical source identities;
- median, p95, max and allocation ceilings;
- the negative full-domain classification itself.

The next gate is therefore not a new thermodynamic repair search. It asks whether the rare exact-v9 tail is sensitive to controlled .NET compilation modes while keeping the candidate and workload identical.

## 3. Frozen attribution hypothesis

Primary hypothesis:

```text
RARE-EXACT-V9-WALL-CLOCK-TAIL-MAY-BE-SENSITIVE-TO-
TIERED-COMPILATION-OR-DYNAMIC-PGO-TRANSITION
```

Alternative:

```text
TAIL-IS-NOT-SEPARATED-BY-CONTROLLED-JIT-MODES-AND-REMAINS-
UNATTRIBUTED-RUNTIME/SCHEDULING-EVIDENCE
```

Neither hypothesis is promoted to a causal conclusion by this planning document.

In particular:

```text
thermodynamic slow-path proven = false
runtime/tiering cause proven   = false
threshold relaxation justified = false
candidate mutation justified   = false
```

## 4. Why process environment is the correct test seam

The future gate will vary only documented .NET runtime compilation knobs in fresh child processes:

```text
DOTNET_TieredCompilation
DOTNET_TieredPGO
DOTNET_TC_QuickJit
DOTNET_TC_QuickJitForLoops
DOTNET_ReadyToRun
```

These are runtime/JIT configuration controls, not simulation-physics controls. The project and runtimeconfig remain unchanged.

For .NET 9 and later, runtime configuration supplied through environment variables has precedence over runtimeconfig/MSBuild configuration. The project targets .NET 10, so per-process environment settings provide a bounded experimental seam without editing project files.

External runtime references used only to justify the experimental seam:

- Microsoft Learn — Runtime configuration options for compilation: `https://learn.microsoft.com/dotnet/core/runtime-config/compilation`
- Microsoft Learn — Environment-variable precedence from .NET 9: `https://learn.microsoft.com/dotnet/core/compatibility/deployment/9.0/envvar-precedence`

## 5. Caller-environment fail-closed preflight

FDPC1 did not record these five `DOTNET_*` compilation variables. The next gate must therefore not silently assume what the parent shell inherited.

Before build or focused execution, the future runner must require all five variables to be unset in the calling environment:

```text
DOTNET_TieredCompilation
DOTNET_TieredPGO
DOTNET_TC_QuickJit
DOTNET_TC_QuickJitForLoops
DOTNET_ReadyToRun
```

If any are already set, the gate stops before evidence generation with:

```text
CALLER-RUNTIME-CONFIGURATION-NOT-CLEAN
```

The runner must not silently clear an inherited value and call that a clean control. The user must remove the external configuration and rerun.

## 6. Frozen four-mode experiment

The future separately versioned evidence gate is:

```text
RP1C-C4-EXACT-V9-WALL-CLOCK-TAIL-ATTRIBUTION1
```

It will use four process modes.

### Mode A — AMBIENT-UNSET-CONTROL

All five runtime variables remain unset in the child process.

Purpose: reproduce the FDPC1-style environment from a caller that has passed the clean-environment preflight.

### Mode B — TIERING-OFF

```text
DOTNET_TieredCompilation=0
DOTNET_TieredPGO=0
DOTNET_TC_QuickJit=0
DOTNET_TC_QuickJitForLoops=0
DOTNET_ReadyToRun=1
```

Purpose: test sensitivity to tiered compilation as a whole.

### Mode C — TIERING-ON-PGO-OFF

```text
DOTNET_TieredCompilation=1
DOTNET_TieredPGO=0
DOTNET_TC_QuickJit=1
DOTNET_TC_QuickJitForLoops=0
DOTNET_ReadyToRun=1
```

Purpose: preserve tiering while removing Dynamic PGO as the differentiating factor.

### Mode D — TIERING-ON-PGO-ON

```text
DOTNET_TieredCompilation=1
DOTNET_TieredPGO=1
DOTNET_TC_QuickJit=1
DOTNET_TC_QuickJitForLoops=0
DOTNET_ReadyToRun=1
```

Purpose: explicit tiering + Dynamic PGO comparator under controlled child-process configuration.

No mode changes C4, the corpus or any engineering threshold.

## 7. Frozen measurement protocol

Each mode uses five fresh processes:

```text
4 modes x 5 fresh processes = 20 fresh processes
```

Each process reproduces the exact-v9 portion of FDPC1:

```text
360 exact-v9 rows
16 warm-up passes
64 measured passes
rotation stride = 37
23,040 measured calls/process
```

Total future measured calls:

```text
20 x 23,040 = 460,800
```

The seam is not repeated. It already returned 102,400/102,400 green calls and does not own the open classification.

The harness must remain allocation-neutral and record:

- measured pass index;
- row index and frozen probe identity;
- logical step and node;
- elapsed microseconds;
- candidate allocated bytes;
- resolved/unresolved state;
- C4 resolution path;
- GC collection deltas for the measured region;
- the five effective `DOTNET_*` values for that child process;
- framework, OS, process architecture and Stopwatch frequency.

## 8. Two thresholds with different meanings

The qualification maximum stays unchanged:

```text
409.30666666666673 us
```

A second number is frozen only for diagnostic grouping:

```text
100 us
```

`100 us` is not a new acceptance threshold. It is used only because the returned FDPC1 evidence has exactly five calls above that value and therefore gives a useful tail-population view around measured passes 15–18.

The future aggregate evidence must separately report:

```text
calls > 100 us
calls > 409.30666666666673 us
pass-position histogram
resolution-path histogram
row/probe histogram
```

No engineering ceiling is widened or replaced.

## 9. No automatic causal verdict

This gate is deliberately evidence-only.

The runner/adjudicator may summarize mode differences, but it must not automatically declare:

```text
TIERING-IS-THE-CAUSE
PGO-IS-THE-CAUSE
OS-SCHEDULING-IS-THE-CAUSE
C4-IS-SELECTION-READY
```

Returned evidence must be reviewed before a causal label or next branch is authorized.

Examples of possible later interpretations include:

- tail disappears only when tiering is disabled;
- tail separates specifically with PGO off/on;
- all modes show comparable tails;
- control fails to reproduce the original tail strongly enough for attribution.

Those are later adjudication outcomes, not planning-time decisions.

## 10. Planned evidence tree

Each of 20 fresh processes writes exactly four files:

```text
01-process-contract.txt
02-exact-v9-call-timing.csv
03-runtime-context.txt
04-process-summary.txt
```

Aggregate evidence writes exactly six files:

```text
01-contract-and-provenance.txt
02-runtime-mode-run-summary.csv
03-tail-pass-window-summary.csv
04-tail-row-path-summary.csv
05-attribution-evidence-summary.txt
06-attribution1-summary.txt
```

Total:

```text
20 x 4 + 6 = 86 files
```

Negative or inconclusive engineering evidence is not an xUnit failure. Only harness/evidence-integrity failure is a technical RED.

## 11. Future test identity

Planning freezes the future test/runner identity before implementation:

```text
test file:
M10FinalVr2EngineeringRepairPlanning1Rp1cC4ExactV9TailAttribution1Tests.cs

class:
M10FinalVr2EngineeringRepairPlanning1Rp1cC4ExactV9TailAttribution1Tests

method:
Rp1cC4ExactV9TailAttribution1_MeasuresOneFreshRuntimeModeProcess

runner:
scripts/run-m10-final-vr2-engineering-repair-planning1-rp1c-c4-exact-v9-tail-attribution1.cmd
```

The test must be xUnit v3 `Explicit`, Release, `--parallel none` and isolated from the ordinary suite unless its dedicated opt-in variables are supplied.

## 12. Authority boundary

A local planning PASS does not authorize implementation by itself. The complete planning artifact folder must first be returned and adjudicated.

Until that happens:

```text
ATTRIBUTION-GATE-IMPLEMENTATION-AUTHORIZED=False
RP1C-SELECTION-AUTHORIZED=False
PRODUCTION-REPAIR-AUTHORIZED=False
THRESHOLD-CHANGE-AUTHORIZED=False
EXACT-V9-CHANGE-AUTHORIZED=False
VR3-AUTHORIZED=False
P3-R1-AUTHORIZED=False
SECOND-REPLACEMENT-LONG-AUTHORIZED=False
```

The next action after local PASS is only:

```text
RETURN-COMPLETE-TAIL-ATTRIBUTION-PLANNING-ARTIFACTS-FOR-ADJUDICATION
```
