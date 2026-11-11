# M10 Final - VR2 Engineering Repair Planning 1 - RP1C C4 Full-Domain Performance Confirmation 2

## Status

**CANDIDATE - EVIDENCE ONLY**

Prerequisite: `RP1C-C4-FULL-DOMAIN-PERFORMANCE-CONFIRMATION2-PLANNING1` returned `PASS-AS-AUTHORED`.

## Purpose

FDPC2 re-runs the immutable C4 full-domain performance qualification under the runtime context established by A2:

- runtime profile: `AMBIENT-UNSET`;
- same physical host as A2;
- same active Windows power scheme;
- no explicit production runtime override.

FDPC1 remains authoritative negative historical evidence and is not reinterpreted.

## Frozen protocol

Five fresh processes execute:

- exact-v9: 360 rows, 16 warm-up passes, 64 measured passes, 23,040 measured calls/process;
- seam: 1,280 rows, 4 warm-up passes, 16 measured passes, 20,480 measured calls/process;
- total: 43,520 measured calls/process, 217,600 measured calls.

The final evidence tree contains exactly 32 files: 25 process files plus 7 aggregate/host/adjudication files.

## Same-host runtime boundary

FDPC2 must execute on:

`host-fingerprint-sha256=931EAC000C09399B8EAABEB0316F16980620CAC45D1FC7F13516CE55DAF69336`

with active power scheme GUID:

`8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c`

The five controlled `DOTNET_*` variables and the five `COMPlus_*` aliases must be absent in the caller and are removed from child environments.

## Frozen predicate

Each process is evaluated against the unchanged predicate:

- exact-v9 median <= 94.8 us;
- exact-v9 p95 <= 158.80666666666667 us;
- exact-v9 max <= 409.30666666666673 us;
- seam max <= 409.30666666666673 us;
- exact-v9 median candidate allocation <= 2816 B;
- all exact-v9 and seam calls resolved;
- measured harness allocation = 0.

A negative engineering classification is retained as evidence and does not become infrastructure RED.

## Authority boundary

This gate does not perform RP1C selection and does not authorize production runtime change, production repair, threshold change, exact-v9 change, VR3, P3-R1 or a second replacement-long baseline.

Returned FDPC2 evidence must be independently adjudicated before any RP1C selection step.
