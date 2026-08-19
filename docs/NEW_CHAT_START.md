# Nuclear Reactor Simulator — authoritative new-chat start

- **Authoritative validated baseline:** `M10.9.4.1-I.2 — Audit Consolidation & CI Baseline Hardening` VALIDATED on 2026-08-19.
- **Phase H:** CLOSED as `OPT-IN ONLY`.
- **Authoritative default / rollback / reference:** exact v2 `integrated-operations-desktop-stable` using `ExplicitCommittedState` at 10 ms.
- **Qualified opt-in:** exact v3 using `FourNodeBranchContinuityCorrectedCommitOptIn`.
- **I.1 validated compatibility:** 12 exact versions across 9 IDs; 2 compatibility-retained; 0 delete-now.
- **I.2 validated CI baseline:** ordinary/current-evidence/scheduled-long/historical-frozen tiers established; H.5/H.21 no longer current-CI dependencies but remain source-retained.
- **Current candidate:** `M10.9.4.1-I.3 Hotfix 5 — Corrected 300 s Healthy Reference Requalification`; I.3 is not validated. Hotfix 4 diagnostic evidence is green: 338/338 explicit drops coincide with targeted reverse flow, exact v3 has 0 drops/reverse flow over 100 s. Hotfix 5 extends v3 to 300 s before any H.30 re-review.
- **I.3 scope:** exact-v2 300-second healthy reference trajectory, one-second samples, consolidated conservation/inventory observations, final-60-second slopes and first-generation versioned regression tolerance budgets. No numerical/runtime retuning.

## Current gate

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scriptsun-phase-i-corrected-300s-healthy-reference-requalification-audit.cmd
```

Required flags:

```text
corrected-300s-generation-health-passes=True
corrected-300s-targeted-train-continuity-passes=True
corrected-300s-conservation-inventory-passes=True
corrected-300s-deterministic-repeat=True
i3-hotfix5-corrected-reference-requalification-passes=True
h30-policy-rereview-unblocked=True
```

After a green I.3, freeze its exact trajectory/slope/tolerance artifacts and continue with known-limitations/compatibility closure plus the remaining cumulative M10.9.4.1 gate. Do not begin M10.9.5 yet and do not retire H.5/H.21 until their remaining source-level historical dependencies are explicitly removed or archived.

Read `docs/PROJECT_HANDOFF.md`, the I.3 document/checklist/static review, ADR 0162 and `docs/OPERATIONAL_ENVELOPE_NUMERICAL_HARDENING_PLAN.md` before changing code.
