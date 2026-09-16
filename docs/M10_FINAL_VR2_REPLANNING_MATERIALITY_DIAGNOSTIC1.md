# M10 Final — VR2 Replanning / Materiality Diagnostic 1

**Status:** ATTEMPT 5 EXECUTED RED POST-EVIDENCE; RETURNED-EVIDENCE ADJUDICATION ACTIVE — HOTFIX 5  
**Prerequisite:** VR2 IAPWS-IF97 Water/Steam Error Map — returned `MODEL-DISCREPANCY-BLOCKING`  
**Authority boundary:** observation-only impact localization. No production thermodynamic repair, tolerance change, exact-v9 change, VR3 execution, P3-R1 execution or second replacement-long authorization.

**Hotfix 1 validator note:** the first local attempt stopped in the static preflight before ordinary/focused execution because the validator requested the nonexistent frozen-summary marker `thermodynamic-repair-authorized=False`. The authoritative returned-VR2 summary uses `production-repair-authorized=False`; Hotfix 1 aligns the validator to that frozen evidence while the JSON contract independently preserves `thermodynamic_repair_authorized=false`. No diagnostic semantics changed.

**Hotfix 2 validator note:** the second local attempt also stopped in static preflight before ordinary/focused execution. The CMD runner intentionally invokes Windows PowerShell 5.1 (`powershell.exe`); because the validator source was UTF-8 without BOM, its literal em dash was decoded through the legacy code page and became mojibake while the Markdown target was correctly read as UTF-8. Hotfix 2 removes Unicode-sensitive validator literals, checks the same documented state with ASCII-only markers, and enforces an ASCII-only validator-source invariant. No diagnostic semantics changed.

**Hotfix 3 build note:** the third local attempt passed the static preflight and entered the ordinary Release build, which then failed only in `NuclearReactorSimulator.Simulation.Tests` with five CS1061 errors. The diagnostic test had directly referenced the Application runtime's `LatestCanonicalSnapshot` and `FixedDeltaTime` evidence seams even though both are intentionally `internal` and `NuclearReactorSimulator.Application` grants friend access only to `NuclearReactorSimulator.Application.Tests`. Hotfix 3 keeps the diagnostic in `Simulation.Tests`, preserves production encapsulation, and reads those two existing evidence seams through bounded test-only reflection helpers. The focused diagnostic still did not execute, so no materiality evidence or engineering classification exists from that attempt.

**Hotfix 4 inverse-helper note:** the fourth local attempt passed static audit and ordinary build and entered the focused explicit test, but stopped inside the Region-1 inverse self-check before any exact-v9/P1B sample was collected. The returned materiality artifact directory is empty, so the attempt produced no materiality evidence or classification. Numeric reproduction showed that the fixed 0.875 K scan can enter the fixed-volume Region-1 reachable interval *after* an energy root that lies close to the saturation-side reachability boundary. Hotfix 4 preserves the 400-segment scan but bisects each unreachable-to-reachable transition to recover that physical boundary and use it as the first energy-bracketing point. No production or diagnostic decision thresholds change.

## 1. Why this gate exists

The returned VR2 artifacts qualified the independent IF97 reference harness and localized the blocking result to the inverse compressed-liquid pressure closure. The three M10-core forward-reference states at 200 C / 2 MPa, 250 C / 7 MPa and 280 C / 10 MPa retained the expected `SubcooledLiquid` phase but the production inverse closure returned approximately 9.84, 40.87 and 84.72 MPa. Saturation behavior, density, phase ownership and the superheated-vapor branch were materially better.

That result is blocking under the frozen VR2 contract, but VR2 alone does not answer whether the absolute-pressure bias materially changes the *pressure differences that drive exact-v9 hydraulic motion*. P1B observed persistent slow multi-domain motion at 6 MWe. Before any repair is considered, the current model must therefore be examined on the same exact-v9 trajectory and through the same canonical hydraulic laws.

This gate asks one question:

> On the actual exact-v9 P1B path, if the same committed node inventories `(v,u)` are reinterpreted with the independent IF97 reference, how much do the resulting pressure differences and existing quadratic hydraulic-flow outputs change?

## 2. What is frozen and what is not changed

The diagnostic repeats the already validated P1B execution shape:

- exact-v9 5 MWe background reference: 600 s;
- exact-v9 5→6 MWe probe with the same supervisory preparation and +1 MWe test-only generator-load command policy;
- expected load-command logical step: 2785;
- mandatory P1B checkpoint reproduction at 900 / 1800 / 3600 s;
- maximum hold: 3600 s after load;
- protection and hydraulic numerical sentinels audited every 10 ms step.

Materiality samples are downsampled every 60 s. This sampling does not drive runtime and is not a new physics timestep.

The gate changes none of the following:

- no file under `src/`;
- no thermodynamic coefficient or branch ordering;
- no hydraulic resistance;
- no pump speed, pump curve or active pressure boost law;
- no topology;
- no energy-transport convention;
- no controller, authority, workload or protection semantics;
- no exact-v9 initial-condition value;
- no VR2 claim band or tolerance.

## 3. Independent IF97 inverse interpretation

VR2 already qualified the test-only IAPWS R7-97(2012) forward helper against the official verification values. VR2-D1 extends only that test helper with a numerical inverse needed for actual runtime inventories:

- Region 1 inverse from `(v,u)` for stable compressed/subcooled liquid states within `273.15 K <= T <= 623.15 K` and `p_sat(T) <= p <= 100 MPa`;
- Region 4 + saturated Region 1/2 mixture inverse from `(v,u)` for two-phase states below 623.15 K.

The new inverse path is self-checked by round-tripping independent forward IF97 states before exact-v9 evidence is interpreted. Frozen ceiling:

```text
maximum inverse-reference round-trip relative error <= 1e-7
```

IF97 is never committed into the runtime state. The exact-v9 simulation evolves entirely with the existing production model.

## 4. Required runtime nodes

The diagnostic interprets these committed nodes from their actual mass, volume and internal-energy inventories:

```text
suction
pressure
outlet
drum
feedwater-inventory
```

For every 60 s sample it records:

- production phase and vapor quality;
- production density and specific internal energy;
- production temperature and pressure;
- whether the independent IF97 inverse resolves the same inventory;
- IF97 region/phase, temperature, pressure and quality where applicable;
- `IF97 pressure - production pressure`.

A failed IF97 inverse on a required path does not authorize a repair. It yields `REFERENCE-INVERSION-GAP` and requires review of the returned artifacts.

## 5. Pressure-only hydraulic counterfactual

For each sampled committed state the diagnostic evaluates four already-existing canonical hydraulic paths:

```text
MCP              suction -> pressure
CHANNEL          pressure -> outlet
RETURN           outlet -> drum
FEEDWATER-PUMP   feedwater-inventory -> drum
```

The production flow-law mapping is first reproduced from the committed production pressures. Maximum allowed reproduction error is:

```text
1e-9 kg/s
```

The counterfactual then changes **only** the endpoint pressure values supplied to that same quadratic law:

```text
m_dot = sign(DeltaP) * sqrt(abs(DeltaP) / R)
```

For pump paths the existing active pressure boost, speed-squared law, total resistance and check-valve semantics remain unchanged. This is an offline calculation over the committed sample. It is not a second runtime solve and does not alter any inventory.

## 6. Materiality scale

Materiality is judged against the phenomenon P1B was trying to localize, not against a new thermodynamic percentage tolerance.

The final 1200 s loaded tail is divided into the same four contiguous 300 s windows used by P1B. For each hydraulic path and each window:

```text
load-specific shift = abs(loaded-window production mean - final-300s 5 MWe background mean)
within-window drift = abs(window-end production flow - window-start production flow)
phenomenon scale    = max(load-specific shift, within-window drift, 0.01 kg/s)
impact ratio        = mean(abs(IF97-pressure-only flow - production flow)) / phenomenon scale
```

The `0.01 kg/s` floor is inherited from the existing P1B flow-checkpoint comparison scale. It is a diagnostic denominator floor, not a new physical accuracy requirement.

Window bands are frozen before execution:

- `CONFIRMED` — driving-pressure sign changes, or `impact ratio >= 1.0`;
- `NOT-EXCLUDED` — `0.1 <= impact ratio < 1.0`;
- `NOT-DEMONSTRATED` — `impact ratio < 0.1`;
- `REFERENCE-INVERSION-GAP` — the required counterfactual cannot be evaluated.

As in P1B, persistence requires at least **3 of 4** late windows in the same path.

## 7. Engineering classifications

The focused diagnostic itself is an evidence gate. A healthy execution may therefore pass while returning any of these engineering classifications:

### `HYDRAULIC-MATERIALITY-CONFIRMED`

At least one canonical path has `CONFIRMED` impact in at least 3/4 late windows. The VR2 pressure defect is on the scale of, or larger than, the actual P1B hydraulic phenomenon and cannot be dismissed as common-mode absolute-pressure bias.

This result still does **not** authorize a production repair. It authorizes only a separate thermodynamic-repair planning/decision step after artifact review.

### `HYDRAULIC-MATERIALITY-NOT-EXCLUDED`

No path reaches persistent `CONFIRMED`, but at least one path has `CONFIRMED` or `NOT-EXCLUDED` impact in at least 3/4 windows. The discrepancy remains capable of materially changing P1B interpretation and requires further decision/diagnostic work before VR3.

### `HYDRAULIC-MATERIALITY-NOT-DEMONSTRATED`

Every required path remains below the 10% phenomenon-scale band in the persistent-window test and no driving-pressure sign change occurs. This is evidence against material hydraulic impact on the observed P1B phenomenon, but the diagnostic itself still does not erase the frozen VR2 RED result or authorize VR3. A separate engineering decision is required.

### `REFERENCE-INVERSION-GAP`

At least one required exact-v9 node/path cannot be independently interpreted with the bounded Region 1 / Region 4+1/2 inverse used by this gate. No materiality conclusion is authorized.

## 8. Required artifacts

The focused run must return the complete folder:

```text
artifacts/m10-final-physical-reference-vr2-materiality-diagnostic1
```

with:

1. `01-contract-and-provenance.txt`
2. `02-p1b-checkpoint-reproduction.csv`
3. `03-node-if97-inverse-map.csv`
4. `04-hydraulic-path-counterfactual.csv`
5. `05-late-window-materiality.csv`
6. `06-materiality-summary.txt`
7. `07-sentinels.txt`

## 9. Exit and authority boundary

Execution PASS requires:

- inverse-reference self-check within `1e-7`;
- exact-v9 background and load probes complete without protection/numerical sentinel failure;
- frozen P1B load-command step and 900/1800/3600 s checkpoints reproduced;
- production hydraulic law reproduced to `1e-9 kg/s` before the IF97 pressure-only substitution;
- deterministic repeat of the offline materiality analysis;
- all required artifacts emitted.

After execution, **return the complete artifact folder before any other change**.

Regardless of classification, this candidate authorizes none of:

```text
VR3
production thermodynamic repair
thermodynamic tolerance widening
exact-v9 modification
P3-R1
second replacement-long baseline
```

Those decisions belong to the post-artifact engineering review.

## Attempt 5 returned evidence and adjudication

Attempt 5 is the first execution that completed the full exact-v9/P1B observation path and wrote the complete seven-file artifact set. The trajectory reproduces all three frozen P1B checkpoints, records zero protection/numerical/rollback sentinels, resolves all 360 node samples and all 288 hydraulic-path counterfactual rows, and the generated summary records `execution-pass=True` with provisional classification `HYDRAULIC-MATERIALITY-CONFIRMED`.

The focused test still returned RED after artifact generation because the historical diagnostic self-consistency assertion required the committed canonical flow to reproduce the instantaneous quadratic-map flow to `1e-9 kg/s`. Attempt 5 measured a maximum difference of `0.009952798974779853 kg/s`. That difference is not evidence that the quadratic flow law was reconstructed incorrectly: exact-v9 uses `H22FourNodeBranchContinuityCorrectedCommitOptIn`, whose authoritative absolute fixed-point flow residual ceiling is `1e-2 kg/s`. A committed H.22 iterate is therefore allowed to differ from its instantaneous mapped flow by up to that bound while remaining converged. The original `1e-9` assertion remains frozen as historical failed-gate provenance and is not silently relaxed or rewritten.

The returned-evidence adjudication instead applies the production numerical contract as an uncertainty bound. For each late materiality window it conservatively subtracts the full `0.01 kg/s` H.22 fixed-point bound from the returned mean absolute IF97 counterfactual shift before recomputing the impact ratio. The original materiality thresholds remain unchanged: confirmed ratio `>= 1.0`, not-excluded ratio `>= 0.1`, and persistence in at least three of four windows on the same path. Under this conservative bound, `CHANNEL` remains confirmed in 4/4 windows, including driving-head sign reversal; `FEEDWATER-PUMP` remains confirmed in 4/4 windows; `MCP` remains not-excluded in 4/4 windows; and `RETURN` remains not-excluded in 4/4 windows. The smallest returned RETURN ratio is about `0.11627`, and its conservative bound remains above `0.11616`, still above the frozen `0.1` threshold.

Attempt 5 also exposes a material phase-boundary issue that the original fixed VR2 matrix did not reveal on the actual trajectory. The `pressure` node is production `SubcooledLiquid` in 72/72 samples while the same inventory `(v,u)` resolves independently to IF97 Region 4 `SaturatedMixture` in 72/72 samples. For `suction`, production is `SubcooledLiquid` while IF97 resolves Region 4 mixture in 55/72 samples. Therefore any later repair planning must treat the discrepancy as a closure/phase-boundary problem, not merely as retuning a compressed-liquid pressure coefficient.

The active Hotfix 5 gate is a returned-evidence adjudication only. It freezes the complete Attempt-5 artifacts, verifies their internal counts and sentinels, verifies the authoritative H.22 `0.01 kg/s` fixed-point residual contract, recomputes conservative materiality robustness, and emits a short adjudication artifact. It does not replay the 3,600 s trajectory and does not authorize production repair, thermodynamic tolerance changes, exact-v9 changes, VR3, P3-R1 or a second replacement-long baseline.
