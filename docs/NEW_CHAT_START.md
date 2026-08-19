# Nuclear Reactor Simulator — authoritative new-chat start

- **Authoritative validated baseline:** `M10.9.4.1-H.24 Requalification 1 — Post-H.28 Committed Long-Horizon & Cross-Profile Regression` VALIDATED on 2026-08-19, built on H.28 VALIDATED.
- **H.28 classification:** `bounded-but-costly`; median wall ratio 4.6215 <= 8, p95 ratio 10.6844 <= 12, allocation ratio 1.1164 <= 16.
- **Post-H.28 H.24 result:** 30,000 qualification intervals + 8 transitions, 9,626/9,626 corrected commits, 0 rollback/fallback/unsafe/untargeted disagreement, all four profiles trip-free, deterministic fingerprint `7AF233CE51A866B3E00C2C032AA58EEFBD7290DE0940725E5F4B7860EA6287BE`.
- **Current candidate:** `M10.9.4.1-H.29 — Production Activation Candidate`.
- **Authoritative production/default:** exact v2 `integrated-operations-desktop-stable` using `ExplicitCommittedState` at 10 ms.
- **H.29 candidate:** exact v3 of the same validated physical seed using `FourNodeBranchContinuityCorrectedCommitOptIn`.
- **Kill/rollback:** an explicit deployment kill always resolves to v2; H.20 remains same-step fail-closed inside corrected runtime.
- **Final authority:** H.29 does not activate corrected ownership by default; H.30 decides `ACTIVATE`, `OPT-IN ONLY` or `REMAIN EXPLICIT`.

## Current gate

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-four-node-production-activation-candidate-audit.cmd
```

H.29 fingerprints validated H.23/H.24-post-H.28/H.25/H.26/H.27/H.28 prerequisites and does not rerun the expensive H.24/H.28 gates. It then qualifies selection/kill, internal telemetry, deterministic v3 operation and exact-version save/replay/checkpoint compatibility.

Required final flags:

```text
four-node-production-activation-candidate-passes=True
h29-audit-passes=True
h30-closure-review-unblocked=True
```

After a green result, promote H.29 and proceed to **H.30 — Phase H Closure & Production Qualification Decision**. Until H.30 explicitly chooses otherwise, v2 `ExplicitCommittedState` remains authoritative.

Read `docs/PROJECT_HANDOFF.md`, `docs/M10_9_4_1_H29_PRODUCTION_ACTIVATION_CANDIDATE.md`, its validation checklist, ADR 0158, and the Phase H completion roadmap before changing code.
