# Project status

## Current checkpoint

**Authoritative validated baseline:** `M10.9.4.1-I.2 — Audit Consolidation & CI Baseline Hardening`.

**Validated post-I.2 evidence:**

- I.3 Hotfix 4 Classifier Fix 1 — validated diagnostic evidence;
- I.3 Hotfix 5 — corrected 300 s healthy reference requalification — validated evidence.

**Current candidate:** `M10.9.4.1-H.30 Requalification 1 — Production Policy Re-review after I.3 Continuity Evidence`.

Until H.30 RQ1 is explicitly validated, the prior H.30 production decision remains authoritative: `OPT-IN ONLY`, exact v2 explicit default, exact v3 corrected opt-in.

## Why H.30 was re-opened

Original H.30 had only one material argument against default activation of v3: H.28 showed a higher but bounded runtime cost.

Phase-I I.3 then demonstrated a previously unaccounted operational distinction:

- exact v2, first 100 s at 10 ms resolution: 338 generation-drop steps;
- all 338/338 drops coincide one-for-one with reverse flow in the targeted stop/control/admission train;
- exact v3 over the same 100 s: 0 drops and 0 targeted reverse-flow steps;
- exact v3 full 300 s / 30,000-step reference: 0 health violations, 0 targeted reverse-flow violations, 3,757 corrected commits, 0 rollback/fallback/unsafe/untargeted disagreement, conservation green and deterministic repeat.

The corrected path is therefore not merely an expensive alternative. It suppresses a validated continuity defect in the cheaper explicit path.

## H.30 Requalification 1 candidate policy

If the candidate gate passes, the production policy becomes:

| Role | Exact version | Numerical mode |
| --- | --- | --- |
| Authoritative desktop default | `integrated-operations-desktop-stable@3` | `FourNodeBranchContinuityCorrectedCommitOptIn` |
| Fail-closed rollback/reference | `integrated-operations-desktop-stable@2` | `ExplicitCommittedState` |
| Historical compatibility | `integrated-operations-desktop-stable@1` | explicit historical |

The v3 enum/type names retain H.29 lineage; renaming them is not required for activation.

## Numerical contract

H.30 RQ1 does **not** retune or replace:

- H.9 Jacobian mathematics;
- H.20 authority/rollback contract;
- H.22 commit ownership;
- P060/F040 trigger thresholds;
- branch-continuity hysteresis limits;
- physical coefficients;
- the deterministic 10 ms fixed step.

H.28 remains `bounded-but-costly` and its measured cost evidence stays part of the decision record.

## Phase I status

I.1 and I.2 are validated. I.3 is still not closed because its original reference/budget work was started against exact v2, which is now known to have a healthy-operation continuity defect.

After H.30 RQ1 is validated, I.3 should be resumed using the resulting authoritative production policy. Only then should versioned tolerance budgets be frozen.

Legacy H.5/H.21 numerical modes remain source-retained historical audit dependencies; retirement is still deferred.

## CI / audit tiers

Current CI distinguishes:

- ordinary build/test;
- lightweight current-evidence gates;
- scheduled/manual long-running gates;
- frozen historical evidence that is fingerprint-checked instead of rerun routinely.

H.30 RQ1 becomes the current production-policy evidence gate. Original H.30, I.1 and the I.3 re-review prerequisites become frozen superseded-policy evidence.

## Next step

Validate the current candidate with:

```bat
dotnet build
dotnet test
scripts\run-h30-rq1-production-policy-rereview-audit.cmd
```

If green, promote H.30 RQ1 and return to I.3 to establish the authoritative v3 reference trajectory/tolerance budgets before final Phase-I closure and M10.9.5.
