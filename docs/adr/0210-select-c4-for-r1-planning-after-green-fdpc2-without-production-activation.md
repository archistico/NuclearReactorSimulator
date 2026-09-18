# ADR-0210 — Select C4 for R1 planning after green FDPC2 without production activation

## Status

Accepted for RP1C selection decision only.

## Context

Selection Planning 1 returned `PASS-AS-AUTHORED`. C4 + `AMBIENT-UNSET` is the only selection-ready candidate/runtime pair after green FDPC2, while D3 remains blocked by its frozen seam maximum. The planning contract requires `SELECT-NONE` to remain available and prohibits automatic selection.

## Decision

Author the RP1C decision as `SELECT-C4` while preserving `SELECT-NONE` as a considered alternative. The choice is an engineering decision supported by the frozen readiness evidence; it is not inferred automatically from `selection-ready-count=1`.

The selection authorizes no production change. After returned selection adjudication, it may open only `R1-IMPLEMENTATION-PLANNING-ONLY`.

## Consequences

- no new measurement is performed by the selection gate;
- C4, D3, exact-v9, seam corpus, runtime profile and thresholds remain immutable;
- FDPC1 remains negative historical evidence and FDPC2 remains the confirming evidence;
- production repair/runtime change, VR3, P3-R1 and second replacement-long remain unauthorized.
