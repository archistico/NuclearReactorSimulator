# Nuclear Reactor Simulator — authoritative new-chat start

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

- **Authoritative numerical baseline:** `M10.9.4.1-H.27 Hotfix 1` VALIDATED.
- **Validated diagnostic evidence:** `M10.9.4.1-H.28.1-A Hotfix 2` VALIDATED; Jacobian/probes dominate H.9 triggered cost.
- **Validated performance implementation evidence:** `M10.9.4.1-H.28.1-C Hotfix 2` VALIDATED; ~97.6% Jacobian/H.9 allocation reduction with unchanged fingerprint.
- **Failed performance evidence:** `M10.9.4.1-H.28` remains FAILED (`unbounded-regression`).
- **Validated predictor optimization:** `M10.9.4.1-H.28.1-B — Historical Explicit Predictor Reuse` VALIDATED.
- **Working candidate:** `M10.9.4.1-H.28 Requalification 2 — Performance, Cost & Long-Running Operational Soak`.
- **Production/default:** `ExplicitCommittedState` at 10 ms.
- **H.29:** blocked until the original H.28 gate is rerun and becomes green.

## Current checkpoint

- **Authoritative numerical baseline:** `M10.9.4.1-H.27 Hotfix 1 — Off-Design Robustness & Qualification Envelope` VALIDATED.
- **Validated diagnostic evidence:** `M10.9.4.1-H.28.1-A Hotfix 2 — Corrected-Path Performance Attribution` VALIDATED.
- **Failed performance evidence:** `M10.9.4.1-H.28 — Performance, Cost & Long-Running Operational Soak` remains `unbounded-regression`.
- **Validated predictor optimization:** `M10.9.4.1-H.28.1-B — Historical Explicit Predictor Reuse` VALIDATED.
- **Working candidate:** `M10.9.4.1-H.28 Requalification 2 — Performance, Cost & Long-Running Operational Soak`.
- **Standard production mode:** `ExplicitCommittedState` at **10 ms**.
- **Corrected committed mode:** `FourNodeBranchContinuityCorrectedCommitOptIn`, separately opt-in/audit-only.
- **H.29:** blocked until H.28 is rerun and passes.
- **Phase H:** open. **Phase I:** deferred.

## Validated H.27 result

```text
scenarios                         6
runtime steps                  2080
P060/F040 triggers              529
corrected commits               529
rollback/fallback/unsafe          0
untargeted disagreements          0
corrected-qualified scenarios     4
protected-boundary scenarios      2
fingerprint DB61C3D3815D6EB81EB3D26B6B44283C21094EADB9B63E86E022594ED94E40A9
```

H.24 remains the rare 4h31m55s long-horizon qualification gate and is not chained while numerical runtime is unchanged.

## Failed H.28 evidence

H.28 remained numerically green but failed performance:

```text
median corrected/explicit wall ratio    9.1252571494799053
p95 corrected/explicit wall ratio     100.01553278882017
triggered average corrected step     1702179.99 us
triggered average allocation         43460418 bytes
soak commits                               379
unsafe/fallback commits                      0
deterministic repeat                       True
```

Do not raise the H.28 ceilings or start H.29 from this result.

## H.28.1-A validated attribution result

After the CS0136 and architecture-safe measurement-provider repairs, H.28.1-A Hotfix 2 passed build, the complete ordinary suite and its focused attribution gate. It measured 20 trigger/commit steps, 35 hydraulic evaluations and 32 Jacobian probes per trigger; H.9 averaged ~1.654 s and ~41.52 MB, with Jacobian build/probes ~1.556 s and ~39.07 MB. The deterministic fingerprint remained `518BA948637F0C270C7F8228AB97FEF9148E29A4F5CE4376319AB5D1CFBE7F38`.

## H.28.1-C Hotfix 2 validated result

H.28.1-C Hotfix 2 passed build, ordinary tests and focused audit. It preserved 35 hydraulic evaluations, 32 finite-difference probes, Jacobian dimension 32 and the exact H.28 deterministic fingerprint. Jacobian/probe allocation fell to 925,328 B and total H.9 allocation to about 1,004,460 B per trigger, but Jacobian wall time remained ~1.558 s, confirming that CPU work rather than GC churn dominates the trigger path.

H.28.1-B is validated: non-trigger predictor wall fell to ~392 us and non-trigger engine wall to ~10.36 ms, while the triggered Jacobian remained ~1.561 s. H.28 remains failed and H.29 remains blocked. H.28.1-D now removes exact duplicate probe thermodynamic work and precomputes the fixed 513-point coarse saturation grid without changing the 32-probe Newton contract. H.24 is not chained into this focused development gate, but one H.24 long-horizon rerun is required after the performance optimization branch stabilizes and before H.29.

## Validation

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-hydraulic-probe-cpu-hot-path-optimization-audit.cmd
```

Read `docs/PROJECT_HANDOFF.md`, `docs/M10_9_4_1_H28_1D_HYDRAULIC_PROBE_CPU_HOT_PATH_OPTIMIZATION.md`, `docs/M10_9_4_1_H28_1D_VALIDATION_CHECKLIST.md`, `docs/M10_9_4_1_H28_1D_STATIC_REVIEW.md` and ADR 0157 before changing code.

## H.28.1-G working checkpoint

H.28.1-G is now user-validated: build, complete ordinary suite and focused G gate passed. It preserved 20/20 trigger/commit behavior, 35 hydraulic evaluations, 32 probes, Jacobian dimension 32 and the exact deterministic fingerprint, while reducing triggered p95 to 79.7023 ms below the unchanged H.28 readiness threshold 88.3812 ms. H.28 Requalification 2 is the current candidate and reruns the original H.28 ceilings unchanged. H.29 remains blocked; if H.28 becomes green, H.24 long-horizon qualification must be rerun once before activation review.