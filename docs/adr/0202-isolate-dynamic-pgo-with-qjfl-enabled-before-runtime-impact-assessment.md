# ADR 0202 — Isolate Dynamic PGO with QJFL enabled before runtime-impact assessment

## Status

Accepted for planning.

## Context

Runtime Factor Isolation 1 independently varied TieredCompilation, QuickJit and QuickJitForLoops while keeping Dynamic PGO OFF. TieredCompilation has a large repeatable single-factor effect on exact-v9 wall-clock performance. QuickJit has only a small central effect and no repeatable rare-tail ownership; QuickJitForLoops is materially neutral in the returned gate.

Historical Attribution 1 compared Dynamic PGO OFF/ON only with QuickJitForLoops disabled. Because the target path contains a loop and a future production/runtime decision should avoid unnecessary process-wide settings, PGO remains worth one final isolated comparison with QJFL enabled.

## Decision

Before Branch A2 Runtime Configuration Impact Assessment, plan a two-mode fresh-process comparator in which:

- `DOTNET_TieredCompilation=1`;
- `DOTNET_TC_QuickJit=1`;
- `DOTNET_TC_QuickJitForLoops=1`;
- `DOTNET_ReadyToRun=1`;
- only `DOTNET_TieredPGO` changes `0 ↔ 1`.

The comparator is evidence-only. It may not auto-promote a causal production decision.

Ambient/unset mode is excluded from this gate. Effective-default equivalence is a separate Branch A2 question.

## Consequences

- C4, exact-v9 and thresholds remain immutable.
- A PGO OFF/ON difference must repeat across fresh processes before it is treated as material.
- A single isolated maximum is insufficient to declare PGO the rare-tail owner.
- If both modes are equivalent and strict-clean, proceed to planning Branch A2 rather than adding more compilation-factor micro-gates.
- If PGO ON is repeatably adverse, Branch A2 must qualify the proposed PGO state across broader project/runtime behavior before any full-domain requalification.
- RP1C selection and production/runtime changes remain unauthorized.
