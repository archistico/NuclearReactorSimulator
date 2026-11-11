# M10 Final — VR2 Engineering Repair Planning 1 — RP1A Reference Domain Corpus & Seam Map Freeze

**Status:** VALIDATED — RETURNED RP1A REV1 PASS  
**Prerequisite:** VR2 Engineering Repair Planning 1 returned static audit — `PASS-AS-AUTHORED`  
**Authority:** observation/test-only corpus, seam-map and machine-local performance freeze. No production repair, tolerance change, exact-v9 reinterpretation, VR3, P3-R1 or second replacement-long execution is authorized.

## Pre-execution static review REV1

Before the first local RP1A execution, the candidate was reviewed against the returned Attempt-5 CSV schemas, current exact-v9 APIs, the Windows PowerShell 5.1 validator path and the warnings-as-errors build contract.

The review found one definite execution blocker in the original unexecuted candidate: the focused test read `reference_resolved` from CSV index 11 even though index 11 is `reference_region`. The returned node header is frozen as:

```text
0 probe_id
1 logical_step
2 elapsed_s
3 node_id
4 production_phase
5 production_quality
6 production_density_kg_m3
7 production_u_j_kg
8 production_temperature_c
9 production_pressure_mpa
10 reference_resolved
11 reference_region
12 reference_phase
13 reference_temperature_c
14 reference_pressure_mpa
15 reference_quality
16 reference_minus_production_pressure_mpa
```

REV1 corrects the parser to `bool.Parse(parts[10])` / `parts[11]` and freezes both returned CSV headers before any numeric work. It also freezes the corpus topology expected from the already-returned evidence: 40 derived VR2 reference rows, 348 Region-4 rows collapsing to 310 distinct Region-4 reference temperatures, 320 total seam boundaries after union with the ten fixed VR2 saturation temperatures, and 1,280 seam probes. The focused test requires exactly 320 rows on each of the four probe sides and rejects any generated seam temperature above 623.15 K. These are integrity checks, not new thermodynamic tolerances.

The review also confirms that RP1A uses only public production APIs from `Simulation.Tests`; unlike the earlier materiality diagnostic, it does not directly access `LatestCanonicalSnapshot`, `FixedDeltaTime` or other `internal` Application evidence seams. The bounded whole-step measurement asserts the exact-v9 identity at runtime before stepping.

No file under `src/` changes in REV1.

## 1. Purpose

RP1A freezes the evidence domain that every RP1B shadow candidate must face before candidate code or candidate timing results are inspected. It does not select or implement a repair.

The corpus combines:

1. the complete frozen VR2 thermodynamic point matrix, represented as inverse-closure reference states where `(v,u)` exists;
2. all 360 frozen exact-v9/P1B Attempt-5 node observations;
3. all 288 frozen hydraulic-path rows as downstream materiality context;
4. deterministic Region-1/4 and Region-4/2 seam probes at every supported saturation-boundary temperature actually encountered by the reference-classifiable corpus;
5. a machine-local performance baseline for the current exact-v9 `CorrelationConsistentInverseDomain` closure and a bounded exact-v9 whole-step benchmark.

RP1A is deliberately run before Families B/C/D are implemented in RP1B.

## 2. Returned Planning 1 authority

The returned Planning 1 artifact is frozen at:

```text
eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_UserReturnedAudit.txt
```

It records:

```text
status=PASS-AS-AUTHORED
next-gate=RP1A-REFERENCE-DOMAIN-CORPUS-AND-SEAM-MAP-FREEZE
production-src-change-authorized=False
thermodynamic-repair-authorized=False
exact-v9-change-authorized=False
vr3-authorized=False
```

The returned audit is authority to execute RP1A only. It is not authority to implement RP1B candidates or change production thermodynamics.

## 3. Frozen corpus sources

### 3.1 VR2 point matrix

RP1A reconstructs the same VR2 reference matrix from the already-qualified test-only IF97 Regions 1/2/4 helper:

- 10 full saturation temperatures: 100, 125, 150, 175, 200, 225, 250, 280, 300 and 340 °C;
- one saturation-pressure-only point at 360 °C;
- five compressed-liquid points: 100 °C / 0.5 MPa, 150 / 1, 200 / 2, 250 / 7 and 280 / 10;
- four superheated-vapor points: 200 °C / 0.2 MPa, 250 / 0.5, 300 / 1 and 400 / 5.

For each full saturation temperature RP1A freezes saturated-liquid endpoint, quality-0.5 mixture and saturated-vapor endpoint `(v,u)` states. The 360 °C row remains boundary/pressure-only because the bounded inverse-repair study does not silently invent a Region-3 implementation.

### 3.2 exact-v9/P1B Attempt-5 corpus

RP1A copies without reinterpretation the frozen returned files:

```text
03-node-if97-inverse-map.csv      360 rows
04-hydraulic-path-counterfactual.csv 288 rows
```

The node corpus already contains 348 IF97 Region-4 mixture interpretations and 12 Region-1 interpretations. These rows are inputs for candidate comparison; RP1A does not discard production/reference disagreement.

## 4. Seam-map construction

The seam-map is generated only from reference-supported Region-1/4/2 temperatures at or below 623.15 K. Boundary temperatures are the union of:

- the ten full VR2 saturation temperatures; and
- every exact-v9 Attempt-5 row resolved by IF97 as Region-4 mixture within the supported Region-1/2 topology.

For every distinct boundary temperature, four deterministic probes are frozen:

```text
R1-SIDE       p = psat * (1 + 1e-5)
R4-LIQUID     quality = 1e-6
R4-VAPOR      quality = 1 - 1e-6
R2-SIDE       p = psat * (1 - 1e-5)
```

Each probe records reference `(v,u,T,p,phase,quality)` plus current production resolve success, phase, temperature and pressure. The offsets are corpus-construction coordinates, not new production tolerances.

RP1A repeats the complete seam-map calculation with a fresh production model instance and requires exact row equality. Candidate families in RP1B must use this frozen map; they may not move probe coordinates after observing their results.

## 5. Performance baseline before candidate timing

RP1A measures current behavior before Families B/C/D exist:

### 5.1 Resolve baseline

- closure: `CorrelationConsistentInverseDomain`;
- states: all 360 frozen exact-v9 node inventories;
- 8 warm-up passes;
- 64 measured passes;
- metrics: per-resolve median, p95, maximum wall time and median allocated bytes.

### 5.2 Whole-step context

- exact version: `integrated-operations-desktop-stable@9`;
- authority: supervisory automatic / hold current operating point;
- 128 warm-up steps;
- 512 measured 10 ms numerical steps;
- metrics: step median, p95, maximum wall time, median allocated bytes, trip count, and p95 wall-time margin to 10,000 µs.

The whole-step measurement is context only. RP1B test-only shadow candidates cannot claim integrated runtime qualification from a projected cost.

## 6. Pre-candidate RP1B performance ceilings

To avoid choosing a ceiling after candidate timing is visible, RP1A converts the measured current-closure baseline into absolute machine-local ceilings using the already validated H.28 relative-cost policy:

```text
candidate resolve median wall ceiling = RP1A baseline median * 8
candidate resolve p95 wall ceiling    = RP1A baseline p95 * 12
candidate resolve max wall ceiling    = RP1A baseline max * 12
candidate median allocation ceiling   = RP1A baseline median allocation * 16
```

These ratios are inherited from the validated H.28 performance contract; they are not thermodynamic accuracy tolerances. RP1B must use the absolute values written by RP1A and may not recompute them after candidate measurements are known.

A candidate satisfying these test-only cost ceilings is not thereby approved for runtime activation. Any selected repair still requires later integrated runtime requalification.

## 7. RP1A artifact contract

Successful execution writes exactly these primary artifacts:

```text
01-contract-and-provenance.txt
02-vr2-reference-point-corpus.csv
03-exact-v9-node-corpus.csv
04-hydraulic-context.csv
05-seam-probe-map.csv
06-performance-baseline.csv
07-rp1a-summary.txt
```

RP1A PASS requires:

- the returned Planning 1 prerequisite remains frozen and authoritative;
- 360/360 exact-v9 node rows remain reference-resolved;
- Region-4 / Region-1 counts remain 348 / 12;
- 288 hydraulic rows are present;
- the derived VR2 reference corpus contains exactly 40 rows;
- the seam map contains exactly 320 boundaries / 1,280 probes;
- seam-map repeat is deterministic;
- the expected resolve and whole-step measurement counts are completed;
- no trip occurs in the bounded exact-v9 whole-step benchmark.

Production seam mismatches or unresolved current-production seam probes are evidence to freeze, not reasons to erase the corpus. They become RP1B comparison obligations.

## 8. Authority after RP1A

Even a PASS does not automatically authorize RP1B implementation until the complete RP1A artifact folder is returned and reviewed.

Until that review:

```text
production-src-change-authorized=False
thermodynamic-repair-authorized=False
thermodynamic-tolerance-change-authorized=False
exact-v9-change-authorized=False
rp1b-authorized=False
vr3-authorized=False
p3-r1-authorized=False
second-replacement-long-authorized=False
```

## Returned RP1A review closure

The locally returned RP1A REV1 artifact set is complete `7/7` and has been reviewed as `VALIDATED`. It freezes 40 VR2 rows, 360 exact-v9 node rows, 288 hydraulic rows, 320 seam boundaries / 1,280 seam probes, deterministic seam repeat and the machine-local RP1B candidate ceilings before any B/C/D timing result existed.

Returned evidence also preserves the current closure baseline rather than treating it as an RP1A failure: four seam probes are unresolved and 638 resolved probes disagree with the IF97 coarse phase. These findings are comparison inputs for RP1B.

The returned review authorizes only `RP1B-TEST-ONLY-SHADOW-CANDIDATE-MATRIX`. It does not authorize RP1C selection, production repair, thermodynamic tolerance changes, exact-v9 changes or VR3.
