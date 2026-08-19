# Nuclear Reactor Simulator — Project Handoff

> **Authoritative validated baseline:** M10.9.4.1-H.29 — Production Activation Candidate — VALIDATED on 2026-08-19.
>
> **Current candidate:** M10.9.4.1-H.30 — Phase H Closure & Production Qualification Decision.
>
> **H.30 candidate decision:** `OPT-IN ONLY`.
>
> **Authoritative default / rollback / reference:** exact v2 `integrated-operations-desktop-stable` using `ExplicitCommittedState` at 10 ms.
>
> **Qualified opt-in:** exact v3 using `FourNodeBranchContinuityCorrectedCommitOptIn`.

## 1. Validated Phase H chain

- **H.19 VALIDATED:** 473/473 four-node representatives across the complete 30,000-interval/four-profile census.
- **H.20 VALIDATED:** fail-closed authority plus 8/8 typed rollback challenges.
- **H.21 Hotfix 1 VALIDATED:** real-orchestrator sidecar wiring with trajectory transparency.
- **H.22 VALIDATED:** separately opt-in corrected ownership, 443 corrected commits / 2,000 steps, zero unsafe/fallback commits.
- **H.23 Hotfix 2 VALIDATED:** replay/checkpoint/protection qualification with 242 corrected commits.
- **H.24 Hotfix 1 VALIDATED:** original committed long-horizon/cross-profile qualification.
- **H.25 VALIDATED:** protection/operational-transient matrix.
- **H.26 Hotfix 1 VALIDATED:** 12/12 integrated explicit fallbacks across typed denial/rollback controls.
- **H.27 Hotfix 1 VALIDATED:** six-scenario off-design envelope.
- **H.28 VALIDATED:** performance/cost/soak green, classification `bounded-but-costly`.
- **H.24 Requalification 1 post-H.28 VALIDATED:** 30,008 runtime steps, 9,626 commits, zero rollback/fallback/unsafe/untargeted disagreement.
- **H.29 VALIDATED:** 1,026 runtime steps, 400/400 corrected commits, zero rollback/fallback/unsafe/untargeted disagreement, deterministic repeat and exact-version replay/checkpoint compatibility.

## 2. H.29 validated activation-candidate evidence

```text
qualification intervals       1024
transition steps                  2
runtime steps                   1026
trigger / eligible             400 / 400
authorized / committed         400 / 400
rollback / fallback              0 / 0
unsafe / untargeted              0 / 0
deterministic repeat             True
activation fingerprint BB16A2395682226B6E037901317D70B4A12E8E5C184CFC0E7C4B044643B05D68
full replay equivalent           True
checkpoint equivalent            True
v3 preserved                     True
v2 still loadable                True
```

H.29 made v3 a technically qualified production activation candidate but intentionally left v2 explicit authoritative pending H.30.

## 3. H.28 cost classification carried into closure

```text
median wall-cost ratio       4.6214685710690242 <= 8
p95 wall-cost ratio         10.684444741413872  <= 12
median allocation ratio      1.1164372201028363 <= 16
classification               bounded-but-costly
```

The gate is green, but the cost penalty is material. Do not hide it by changing the timestep, tolerances or numerical contract.

## 4. H.30 candidate decision

H.30 is an evidence-only closure gate. It fingerprints frozen H.19-H.29 summaries/manifests, verifies the v2/v3/kill selector contract and derives:

```text
OPT-IN ONLY
```

Rationale: numerical, long-horizon, protection, rollback, replay, off-design and activation-candidate evidence are all green, so v3 is production-qualified as an opt-in. H.28 remains `bounded-but-costly`, so there is no evidence-based reason to replace the cheaper validated v2 explicit default.

No production selector change is required:

```text
v2 -> ExplicitCommittedState -> authoritative default / rollback / reference
v3 -> FourNodeBranchContinuityCorrectedCommitOptIn -> qualified opt-in
kill -> v2 ExplicitCommittedState
```

## 5. H.30 validation gate

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-phase-h-closure-production-qualification-decision-audit.cmd
```

Required final output:

```text
phase-h-production-policy-decision=OPT-IN ONLY
phase-h-closure-evidence-chain-passes=True
h30-audit-passes=True
phase-h-closed=True
phase-i-unblocked=True
```

The focused gate is cheap: it does **not** rerun H.24 or H.28.

## 6. After a green H.30

Promote H.30 as the authoritative Phase H closure. Keep exact v2 explicit as default/rollback/reference and exact v3 corrected as the qualified opt-in path. Phase I is then unblocked. Any future attempt to reach `ACTIVATE` requires separately scoped performance work and regression qualification; it is not part of H.30.

Read also:

- `M10_9_4_1_H30_PHASE_H_CLOSURE_PRODUCTION_QUALIFICATION_DECISION.md`;
- `M10_9_4_1_H30_VALIDATION_CHECKLIST.md`;
- `M10_9_4_1_H30_STATIC_REVIEW.md`;
- `adr/0159-close-phase-h-opt-in-only-because-corrected-path-is-qualified-but-bounded-costly.md`;
- `M10_9_4_1_PHASE_H_COMPLETION_ROADMAP_H24_H30.md`.
