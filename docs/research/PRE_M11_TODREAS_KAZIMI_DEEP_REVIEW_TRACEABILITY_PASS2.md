# Todreas/Kazimi Deep Review Pass 2 — Plan Amendment 2 Traceability

## Status

**VALIDATED POST-PLAN-AMENDMENT-2 LITERATURE / RUNTIME OWNERSHIP AUDIT — local PASS reported 2026-08-24.**

Disposition: `PASS-AS-AUTHORED` subject to the binding P1B implementation constraints in `PRE_M11_TODREAS_KAZIMI_THERMAL_HYDRAULIC_DEEP_REVIEW_PASS2.md`.

This matrix traces the **actual frozen Plan Amendment 2** to source principles and current runtime owners. It is not P1B execution evidence and cannot authorize P3.

| ID | Plan Amendment 2 item | Literature basis | Current project owner / evidence | Pass 2 constraint | Result |
| --- | --- | --- | --- | --- | --- |
| `TKP2-A01` | Fluid mass/internal-energy inventory | Vol I Ch.4–5; Vol II App. L | `PlantSnapshot.FluidNodes`; `PlantNetworkAudit` | committed state only | PASS |
| `TKP2-A02` | Whole thermofluid mass/energy closure | Vol II App. L | `PlantNetworkAudit`; `IntegratedPrimaryCircuitSnapshot` | use canonical audit residuals | PASS |
| `TKP2-A03` | Coupled reactor-to-grid stored energy | conservation / system balance principle | `SecondaryCycleHeatBalanceAudit` | no new total-energy equation | PASS |
| `TKP2-A04` | Feedwater / steam export | control-volume transfers | integrated primary boundaries | exact canonical sums only | PASS |
| `TKP2-A05` | Drum inventories and separation flows | Vol I two-phase CV principles | `SteamDrumSnapshot` | current reduced separator only | PASS |
| `TKP2-A06` | Loop/branch flow and pressure compatibility | Vol I Ch.14; Vol II Ch.1/4 | `MainCirculationLoopSnapshot`, `MainCirculationBranchSnapshot` | observe, do not independently solve | PASS |
| `TKP2-A07` | Pump/head/loss evidence | Vol II Ch.7 | pump/loop/hydraulic numerical snapshots | report only represented terms | PASS |
| `TKP2-A08` | Reactivity / kinetics / thermal source | owner-localization sequencing | `ReactorPrimaryControlSnapshot`, core/plant thermal state | no new neutronic model | PASS |
| `TKP2-A09` | Steam path / admission | system transfer sequencing | `MainSteamNetworkSnapshot`, turbine stage/admission snapshots | current canonical steam model only | PASS |
| `TKP2-A10` | Turbine mechanical balance | energy/shaft transfer principle | `TurbineRotorSnapshot`, stage snapshots | raw torque/power evidence | PASS |
| `TKP2-A11` | Generator/grid state and closure | downstream energy closure | `SynchronousGeneratorSnapshot`, `GeneratorElectricalAudit` | no change to coupling semantics | PASS |
| `TKP2-A12` | Controller diagnostics | closed-loop stationarity principle | `ControllerDiagnosticSnapshot` | separate arithmetic from memory | PASS |
| `TKP2-A13` | Controller integral memory | closed-loop stationarity principle | `ControllerChannelState` | explicitly not plant physical state | PASS |
| `TKP2-A14` | Actuator command / physical state | control ownership | `ControlAndActuatorSnapshot` + plant valve/pump/rod state | keep command/effective/physical distinct | PASS |
| `TKP2-A15` | Hydraulic numerical sentinels | Vol I §11.8 numerical-vs-physical caution | `PlantNetworkHydraulicNumericalSnapshot` + Phase-H provenance | ambiguity cannot become physical diagnosis | PASS |
| `TKP2-T01` | 600 s background | Vol II scaling claim discipline; project-only duration | P1B control observation | no prototype time claim | PASS |
| `TKP2-T02` | 3,600 s replay | project P1A evidence, not textbook number | frozen P1A trajectory | no blind extension | PASS |
| `TKP2-T03` | 1 s sampling | reduced-model slow-tail purpose | diagnostic downsample | per-step/event sentinels remain required | PASS |
| `TKP2-T04` | 4×300 s late windows | project trend design | diagnostic windows | no claim of statistical independence | PASS |
| `TKP2-R01` | Raw dimensional ranking | Vol II Ch.13 local sensitivity discipline | P1B reporting layer | raw units first | PASS |
| `TKP2-R02` | Cross-domain normalized score | no universal source normalization | optional in amendment | **disabled for P1B v1** | PASS |
| `TKP2-R03` | Same-observable background comparison | project-only diagnostic rationale | 5 MWe vs 6 MWe same canonical metric | no post-hoc threshold | PASS |
| `TKP2-C01` | Owner labels | project diagnostic taxonomy | evidence aggregation only | not root-cause proof by largest slope | PASS |
| `TKP2-C02` | `COUPLED-MULTI-DOMAIN` | project conservative classification | P1B engineering result | use when causal isolation is not supported | PASS |
| `TKP2-C03` | `NO-MATERIAL-LATE-DRIFT` | project conservative classification | P1B engineering result | not equivalent to stationarity qualification | PASS |
| `TKP2-X01` | Slip/phasic temperatures | Vol I fidelity hierarchy | absent | forbidden | PASS |
| `TKP2-X02` | ONB/NVG / new flow regimes | Vol I Ch.13 | absent | forbidden | PASS |
| `TKP2-X03` | Density-wave stability margin | Vol I §11.8 | absent/dedicated qualification not present | forbidden | PASS |
| `TKP2-X04` | New natural-circulation/CHF/Δp laws | Vol I/II future fidelity only | absent as new P1B owner | forbidden | PASS |
| `TKP2-G01` | P1B direct P3 decision | project governance | none | forbidden; P2R2 owns branch | PASS |

## Cross-source conclusion

The final Pass 2 rule is:

> **P1B may localize only among states and balance owners the current exact-v9 model actually represents. Conservation/inventory evidence precedes downstream response evidence; missing higher-fidelity physics is an explicit limitation, never an inferred diagnosis.**

## Implementation handoff

When Pass 2 is validated locally, P1B implementation may be prepared with these binding constraints:

- canonical/exact-derived observables only;
- no second integrator or hydraulic solve;
- no Tier C physics;
- no single scalar cross-domain owner score;
- 1 s slow trajectory plus canonical-step/event sentinels;
- P1A checkpoint reproduction remains fail-closed;
- P1B returns to P2R2 and cannot authorize P3 itself.
