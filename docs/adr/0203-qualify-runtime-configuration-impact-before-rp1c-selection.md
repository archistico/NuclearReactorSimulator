# ADR-0203 — Qualify runtime configuration impact before RP1C selection

## Status
Accepted for planning only.

## Context
Runtime Factor Isolation 1 established TieredCompilation as materially causal for the gross exact-v9 slowdown. Dynamic PGO Comparator 1 then isolated PGO with QuickJitForLoops enabled and demonstrated a repeated material improvement in the central timing distribution when PGO is ON. The rare strict tail remains too sparse for PGO tail-causality promotion.

The project must therefore avoid converting a microbenchmark observation directly into a process-wide runtime setting.

## Decision
Before RP1C selection, freeze a Branch A2 Runtime Configuration Impact Assessment that compares:

1. `AMBIENT-UNSET` — all five planned DOTNET compilation variables absent from the child process environment;
2. `EXPLICIT-REFERENCE-ALL-ON` — TieredCompilation, TieredPGO, QuickJit, QuickJitForLoops and ReadyToRun explicitly set to `1`.

The future assessment must cover ordinary Release tests, M10 replay/determinism checks, a representative non-VR2 performance owner, and a fresh exact-v9 ambient-versus-explicit profile comparison. The ambient comparison establishes project-level operational equivalence only; it does not claim low-level JIT switch introspection.

A negative engineering outcome is evidence, not an infrastructure RED. No production/runtime setting is selected automatically.

## Consequences
- C4, exact-v9 and thresholds remain immutable.
- The explicit all-ON profile is a qualification reference only.
- If ambient and explicit profiles are project-equivalent and both green, later adjudication may conclude that no explicit production runtime change is necessary.
- If explicit configuration improves VR2 behavior but regresses ordinary, replay/determinism or unrelated validated performance behavior, it cannot become an RP1C repair.
- Full-Domain Performance Confirmation 2 remains blocked until A2 returned evidence is adjudicated green.
