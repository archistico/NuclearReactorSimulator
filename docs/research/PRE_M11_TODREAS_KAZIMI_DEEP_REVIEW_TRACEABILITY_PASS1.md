# Todreas/Kazimi Deep Review Pass 1 — Traceability Matrix

## Status

**Pre-Plan-Amendment-2 research traceability only.** This matrix maps the selected Volume I/II sections to project consequences. It is not a P1B contract, not a validation matrix and not implementation authorization.

| ID | Source / section | Deep-review finding | Transferability | Immediate planning consequence | Earliest owner/home |
|---|---|---|---|---|---|
| `TK1-HEM-01` | Vol I §5.1.3 | HEM assumes equal phase velocity and thermodynamic equilibrium; richer physics needs added closure/equations. | `DIRECT_CONCEPT` | P1B-style diagnostics may observe current HEM-like state but may not invent slip/phasic-temperature variables. | M10 planning; M12/M14 fidelity |
| `TK1-FLOW-01` | Vol I §11.3 | HEM, slip/drift and two-fluid models have different domains and capabilities. | `DIRECT_CONCEPT` | Keep current fidelity label explicit; no M10 drift-flux/two-fluid upgrade. | Known limits; M12+ |
| `TK1-PDROP-01` | Vol I §11.6 | Two-phase pressure change has acceleration, friction and gravity components plus local/form losses. | `DIRECT_CONCEPT` | Prefer canonical component decomposition; mark unavailable components instead of reimplementing correlations. | Plan Amendment 2 candidate observables |
| `TK1-LED-01` | Vol I §11.8.1.1 | One imposed ΔP may intersect heated-channel characteristic at multiple flows; not every root is stable. | `DIRECT_CONCEPT` | Separate stationarity/root closure from stability and branch qualification. | M12.0; M10 ordering rule |
| `TK1-DWO-01` | Vol I §11.8.1.2 | Density-wave dynamics arise from delayed thermal-hydraulic response; stability boundary depends on model assumptions. | `DIRECT_CONCEPT` / numbers non-transferable | Record oscillatory trends if observed, but do not label them density-wave physics in current lumped model. | M10 diagnostics; M12/M14 later |
| `TK1-NUM-01` | Vol I §11.8 | Time-domain instability can be numerical as well as physical. | `DIRECT_CONCEPT` | Keep deterministic-repeat/coupling/numerical sentinels beside physical residuals. | Plan Amendment 2 candidate |
| `TK1-BOIL-01` | Vol I Ch.13 | ONB, NVG, saturated boiling and thermal equilibrium are distinct stages. | `DIRECT_CONCEPT` / `FUTURE_ONLY` | Current model cannot diagnose ONB/NVG; record only canonical phase/quality/void. | Known limits; M12/M14 |
| `TK1-CHF-01` | Vol I §13.4 | DNB and dryout are distinct critical mechanisms. | `FUTURE_ONLY` | Future thermal-limit model must state which mechanism/surrogate it represents. | M12/M15 |
| `TK1-BC-01` | Vol I Ch.14 | Pressure-pressure heated-channel BC can admit multiple flow roots; boundary choice changes problem. | `DIRECT_CONCEPT` | Do not treat emergent flow and pressure as two arbitrarily prescribed acceptance targets. | Plan Amendment 2 / M12.0 |
| `TK1-BUOY-01` | Vol I Ch.14 | Forced, natural and mixed convection differ by significance of density/buoyancy head. | `DIRECT_CONCEPT` | Observe canonical head balance if represented; do not synthesize a new buoyancy model in M10. | M12 extreme hydraulics |
| `TK2-PLEN-01` | Vol II Ch.1 | Parallel heated channels between common plena have equal ΔP when plenum radial gradients are neglected. | `DIRECT_CONCEPT` / RBMK geometry `ANALOGICAL` | If current branch/group data exist, report redistribution/common-boundary compatibility. | Plan Amendment 2 candidate; M14 |
| `TK2-FSPLIT-01` | Vol II Ch.1/4 | Flow split is a solution constrained by total flow, heating, properties and shared hydraulic boundaries. | `DIRECT_CONCEPT` | Future channel-group hydraulics must not prescribe every group flow independently. | M14 hydraulic fidelity |
| `TK2-SCALE-01` | Vol II Ch.2 | Scaling preserves selected responses/phenomena and accepts distortions; it does not preserve everything automatically. | `DIRECT_CONCEPT` | Keep “reduced-order RBMK-like” claim; do not map simulator seconds directly to prototype seconds. | Known limits / docs |
| `TK2-SCALE-02` | Vol II §2.7 | Level-2 scaling emphasizes component mass/energy inventories and inter-component transfers. | `DIRECT_CONCEPT` | Rank slow owners using inventory/transfer closure, not display-variable slope alone. | Plan Amendment 2 candidate |
| `TK2-TRAN-01` | Vol II Ch.3 | Reduced transient models can filter fast compressible/acoustic behavior while retaining slower long-time response. | `DIRECT_CONCEPT` | Diagnose within represented bandwidth; do not add fast physics to explain slow tail. | M10 interpretation |
| `TK2-MULTI-01` | Vol II Ch.4 | Low-flow channel arrays can enter gravity-dominated/reversal branches and have multiple solutions. | `FUTURE_ONLY` for current normal-load case | Do not infer reversal from P1A; retain for M12 low-flow physics. | M12 |
| `TK2-LOOP-01` | Vol II Ch.7 | Simplified loop analysis is valid for macro operational/control trends; detailed local models answer different questions. | `DIRECT_CONCEPT` | P1B can be a macro owner-localization gate without becoming a high-fidelity channel solver. | Plan Amendment 2 |
| `TK2-NC-01` | Vol II Ch.7 | Pump head, buoyancy and hydraulic resistance collectively determine loop behavior; natural circulation emerges when buoyancy supports flow. | `DIRECT_CONCEPT` / geometry `ANALOGICAL` | Report represented drive/loss terms only; natural-circulation fidelity remains later work. | M12 |
| `TK2-FID-01` | Vol II Ch.12 | 3/4/5-equation and 6/7-equation formulations constitute a real model-fidelity ladder. | `DIRECT_CONCEPT` | Label current HEM-like model; upgrades require new V&V, not M10 tuning. | Known limits / M12+ |
| `TK2-CV-01` | Vol II Appendix L | Lumped CV mass/energy/momentum equations make stored-state rates first-class dynamic quantities. | `DIRECT_CONCEPT` | Mass/energy inventory rates should be central candidate observables after P1A. | Plan Amendment 2 candidate; M12.0 |
| `TK2-SENS-01` | Vol II Ch.13 | First-order gradient/Jacobian sensitivity is local around nominal state. | `DIRECT_CONCEPT` | Normalized ranking is diagnostic only; always retain raw physical-unit residuals. | Diagnostic reporting |

## Cross-source planning rule

The strongest joint Volume I/II rule for the next step is:

> **Observe the reduced model's conserved states and canonical balance owners first; do not use missing high-fidelity physics as an explanation until a dedicated model owner exists.**

## Second-pass requirement

After Plan Amendment 2 is authored, every amendment observable and decision criterion must map to one or more rows above, an existing non-Todreas source, or an explicit project-only rationale. Any criterion that requires an unrepresented physical variable or a newly imported correlation is a planning defect, not an implementation task.
