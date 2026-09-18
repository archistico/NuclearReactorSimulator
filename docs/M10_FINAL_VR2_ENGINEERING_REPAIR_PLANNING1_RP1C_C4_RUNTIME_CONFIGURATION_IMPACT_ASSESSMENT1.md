# M10 Final - VR2 Engineering Repair Planning 1 - RP1C C4 Runtime Configuration Impact Assessment 1

## Status

**CANDIDATE - evidence-only Branch A2 implementation authorized by returned Planning 1 and Host-Provenance Amendment 1 adjudications.**

This gate asks whether the project behaves equivalently enough for qualification under the caller's normal ambient runtime configuration and under the explicit all-ON reference profile that produced the best bounded exact-v9 central-distribution evidence.

It does not recommend a production runtime configuration and does not authorize RP1C selection or Full-Domain Performance Confirmation 2.

## Frozen runtime profiles

| Profile | TieredCompilation | TieredPGO | QuickJit | QuickJitForLoops | ReadyToRun |
| --- | --- | --- | --- | --- | --- |
| `AMBIENT-UNSET` | unset | unset | unset | unset | unset |
| `EXPLICIT-REFERENCE-ALL-ON` | 1 | 1 | 1 | 1 | 1 |

The caller must enter with all five variables unset. Every child process receives one of the two frozen profile environments; the runner does not silently sanitize a contaminated caller.

`EXPLICIT-REFERENCE-ALL-ON` is a qualification reference only.

## Execution-host boundary

One complete A2 run belongs to one physical host.

Before any evidence process, the orchestrator captures a SHA-256 host fingerprint derived from the system UUID, CPU name, system manufacturer/model, logical processor count, physical memory and OS version/build. The raw UUID, machine name and user name are not retained.

The same fingerprint is injected into every child-run contract and every exact-v9 runtime-context file. The host is captured again at gate end. A fingerprint change is infrastructure RED.

The active Windows power scheme is captured at start and end and must remain unchanged. Absolute wall-clock observations are host-scoped; this gate does not authorize cross-host timing extrapolation or threshold scaling.

## Assessment families

### A. Fresh exact-v9 profile comparison

- five fresh processes per profile, ten total;
- counterbalanced process order across five blocks;
- 360 frozen exact-v9 rows;
- 16 warm-up passes;
- 64 measured passes;
- rotation stride 37;
- 23,040 measured calls per process;
- 230,400 measured calls total;
- unchanged strict maximum `409.30666666666673 us`;
- diagnostic-only `100 us` tail floor;
- complete 64 x 360 identity/order verification per process;
- per-call timing, allocation, resolution state/path and strict/diagnostic flags;
- candidate/harness allocation and GC deltas retained.

A repeated strict owner requires the same frozen row/boundary group to exceed the strict ceiling in at least two independent processes of the same profile.

### B. Ordinary Release suite

The complete non-explicit test inventory is executed under each profile. Each of the five test projects emits a structured xUnit XML report. Counts are derived only from structured XML, not from localized console text.

A test failure with a valid structured report is engineering impact evidence and does not turn the A2 harness RED. Missing or malformed structured reports are infrastructure failures.

### C. M10 replay/determinism focused set

The four frozen replay/determinism classes are executed under both profiles without weakening any equality, replay, fingerprint or determinism predicate. Structured xUnit XML is again the authoritative count source.

### D. Representative non-VR2 performance owner

`M10972Hotfix2TenMillisecondHotPathHardeningTests` is executed in three fresh processes per profile. Existing allocation and same-process relative-performance assertions remain unchanged. Its generated summary and metrics are copied immediately after each process.

This family is intentionally outside the exact-v9/C4 micro-path.

## Evidence tree

A completed A2 gate contains exactly 78 files:

- exact-v9: 10 processes x 4 files = 40;
- ordinary Release: 2 profiles x 3 files = 6;
- replay/determinism: 2 profiles x 3 files = 6;
- non-VR2 performance: 6 processes x 3 files = 18;
- aggregate: 8 files.

Aggregate files are:

1. `01-contract-and-provenance.txt`
2. `02-execution-host-provenance.txt`
3. `03-host-consistency-audit.txt`
4. `04-exact-v9-profile-summary.csv`
5. `05-ordinary-suite-impact-summary.csv`
6. `06-replay-determinism-impact-summary.csv`
7. `07-non-vr2-performance-impact-summary.csv`
8. `08-runtime-configuration-impact-assessment1-summary.txt`

## Engineering-negative versus infrastructure RED

The harness fails only when the experiment cannot be trusted: caller runtime contamination, build failure, host/power-plan drift, missing structured test reports, incomplete exact-v9 matrices, evidence shape corruption or other contract-integrity defects.

These are not infrastructure RED by themselves:

- a strict exact-v9 exceedance;
- different timing distributions between profiles;
- ordinary-suite test failures with complete structured results;
- replay/determinism failures with complete structured results;
- non-VR2 hot-path assertion failures with complete structured results.

Those outcomes are retained for returned-evidence engineering adjudication.

## Interpretation boundary

The aggregate adjudicator verifies integrity and summarizes observations only. It records no automatic ambient/default equivalence promotion and no production runtime recommendation.

Returned-evidence adjudication must decide whether ambient defaults are sufficient, whether the explicit reference has any project-wide regression, whether any strict owner is reproducible, and whether Full-Domain Performance Confirmation 2 planning may open.

## Authority boundary

The following remain false:

- Full-Domain Performance Confirmation 2 authorization;
- RP1C selection authorization;
- production runtime change authorization;
- production repair authorization;
- C4 mutation authorization;
- threshold change authorization;
- exact-v9 change authorization;
- VR3 authorization;
- P3-R1 authorization;
- second replacement-long authorization.

## Hotfix 2 execution-contract clarification

The structured test contract follows native .NET 10 Microsoft Testing Platform / xUnit v3 semantics. `--results-directory` and `--minimum-expected-tests 1` are `dotnet test` options and occur before the `--` forwarding separator. xUnit report/filter switches occur after the separator. The xUnit XML reporter is treated schema-aware and observation-aware. On the returned .NET 10 / xUnit v3 3.2.2 schema-3 report, `total` included `not-run`; other documented reporter semantics may expose `total` as executed-only. The harness therefore derives `executed = passed + failed + skipped`, accepts only `total = executed` or `total = executed + not-run`, records the detected accounting mode, and uses `executed` for non-zero and hot-path cardinality checks. This clarification is harness-only and does not change the engineering experiment.


## Working-copy generated-output policy (Hotfix 4)

Repository identity for A2 is defined over canonical `src` and pre-existing `tests` files, excluding generated `bin/` and `obj/` subtrees. These directories may legitimately exist in a developer working copy after restore/build/test and must not be interpreted as source drift. Candidate ZIP packaging remains clean and excludes generated build output. No canonical source hash, C4/exact-v9 content, threshold or authority is changed by this policy.


A previous failed A2 attempt may leave partial files under the owned A2 artifact root. This is permitted in the working copy because the A2 orchestrator deletes and recreates that entire owned root before new evidence collection, preventing stale/new evidence mixing.
