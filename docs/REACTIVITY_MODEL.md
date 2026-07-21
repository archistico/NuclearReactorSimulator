# Reactivity Model

M2.1 introduces the first reactor-physics primitive: signed reactor reactivity and a deterministic diagnostic breakdown of its independent contributions.

## Scope

Reactivity is represented as the dimensionless quantity:

```text
rho = delta-k / k
```

`Reactivity` stores `delta-k/k` canonically and exposes explicit conversions for:

- `delta-k/k`;
- percent `delta-k/k`;
- pcm (`1 pcm = 1e-5 delta-k/k`).

Reactivity is signed. Positive values add reactivity; negative values remove it.

M2.1 deliberately does **not** introduce dollars/cents because those units require an effective delayed-neutron fraction model that belongs with neutron kinetics.

## Contribution model

Each contribution is immutable and explicitly identified:

```text
ReactivityContribution
    Id
    Kind
    Value
```

Initial categories are:

```text
ControlRods
FuelTemperature
CoolantTemperature
Void
Xenon
Other
```

The categories are diagnostic source labels only. M2.1 does not yet calculate their values from rod position, temperature, void fraction or isotope inventories. Those physical mappings arrive in later M2 milestones.

## Deterministic composition

`ReactivityModel`:

1. validates globally unique contribution IDs;
2. canonicalizes contribution order by kind and ordinal ID;
3. performs compensated summation in that canonical order;
4. returns an immutable `ReactivityBreakdownSnapshot`.

This means input collection enumeration order is not part of the physical result or diagnostic ordering.

The snapshot exposes:

```text
Total
Contributions
TotalFor(kind)
```

## Critical boundary

M2.1 establishes this architecture:

```text
rod / temperature / void / xenon models
                 │
                 ▼
       reactivity contributions
                 │
                 ▼
          ReactivityModel
                 │
                 ▼
       total rho + diagnostics
                 │
                 ▼
       M2.3 point kinetics
```

There is intentionally no shortcut such as:

```text
reactivity -> reactor power
```

M2.3 will define the dynamic neutron-kinetics response to reactivity.

## Determinism

The model is pure and stateless. Equal contribution sets produce equal totals and canonical diagnostics. Runtime integration tests verify that the result is unchanged by external UI/scheduler pulse segmentation.


## M2.3 integration

M2.3 consumes `ReactivityBreakdownSnapshot.Total` as the signed input to `PointKineticsSolver`. Reactivity composition remains algebraic and independently diagnosable; neutron kinetics owns all time-dependent population/precursor response.
