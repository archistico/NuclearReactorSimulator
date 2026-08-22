# Pre-M11 Nuclear Code V&V Review — Reviewed pre-M11 baseline

## Status

**REVIEWED / PRE-FREEZE — not promotion evidence.**

This pass converts the earlier phenomenon inventory into an executable traceability design for the final M10 closure. It is based on the actual M10.9.8.5 candidate tree and does not modify runtime or tests.

## 1. Source-driven consequence

The review keeps the same methodological position established in Drafts 1–2:

- code quality must be traceable from model/requirement to test and acceptance criterion;
- separate-effect/model evidence must be distinguished from integral system qualification;
- coupling/timestep/convergence are qualification subjects in their own right;
- current results should be compared against previously accepted evidence without blindly rerunning every superseded historical experiment;
- qualification claims must be phenomenon-specific and scoped to known limits.

This is consistent with the book's TRACE discussion of separate-effect versus integral-effect assessment and its explicit reporting of where a code performs well or overpredicts; with AC2's unit/single-effect/integral-effect regression tiers and continuous validation; and with CASL/VERA's unit/nightly/weekly testing of coupled multiphysics software.

## 2. New artifact: M10 final V&V matrix

The reviewed work produced the matrix design that is now frozen in `../eng/m10-final-vv-matrix.json`; the earlier Draft-1 JSON/CSV were planning intermediates and are not required in the integrated source tree.

The matrix has **27 rows**. Every row now contains:

- exact phenomenon/model owner;
- evidence class and controlled qualification label;
- exact source/test/script references where present;
- exact scope/qualified range;
- known limitation;
- final-gate tier(s);
- current routes that must be rerun;
- historical routes that are provenance only;
- explicit acceptance criteria;
- whether the one-hour long campaign must observe that phenomenon;
- closure status.

The original placeholder forms such as `tests/.../PointKineticsSolverTests.cs` have been resolved to concrete repository paths and fully qualified test classes.

## 3. Current qualification result

The matrix still has only two deliberately pending closure rows:

- `HMI-OPS-01` — pending explicit M10.9.8.5 manual acceptance;
- `LONG-SOAK-01` — pending implementation and pass of the final one-hour-scale long campaign.

All other rows have existing owner evidence but are marked `EVIDENCE-PRESENT-REQUIRES-FINAL-RERUN`: this prevents earlier green evidence from being mistaken for a final M10 non-regression pass.

## 4. Exact-v4 reference authority incorporated

The matrix imports the existing authoritative 300 s exact-v4 contract rather than inventing new short-run tolerances:

- `integrated-operations-desktop-stable@4`;
- `FourNodeBranchContinuityCorrectedCommitOptIn`;
- `CorrelationConsistentInverseDomain`;
- 10 ms fixed step;
- 300 s / 30,000 steps;
- all **19 frozen I.3 budgets**;
- instantaneous conservation ceilings already frozen in the current test:
  - mass closure `<= 1e-6 kg`;
  - energy closure `<= 1e-2 J`;
  - balance mass-rate `<= 1e-8 kg/s`;
  - balance power `<= 1e-3 W`.

This is important: the final long gate will not create a new conservation standard after seeing its result.

## 5. Curated current evidence, not historical-script flooding

The final cumulative gate should be assembled from current owner routes. Examples:

- Phase I exact-v4 thermodynamic/coupling stages and 300 s requalification;
- current generator/grid and electrical protection gates;
- current main-steam/turbine focused gates;
- M10.9.7 mission closure;
- M10.9.8.2 healthy matrix;
- M10.9.8.3 degraded/fault/protection/takeover matrix;
- M10.9.8.4 replay/checkpoint/same-seed integrity;
- M10.9.8.5 integrated-HMI preflight/manual acceptance.

Historical H.19–H.30 and earlier failed/superseded numerical experiments remain provenance unless a current gate explicitly depends on their frozen evidence. For example, the post-H28 long-horizon audit is recorded as historical/frozen for `TH-HYD-01`, while the current I.5 cross-profile requalification is the rerun route.

## 6. Frozen long validation contract

The exploratory Draft 1 proposed a 3,600 simulated-second campaign. After the validated cumulative gate supplied a timing-only calibration point (the current exact-v4 300 s reference required approximately 62 s wall clock on the validation workstation), that draft was deliberately superseded **before the first long acceptance run**. No physics model or pass/fail tolerance was changed.

The authoritative contract is now `eng/m10-final-long-validation-contract.json`, schema `m10-final-long-validation-contract-v2`, with **14,400 simulated seconds / 1,440,000 authored 10 ms steps** across five independently interpretable legs:

- LR-H1: 7,200 s healthy exact-v4 whole-plant soak;
- LR-M1: 4,400 s production mission @2 continuation;
- LR-D1: 1,800 s degraded required-measurement/recovery;
- LR-P1: 900 s protection/takeover precedence;
- LR-R1: 100 s replay/checkpoint sentinel.

The replay leg is intentionally shorter because its recorder retains immutable frame history and then performs full replay plus checkpoint continuation. Long physical exposure is carried by the first four legs; replay reconstruction adds deterministic execution beyond the 1,440,000 authored steps. Wall clock remains diagnostic rather than an acceptance threshold.

## 7. Long criteria frozen before execution

The following are blocking criteria and were frozen before the first acceptance execution:

- unhandled exceptions = 0;
- non-finite physical observations = 0;
- unsupported water/steam envelope excursions = 0;
- fingerprint mismatches = 0;
- fallback-commit violations = 0;
- unsafe corrected commits = 0;
- untargeted branch disagreements = 0;
- unexpected trip/protection events outside the intentional LR-P1 SCRAM = 0;
- unexpected fault activations = 0;
- exact-version identity drift = 0;
- continuation-induced duplicate timeline rows = 0;
- existing exact-v4 instantaneous conservation ceilings remain unchanged.

LR-H1 additionally reuses the 19 frozen I.3 budgets on 24 rolling 60-second windows ending every 300 s through 7,200 s, for 456 comparisons. A failure is diagnostic/blocking; it is **not permission to widen a budget after seeing the result**.

Evidence growth is also bounded structurally: MISSION lifecycle spine <= 32, recent operational evidence <= 100, replay frame growth exactly contiguous, and serialized full/half archive size ratio <= 2.25.

## 8. Workload-shape decisions are now frozen

The fault activation/clear points, SCRAM/takeover steps, replay checkpoint/action steps and progress strides are all part of contract v2. `eng/validate-m10-final-long-validation-contract.ps1` also proves that production `src/` is byte-identical to the validated cumulative baseline and that the previous test surface is unchanged apart from the single explicit long-validation test class.

Absolute process-memory and wall-clock thresholds are intentionally not invented. Wall time is machine dependent; structural evidence growth is checked directly and existing H.28/I.5 performance evidence remains the authority for numerical cost.

## 9. Current final M10 sequence

Completed:

1. M10.9.8.5 manual acceptance;
2. M10.9.8 CLOSED;
3. 27-row `eng/m10-final-vv-matrix.json` frozen pre-long;
4. `scripts/run-m10-final-validation.cmd` implemented and validated through Hotfix 1;
5. `m10-final-cumulative-validation-passes=True`.

Remaining:

1. execute `scripts/run-m10-final-long-validation.cmd`;
2. inspect/finalize the long evidence without retuning the frozen contract;
3. produce the M10 closure record and promote the final V&V matrix;
4. only then declare **M10 CLOSED** and authorize M11.

## 10. Decision from this pass

**No new production physics is recommended before these gates.** The review has not found a reason to widen the physical scope merely to improve a qualification table. The appropriate engineering action is to make the current scope, evidence and limitations explicit, then execute the final non-regression/long campaign.
