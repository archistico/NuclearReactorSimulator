# ADR 0008 — Strongly typed SI physical quantities

## Status

Accepted for M1.1.

## Context

The simulator will combine neutronics, thermal hydraulics, mechanical flows, control systems and electrical models. Passing unlabelled `double` values between these subsystems would make unit mismatches easy to introduce and difficult to detect in tests.

The model also needs to distinguish quantities that share a dimension but have different physical constraints. Absolute temperature cannot be negative in kelvin, while a temperature difference can be signed. Absolute pressure cannot be below vacuum, while a pressure difference can be signed.

## Decision

Canonical physical values are represented by immutable strongly typed value objects in `NuclearReactorSimulator.Domain.Physics.Quantities`.

Rules:

1. Canonical storage uses SI units.
2. Public factories and conversion properties make non-SI units explicit.
3. There are no implicit conversions to or from `double`.
4. Every stored numeric value must be finite; `NaN` and infinities are rejected at construction boundaries.
5. Intrinsically non-negative absolute quantities reject negative values.
6. Signed differences/rates remain signed where direction or change has physical meaning.
7. Dimensionally meaningful operators may return another physical quantity (`Length × Length → Area`, `Mass / Volume → Density`, `Energy / Time → Power`).
8. Solvers may unwrap canonical SI scalars internally for numerical algorithms, but public physical model boundaries should prefer the strong quantity types.
9. New quantity types are introduced only when required by an implemented physical model, not speculatively.

## Initial M1.1 quantity set

- `Length`, `Area`, `Volume`;
- `Mass`, `Density`;
- `Temperature`, `TemperatureDifference`;
- `Pressure`, `PressureDifference`;
- `Energy`, `SpecificEnergy`, `Power`;
- `MassFlowRate`, `VolumetricFlowRate`.

## Consequences

Benefits:

- dimensional intent is visible in APIs;
- many unit mistakes become compile-time type errors;
- invalid non-finite/negative absolute values fail at construction boundaries;
- tests can assert conversions independently from physical solvers;
- later plant models have one canonical unit convention.

Costs:

- more small value types and conversion code;
- numerical solvers must explicitly unwrap SI values when operating on vectors/matrices;
- dimension types do not by themselves prove physical correctness, so model-specific invariants remain necessary.
