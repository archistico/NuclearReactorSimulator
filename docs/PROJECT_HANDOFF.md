# Nuclear Reactor Simulator — Project Handoff

> **CURRENT AUTHORITATIVE CHECKPOINT — 2026-08-19**
>
> **Validated performance baseline:** `M10.9.4.1-H.28.1-G — Untargeted Branch-Disagreement Scan Fast Path` VALIDATED.
>
> **G evidence:** 20/20 trigger/commit, 0 rollback/unsafe/fallback violation, 35 hydraulic evaluations, 32 probes, Jacobian dimension 32, triggered p95 `79.7023 ms`, estimated unchanged-H.28 p95 ratio `10.821618172190465`, exact deterministic fingerprint `518BA948637F0C270C7F8228AB97FEF9148E29A4F5CE4376319AB5D1CFBE7F38`.
>
> **Current candidate:** `M10.9.4.1-H.28 Requalification 2 — Performance, Cost & Long-Running Operational Soak`.
>
> **Default production:** `ExplicitCommittedState` at 10 ms. **H.29 remains blocked.**
>
> If H.28 Requalification 2 passes unchanged ceilings, rerun H.24 once over the stabilized runtime before H.29. This block supersedes older status lines below where they conflict.

> **Authoritative numerical baseline:** M10.9.4.1-H.27 Hotfix 1 VALIDATED.
>
> **Validated diagnostic baseline:** M10.9.4.1-H.28.1-A Hotfix 2 — Performance Attribution.
>
> **Validated performance implementation evidence:** M10.9.4.1-H.28.1-C Hotfix 2 — H.9 Jacobian/Probe Allocation & Hot-Path Optimization.
>
> **Failed performance evidence:** H.28 remains `unbounded-regression`; it is not a validated baseline.
>
> **Validated predictor optimization:** M10.9.4.1-H.28.1-B — Historical Explicit Predictor Reuse — VALIDATED.
>
> **Working candidate:** M10.9.4.1-H.28 Requalification 2 — Performance, Cost & Long-Running Operational Soak.
>
> **Activation status:** H.29 blocked; standard current-v2 remains `ExplicitCommittedState` at 10 ms.

## 1. Validated activation-hardening chain

- **H.19 VALIDATED:** four-node shadow policy 473/473.
- **H.20 VALIDATED:** fail-closed authority and 8/8 typed rollback challenges.
- **H.21 Hotfix 1 VALIDATED:** real-orchestrator sidecar wiring with zero corrected commits.
- **H.22 VALIDATED:** first opt-in corrected ownership, 443 commits / 2,000 steps.
- **H.23 Hotfix 2 VALIDATED:** replay/checkpoint/reverse-power protection equivalence.
- **H.24 Hotfix 1 VALIDATED:** 30,008 committed runtime steps, 9,626 corrected commits, all four nominal profiles trip-free; 4h31m55s rare qualification gate.
- **H.25 VALIDATED:** five targeted protection/transient scenarios, 837 runtime steps, 178 commits.
- **H.26 Hotfix 1 VALIDATED:** 12/12 atomic explicit fallbacks across all typed rollback reasons/denial controls.
- **H.27 Hotfix 1 VALIDATED:** six bounded off-design scenarios, 2,080 steps, 529 commits, four corrected-qualified and two protected-boundary outcomes, zero unsafe/fallback commits.

## 2. Why H.28 did not advance the baseline

H.28 did not change the numerical runtime and remained correct/deterministic, but its focused cost gate failed:

```text
median wall ratio        9.1252571494799053
p95 wall ratio         100.01553278882017
trigger average step   1702179.99 us
trigger allocation     43460418 bytes
performance class      unbounded-regression
```

Therefore H.27 Hotfix 1 remains the authoritative validated baseline. H.29 default activation is blocked.


## 2A. H.28.1-A first build and Hotfix 1

The first local H.28.1-A build failed only in `FourNodeBranchContinuityShadowIntegrationSolver.cs` with eight CS0136 errors. The new non-trigger attribution branch declared `authority*`, `sidecar*`, `result` and `attribution` locals whose names were reused later in the containing method scope.

Hotfix 1 renames only the non-trigger locals to `noTriggerAuthority*`, `noTriggerSidecar*`, `noTriggerResult` and `noTriggerAttribution`. Measurement formulas, registry writes, H.9/H.20/H.22 decisions and all numerical behavior are unchanged. H.27 Hotfix 1 remains the validated baseline through the H.28.1-A repair sequence.

## 2B. H.28.1-A Hotfix 2 architecture repair

Hotfix 1 compiled, but the ordinary suite then failed only `ArchitectureRulesTests.SimulationProject_DoesNotUseWallClockTimerOrDelayApis`: direct `Stopwatch` calls in the new Simulation-layer attribution violated the existing deterministic architecture contract.

Hotfix 2 removes direct wall-clock and allocation-counter API calls from the Simulation project. Simulation reads only an internal audit measurement provider; the H.28.1-A focused Application test temporarily injects `Stopwatch.GetTimestamp` and `GC.GetAllocatedBytesForCurrentThread` readers and restores the prior provider on scope disposal. Public/production construction cannot configure this provider. The weak-reference attribution registries and deterministic H.9/H.21/H.22 records remain unchanged.

H.27 Hotfix 1 remains the validated baseline until build, ordinary tests and the focused H.28.1-A Hotfix 2 gate all pass.

## 3. H.28.1-A question

Where is the corrected-path cost actually spent, and how much is intrinsic H.9 numerical work versus duplicated/allocating implementation work?

H.28.1-A adds diagnostic timing/allocation around existing phases only. It performs no optimization and no retuning.

## 4. Determinism-safe diagnostic design

Nondeterministic wall-clock/allocation values must not enter deterministic record equality. H.28.1-A therefore uses `ConditionalWeakTable` registries keyed to existing result/telemetry object identity.

A fresh 128-step numerical trace must still equal the failed-H.28 fingerprint:

```text
518BA948637F0C270C7F8228AB97FEF9148E29A4F5CE4376319AB5D1CFBE7F38
```

## 5. Frozen contract

Do not retune or replace:

- P060/F040;
- H.9 finite-difference Newton/tolerances;
- 2% pressure / 5 K hysteresis;
- `steam|stop-out|header|turbine-inlet` target set;
- H.20 authority;
- H.22 commit seam;
- physical coefficients;
- 10 ms simulated fixed step.

Standard factory remains explicit. H.24 is not rerun.

## 6. Validation

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-four-node-performance-attribution-audit.cmd
```

## 7. Remaining Phase H roadmap

```text
H.28.1-A  performance attribution (VALIDATED)
H.28.1-C  H.9 allocation/hot-path optimization (VALIDATED)
H.28.1-B  historical explicit predictor reuse (VALIDATED)
H.28.1-G  untargeted branch-disagreement scan fast path (VALIDATED)
H.28      Requalification 2 performance/cost/soak qualification (CURRENT CANDIDATE)
H.29      production activation candidate only if H.28 becomes green
H.30      closure decision: ACTIVATE / OPT-IN ONLY / REMAIN EXPLICIT
```

## H.28.1-C Hotfix 2 validated optimization

Validated H.28.1-A measured H.9 at ~1.654 s and ~41.52 MB per triggered step, with Jacobian build/probes ~1.556 s and ~39.07 MB. Work is regular: 35 hydraulic evaluations, 32 probes, dimension 32, one accepted Jacobian build, no fallback pathology.

H.28.1-C therefore changes implementation cost only: transient Jacobian trials use canonical fluid-node lists rather than materializing full `PlantState` graphs; immutable hydraulic topology indexes are cached in the evaluator; intermediate combined-balance dictionaries and duplicate evaluation canonical copies are removed. The internal water/steam saturation scan also uses a private value-type carrier so the 512-sample inverse scans do not allocate a public saturation-property object at every sample; the public API, equations and branch/search order are unchanged. Final H.9 candidate state materialization remains unchanged at the ownership boundary.

H.28.1-C Hotfix 2 passed build, ordinary tests and focused audit. It preserved 20/20 trigger/commit behavior, 35/32 work counts, dimension 32 and fingerprint `518BA948637F0C270C7F8228AB97FEF9148E29A4F5CE4376319AB5D1CFBE7F38`. Jacobian/probe allocation fell from 39,071,378 B to 925,328 B per trigger and total H.9 allocation from 41,523,908 B to about 1,004,460 B, while Jacobian wall time remained ~1.558 s.

## H.28.1-B validated optimization

H.28.1-B targets the remaining ~9.31 ms sidecar predictor on non-trigger steps. The real orchestrator already computes both the same-step fallback fluid-node candidate and the committed hydraulic solve. The candidate passes those results plus the historical applied total balances into an internal exact-reuse seam. For each fluid node, reuse is allowed only when the historical applied total balance exactly equals the canonical H.4 hydraulic-plus-frozen-non-hydraulic balance; mismatched nodes are reintegrated through the unchanged H.4 path. Predictor-end hydraulics are still evaluated for F040, and the legacy public predictor path remains unchanged.

The H.28.1-B gate passed with exact 20/20 trigger/commit behavior, exact 35/32 H.9 work counts and the same deterministic fingerprint. Non-trigger predictor wall fell to ~392 us (~4.2% of the H.28.1-C value) and non-trigger engine wall to ~10.36 ms, while the triggered Jacobian remained ~1.561 s.

## H.28.1-D current candidate

H.28.1-D targets that remaining CPU bottleneck without reducing the 32 probes or changing Newton. Probe fluid-node states are reused only on exact hydraulic-balance equality; changed nodes use the original integration path. The fixed 513-point coarse saturated-mixture grid reuses precomputed immutable saturation properties, while boundary-aware and bisection paths stay dynamic. The focused gate requires material wall reduction versus H.28.1-B and the exact H.28 fingerprint. H.28 remains failed until its original gate is rerun. Because runtime implementation code changed in C/B/D, H.24 must be rerun once after the performance branch stabilizes and before H.29; it is not chained into each optimization iteration.

## H.28.1-G working checkpoint

H.28.1-G is now user-validated: build, complete ordinary suite and focused G gate passed. It preserved 20/20 trigger/commit behavior, 35 hydraulic evaluations, 32 probes, Jacobian dimension 32 and the exact deterministic fingerprint, while reducing triggered p95 to 79.7023 ms below the unchanged H.28 readiness threshold 88.3812 ms. H.28 Requalification 2 is the current candidate and reruns the original H.28 ceilings unchanged. H.29 remains blocked; if H.28 becomes green, H.24 long-horizon qualification must be rerun once before activation review.