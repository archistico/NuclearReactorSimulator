# M10 Final Long Failure Diagnostic 10 — Exact-v8 Turbine Moisture-Drain Ownership Requalification

**HOTFIX 1 CANDIDATE — Diagnostic 10 original ordinary suite RED on one new test-only node-balance assertion; exact-v8 runtime not yet requalified; exact-v4 remains production; no production activation or replacement-long authorization.**

## Hotfix 1 — canonical turbine-inlet net-balance regression alignment

The original Diagnostic 10 candidate compiled, but the complete ordinary suite stopped with one failure out of 1480 tests. The failing moisture-drain regression asserted that `turbine-inlet` mass must decrease by exactly the 1 kg/s turbine transfer over the 1 ms fixture step. That assertion ignored the same-step canonical hydraulic inflow through the admission valve.

The returned value identifies the omitted owner exactly:

```text
admission-valve inflow              +2.236067978 kg/s
turbine total transferred flow       -1.000000000 kg/s
------------------------------------------------------
net turbine-inlet balance            +1.236067978 kg/s
1 ms inventory change                +0.001236067978 kg
observed final mass                   10000.001236067978 kg
```

Hotfix 1 does not alter the solver. The regression now derives the expected final `turbine-inlet` mass from the canonical admission-train snapshot and the stage `TotalTransferredMassFlowRate`. The independent exhaust-vapor and hotwell-moisture owner checks remain unchanged, as do the stage energy ownership and global mass/energy closure tolerances.

Therefore the original Diagnostic 10 result is **test-contract RED**, not evidence that the moisture-drain source-term implementation violates conservation. Exact-v8 still has no 600 s qualification evidence because the gate correctly stopped at the ordinary suite.

## 1. Diagnostic 9 decision

Diagnostic 9 directly closes the exact-v7 turbine-inlet mass balance over the late 120–180 s window:

```text
stage commanded - stage effective                 0.2688274455 kg/s
stage commanded * (1 - inlet vapor fraction)      0.2688274455 kg/s
measured turbine-inlet dm/dt                      0.2687819297 kg/s
closure difference                               -0.0000455158 kg/s
```

The exhaust independently closes on `stage effective - condensation` to the same order. The non-vapor fraction rejected by `VaporMassFractionLimited` therefore has no downstream owner and remains in `turbine-inlet`. This is a structural contributor to exact-v7 drift and cannot be repaired by a new seed alone.

## 2. Design choice

Diagnostic 10 does **not** restore total-mixture transport through the work-producing turbine stage. That would reintroduce the D.1 zero-work liquid bypass that `VaporMassFractionLimited` intentionally removed.

Instead a new explicit, versioned policy is introduced:

```text
VaporMassFractionLimitedWithMoistureDrain

admitted vapor      -> turbine stage -> exhaust -> condenser
rejected non-vapor  -> explicit moisture drain -> hotwell
```

The policy requires a canonical `MoistureDrainNodeId`; exact-v8 binds it to `hotwell`. Historical policies cannot name a moisture-drain node.

For saturated-mixture admission, the solver resolves saturated-vapor and saturated-liquid transport properties at the committed inlet pressure. The vapor stream alone produces shaft work; the liquid stream carries its own conservative advected energy to the drain. The stage energy audit becomes:

```text
inlet phase-resolved energy
  - vapor exhaust energy
  - moisture-drain energy
  - shaft power
= ownership residual
```

Subcooled-liquid admission produces zero work-producing stage flow and is diverted only through the explicit moisture owner. Trip blocks both stage and drain transfer.

## 3. Versioning and preservation

Exact-v8 is:

```text
integrated-operations-desktop-stable@8
```

It preserves the exact-v7 authored whole-cycle seed and the versioned breaker-closed governor integral-reference repair. The only intended runtime semantic difference is the new turbine-admission moisture owner.

Exact-v4 remains the authoritative production selector. Exact-v4, exact-v5, exact-v6 and exact-v7 remain historical/frozen evidence and continue using their previous admission policy.

## 4. Qualification workload

Diagnostic 10 runs exact-v8 for 600 simulated seconds at the unchanged 10 ms fixed step and records:

- whole-cycle mass/energy trajectory;
- all canonical fluid-node states;
- final-60 s node mass/pressure/temperature/specific-energy slopes;
- governor integral/output/control-valve drift;
- stage commanded flow;
- effective vapor flow;
- explicit moisture-drain flow;
- total transferred admission mass;
- stage energy-ownership residual;
- drum/feedwater/hotwell balances;
- full coupled energy closure;
- trip and rollback counts.

Artifacts:

```text
artifacts\m10-final-long-diagnostic10
  00-progress.txt
  110-v8-whole-cycle-moisture-drain-trajectory.csv
  111-v8-node-state-trajectory.csv
  112-v8-final60-node-slopes.csv
  113-v8-whole-cycle-moisture-drain-summary.txt
```

## 5. Decision rule

Exact-v8 may be considered for a later production-activation candidate only if returned evidence shows all of the following without widening tolerances:

```text
moisture drain owns the rejected non-vapor mass
stage energy ownership remains conservative
turbine-inlet accumulation is removed rather than displaced
exhaust/hotwell/feedwater/drum inventories remain bounded
governor repair remains effective
primary/secondary pressure and thermal slopes are near stationary
electrical export remains stable near 5 MWe
trip / rollback = 0 / 0
full-cycle energy closure remains conservative
```

If mass merely migrates to `hotwell`, or if the phase-separated energy path exposes another imbalance, exact-v8 is NOT QUALIFIED and owner diagnosis continues. Production activation and replacement long remain unauthorized until a separate qualification/activation gate passes.

## 6. Gate

Run:

```bat
scripts\run-m10-final-long-failure-diagnostic10.cmd
```

Return the complete `artifacts\m10-final-long-diagnostic10` folder before production activation, further operating-point changes or replacement-long authorization.
