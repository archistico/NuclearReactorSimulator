# M10 Final - VR2 Engineering Repair Planning 1 - RP1C C4 Full-Domain Performance Confirmation 2 Planning 1

## Status

**CANDIDATE - PLANNING ONLY**

Prerequisite: complete returned A2 evidence independently adjudicated PASS with `AMBIENT-UNSET` project-qualification sufficient on the frozen A2 host.

This planning gate does not execute FDPC2, select C4, change production runtime settings, modify C4/exact-v9, change any threshold, authorize VR3/P3-R1, or authorize a second replacement-long baseline.

## Purpose

Full-Domain Performance Confirmation 1 remains authoritative negative historical evidence: two rare exact-v9 wall-clock calls exceeded the frozen `409.30666666666673 us` maximum. The subsequent Branch A program established that the gross timing behavior is runtime-sensitive, isolated TieredCompilation and Dynamic PGO effects, and then qualified project impact under ambient versus explicit runtime profiles.

A2 now shows that `AMBIENT-UNSET` is operationally sufficient on the frozen A2 host and does not require an explicit production runtime override. Planning 1 therefore freezes a new, separately versioned full-domain confirmation under that exact qualification context.

FDPC2 does not reinterpret or overwrite FDPC1.

## Frozen candidate and corpora

The future gate uses the immutable candidate:

```text
C4-ALLOCATION-NEUTRAL-VAPOR-SEAM-COMPLETE-SURROGATE
```

It preserves the existing RP1A exact-v9 360-row corpus, 1,280-row seam corpus, rotation/warm-up/measured-pass structure and corrected performance predicate. C4, C2/C3, exact-v9 and all thresholds remain immutable.

## Runtime profile

The future qualification profile is:

```text
AMBIENT-UNSET
```

All five controlled `DOTNET_*` variables and their `COMPlus_*` aliases must be absent in the caller and absent from every child process environment:

```text
DOTNET_TieredCompilation
DOTNET_TieredPGO
DOTNET_TC_QuickJit
DOTNET_TC_QuickJitForLoops
DOTNET_ReadyToRun
COMPlus_TieredCompilation
COMPlus_TieredPGO
COMPlus_TC_QuickJit
COMPlus_TC_QuickJitForLoops
COMPlus_ReadyToRun
```

No explicit all-ON runtime deployment is planned or recommended by this gate.

## Host scope

Because the strict maximum is an absolute wall-clock criterion, FDPC2 must execute on the same physical host used for A2:

```text
host-fingerprint-sha256=931EAC000C09399B8EAABEB0316F16980620CAC45D1FC7F13516CE55DAF69336
active-power-scheme-guid=8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c
```

The host fingerprint and active power scheme are captured before and after the future run. Drift is infrastructure RED. Running FDPC2 on the user's other PC is not authorized by this planning; that requires a separately adjudicated host-scope amendment.

## Future FDPC2 protocol

Five fresh processes measure the unchanged full domain:

```text
exact-v9:
  360 rows
  16 warm-up passes
  64 measured passes
  23,040 measured calls/process

seam:
  1,280 rows
  4 warm-up passes
  16 measured passes
  20,480 measured calls/process

per process = 43,520 calls
5 processes  = 217,600 calls
```

The process harness remains allocation-neutral. A negative engineering result is retained as evidence and is not converted into infrastructure RED.

## Frozen predicate

Every process must satisfy the unchanged corrected predicate:

```text
exact-v9 median <= 94.8 us
exact-v9 p95 <= 158.80666666666667 us
exact-v9 single-call max <= 409.30666666666673 us
all seam-side single-call max <= 409.30666666666673 us
exact-v9 median candidate allocation <= 2816 B
all exact-v9 calls resolved
all seam calls resolved
```

The `100 us` diagnostic floor remains diagnostic only and is not a threshold.

## Future evidence shape

The planned future tree contains exactly 32 files:

- 5 process directories x 5 files = 25 files;
- 7 aggregate/host/adjudication files.

Per-process files:

```text
01-process-contract.txt
02-exact-v9-call-timing.csv
03-seam-call-timing.csv
04-runtime-context.txt
05-process-summary.txt
```

Aggregate files:

```text
01-contract-and-provenance.txt
02-execution-host-provenance.txt
03-host-consistency-audit.txt
04-cross-process-run-summary.csv
05-cross-process-seam-side-summary.csv
06-confirmation-evidence-adjudication.txt
07-rp1c-c4-full-domain-performance-confirmation2-summary.txt
```

## Decision boundary after returned FDPC2

Only complete returned FDPC2 evidence may determine whether the C4 + `AMBIENT-UNSET` pair is selection-ready.

If all five processes satisfy the frozen predicate, returned-evidence adjudication may mark the pair `selection-ready=True` and open a separate RP1C selection gate whose decision space remains:

```text
SELECT-C4
SELECT-NONE
```

A green FDPC2 does not itself select C4.

If FDPC2 is negative, FDPC1 remains historical truth and the Branch A path does not silently continue to selection.

## Authority

Planning 1 keeps all of the following false:

```text
FDPC2-IMPLEMENTATION-AUTHORIZED=False
RP1C-SELECTION-AUTHORIZED=False
PRODUCTION-RUNTIME-CHANGE-AUTHORIZED=False
PRODUCTION-REPAIR-AUTHORIZED=False
THRESHOLD-CHANGE-AUTHORIZED=False
EXACT-V9-CHANGE-AUTHORIZED=False
VR3-AUTHORIZED=False
P3-R1-AUTHORIZED=False
SECOND-REPLACEMENT-LONG-AUTHORIZED=False
```
