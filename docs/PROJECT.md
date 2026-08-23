# Project — current authoritative state
**M10.9.8 is VALIDATED / CLOSED.** M10.9.8.5 manual integrated HMI acceptance completed on 2026-08-22. **M10 Final Pre-M11 Cumulative Validation Hotfix 1 is VALIDATED** with the complete Release ordinary suite and curated current-authority focused gates green.

The first **M10 Final Pre-M11 Long Validation Hotfix 1** campaign is now **FAILED / ABORTED AFTER DIAGNOSTIC EVIDENCE COLLECTION**. LR-H1 raised `WaterSteamStateOutOfRangeException` at fluid node `outlet` (`v=0.0026153411609661885 m^3/kg`, `u=1615124.4119888516 J/kg`) after the preserved 300 s checkpoint and before 600 s. LR-M1 was manually stopped at logical step 360000 / 440000 after equal 300 s simulated chunks grew from roughly 10 to roughly 36 minutes wall-clock. M10 cannot close on this evidence.

This full package additionally consolidates the three pre-M11 engineering review/planning streams (nuclear-code V&V, Digital I&C/human-system safety, and operating-point equilibrium/stability). Those documents are **planning only** and do not alter the frozen long workload, runtime physics or acceptance criteria.

M10 remains OPEN. M11 is blocked until the long failure is diagnosed, any required owner correction is separately validated, the full long gate passes, and M10 closure is explicitly recorded.

This is the **single current-state and handoff document** for Nuclear Reactor Simulator. Do not duplicate the current checkpoint in README, roadmap, milestone files or candidate-specific notes.

## Current checkpoint

**M10.9.4.1 / Phase I, M10.9.5, M10.9.6 and M10.9.7 are VALIDATED / CLOSED.** M10.9.8 is VALIDATED / CLOSED and the final cumulative Hotfix 1 gate is VALIDATED. Diagnostics 1–2, Diagnostic 3 Hotfix 1 execution, Diagnostic 4, Diagnostic 5, Diagnostic 6 execution, Diagnostic 7, Diagnostic 8 execution and Diagnostic 9 have passed locally. LR-M1 is accepted as repaired evidence. Exact-v5, exact-v6 and exact-v7 are all **NOT QUALIFIED** operating points. Diagnostic 7 proved the historical breaker-closed governor integral-reference defect; Diagnostic 8 reduced the associated late governor/control-valve drift by roughly sixty times; Diagnostic 9 then proved the remaining turbine-inlet mass accumulation is the rejected non-vapor fraction left without a downstream owner by `VaporMassFractionLimited`. Diagnostic 10 original compiled but its ordinary suite stopped RED at 1/1480 on a new test-only `turbine-inlet` balance assertion that omitted the canonical admission-valve inflow. LR-H1 remains blocking; Diagnostic 10 Hotfix 1 is the active candidate, exact-v8 has not yet completed its 600 s requalification, and production activation is not authorized.

The validated M10.9.7 baseline includes the live read-only MISSION workspace, deterministic logical-step timeline, presentation-only drill-down, exact mission/archive binding, replay/checkpoint reconstruction, closure coverage for active/completed/failed mission states, assistance changes and requested/effective authority divergence. F1–F8 remain preserved, F9 remains absent and MISSION has no plant-command authority.

Authoritative desktop production remains:

`integrated-operations-desktop-stable@4 | CorrelationConsistentInverseDomain | FourNodeBranchContinuityCorrectedCommitOptIn | 10 ms`

Historical exact-version identities remain immutable; M10.9.8 validation work does not reopen Phase-I numerical ownership without direct contradictory evidence.

## Active validation candidate and parallel planning overlay

**Active candidate: M10 Final Long Failure Diagnostic 10 Hotfix 1 / Turbine-Inlet Canonical Net-Balance Regression Alignment + Exact-v8 Requalification — CANDIDATE.**

The cumulative Hotfix 1 gate, Diagnostic 2 / LR-M1 Hotfix 1 and Diagnostic 3 Hotfix 1 execution are locally validated. Diagnostic 3 crossed the historical exact-v4 failure interval, but its engineering decision is negative: the 260 kg/s exact-v5 probe is not a qualified operating point. It starts near 260 kg/s but evolves toward roughly 103 kg/s while outlet inventory moves into the drum; by 600 s drum level is ~0.9567 and still increasing, while outlet/drum pressure and fuel/structure temperature continue monotonic decline.

Diagnostic 4 completed PASS and established that the late drum accumulation is dominated by the internal M4.4 feedwater-pump minus separated-steam mismatch, while the full energy path remains conservative. Diagnostic 5 returned the complete whole-cycle state. Diagnostic 6 authored exact-v6 from the unchanged closed equations: 100 kg/s primary flow, 13.028 kg/s secondary throughput and 32.484 MW thermal input. Its 600 s execution passes but remains engineering NOT QUALIFIED. Diagnostic 7 then proved the breaker-closed governor integral-reference defect. Diagnostic 8 introduced versioned exact-v7 and reduced late governor/control-valve drift from ~+0.01474 %/s to ~+0.000240 %/s, but exact-v7 remained NOT QUALIFIED. Diagnostic 9 now closes the turbine-admission owner directly: late `commanded-effective` and `commanded*(1-x)` are both ~0.268827 kg/s, while measured turbine-inlet `dm/dt` is ~0.268782 kg/s (difference ~-4.55e-5 kg/s). Diagnostic 10 therefore introduces exact-v8 with `VaporMassFractionLimitedWithMoistureDrain`: vapor remains the only work-producing stage flow, rejected non-vapor mass/energy is owned by an explicit hotwell moisture drain, and historical policies remain unchanged. Its first ordinary-suite attempt stopped at one new regression because that assertion modeled only the turbine sink and omitted the simultaneous admission-valve inflow; the observed mass delta closes exactly on the canonical net owner composition. Hotfix 1 corrects only that test contract. Exact-v4 remains production. See `M10_FINAL_LONG_FAILURE_DIAGNOSTIC10.md` and ADR-0189.

**Parallel documentation overlay:** this package also includes the reviewed pre-M11 planning set from the three book studies. It does not supersede the executable long baseline and is not promotion evidence.

The historical first-long workload remains frozen. The 19 I.3 budgets and exact-v4 conservation ceilings are unchanged. M10 closes only after the owner correction is separately validated, a replacement long manifest is deliberately created, the full long artifact reports `m10-final-long-validation-passes=True`, and a closure/promotion step records that evidence.

## Validation required for active diagnostic candidate

Run `scripts\run-m10-final-long-failure-diagnostic10.cmd`. It performs Debug build with warnings-as-errors, the complete ordinary suite, the LR-M1 Hotfix-1 semantic-equivalence regression, and the explicit 600 s exact-v8 turbine moisture-drain whole-cycle requalification. Return the complete `artifacts/m10-final-long-diagnostic10` folder before production activation, further operating-point changes or replacement-long authorization.

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

Phase I, M10.9.5, M10.9.6 and M10.9.7 are closed. Continue milestone-by-milestone from the latest validated chain: **M10.9.8.5 VALIDATED / M10.9.8 CLOSED → M10 Final Pre-M11 Cumulative Hotfix 1 VALIDATED → failed/aborted first long campaign → Diagnostic 1 PASS → Diagnostic 2 / LR-M1 Hotfix 1 PASS → Diagnostic 3 original build RED (test-only CS0103) → Diagnostic 3 Hotfix 1 execution PASS / exact-v5 NOT QUALIFIED → Diagnostic 4 PASS / mass-energy owners identified → Diagnostic 5 PASS / whole-cycle state captured → Diagnostic 6 execution PASS / exact-v6 NOT QUALIFIED → Diagnostic 7 PASS / breaker-closed governor integral-reference defect proven → Diagnostic 8 execution PASS / exact-v7 NOT QUALIFIED → Diagnostic 9 PASS / turbine-admission non-vapor owner proven → Diagnostic 10 original ordinary-suite RED (test-only inlet balance assertion) → Diagnostic 10 Hotfix 1 exact-v8 explicit moisture-drain requalification → separate production activation only after equilibrium qualification → replacement long <=60 min wall budget → full long PASS → explicit M10 closure → M11**.

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
