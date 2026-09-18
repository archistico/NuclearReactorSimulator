# ADR-0201 — Isolate TieredCompilation, QuickJit and QuickJitForLoops before PGO or RP1C selection

## Status

Accepted — planning decision only; no runtime configuration change is authorized.

## Context

Returned Attribution 1 evidence establishes strong runtime sensitivity, but its `TIERING-OFF` to `TIERING-ON-PGO-OFF` comparison changes both tiering and QuickJit settings. The target C4 path is loop-bearing and Attribution 1 also kept `QuickJitForLoops=0`, limiting negative inference about Dynamic PGO.

## Decision

Before any PGO-specific comparator or RP1C selection, execute a separately adjudicated single-factor runtime-isolation experiment whose adjacent mode pairs change only:

1. `DOTNET_TieredCompilation`;
2. `DOTNET_TC_QuickJit`;
3. `DOTNET_TC_QuickJitForLoops`.

Hold Dynamic PGO off and ReadyToRun fixed throughout this gate. If PGO remains material afterward, plan a separate QJFL=1 PGO OFF/ON comparator.

The gate is evidence-only and cannot automatically prove causality, select C4 or authorize a production/runtime configuration change.
