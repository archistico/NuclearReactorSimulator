# Nuclear Reactor Simulator — authoritative new-chat start

- **Authoritative validated baseline:** `M10.9.4.1-H.29 — Production Activation Candidate` VALIDATED on 2026-08-19.
- **H.29 result:** 1,026 runtime steps; 400/400 trigger/eligible/authorized/commit; zero rollback/fallback/unsafe/untargeted disagreement; deterministic fingerprint `BB16A2395682226B6E037901317D70B4A12E8E5C184CFC0E7C4B044643B05D68`; replay/checkpoint exact; v3 preserved and v2 still loadable.
- **H.28 classification:** `bounded-but-costly`; median wall ratio 4.6215, p95 ratio 10.6844, allocation ratio 1.1164.
- **Current candidate:** `M10.9.4.1-H.30 — Phase H Closure & Production Qualification Decision`.
- **Candidate closure decision:** `OPT-IN ONLY`.
- **Authoritative default:** exact v2 `ExplicitCommittedState` at 10 ms.
- **Qualified opt-in:** exact v3 `FourNodeBranchContinuityCorrectedCommitOptIn`.
- **Kill/rollback:** explicit deployment kill always resolves to exact v2; H.20 remains same-step fail-closed inside corrected runtime.
- **H.30 runtime scope:** none beyond `ApplicationDescriptor` metadata; selector, H.9/H.20/H.22, P060/F040, hysteresis, coefficients and timestep remain frozen.

## Current gate

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-phase-h-closure-production-qualification-decision-audit.cmd
```

H.30 fingerprints frozen H.19-H.29 evidence and does not rerun H.24/H.28. Required final flags:

```text
phase-h-production-policy-decision=OPT-IN ONLY
phase-h-closure-evidence-chain-passes=True
h30-audit-passes=True
phase-h-closed=True
phase-i-unblocked=True
```

After a green result, promote H.30, close Phase H and resume Phase I with v2 explicit still authoritative and v3 corrected retained as qualified opt-in.

Read `docs/PROJECT_HANDOFF.md`, `docs/M10_9_4_1_H30_PHASE_H_CLOSURE_PRODUCTION_QUALIFICATION_DECISION.md`, its validation checklist/static review, ADR 0159 and the Phase H completion roadmap before changing code.
