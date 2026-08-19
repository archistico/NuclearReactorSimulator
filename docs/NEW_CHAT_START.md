# Nuclear Reactor Simulator — authoritative new-chat start

- **Authoritative validated baseline:** `M10.9.4.1-H.28 — Performance, Cost & Long-Running Operational Soak` VALIDATED on 2026-08-19.
- **H.28 classification:** `bounded-but-costly`.
- **H.28 key result:** median wall ratio 4.6215 <= 8; p95 ratio 10.6844 <= 12; allocation ratio 1.1164 <= 16; soak 379/379 commit, 0 rollback/fallback/unsafe/untargeted disagreement; deterministic fingerprint `518BA948637F0C270C7F8228AB97FEF9148E29A4F5CE4376319AB5D1CFBE7F38`.
- **Current candidate:** `M10.9.4.1-H.24 Requalification 1 — Post-H.28 Committed Long-Horizon & Cross-Profile Regression`.
- **Reason:** H.28.1 changed committed-runtime implementation code, so the roadmap requires one H.24 long-horizon rerun after optimization stabilizes.
- **Production/default:** `ExplicitCommittedState` at 10 ms.
- **Corrected mode:** `FourNodeBranchContinuityCorrectedCommitOptIn`, still separately opt-in.
- **H.29:** blocked until this post-H.28 H.24 regression is green.

## Current gate

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-four-node-post-h28-committed-long-horizon-requalification-audit.cmd
```

Focused domain: 30,000 qualification intervals + 8 transition steps across `steady-long`, `load-pulse`, `cooling-pulse`, `combined-load-cooling`. No H.9/P060-F040/hysteresis/physics/timestep retuning is allowed.

Required final flags:

```text
post-h28-four-node-committed-long-horizon-cross-profile-requalification-passes=True
h24-post-h28-requalification-audit-passes=True
```

After a green result, promote H.24 Requalification 1 and proceed to **H.29 — Production Activation Candidate**. Carry the H.28 `bounded-but-costly` classification into H.29/H.30; do not treat the corrected path as automatically eligible for default activation.

Read `docs/PROJECT_HANDOFF.md`, `docs/M10_9_4_1_H24_POST_H28_REQUALIFICATION.md`, its validation checklist, and the Phase H completion roadmap before changing code.
