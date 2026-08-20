# Project — current authoritative state

This is the **single current-state and handoff document** for Nuclear Reactor Simulator. Do not duplicate the current checkpoint in README, roadmap, milestone files or candidate-specific notes.

## Current checkpoint

**Historical H.30 production identity:** `integrated-operations-desktop-stable@3` / `FourNodeBranchContinuityCorrectedCommitOptIn` / `HistoricalCorrelationTopology` remains immutable and replayable.

**Fail-closed rollback/reference:** `integrated-operations-desktop-stable@2` / `ExplicitCommittedState` remains immutable.

**Validated repaired exact identity:** `integrated-operations-desktop-stable@4`.

- physical seed: validated desktop exact-v2 seed;
- hydraulic ownership: `FourNodeBranchContinuityCorrectedCommitOptIn`;
- thermodynamic closure: `CorrelationConsistentInverseDomain`;
- fixed simulation step: **10 ms**;
- exact @2/@3 are not reinterpreted.

**Validated repair evidence:** Hotfix 10 plus Stages 1–4 and exact-v4 readiness.

- topology repair: historical no-root probes 3/3 resolved; vapor seam 7/7 saturated-only below and 7/7 superheated-only above; low-temperature census 231/231 resolved;
- frozen 7000-step load raise/lower reproduction: explicit and corrected paths complete without thermodynamic out-of-range failure;
- Stage 1: 1024-interval healthy control green, corrected authority exercised, fail-closed safety/determinism/conservation green;
- Stage 2: 30,000 qualification intervals across `steady-long|load-pulse|cooling-pulse|combined-load-cooling`, 58/58 trigger/commit, zero rollback/fallback/unsafe/untargeted disagreement, deterministic repeat green;
- Stage 3: replay/checkpoint/continuation/reverse-power protection green; H.27 six-scenario off-design matrix green;
- Stage 4: H.28 relative-cost ceilings green (`median ratio 0.97677554038185532`, `p95 ratio 0.83442338580147768`, `allocation ratio 1.1163446388589435`), 1536-step soak with 8/8 trigger/commit, zero trip/rollback/fallback/unsafe/disagreement and deterministic repeat;
- Hotfix 15 readiness: exact @1/@2/@3/@4 resolve independently; actual @4 factory completes the 7000-step historical gap journey with 9/9 trigger/commit and zero rollback/fallback/unsafe/untargeted disagreement.

**Validated synchronization qualification remains separate:** `pre-synchronization-grid-loading@3` is unchanged and remains the supported corrected synchronization identity. Exact synchronization @1/@2 remain historical/reference identities. The qualified contract is bounded stabilization at 10 s and strict healthy sustained operation from 20–60 s.

**Historical I.3 reference remains frozen provenance:** desktop exact @3, 300 s / 30,000 steps, seven final-window slopes and 19 regression budgets. It is not reinterpreted as @4 evidence.

## Active candidate

**M10.9.4.1-I.5 REV1 Hotfix 17.1 — Final Repaired-v4 Phase-I Closure / Preflight Documentation Alignment.**

Hotfix 16.2 production activation is locally validated:

- authoritative desktop production: `integrated-operations-desktop-stable@4` / `CorrelationConsistentInverseDomain` / `FourNodeBranchContinuityCorrectedCommitOptIn`;
- 1200-step healthy activation control: zero health/trip/breaker violations, 2/2 corrected trigger/commit, zero rollback/fallback/unsafe/untargeted disagreement;
- deterministic repeat: green;
- exact @3 remains immutable historical H.29/H.30/I.3 replay provenance;
- exact @2 remains fail-closed explicit rollback/reference;
- synchronization family remains independently qualified on `pre-synchronization-grid-loading@3`.

Hotfix 17 is the single final technical closure candidate. It does **not** add another repair or requalification stage.

It changes the audit/CI contract so that:

- H.30 RQ1, I.3 exact-v3 authoritative reference and I.4 are `HISTORICAL-FROZEN` and are not dynamically rerun against exact @4;
- current ordinary evidence is I.2 CI contract + synchronization exact-v3 activation + repaired exact-v4 production activation;
- scheduled long evidence is GameplayLong + OperationalEnvelope + ReferencePlantScale + repaired exact-v4 300-second production-reference requalification;
- the exact-v4 300-second gate reuses the **same 19 frozen I.3 budgets** as acceptance authority and may not regenerate or widen them;
- the cumulative closure requires all current/long gates to be green before writing `phase-i-closed=True` and `m1095-unblocked=True`.

No physical coefficient, thermodynamic equation, H.9 tolerance, P060/F040 threshold, H.20/H.22 authority rule, previous-phase hysteresis bound, synchronization identity or fixed timestep is retuned by Hotfix 17. The only `src/` change is application metadata describing the final closure candidate.

### Hotfix 17.1 static preflight

Before the multi-hour closure run, the final delta was statically rechecked for the compile/configuration mistakes that previously caused avoidable hotfixes:

- all namespaces used by the new 300 s repaired-v4 test are present; `ControlRoomSnapshotFingerprint` is covered by `NuclearReactorSimulator.Application.Scenarios.Recording`;
- no escaped `\"...\"` string-literal pattern remains inside interpolated expression holes;
- every `call` target referenced by `ci-ordinary`, `ci-current-evidence`, `ci-long` and the cumulative script exists;
- every `--filter-method` target in the new repaired-v4 and cumulative runners resolves to an existing test method;
- `NRS_I5_REPAIRED_V4_300S_REFERENCE_AUDIT` and `NRS_I5_CLOSURE_AUDIT` match their test-side opt-in contracts;
- the canonical frozen-budget SHA-256 is `9B7A2653F08059ECBD16F39FEB0DD7350F62C98A5892A8215D34404D6C9301BB`; 19 budget rows and the 19-row tier manifest are present;
- the tier split is exactly `1 ORDINARY / 3 CURRENT-EVIDENCE / 4 SCHEDULED-LONG / 11 HISTORICAL-FROZEN`;
- repaired-v4 artifact directory/file names consumed by cumulative closure match the producers;
- root `APPLY_UPDATE.cmd` now identifies Hotfix 17.1, clears the final repaired-v4/current-evidence artifact directories and directs validation to `dotnet build`, `dotnet test`, then the cumulative closure rather than the already-completed Hotfix-16 activation audit.

Hotfix 17.1 does not change C# runtime/test logic or CI gate execution logic; it records this preflight, corrects stale `APPLY_UPDATE.cmd` packaging instructions and updates stale current documentation. Build remains the authoritative compiler check.

## Final local validation

Run the short preflight first:

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
```

If build and ordinary tests are green, run exactly one long command:

```bat
scripts\run-m10941-cumulative-closure-audit.cmd
```

That command performs:

1. ordinary CI and current evidence;
2. GameplayLong;
3. OperationalEnvelopeAudit;
4. ReferencePlantScaleAudit;
5. authoritative repaired exact-v4 300 s / 30,000-step reference requalification against the frozen 19 I.3 budgets;
6. final evidence-derived cumulative closure.

Return the full generated `artifacts` relevant to any failing gate, or at minimum:

```text
artifacts\i5-repaired-v4-300s-reference-requalification
artifacts\i5-m10941-cumulative-closure
```

If the cumulative command is green, **M10.9.4.1 / Phase I is closed**. No additional I.5 repair stage is planned; proceed to **M10.9.5 — Contextual Command Consequence Model**.

If a final gate fails, fix only the demonstrated failing contract. Do not reopen Stages 1–4 or alter frozen budgets unless the evidence directly proves those validated contracts are invalid.

## Evidence and package policy

Candidate source ZIPs intentionally exclude `tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/Evidence/`, generated `artifacts/`, `bin/` and `obj/`.

Compact immutable prerequisites required by ordinary/current tests live under `eng/frozen-evidence/ordinary/`; manifests live under `eng/evidence-manifests/`. Generated audit CSV/TXT payloads remain local validation records and are not copied into each subsequent candidate ZIP.

## Current unresolved items

The authoritative limitation register is `KNOWN_MODEL_LIMITATIONS.md`. In particular:

- Phase I is blocked only on the final repaired-v4 long/reference/cumulative closure; exact-v4 authoritative activation is validated;
- the historical exact-v3 I.3 drift observations remain regression provenance and are not evidence that exact @4 has identical long-horizon means/slopes;
- historical H.28 remains `bounded-but-costly`; repaired Stage 4 separately demonstrated bounded-at-or-below repaired explicit relative wall cost on the validation machine;
- branch overrides disappeared in repaired long-horizon evidence, but previous-phase hysteresis remained materially active and must not be removed before Phase-I closure;
- H.5/H.21 historical numerical source seams remain retained for provenance;
- severe-incident, structural-damage and several plant-system models remain reduced-order or incomplete.

## Continuation rule

Hotfix 17/17.1 is the final technical closure candidate. Run build and ordinary tests, then `scripts\run-m10941-cumulative-closure-audit.cmd`. Do not add another diagnostic/requalification stage if the chain is green. If a final long gate fails, fix only the demonstrated failing contract and rerun that chain; do not reopen already-green repair Stages 1–4 unless evidence directly implicates them.

Do not add an `exhaust` special case, clamp conserved inventories, widen acceptance tolerances, retune condenser coefficients to dodge the seam, remove previous-phase hysteresis before Phase-I closure, or weaken fail-closed behavior for genuinely unsupported states.

M10.9.4.1 / Phase I closes only when the final exact-@4 production chain is green; only then proceed to **M10.9.5 — Contextual Command Consequence Model**.

The post-Phase-I execution order is now pre-planned rather than open-ended: M10.9.5 consequence semantics → M10.9.6 deterministic challenge/demand/scoring → M10.9.7 mission/performance presentation → M10.9.8 integrated M10 validation → M11 release hardening. Detailed contracts live in `ROADMAP.md` and `docs/milestones/M10.9.5.md` through `M11.md`. This planning does not authorize work before the final Phase-I gate is green.

## Documentation authority

For current work use only:

- `PROJECT.md` — current checkpoint, handoff and active validation contract;
- `ROADMAP.md` — future work only;
- `KNOWN_MODEL_LIMITATIONS.md` — unresolved model limitations only;
- `ARCHITECTURE.md` — stable architecture and ownership rules;
- `README.md` — documentation navigation.

Historical milestone/hotfix detail belongs under `history/`, ADRs or the changelog and must not be copied back into current-state documents.
