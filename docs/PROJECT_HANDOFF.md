# Nuclear Reactor Simulator — Project Handoff

> **Authoritative validated baseline:** M10.9.4.1-H.28 — Performance, Cost & Long-Running Operational Soak — VALIDATED on 2026-08-19.
>
> **Current candidate:** M10.9.4.1-H.24 Requalification 1 — Post-H.28 Committed Long-Horizon & Cross-Profile Regression.
>
> **Production/default:** `ExplicitCommittedState` at 10 ms. Corrected commit remains `FourNodeBranchContinuityCorrectedCommitOptIn`.
>
> **Activation status:** H.29 remains blocked until the single post-optimization H.24 requalification is green.

## 1. Validated Phase H chain

- **H.19 VALIDATED:** four-node shadow policy 473/473 over the complete 30,000-interval/four-profile representative contract.
- **H.20 VALIDATED:** fail-closed authority plus 8/8 typed rollback challenges.
- **H.21 Hotfix 1 VALIDATED:** real-orchestrator sidecar wiring with committed trajectory transparency.
- **H.22 VALIDATED:** separately opt-in corrected ownership, 443 corrected commits / 2,000 steps, zero unsafe/fallback commits.
- **H.23 Hotfix 2 VALIDATED:** exact replay/checkpoint/protection interaction.
- **H.24 Hotfix 1 VALIDATED:** 30,000 qualification intervals + 8 transition steps, 9,626 corrected commits, all four nominal profiles trip-free; rare 4h31m55s gate.
- **H.25 VALIDATED:** five protection/transient scenarios, 837 runtime steps, 178 corrected commits.
- **H.26 Hotfix 1 VALIDATED:** 12/12 same-step explicit fallbacks over typed rollback/denial controls.
- **H.27 Hotfix 1 VALIDATED:** six off-design scenarios, 2,080 steps, 529 corrected commits, four `corrected-qualified` and two `protected-boundary` outcomes.
- **H.28 VALIDATED:** performance/cost/soak gate passed after H.28.1 optimization.

## 2. Validated H.28 result

The unchanged H.28 limits passed:

```text
benchmark steps/mode                  256
explicit median step                1589.3 us
corrected median step               7344.9 us
median wall-cost ratio               4.6214685710690242 <= 8
explicit p95                        7483 us
corrected p95                      79951.7 us
p95 wall-cost ratio                 10.684444741413872 <= 12
median allocation ratio              1.1164372201028363 <= 16
corrected trigger/commit              20/20
soak steps                            1536
soak trigger/commit                  379/379
rollback/fallback/unsafe               0/0/0
untargeted disagreements                  0
trip steps                                0
deterministic repeat                   True
fingerprint 518BA948637F0C270C7F8228AB97FEF9148E29A4F5CE4376319AB5D1CFBE7F38
classification            bounded-but-costly
```

The result is intentionally **not** interpreted as cheap enough to activate by default. It is only bounded enough to continue qualification. Do not change the 10 ms timestep or weaken the numerical contract to hide cost.

## 3. Why H.24 must be rerun once

H.24 itself is already validated, but H.28.1-B/C/D/E/F/G changed committed-runtime implementation code while preserving the numerical contract. The Phase H completion roadmap therefore requires one post-optimization long-horizon/cross-profile regression after the performance branch is stable.

This rerun is intentionally not chained after each optimization iteration. H.28 is now green, so this is the one required requalification.

## 4. Current candidate

`M10.9.4.1-H.24 Requalification 1 — Post-H.28 Committed Long-Horizon & Cross-Profile Regression`:

- freezes the user-supplied H.28 green artifacts under `Application.Tests/.../Evidence`;
- fingerprint-checks those artifacts in the ordinary suite;
- leaves the historical H.24 test/evidence intact;
- reruns the exact H.24 domain: `steady-long 12000`, `load-pulse 6000`, `cooling-pulse 6000`, `combined-load-cooling 6000`, plus 8 deterministic action-transition steps;
- writes separate post-H.28 artifacts;
- changes no production numerical runtime behavior.

Frozen controls remain:

```text
current-v2 default       ExplicitCommittedState
corrected mode           FourNodeBranchContinuityCorrectedCommitOptIn
fixed step               10 ms
trigger                  P060/F040
corrector                H.9 finite-difference Jacobian + damped Newton
branch continuity        bounded previous-phase hysteresis
hysteresis               2% pressure / 5 K
targets                  steam | stop-out | header | turbine-inlet
```

## 5. Validation command

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-four-node-post-h28-committed-long-horizon-requalification-audit.cmd
```

Promotion requires all three gates to be explicitly green. The focused pass flags are:

```text
post-h28-four-node-committed-long-horizon-cross-profile-requalification-passes=True
h24-post-h28-requalification-audit-passes=True
```

## 6. After a green result

A green requalification completes the roadmap prerequisite for **H.29 — Production Activation Candidate**. H.29 should not introduce a new numerical solver. It must decide whether the already-qualified corrected path is appropriate as a production-default candidate while carrying forward the H.28 `bounded-but-costly` classification.

Default production remains explicit until H.29/H.30 explicitly decide otherwise. Phase I remains deferred until Phase H closes.

See also:

- `M10_9_4_1_H24_POST_H28_REQUALIFICATION.md`;
- `M10_9_4_1_H24_POST_H28_REQUALIFICATION_VALIDATION_CHECKLIST.md`;
- `M10_9_4_1_PHASE_H_COMPLETION_ROADMAP_H24_H30.md`;
- historical H.24/H.28/H.28.1 audit documents for detailed provenance.
