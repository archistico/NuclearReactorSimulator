# M10 Final — Detailed Next-Steps Execution Plan

**Status: PLANNING CANDIDATE — P2R2 is validated; P3-R1 is deliberately held behind Plan Amendment 3 physical-reference assessment.**

This document is the detailed execution map from the validated P2R2 decision to M10 closure. It does not change production physics, exact-v9, the replacement workload, protection semantics, authority policy or the mission pack. It refines *when* the already-authorized P3-R1 investigation may begin and inserts an external-reference model-assessment program before any production repair is considered.

The plan is intentionally finite. Every gate has a question, inputs, outputs, stop conditions and a declared successor. An inconclusive result is a planning stop, not permission to invent the next diagnostic.

## 1. Current evidence boundary

Frozen facts at entry:

- P1B execution PASS;
- P1B owner-evidence label `COUPLED-MULTI-DOMAIN`;
- 3/3 P1A checkpoints reproduced;
- no trip or numerical-sentinel failure in P1B;
- exact-v9 6 MWe load reachability demonstrated;
- exact-v9 6 MWe whole-operating-point stationarity not demonstrated;
- P2R2 validated decision `P3-R-OWNER-LOCALIZATION`;
- P3-W unauthorized;
- P3-R owner localization authorized in principle;
- production repair unauthorized;
- exact-v9 immutable;
- second replacement-long baseline unauthorized.

The P2R2 returned summary is frozen at `eng/frozen-evidence/ordinary/M10FinalReplacementLongClosurePlan1_P2R2_ValidatedSummary.txt`.

## 2. Revised route

The route is amended as follows:

```text
P2R2 VALIDATED
    |
    v
PLAN AMENDMENT 3
Physical Reference Model Assessment hold
    |
    v
VR0 Reference/Provenance Contract Freeze
    |
    v
VR1 Point-Kinetics Independent Benchmark
    |
    v
VR2 IAPWS-IF97 Water/Steam Error Map
    |
    v
VR3 I-135/Xe-135 Shutdown Reference Trajectory
    |
    v
VR4 ANS-5.1 Decay-Heat Model Assessment
    |
    v
VR5 Consolidated Physical-Reference Decision
    |
    +----------------------------+
    |                            |
    | PROCEED                    | MODEL-REPAIR / REFERENCE-GAP
    v                            v
P3-R1 Owner Localization      bounded repair planning or plan stop
    |
    v
P3-R2 Runtime Decision
    |
    +-------------+--------------------+
    |             |                    |
    | NO REPAIR   | REPAIR REQUIRED    | INCONCLUSIVE
    v             v                    v
P4 prep       P3-R3 repair         planning stop
                  |
                  v
            exact-v10 if semantics change
                  |
                  v
            impacted-gate requalification
                  |
                  v
                 P4
    |
    v
P4 Short 5->10->5 Qualification
    |
    v
P5A Replacement-Long Baseline 2 Freeze
    |
    v
P5B Replacement-Long Execution 2
    |
    v
P6 M10 Closure
    |
    v
M11 Release/Playable-Slice work
```

## 3. Gate VR0 — Reference and provenance freeze

### Question

Can each external comparison be made against an authoritative, independent and reproducible reference without using the production implementation to generate its own expected values?

### Required source classes

- point kinetics: published equations/parameter set plus an independent analytical or high-precision numerical reference path;
- water/steam: official IAPWS-IF97 reference formulation/data or an implementation whose provenance is explicitly tied to IAPWS;
- iodine/xenon: published I-135/Xe-135 equations and parameter set, with an independent closed-form or high-precision reference trajectory;
- decay heat: ANS-5.1 reference data/formulation from an authoritative accessible source or traceable implementation.

### Freeze requirements

Before executing VR1–VR4, freeze for every benchmark:

1. source title/version/revision and provenance;
2. exact equations or tabulated reference quantities used;
3. parameter set and units;
4. sign conventions and normalization;
5. reference-case initial conditions;
6. requested sample times/points;
7. independent method used to produce expected values;
8. numerical precision of the reference path;
9. tolerance-derivation method;
10. classification rules and claim impact.

### Non-circularity rule

Production code may be *evaluated* by the benchmark but may not be used to generate the expected reference trajectory/value. Reusing the same constants is allowed only when those constants are frozen from the external reference source and the independent reference algorithm remains separate.

### Output

`VR0-REFERENCE-CONTRACT-PASS`, `VR0-REFERENCE-GAP` or `VR0-NONCIRCULARITY-FAIL`.

Only PASS authorizes VR1.

## 4. Gate VR1 — Point-kinetics independent benchmark

### Objective

Upgrade `PHY-NK-01` from internal verification only to quantified independent model assessment for the configured reduced-order point-kinetics equations.

### Canonical case set

At minimum:

- critical equilibrium, rho = 0;
- positive delayed-subcritical step around +0.25 dollar;
- positive delayed-subcritical step around +0.50 dollar;
- negative step around -0.50 dollar;
- negative step around -1.00 dollar.

The dollar conversion and delayed-neutron data are frozen in VR0. No prompt-supercritical plant-safety claim is created by this benchmark.

### Reference method

Use a mathematically independent solution path, preferably matrix exponential / closed-form modal solution for the frozen linear step-reactivity problem or a high-precision solver not sharing the production integrator implementation.

### Sample times

Freeze sample times that cover prompt response and delayed-group evolution. The plan target is a compact set such as 0+, 0.1 s, 1 s, 5 s, 20 s and 60 s where the reference remains meaningful; the exact list is frozen in VR0 before execution.

### Numerical-tolerance derivation

The reference solver precision is one input. Production 10 ms temporal-discretization error is estimated independently using a bounded timestep-refinement study (for example 10/5/2.5 ms in test-only reference execution). The acceptance tolerance is frozen before comparing the final production trajectory and must not be widened after viewing it.

### Required artifacts

- reference parameters;
- expected trajectory values;
- production trajectory values;
- absolute/relative error by sample;
- timestep refinement table;
- maximum/median error summary;
- classification and claim statement.

### Result classes

- `REFERENCE-CONCORDANT`;
- `BOUNDED-NUMERICAL-DISCREPANCY`;
- `MODEL-DISCREPANCY`;
- `REFERENCE-GAP`.

The first two may proceed to VR2. `MODEL-DISCREPANCY` goes to VR5 impact assessment before any repair. `REFERENCE-GAP` is a planning stop.

## 5. Gate VR2 — IAPWS-IF97 water/steam error map

### Objective

Quantify the accuracy of `SimplifiedWaterSteamThermodynamicModel` rather than describing it only as an educational approximation.

### Reference

IAPWS-IF97 is the external reference. The benchmark must record the exact release/revision and the source/implementation used to obtain reference values.

### Point matrix

Freeze a set of at least 15 representative points spanning the supported production domain. It should include:

- saturation-line points distributed through the normal operating temperature range, including approximately 100, 150, 200, 250, 280 and 300 degC where supported;
- representative subcooled/compressed-liquid points inside the claimed envelope;
- representative superheated-vapor points if that regime is exposed by the current model and used by production;
- seam/boundary points that are important to exact-v9 thermodynamic branch ownership.

### Quantities

Where represented by the production model:

- saturation pressure;
- saturated liquid density;
- saturated vapor density;
- liquid internal energy;
- vapor internal energy;
- latent-heat / phase-energy difference;
- any additional canonical property that production uses in branch selection or turbine/steam calculations.

### Outputs

For every property and point record:

- reference value;
- model value;
- absolute error;
- relative error;
- domain/phase label;
- whether the point is inside the current claimed operating envelope.

Also report maximum, median and p95 absolute relative error by property.

### Claim bands

The first pass is an *assessment*, not an automatic rewrite of the model. The report shall assign each property/domain one of:

- `QUANTITATIVE-EDUCATIONAL` — tight bounded error suitable for quantitative teaching claims;
- `BOUNDED-EDUCATIONAL` — material but explicitly bounded error, acceptable only with documented limitation;
- `QUALITATIVE-ONLY` — error too large for quantitative claim but not necessarily a runtime blocker;
- `MODEL-DISCREPANCY-BLOCKING` — incorrect phase ordering/domain behavior, non-finite behavior, or discrepancy large enough to invalidate current M10 reasoning.

Exact policy thresholds for these labels are frozen in VR0 before seeing the VR2 result. They are engineering claim policy, not IAPWS physics constants.

### M10 relevance

Because exact-v9/P1B slow-state behavior uses this thermodynamic closure, a blocking VR2 discrepancy is presumed M10-relevant until impact analysis proves otherwise.

## 6. Gate VR3 — I-135/Xe-135 shutdown reference trajectory

### Objective

Add quantitative model assessment to the reduced lumped iodine/xenon owner without treating a generic 9–11 h xenon peak as a universal law.

### Canonical reference case

Freeze a published parameter set and a completely specified irradiation history:

1. constant pre-shutdown flux/power long enough to establish the chosen equilibrium condition;
2. frozen I-135 and Xe-135 yields/decay constants and xenon absorption term;
3. instantaneous shutdown at t=0;
4. reference trajectory for I(t) and Xe(t) generated independently;
5. reference peak time and peak magnitude derived from that case.

### Sample times

Include early, peak-region and late points. The exact list is frozen in VR0; the target span should cover minutes through at least one day.

### Required comparisons

- iodine inventory trajectory;
- xenon inventory trajectory;
- time of Xe maximum;
- magnitude of Xe maximum;
- late decay trend;
- deterministic repeat.

### Result classes

Same four classes as VR1. A discrepancy is M10-blocking only if the current exact-v9/P1B path actually uses the affected xenon owner in a way that can materially alter the observed slow-state branch; otherwise it becomes an explicit post-M10/M14 fidelity item.

## 7. Gate VR4 — ANS-5.1 decay-heat model assessment

### Objective

Quantify how the existing reduced equivalent-group decay-heat model differs from an accepted external decay-heat reference.

### Reference case

Freeze a long-equilibrium irradiation history and normalized pre-trip power. Compare shutdown decay power at, at minimum:

- 1 s;
- 10 s;
- 100 s;
- 1 h;
- 10 h;
- 1 day.

Additional points may be frozen in VR0.

### Required artifacts

- reference normalized decay power;
- reduced-model normalized decay power;
- absolute/relative error by time;
- integrated decay-energy discrepancy over declared windows where practical;
- claim classification.

### Result interpretation

VR4 is initially model assessment. A large mismatch does not automatically make M10 invalid because P1B is not a post-trip decay-heat trajectory. It does, however, prevent any strong quantitative decay-heat claim and becomes a mandatory M12.5 input. If impact analysis shows decay heat materially participates in the exact-v9 path under review, VR5 may promote it to an M10 blocker.

## 8. Gate VR5 — Consolidated physical-reference decision

VR5 is documentation/decision only. It does not repair physics.

### Inputs

- VR0 provenance contract;
- VR1 kinetics assessment;
- VR2 water/steam error map;
- VR3 iodine/xenon assessment;
- VR4 decay-heat assessment;
- exact-v9/P1B participation map indicating which owners are active in the 5→6 MWe path.

### Decision outputs

Exactly one:

1. `PROCEED-P3R1-EXACTV9`
   - no external-reference discrepancy invalidates the current owner-localization question;
   - current limitations/claim bands are documented;
   - exact-v9 remains unchanged.

2. `BLOCK-P3R1-MODEL-REPAIR`
   - at least one discrepancy materially undermines an owner active in the P1B trajectory;
   - repair planning is required before P3-R1;
   - exact-v9 remains immutable; changed production semantics require exact-v10.

3. `PLAN-STOP-REFERENCE-GAP`
   - reference provenance, independence or tolerance contract cannot be made defensible.

### V&V claim update

VR5 updates the human-readable V&V matrix and, only after evidence exists, proposes machine-readable row promotion from pure `Verification` to `Model validation / assessment` where justified. No row is upgraded merely because the assessment script executed successfully.

## 9. P3-R1 — Primary inventory/hydraulic slow-state owner localization

P3-R1 begins only after VR5 `PROCEED-P3R1-EXACTV9` or after an explicit repair/requalification route returns to an equivalent validated state.

### Question

Why does exact-v9 continue to redistribute primary/feedwater/drum inventory and raise channel/return flow after electrical load and frequency are essentially settled at 6 MWe, and which *existing canonical runtime contract* owns that motion?

### Investigation order

1. `PlantNetworkOrchestrator` mass/energy derivatives and transfer terms;
2. primary pump snapshots: speed, active boost, internal loss, flow, hydraulic exchange, shaft demand;
3. channel/return/outlet node storage and continuity identities;
4. drum return + feedwater - recirculation - steam balance;
5. condensate/feedwater train inventories and pump transfers;
6. controller integrals, saturation and anti-windup only after physical balances are established;
7. steam/turbine/grid as downstream consequence check.

### Allowed implementation

Test-only diagnostics derived from canonical snapshots/owners. No new constitutive law, no second hydraulic solver, no new natural-circulation correlation, no protection/workload retuning and no exact-v9 semantic change.

### Outputs

- `CANONICAL-CONTRACT-CONTRADICTION` with exact identity/equation/owner;
- `COHERENT-SLOW-REDISTRIBUTION` with no contract violation;
- `INCONCLUSIVE`.

All return to P3-R2.

## 10. P3-R2 — Runtime decision

### If `CANONICAL-CONTRACT-CONTRADICTION`

Authorize bounded repair design only for the proven owner. No broad tuning.

### If `COHERENT-SLOW-REDISTRIBUTION`

Do not label the behavior a bug. Decide whether:

- current reduced-order time scale is acceptable and a workload/readiness procedure may be qualified in P4; or
- the slow state violates the intended educational plant contract and a model-fidelity change is needed.

### If `INCONCLUSIVE`

Planning stop. No automatic P3-R3.

## 11. P3-R3 — Repair and exact-version rule

A production repair is optional and exists only if P3-R2 authorizes it.

If the repair changes physical/control semantics used by production:

- exact-v9 remains immutable historical evidence;
- create exact-v10;
- rerun ordinary Release/CI;
- rerun every external-reference assessment whose owner was changed;
- rerun the earliest affected closure gate according to the impact matrix below.

### Minimum invalidation matrix

| Changed owner | Earliest closure evidence presumed invalid until disproved |
| --- | --- |
| Point kinetics/reactivity semantics | P1 and all later asymptotic/owner evidence |
| Water/steam thermodynamic closure | 5 MWe reference qualification, P1 and all later evidence |
| Primary hydraulic constitutive law/pump law | P1/P1A/P1B/P2R2 and P3-R evidence |
| Drum/feedwater inventory-transfer semantics | P1B/P2R2 and P3-R evidence; earlier load reachability reviewed for impact |
| Governor/generator semantics | P1/P1A/P1B and later load-following evidence |
| Xenon semantics | only gates whose exact configuration enables/depends on xenon, determined by impact audit |
| Decay-heat semantics | post-trip gates and any exact path where decay heat materially contributes |

No old artifact is deleted; it remains exact-v9 provenance.

## 12. P4 — Short 5->10->5 qualification

P4 is the first gate allowed to qualify the actual replacement manoeuvre after owner closure.

Required evidence:

- qualified 5 MWe start state;
- declared branch-approved 5→10 procedure;
- stable 10 MWe window, not merely requested 10 MWe;
- breaker closed/no reactor-turbine-generator trip;
- frequency/phase bounded;
- reactor thermal, steam flow, shaft and electrical power coherent;
- relevant inventory slopes/stationarity within pre-frozen bands;
- return 10→5 and stable recovery;
- replay/checkpoint determinism where applicable.

P4 must freeze its readiness/stationarity criteria before execution. No second long baseline exists until P4 PASS.

## 13. P5 — Replacement-Long 2

### P5A Baseline freeze

Freeze one coherent exact identity, workload identity, mission binding, protection/authority contract, duration/legs, evidence schema and acceptance criteria.

### P5B Execution 2

Execute once against the frozen baseline. Preserve the full artifact folder regardless of PASS/FAIL. A failure returns to the earliest invalidated prior gate; it does not authorize in-place baseline mutation.

## 14. P6 — M10 closure

M10 closes only when:

- ordinary Release PASS;
- GitHub CI green;
- external physical-reference assessments are incorporated with honest claim labels;
- P4 PASS;
- Replacement-Long Execution 2 PASS;
- replay/checkpoint/exact-version invariants preserved;
- final V&V matrix has no blocking pending/failed row;
- final documentation and production identity agree;
- closure artifact states `m10-closed=True` and `next=M11.1`.

## 15. Post-M10 immediate priorities

M11 must not become an indefinite backend expansion. The first release-hardening cycle should include:

1. a simple product version/tag separate from engineering milestone identity;
2. atomic Git commits/tags for validated gates from this point forward;
3. script/ADR lifecycle classification rather than treating all historical audits as equally active;
4. README/quick-start/screenshot/release presentation cleanup;
5. a playable manual vertical slice from cold shutdown through synchronization and stable generation before accepting broad new backend scope.

These are post-M10 priorities; they do not weaken the current M10 closure gates.


## Current execution checkpoint — 2026-09-14

Plan Amendment 3 and VR0 Reference / Provenance Contract Freeze both returned local PASS and are VALIDATED. **VR1 POINT-KINETICS INDEPENDENT BENCHMARK** is now the only authorized execution gate. The frozen VR0 reference/tolerance contract remains binding; VR1 may authorize only VR2 on PASS and cannot calibrate exact-v9 plant parameters or execute P3-R1.
