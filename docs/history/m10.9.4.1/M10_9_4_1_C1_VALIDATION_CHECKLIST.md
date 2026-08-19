# M10.9.4.1-C.1 Validation Checklist — Condenser Phase-Change Energy Closure

## Dependency state

This candidate is built on **M10.9.4.1-B.3**, which was not locally validated when C.1 was prepared. Do not mark either B.3 or C.1 validated unless the complete gates below are green.

## Build and ordinary suite

```text
dotnet clean
dotnet restore
dotnet build --no-restore
dotnet test --no-build
```

Expected: zero build errors/warnings under the repository policy and zero ordinary-suite failures.

## Focused C.1 checks

Confirm the focused tests cover:

- legacy condenser definitions default to `LegacyHotwellSpecificInternalEnergy`;
- sustained current-v2 seeds opt into `SaturatedLiquidAtSteamSpacePressure`;
- a pressure-resolved condenser requires a saturation-property provider;
- saturated-liquid energy is resolved from committed condenser pressure;
- condensed mass and energy close exactly once;
- hotwell energy addition uses condensate energy, not the previous hotwell energy;
- maximum-flow, inventory and thermal limits remain independently observable;
- installed cooling capacity and surface-`UA` are not reported as active while unused headroom remains.

## Explicit operational gates

```text
scripts\run-gameplay-long-tests.cmd
scripts\run-operational-envelope-audit.cmd
```

The exact 300-second 5 MWe journey must remain healthy. Record at minimum:

- condenser pressure min/max and final slope;
- hotwell mass and temperature min/max and final slope;
- actual condensation flow;
- maximum-flow, inventory and thermal condensation limits plus active limiter;
- condensate specific internal energy and phase-change `Δu`;
- installed cooling power, `UA·ΔT`, effective capacity and unused margin;
- turbine stage flow and exhaust inventory;
- protection state and any latched function;
- global mass/energy closure residuals.

## A.2 headroom decision gate

Do **not** automatically remove or retain the current-v2 40 MW / 20 kg/s headroom merely because C.1 passes.

After C.1 is green, compare the 300-second evidence with the active-limit diagnostics:

- if installed capacity and/or maximum condensation flow never govern and have large persistent margin, prepare a separate C.2 evidence change to test whether the A.2 headroom can be reduced without regression;
- if either limit genuinely governs during accepted transients, retain it until a physical capacity model replaces it;
- do not retune the seed to hide a condenser imbalance.

## Manual HMI check

On `TURBINE & SECONDARY CYCLE`, verify the condenser section shows readable MODEL diagnostics for:

- `CONDENSATE ENERGY · MODEL`;
- `PHASE-CHANGE Δu · MODEL`;
- `ACTIVE CONDENSATION LIMIT · MODEL`.

These fields are presentation-only and must not alter replay-v1 fingerprints.

## Promotion rule

C.1 may be promoted only after B.3 and all C.1 gates are green. Any new trip, thermodynamic out-of-range state, persistent hotwell drift or conservation regression blocks promotion and must be investigated before any capacity retuning.
