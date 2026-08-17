# ADR 0140 — Diagnose Extended Trigger 723 Before Changing Hysteresis or Solver

## Status

Accepted for M10.9.4.1-H.15 candidate.

## Context

Validated H.13 Hotfix 2 showed that targeted thermodynamic branch continuity removes the two original H.9 failures in the 500-interval evidence set. Validated H.14 Hotfix 1 then broadened that policy to 2,000 intervals and found 15 P060/F040 events. Fourteen converge; interval 723 is the sole remaining H.9 line-search exhaustion.

At interval 723 the selected H.13 bounded policy records zero branch overrides and zero hysteresis releases. Therefore broadening or retuning the `steam`/`stop-out` hysteresis would be unsupported by the observed failure.

## Decision

Before changing any nonlinear corrector or thermodynamic continuity policy, diagnose interval 723 with the already validated H.10-H.12 evidence mechanisms generalized across all hydraulic paths and fluid nodes.

The diagnosis must include adjacent committed controls, per-node fixed-point mass/energy residual ranking, all-path/all-node two-scale smoothness probes and all-node inverse-map branch inspection.

The audit must be falsifiable: it may conclude that no local switching/non-smooth or inverse-branch mechanism is present. In that case the next investigation is fixed-point existence/residual floor and basin structure.

## Consequences

- Production remains explicit at 10 ms.
- H.13 bounded hysteresis remains unchanged and targeted only where already validated.
- H.9 remains unchanged.
- No activation candidate is authorized by H.15.
- Solver complexity is not increased without new root-cause evidence.
