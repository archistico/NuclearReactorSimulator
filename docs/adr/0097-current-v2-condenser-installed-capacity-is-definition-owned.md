# ADR 0097 — Current-v2 condenser installed capacity is definition-owned

## Status

Accepted as M10.9.4.1-C.2 candidate decision
**Date:** 2026-07-24

## Context

A.2 raised the sustained current-v2 condenser cooling boundary to 40 MW and maximum condensation flow to 20 kg/s. The later root-cause audit showed those values did not fix the original 300-second failure; the operating-seed energy/hydraulic imbalance was the actual cause. C.1 then closed condensate phase-change energy and the cumulative B.3 + C.1 candidate passed local compilation and tests.

The remaining ambiguity was semantic: `AvailableHeatRejectionPower` was being described as installed capacity even though the same runtime input is intentionally scaled by condenser-cooling faults. A plant's installed hardware capacity and its currently available capacity are different concepts.

## Decision

1. `CondenserCoolingBoundaryDefinition` may own an optional `MaximumInstalledHeatRejectionPower`. Null preserves legacy input-only semantics.
2. The two sustained current-v2 profiles explicitly declare **40 MW installed heat-rejection capacity**.
3. `CondenserCoolingBoundaryInput.AvailableHeatRejectionPower` means **runtime available capacity** after operating-condition/fault effects. It starts at 40 MW in the sustained profiles but may fall independently.
4. Effective heat-rejection capacity is the minimum of installed capacity, runtime available capacity and surface-transfer capacity from the existing `UA·ΔT` law.
5. The existing **20 kg/s maximum condensation mass-flow** remains an independent throughput ceiling.
6. No C.2 retuning is performed. The locally green values 40 MW / 20 kg/s / 1.225 MW/K / 20 °C are retained while ownership and diagnostics are corrected.
7. Legacy definitions without explicit installed capacity preserve historical behavior exactly: runtime available capacity remains the effective external installed ceiling.

## Consequences

- Cooling-capacity faults can reduce availability without silently redefining installed plant hardware.
- HMI/audit can distinguish installed, available and surface-UA limits.
- Future detailed circulating-water models can own availability while the condenser definition retains plant design capacity.
- C.2 does not resolve non-condensables, circulating-water hydraulics or the later whole-network enthalpy/flow-work migration.
