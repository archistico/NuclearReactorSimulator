# M10.9.4.1-H.21 — Documentation & Static Consistency Audit

## Status

**PASSED (documentation/static only) — 2026-08-18.**

This review does **not** validate H.21 runtime behavior. The authoritative validated baseline remains **M10.9.4.1-H.20** until the user can run the H.21 build, ordinary suite and focused gate.

## Basis reviewed

- H.20 validated package and user-reported H.20 focused result.
- H.21 full candidate package.
- authoritative restart/status documents (`README.md`, `PROJECT_STATUS.md`, `ROADMAP.md`, `PROJECT_HANDOFF.md`, `NEW_CHAT_START.md`).
- H.19/H.20/H.21 milestone documents and validation checklists.
- ADR 0142–0147.
- H.21 source seams in coupling definition/mode, factories, `HybridSemiImplicitHydraulicGateSolver`, `PlantNetworkOrchestrator`, numerical snapshot and H.21 sidecar types.
- H.21 focused test and gate script.
- four frozen H.20 evidence files embedded in H.21.

## Source-scope verification

Compared with the H.20 package, the original H.21 candidate contains **23 changed files, 12 added files and 0 removed files**. H.21 is therefore not a documentation-only/source-descriptor change: existing integration plumbing is intentionally modified to expose the opt-in mode and telemetry.

Existing production-source files changed by H.21 include:

- `ApplicationDescriptor.cs`;
- `ColdShutdownInitialConditionFactory.cs`;
- `DesktopSustainedGenerationInitialConditionFactory.cs`;
- `HydraulicNumericalCouplingDefinition.cs`;
- `HydraulicNumericalCouplingMode.cs`;
- `HybridSemiImplicitHydraulicGateSolver.cs`;
- `PlantNetworkHydraulicNumericalSnapshot.cs`;
- `PlantNetworkOrchestrator.cs`.

The relevant safety property is narrower and is supported statically: **standard factory defaults remain explicit, while the new mode is separately opt-in and its corrected candidate cannot be committed in H.21.** Runtime equivalence is intentionally left to the H.21 lockstep gate.

## Static contract findings

1. `FourNodeBranchContinuityShadowIntegrated` is a separate numerical mode; the H.21 evidence factory opts into it explicitly, while factory parameters default to `false`.
2. H.21 and the legacy H.5 hybrid mode are mutually exclusive in the operational seed factory.
3. `FourNodeBranchContinuityShadowIntegrationStepResult.CorrectedCandidateCommitted` returns `false` by construction.
4. `PlantNetworkOrchestrator.StepFourNodeBranchContinuityShadowIntegrated(...)` executes the sidecar observationally, then separately builds the returned state through the historical explicit pipe/valve/pump accumulation and fluid/thermal integration order.
5. H.21 telemetry is attached to `PlantNetworkHydraulicNumericalSnapshot`; the generic snapshot still reports `UsedSemiImplicitCorrection=false` for the applied state.
6. The H.21 gate script reruns the full H.19 focused gate, then H.20, then H.21.
7. The H.21 explicit test requires 2,000/2,000 explicit-vs-H.21 presentation equality, 2,000/2,000 repeat equality, 15 triggers, 15/15 candidate eligibility, zero rollback, zero commits, zero untargeted disagreements, residual/closure guards and deterministic telemetry.
8. The four embedded H.20 evidence files are guarded by newline-normalized SHA-256 fingerprints and summary assertions.
9. The four embedded H.20 evidence files were compared directly with the user-supplied `h20-four-node-activation-rollback-contract.zip`; all four newline-normalized canonical SHA-256 values match exactly, and those values also match the constants in the H.21 ordinary regression test.

## Documentation corrections applied

- Removed the stale README checkpoint that still described H.17 as current validated and H.18 as the working source.
- Replaced ambiguous statements that H.21 “returns/applies the explicit predictor”. The implementation actually keeps the **historical explicit orchestration path** authoritative for the returned state while the sidecar predictor is observational.
- Added this distinction explicitly to the H.21 design document and ADR 0147.
- Reconciled ADR 0142–0145 status lines with the now-validated H.17/H.18/H.19 history.
- Kept H.21 unvalidated everywhere; no document infers validation from package existence or static review.

## What this audit can and cannot prove

This static review supports consistency of the package structure, documentation, configuration defaults, authority boundaries, evidence provenance and gate intent. It cannot prove compilation, runtime determinism, exact 2,000-step trajectory equality, trigger count, numerical residual bounds or absence of runtime regressions. Those remain the responsibility of the existing H.21 local gate.

## Required runtime gate when available

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-four-node-orchestrator-shadow-integration-audit.cmd
```

Only a green result from those commands can promote H.21 over H.20.

## Post-audit compiler finding — H.21 Hotfix 1

The subsequent user-run `dotnet build` on 2026-08-18 found one compiler error that this no-.NET static review did not detect: CS0136 in `FourNodeOrchestratorShadowIntegrationAuditTests.cs`. The test used `repeatFingerprint` for a per-interval presentation fingerprint and later reused the same local name for the aggregate repeat-telemetry fingerprint in the containing method scope. C# rejects that nested/containing-scope reuse.

H.21 Hotfix 1 changes only the per-interval name to `repeatPresentationFingerprint`. The structural/provenance conclusions above remain unchanged, but this finding reinforces the explicit limitation of this audit: it never substituted for the required local compiler/test gate.

## Final runtime outcome

After Hotfix 1 repaired the audit-only CS0136 collision, the user reported successful compilation, complete ordinary tests and the cumulative H.21 focused gate on 2026-08-18. The earlier static audit remains a provenance/consistency checkpoint; H.21 Hotfix 1 is now independently runtime-validated.
