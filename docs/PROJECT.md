# Project — current authoritative state
**M10.9.8 is VALIDATED / CLOSED.** M10.9.8.5 manual integrated HMI acceptance completed on 2026-08-22. **M10 Final Pre-M11 Cumulative Validation Hotfix 1 is VALIDATED** with the complete Release ordinary suite and curated current-authority focused gates green.

The first **M10 Final Pre-M11 Long Validation Hotfix 1** campaign remains frozen as **FAILED / ABORTED** evidence. LR-H1 raised `WaterSteamStateOutOfRangeException` at `outlet`; LR-M1 exposed quadratic live MISSION projection cost. The subsequent Diagnostic 1–11 chain has now repaired LR-M1 and qualified exact-v9 as the replacement healthy operating-point candidate, but the failed first-long artifact is not rewritten or reinterpreted.

This full package additionally consolidates the three pre-M11 engineering review/planning streams (nuclear-code V&V, Digital I&C/human-system safety, and operating-point equilibrium/stability). Those documents are **planning only** and do not alter the frozen long workload, runtime physics or acceptance criteria.

M10 remains OPEN. M11 is blocked until exact-v9 activation is explicitly validated, a new replacement-long contract is created, the full replacement long gate passes, and M10 closure is explicitly recorded.

This is the **single current-state and handoff document** for Nuclear Reactor Simulator. Do not duplicate the current checkpoint in README, roadmap, milestone files or candidate-specific notes.

## Current checkpoint

**M10.9.4.1 / Phase I, M10.9.5, M10.9.6, M10.9.7 and M10.9.8 remain VALIDATED / CLOSED.** M10 Final Diagnostic 11 Hotfix 2 is now locally validated and the returned 600 s exact-v9 artifacts qualify the post-moisture analytical whole-cycle operating point: ~5.000000 MWe, ~100.000001 kg/s primary flow, effectively zero late inventory/governor drift, zero trip/rollback and conservative mass/energy ownership. LR-M1 remains repaired. The active candidate is now the exact-v9 **production activation candidate**: @9 is staged as an explicit opt-in production policy and desktop scenario/registry identity while exact-v4 deliberately remains the authoritative default and exact-v2 remains fail-closed rollback. Replacement long and production default switching remain unauthorized until this activation-candidate gate returns green evidence.

The validated M10.9.7 baseline includes the live read-only MISSION workspace, deterministic logical-step timeline, presentation-only drill-down, exact mission/archive binding, replay/checkpoint reconstruction, closure coverage for active/completed/failed mission states, assistance changes and requested/effective authority divergence. F1–F8 remain preserved, F9 remains absent and MISSION has no plant-command authority.

Authoritative desktop production remains:

`integrated-operations-desktop-stable@4 | CorrelationConsistentInverseDomain | FourNodeBranchContinuityCorrectedCommitOptIn | 10 ms`

Historical exact-version identities remain immutable; M10.9.8 validation work does not reopen Phase-I numerical ownership without direct contradictory evidence.

## Active validation candidate and parallel planning overlay

**Active candidate: M10 Final Exact-v9 Qualified Production Activation Candidate — CANDIDATE.**

The cumulative Hotfix 1 gate, Diagnostic 2 / LR-M1 Hotfix 1 and Diagnostic 3 Hotfix 1 execution are locally validated. Diagnostic 3 crossed the historical exact-v4 failure interval, but its engineering decision is negative: the 260 kg/s exact-v5 probe is not a qualified operating point. It starts near 260 kg/s but evolves toward roughly 103 kg/s while outlet inventory moves into the drum; by 600 s drum level is ~0.9567 and still increasing, while outlet/drum pressure and fuel/structure temperature continue monotonic decline.

Diagnostics 4–10 isolated and repaired the full LR-H1 ownership chain: primary operating-point imbalance, breaker-closed governor integral-reference semantics and wet-steam non-vapor ownership. Exact-v8 validated the structural repairs but remained off-root at ~4.8682 MWe / +0.2553 MW stored energy. Diagnostic 11 recomputed the authored post-moisture whole-cycle root as exact-v9; after two test-only regression Hotfixes, Hotfix 2 completed the full 600 s run. Returned evidence qualifies exact-v9 with final export 4.999999982 MWe, primary 100.000001 kg/s, drum level ~0.5, final-60 node mass slopes on the order of 1e-8 kg/s, governor/control-valve slopes on the order of 1e-10 %/s, net/stored power ~9.8e-8 MW, zero trip/rollback and conservative stage/full-cycle closure. The active milestone therefore stages exact-v9 as `M10FinalExactV9QualifiedCandidate` without changing `AuthoritativeDefaultPolicy`. See `M10_FINAL_EXACT_V9_PRODUCTION_ACTIVATION_CANDIDATE.md`, ADR-0189, ADR-0190 and ADR-0191.

**Parallel documentation overlay:** this package also includes the reviewed pre-M11 planning set from the three book studies. It does not supersede the executable long baseline and is not promotion evidence.

The historical first-long workload remains frozen. The 19 I.3 budgets and exact-v4 conservation ceilings are unchanged. M10 closes only after the owner correction is separately validated, a replacement long manifest is deliberately created, the full long artifact reports `m10-final-long-validation-passes=True`, and a closure/promotion step records that evidence.

## Validation required for active candidate

Run `scripts\run-m10-final-v9-production-activation-candidate.cmd`. It performs Debug build with warnings-as-errors, the complete ordinary suite, current exact-v4 evidence, the explicit 600 s exact-v9 Diagnostic-11 requalification on the candidate source tree, and a focused exact-v9 production-policy-path audit. Return the complete `artifacts/m10-final-v9-production-activation-candidate` folder before any authoritative-default switch, production mission rebinding or replacement-long authorization.

## Evidence and package policy

Candidate source ZIPs intentionally exclude `tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/Evidence/`, generated `artifacts/`, `bin/` and `obj/`.

Compact immutable prerequisites required by ordinary/current tests live under `eng/frozen-evidence/ordinary/`; manifests live under `eng/evidence-manifests/`. Generated audit CSV/TXT payloads remain local validation records and are not copied into each subsequent candidate ZIP.

## Current unresolved items

The authoritative limitation register is `KNOWN_MODEL_LIMITATIONS.md`. In particular:

- Phase I is closed; repaired exact-v4 production and the final cumulative/reference chain are validated, while the final long gate is still open and its first LR-H1 healthy soak has failed;
- the historical exact-v3 I.3 drift observations remain regression provenance and are not evidence that exact @4 has identical long-horizon means/slopes;
- historical H.28 remains `bounded-but-costly`; repaired Stage 4 separately demonstrated bounded-at-or-below repaired explicit relative wall cost on the validation machine;
- branch overrides disappeared in repaired long-horizon evidence, but previous-phase hysteresis remained materially active and must not be removed without separately scoped post-Phase-I retirement evidence;
- H.5/H.21 historical numerical source seams remain retained for provenance;
- severe-incident, structural-damage and several plant-system models remain reduced-order or incomplete;
- the reduced quadratic hydraulic map is continuous but not differentiable through some near-zero/reversal transitions, and the generic runtime/numerical-conditioning findings from the post-7.3 Simulation review are assigned to M11.3/M12 rather than patched into M10 presentation work; see `SIMULATION_NUMERICAL_REGULARITY_AND_RUNTIME_REVIEW.md`;
- fingerprint-v1 anchoring plus lifecycle-spine/recent-evidence separation are validated through M10.9.7.4/M10.9.8; recorder notification cost, fingerprint cost, collection-copy traps, long-session memory growth and recorder failure policy remain M11.2/M11.3 ownership; see `APPLICATION_RECORDING_REPLAY_REVIEW.md` and ADR-0184;
- UI-thread runtime/projection responsiveness, notification fan-out and archive-export cost remain M11.3 measurement work; stable command-target identity and `MainWindowViewModel` decomposition remain M13 work; see `DESKTOP_HOST_FAILURE_AND_SESSION_SAVE_INTEGRITY_REVIEW.md` and ADR-0185.

## Continuation rule

Phase I, M10.9.5, M10.9.6 and M10.9.7 are closed. Continue milestone-by-milestone from the latest validated chain: **M10.9.8.5 VALIDATED / M10.9.8 CLOSED → M10 Final Pre-M11 Cumulative Hotfix 1 VALIDATED → failed/aborted first long campaign → Diagnostic 1 PASS → Diagnostic 2 / LR-M1 Hotfix 1 PASS → Diagnostic 3 original build RED (test-only CS0103) → Diagnostic 3 Hotfix 1 execution PASS / exact-v5 NOT QUALIFIED → Diagnostic 4 PASS / mass-energy owners identified → Diagnostic 5 PASS / whole-cycle state captured → Diagnostic 6 execution PASS / exact-v6 NOT QUALIFIED → Diagnostic 7 PASS / breaker-closed governor integral-reference defect proven → Diagnostic 8 execution PASS / exact-v7 NOT QUALIFIED → Diagnostic 9 PASS / turbine-admission non-vapor owner proven → Diagnostic 10 original ordinary-suite RED (test-only inlet balance assertion) → Diagnostic 10 Hotfix 1 PASS / exact-v8 NOT QUALIFIED → Diagnostic 11 original ordinary-suite RED (test-only stale governor-integral range) → Diagnostic 11 Hotfix 1 ordinary-suite RED (test-only ideal pre-step P-term assertion) → Diagnostic 11 Hotfix 2 PASS / exact-v9 QUALIFIED → exact-v9 opt-in production activation candidate → separate authoritative exact-v9 activation decision → replacement long <=60 min wall budget → full long PASS → explicit M10 closure → M11**.

M10.9.6 challenge/demand/scoring state is observational Application state. It may consume existing plant evidence but may not issue plant commands, create supervisory authority, change protection or introduce new physics. Missing physical phenomena discovered while authoring challenges remain post-M11 backlog items rather than M10.9.6 scope expansion.

The post-Phase-I execution order remains fixed: M10.9.7 mission/performance → M10.9.8 integrated M10 validation → mandatory final pre-M11 cumulative/long M10 validation → M11 release hardening → M12–M15 approved post-release epics. The final pre-M11 contract is `M10_FINAL_PRE_M11_VALIDATION_PLAN.md`. Detailed future contracts live in [`ROADMAP.md`](ROADMAP.md) and the milestone plans.

## Documentation authority

For current work use only:

- `PROJECT.md` — current checkpoint, handoff and active validation contract;
- `ROADMAP.md` — future work only;
- `KNOWN_MODEL_LIMITATIONS.md` — unresolved model limitations only;
- `ARCHITECTURE.md` — stable architecture and ownership rules;
- `README.md` — documentation navigation.

Historical milestone/hotfix detail belongs under `history/`, ADRs or the changelog and must not be copied back into current-state documents.
