# M10 Final — P3-R1 to P6 Detailed Gate Matrix

**Status: PLANNING CANDIDATE.**

| Gate | Core question | Allowed changes | Blocking outputs | Success output | Next |
| --- | --- | --- | --- | --- | --- |
| VR0 | are references independent/reproducible and tolerances frozen? | docs/test reference harness only | reference gap, circularity | reference contract PASS | VR1 |
| VR1 | does point kinetics match independent step-reactivity reference within frozen numerical/model budget? | test/reference code only | reference gap; material discrepancy for active owner | model-assessed kinetics | VR2 |
| VR2 | how far is simplified water/steam from IAPWS inside supported envelope? | test/reference code only | thermodynamic discrepancy invalidating exact-v9 reasoning | error map + claim band | VR3 |
| VR3 | does I/Xe reproduce a canonical independent shutdown trajectory? | test/reference code only | active-owner material discrepancy | model-assessed I/Xe | VR4 |
| VR4 | how far is reduced decay heat from ANS-5.1? | test/reference code only | active-owner material discrepancy | model-assessed decay heat | VR5 |
| VR5 | do external results allow exact-v9 owner localization to proceed? | docs/decision only | repair required or reference gap | `PROCEED-P3R1-EXACTV9` | P3-R1 |
| P3-R1 | which canonical owner causes slow inventory/hydraulic redistribution? | test-only diagnostics | inconclusive | exact owner classification | P3-R2 |
| P3-R2 | is there a contract defect, coherent slow state, or unresolved ambiguity? | docs/decision only | inconclusive | no-repair or repair authorization | P4 or P3-R3 |
| P3-R3 | can proven defect be repaired minimally? | bounded production code only | failed reference/ordinary/requalification | new exact version if semantics changed | affected-gate requalification |
| P4 | can 5→10→5 be executed and stabilize under frozen branch-approved semantics? | test/workload only as already authorized by branch decision | trip, stationarity fail, replay fail | short manoeuvre PASS | P5A |
| P5A | can baseline 2 be frozen coherently? | manifests/docs only | identity mismatch | baseline-2 freeze | P5B |
| P5B | does replacement-long 2 pass frozen acceptance? | none during run | any blocking long failure | long PASS | P6 |
| P6 | is M10 genuinely closed? | docs/release state only | CI/V&V/long/document mismatch | `m10-closed=True` | M11 |

## P3-R repair discipline

A P3-R repair must be traceable to a specific violated equation/contract/owner. Tuning a coefficient until P1B slopes look smaller is explicitly forbidden.

## P4 readiness contract

P4 must start from a qualified 5 MWe state and must prove a stable 10 MWe state before return. Mere request observation does not count. Inventory/stationarity criteria are frozen before execution.

## Long-baseline discipline

P5A and P5B are separate. Never edit the P5A freeze after P5B begins. A failed P5B result remains frozen provenance.
