# Nuclear Reactor Simulator — Project Handoff

> **Authoritative validated baseline:** M10.9.4.1-H.24 Requalification 1 post-H.28 — VALIDATED on 2026-08-19.
>
> **Current candidate:** M10.9.4.1-H.29 — Production Activation Candidate.
>
> **Production/default:** exact v2 `integrated-operations-desktop-stable` using `ExplicitCommittedState` at 10 ms.
>
> **H.29 candidate:** exact v3 using `FourNodeBranchContinuityCorrectedCommitOptIn`; v2 remains immediate explicit kill/rollback/reference.
>
> **Final activation authority:** H.30 only.

## 1. Validated Phase H chain

- **H.19 VALIDATED:** four-node shadow policy 473/473 over the complete 30,000-interval/four-profile representative contract.
- **H.20 VALIDATED:** fail-closed authority plus 8/8 typed rollback challenges.
- **H.21 Hotfix 1 VALIDATED:** real-orchestrator sidecar wiring with committed trajectory transparency.
- **H.22 VALIDATED:** separately opt-in corrected ownership, 443 corrected commits / 2,000 steps, zero unsafe/fallback commits.
- **H.23 Hotfix 2 VALIDATED:** exact replay/checkpoint/protection interaction.
- **H.24 Hotfix 1 VALIDATED:** original 30,000 qualification intervals + 8 transition steps, 9,626 corrected commits, all four nominal profiles trip-free.
- **H.25 VALIDATED:** five protection/transient scenarios, 837 runtime steps, 178 corrected commits.
- **H.26 Hotfix 1 VALIDATED:** 12/12 same-step explicit fallbacks over typed rollback/denial controls.
- **H.27 Hotfix 1 VALIDATED:** six off-design scenarios, 2,080 steps, 529 corrected commits, four `corrected-qualified` and two `protected-boundary` outcomes.
- **H.28 VALIDATED:** unchanged performance/cost/soak gate passed after H.28.1 optimization; corrected path classified `bounded-but-costly`.
- **H.24 Requalification 1 post-H.28 VALIDATED:** the required post-optimization long-horizon regression passed.

## 2. H.28 performance classification

```text
median wall-cost ratio       4.6214685710690242 <= 8
p95 wall-cost ratio         10.684444741413872  <= 12
median allocation ratio      1.1164372201028363 <= 16
benchmark trigger/commit     20/20
soak trigger/commit          379/379
rollback/fallback/unsafe     0/0/0
untargeted disagreements     0
deterministic repeat         True
fingerprint                  518BA948637F0C270C7F8228AB97FEF9148E29A4F5CE4376319AB5D1CFBE7F38
classification               bounded-but-costly
```

This permits continued qualification but is not evidence that the corrected path is cheap. Do not weaken the 10 ms timestep or numerical contract to hide cost.

## 3. Post-H.28 H.24 requalification result

```text
profiles                     4
qualification intervals      30000
action transition steps      8
runtime steps                30008
P060/F040 triggers            9626
H.20 eligible                 9626
H.22 authorized               9626
corrected commits             9626
rollbacks                        0
safe fallbacks                   0
fallback commit violations       0
unsafe commits                   0
untargeted disagreements         0
all profiles trip-free        True
determinism control            256
deterministic repeat          True
fingerprint 7AF233CE51A866B3E00C2C032AA58EEFBD7290DE0940725E5F4B7860EA6287BE
```

This closes the single rare long-horizon rerun required after the H.28.1 runtime implementation optimization. H.29 is now unblocked.

## 4. H.29 candidate design

H.29 does not add a solver or retune the plant. It qualifies deployment mechanics around the validated chain `P060/F040 -> four-node continuity -> H.9 -> H.20 -> H.22`.

Versioning is explicit:

```text
v2 = existing validated physical seed + ExplicitCommittedState
     authoritative current default + rollback/reference

v3 = same validated physical seed + FourNodeBranchContinuityCorrectedCommitOptIn
     H.29 production-default candidate only
```

The deployment selector resolves policy before runtime construction. Explicit kill always resolves to v2. The standard integrated-operations scenario remains pinned to v2; a separate H.29 scenario is pinned to v3. Existing saves/replays therefore retain exact version meaning.

H.29 also adds an internal observational telemetry counter over already-emitted H.20/H.22 diagnostics. It has no commit/protection/control authority and is not projected into `ControlRoomSnapshot`.

## 5. H.29 validation gate

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-four-node-production-activation-candidate-audit.cmd
```

The focused audit fingerprints validated H.23/H.24-post-H.28/H.25/H.26/H.27/H.28 evidence instead of rerunning H.24/H.28. It then qualifies:

- v2 authoritative default;
- exact v3 corrected candidate;
- explicit kill -> v2;
- real H.20/H.22 trigger/eligibility/authorization/commit telemetry;
- zero unsafe/fallback commit in the nominal candidate run;
- internal telemetry reason counters;
- deterministic v3 repeat;
- exact-version v3 record/full replay/checkpoint/seek;
- v2 independent rollback/reference resolution;
- separation of internal diagnostics from operator snapshot.

Required final flags:

```text
four-node-production-activation-candidate-passes=True
h29-audit-passes=True
h30-closure-review-unblocked=True
```

## 6. After a green H.29

A green H.29 promotes the corrected path to **qualified production activation candidate**, not to authoritative default. H.30 must then evaluate the complete evidence chain and choose `ACTIVATE`, `OPT-IN ONLY` or `REMAIN EXPLICIT`. Until that decision, v2 explicit remains authoritative. Phase I remains deferred until Phase H closes.

See also:

- `M10_9_4_1_H29_PRODUCTION_ACTIVATION_CANDIDATE.md`;
- `M10_9_4_1_H29_VALIDATION_CHECKLIST.md`;
- `M10_9_4_1_H29_STATIC_REVIEW.md`;
- `adr/0158-version-h29-production-activation-candidate-with-v3-and-explicit-v2-kill.md`;
- `M10_9_4_1_PHASE_H_COMPLETION_ROADMAP_H24_H30.md`.
