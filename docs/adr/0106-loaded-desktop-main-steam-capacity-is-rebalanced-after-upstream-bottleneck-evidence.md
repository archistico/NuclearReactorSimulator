# ADR 0106 — Loaded desktop main-steam capacity is rebalanced after upstream-bottleneck evidence

## Status

Candidate.

## Context

D.3.2 closed the physical isolation seam by limiting pressure-driven stage admission to the complete stop/control/admission train. The first attempted loaded-seed correction increased only the control-valve bias and had almost no effect. Hotfix 2 then corrected the stop-valve pressure grade, but local execution still reported 11.792 kg/s effective stage flow and 4.350 MW shaft power.

The committed seed pressure map shows the remaining upstream limit: the drum/steam-outlet to main-steam-header pipe at 1,000 Pa·s²/kg² carries only about 12.0 kg/s, while the corrected fully open stop valve and 28% control valve each support about 13.02 kg/s. The line therefore prevents the generation-ready desktop profile from reaching its established electrical-support contract.

## Decision

Only the loaded desktop current-v2 profile uses 850 Pa·s²/kg² for the main-steam line. At the existing seed pressure difference this provides about 13.02 kg/s, matching the adjacent valve capacities. The breaker-open synchronization profile remains at 1,000 Pa·s²/kg² because it owns a different unloaded operating contract.

The 28% control-valve bias, 276.7 °C stop-out seed, stage resistance, all valve resistances and all controller parameters remain unchanged.

## Consequences

The loaded profile gains the minimum upstream capacity required to satisfy the existing generation-ready flow, shaft-power and gross-output gates without weakening those gates or bypassing valve isolation. Distinct loaded and synchronization hydraulic contracts are now explicit and regression-tested.
