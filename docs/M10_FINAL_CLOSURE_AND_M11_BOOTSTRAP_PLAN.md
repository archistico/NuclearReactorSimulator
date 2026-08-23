# M10 Final Closure and M11 Bootstrap Plan

## Status

**PLANNING — Replacement-Long Execution 1 is RED; the P0–P6 replacement-long closure route is the active blocking path before M11.**

This document defines the final handoff from replacement-long qualification to M11. The detailed pre-closure route is authoritative in [`M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN.md`](M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN.md). That plan freezes the sequence P0 evidence/planning → P1 asymptotic qualification → P2 branch decision → P3-W/P3-R → P4 short 5→10→5 qualification → P5 replacement-long baseline/execution → P6 closure. This document retains the final long-result intake, closure-only transition and M11 bootstrap contract so that a successful replacement long cannot be followed by an improvised closure sequence.

## 1. Replacement-long result intake

When P5B Replacement-Long Execution 2 finishes, preserve its complete artifact directory before any new candidate is created. The failed first long and failed Replacement-Long Execution 1 remain immutable provenance and are never overwritten.

Record at minimum:

- contract id/workload identity;
- simulated seconds and logical steps;
- per-leg PASS/FAIL;
- unhandled exception/non-finite/envelope/fault/trip counts;
- conservation and I.3 budget results;
- coupling-safety results;
- replay/checkpoint equivalence;
- memory/evidence-growth result;
- wall-clock duration as diagnostic only.

### Required green markers

Closure eligibility requires all contract-defined blocking markers green, including:

- `m10-final-long-validation-passes=True`;
- `m10-closure-eligible=True`.

The exact P5B artifact output is authoritative; this document does not replace the executable contract. `LONG-SOAK-01` remains pending until that execution is green and P6 promotes the evidence.

## 2. If the long run is green

Prepare a **closure-only** candidate. Planned stem:

`NuclearReactorSimulator_M10_Final_Closure_Docs1_CANDIDATE_FULL.zip`

### Allowed changes

- `PROJECT.md` current status;
- `CHANGELOG.md`;
- final M10 closure document;
- final V&V matrix status/provenance;
- documentation/index/navigation;
- compact machine-readable closure record;
- closure validator/script.

### Forbidden changes

- `src/`;
- production runtime behavior;
- baseline tests that define previously validated behavior;
- long workload or acceptance criteria;
- exact historical identities;
- archive/fingerprint semantics.

### Final matrix transition

`LONG-SOAK-01` changes only from the pre-run pending state to the final validated state and records the exact evidence artifact/contract. No other M10 phenomenon row is reinterpreted.

### Planned closure record

Create a machine-readable record containing at least:

- final M10 baseline identity;
- M10.9.8.5 manual acceptance record;
- cumulative-gate PASS identity;
- long-gate PASS identity;
- 27-row final V&V matrix identity;
- explicit `m10-closed=True`;
- explicit `next=M11.1`.

### Planned gate

`run-m10-final-closure-audit.cmd`

The gate should verify:

1. cumulative prerequisite PASS recorded;
2. long prerequisite PASS recorded;
3. manual HMI acceptance recorded;
4. final V&V matrix has no blocking pending/failed row;
5. `src/` is byte-identical to the validated long baseline;
6. baseline tests are unchanged unless a closure validator test is explicitly additive and non-semantic;
7. documentation says M10 CLOSED consistently;
8. M11 is not yet claimed started/validated.

Only after this gate passes is **M10 CLOSED** authoritative.

## 3. If the long run is red

Do not create a closure candidate.

Use `CHANGE_IMPACT_REVALIDATION_POLICY.md` to classify the correction and preserve the failed run as evidence. Under Closure Plan 1, return to the **earliest invalidated gate in P1–P4** rather than starting an unplanned diagnostic chain. For long healthy-leg drift/envelope failures, `M10_LR_H1_EQUILIBRIUM_DIAGNOSTIC_PLAN.md` remains the owner-specific route before changing physical constants or thermodynamic bounds.

### Production semantic fix

If the fix changes the authored production seed or another exact-version semantic, create a new exact version; do not rewrite exact `@4`. A mission/scenario exact identity that intentionally binds the old seed also remains immutable and receives a new version only if a new production contract is needed.

Required sequence:

1. owner diagnosis;
2. minimal fix;
3. focused owner gate;
4. ordinary suite;
5. final cumulative gate rerun;
6. full long gate rerun;
7. only then closure.

### Harness-only fix

If and only if production and baseline tests remain byte-identical and frozen workload/criteria are unchanged:

1. fix validator/harness;
2. validate harness contract;
3. rerun full long gate;
4. cumulative prerequisite may remain authoritative.

## 4. Pre-M11 documentation adoption

After M10 is explicitly closed, stack a documentation-only planning checkpoint on the closed M10 baseline.

It adopts:

- the two engineering review results;
- `PRE_M11_IMPLEMENTATION_DECISIONS.md`;
- `POST_M10_TO_M15_EXECUTION_MASTER_PLAN.md`;
- `CHANGE_IMPACT_REVALIDATION_POLICY.md`;
- `M11_DIGITAL_IC_RELEASE_ASSURANCE_PLAN.md`;
- `M11_RELEASE_EVIDENCE_MATRIX_PLAN.md`;
- M13.9 Digital I&C implementation planning;
- aligned M11–M15 milestone plans and known limitations.

This checkpoint still adds no product feature.

## 5. M11.1 bootstrap

Only after the documentation checkpoint is green:

1. copy the final M10 phenomenon V&V matrix into M11 release provenance without changing its classifications;
2. create the M11 support matrix skeleton;
3. create architecture-invariant and function-allocation machine-readable contracts;
4. inventory direct dependencies/runtime packaging requirements;
5. decide supported target/package strategy;
6. write acceptance criteria before any implementation change;
7. run M11.1 focused + ordinary gates;
8. promote M11.1 before beginning compatibility work.

## 6. Hard stop conditions

Do not begin M11.2 if any of the following remains unresolved:

- M10 closure status ambiguous;
- long artifact missing or incomplete;
- final V&V matrix contains a blocking pending/fail row;
- support target/package policy not frozen;
- architecture/function-allocation contracts not adopted;
- documentation and executable product identity disagree.
