# ADR-0179 — Fail closed at defaultable Domain definition boundaries

## Status

Accepted — M10.9.7.2 Hotfix 1 REV1 VALIDATED on 2026-08-21

## Context

Several Domain quantities are immutable `readonly record struct` values with private validating constructors and public factories. C# nevertheless permits `default(T)`, which bypasses those constructors and yields zero-valued instances. This is harmless for quantities whose domain includes zero, but it violates the contract of strictly positive quantities such as `DecayConstant` and `QuadraticHydraulicResistance`.

The Domain already compensates for this at several component-definition boundaries (`PipeDefinition`, `PumpDefinition`, decay-heat and delayed-neutron definitions), but the policy was not applied consistently. The same review also found that `PlantState` described canonical-definition ownership while using record value equality, and that synchronization and rod-target definition inputs admitted degenerate or undefined values.

## Decision

1. Definition constructors must revalidate strictly-positive value-type invariants when a `default(T)` instance could bypass the quantity factory.
2. Optional strictly-positive value types must be validated when `HasValue` is true.
3. A contract described as using the plant's canonical definition means reference identity and must use `ReferenceEquals`.
4. Synchronization-window definitions must reject zero/degenerate windows and the composed generator/grid system must reject windows that span the complete nominal grid frequency or voltage envelope.
5. Undefined enum values must fail at the earliest owning definition boundary.

These are construction-time fail-closed rules. They do not authorize solver retuning or new operational limits beyond the non-degenerate bounds encoded by the owning definitions.

## Consequences

- invalid `default(DecayConstant)` and `default(QuadraticHydraulicResistance)` inputs fail before division or square-root flow equations are evaluated;
- structurally equal clones cannot masquerade as canonical plant definitions;
- invalid control-rod target enums cannot reach runtime consumers;
- existing validated reference-plant parameters remain unchanged;
- converting positive quantity structs to reference types remains unnecessary and deferred;
- hot-path lookup/index optimization remains a separate measured pre-live task.
