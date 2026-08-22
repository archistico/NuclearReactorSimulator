# Project — current authoritative state

This is the **single current-state and handoff document** for Nuclear Reactor Simulator. Do not duplicate the current checkpoint in README, roadmap, milestone files or candidate-specific notes.

## Current checkpoint

**M10.9.4.1 / Phase I, M10.9.5, M10.9.6 and M10.9.7 are VALIDATED / CLOSED.** M10.9.8.1 REV1 Docs1, M10.9.8.2 Hotfix 1 REV5 and M10.9.8.3 are also VALIDATED. M10.9.8.2 REV5 is the current production/runtime baseline because it added the exact-v2 production mission binding and validated F4/list-stability repairs; M10.9.8.3 adds test/evidence only and is the current stacking baseline for M10.9.8.4.

The validated M10.9.7 baseline includes the live read-only MISSION workspace, deterministic logical-step timeline, presentation-only drill-down, exact mission/archive binding, replay/checkpoint reconstruction, closure coverage for active/completed/failed mission states, assistance changes and requested/effective authority divergence. F1–F8 remain preserved, F9 remains absent and MISSION has no plant-command authority.

Authoritative desktop production remains:

`integrated-operations-desktop-stable@4 | CorrelationConsistentInverseDomain | FourNodeBranchContinuityCorrectedCommitOptIn | 10 ms`

Historical exact-version identities remain immutable; M10.9.8 validation work does not reopen Phase-I numerical ownership without direct contradictory evidence.

## Active candidate

**M10.9.8.4 Hotfix 1 — Protection/Authority Observation Boundary Alignment — CANDIDATE.**

M10.9.8.1 REV1 Docs1 and M10.9.8.2 Hotfix 1 REV5 are **VALIDATED**. M10.9.8.3 is also **VALIDATED** after build, complete ordinary suite and `scripts\run-m10983-degraded-fault-protection-takeover-audit.cmd` passed on 2026-08-22. M10.9.8.3 is the sole baseline for M10.9.8.4. The accepted v1 matrix and validated execution matrices remain immutable prerequisites.

The original M10.9.8.4 candidate was not validated because its protection row observed authority one deterministic tick too early after SCRAM. Hotfix 1 aligns that checkpoint boundary with the validated M5/M10.9.8.3 owner contract and adds **no production runtime change**. Its four-row `RCI-01..RCI-04` matrix verifies representative healthy, degraded/recovered, protection-trip and manual-takeover states through fresh same-seed repeat, canonical full replay, replay-backed checkpoint prefix/live continuation and M10.9.6.5 challenge replay projection. Same-seed means the same exact scenario/initial-condition plus identical accepted operator-action and automation-intent trace; no RNG state or opaque checkpoint blob is introduced. See `M10_9_8_4_REPLAY_CHECKPOINT_SAME_SEED_INTEGRITY.md`.

M10.9.8.2 Hotfix 1 REV5 retains the REV4 PowerShell-compatible validator and adds a presentation-only interactive-list stability repair after residual F4 dependency-chain flicker was observed during RUN. REV5 caches the dependency-chain projection for the selected typed command, preserves equivalent F8 checkpoint list/selection identity, prevents the five programmatic target selectors from resetting `ComboBox.ItemsSource` on unrelated visual-state refresh, and suppresses unchanged MISSION timeline/list replacement notifications. REV4 preserved the REV3 matrix/mission implementation. REV3 passed build and the complete ordinary suite; its focused gate then stopped in the matrix-v2 validator because the local Windows PowerShell host does not expose `Get-FileHash`. REV4 replaces only that validator hash mechanism with the .NET `System.Security.Cryptography.SHA256` API while retaining the exact frozen-v1 digest. REV3 had already superseded REV2 after ordinary-suite evidence disproved REV2's interpretation of the challenge window. HAA-01 through HAA-09 execute on the production-safe exact `bounded-demand-following-5-10-5@2` mission pack / `integrated-operations-desktop-stable@4`; the accepted M10.9.8.1 v1 matrix remains immutable and the versioned execution revision remains `eng/m1098-integrated-human-automation-hmi-matrix-v2.json`. Historical @1 remains exact/replayable. The hotfix also closes F4 COMMANDS refresh/ENTER robustness without Simulation coefficient changes. The challenge activates canonically when `demand:stable-low-load-start` is satisfied; `Window(4_000, 8_000)` is a target-completion window offset from activation, not an activation boundary. Every HAA row verifies active demand evidence, full replay and checkpoint-prefix/live-continuation equivalence.

The nine rows vary only:

- assistance: `Hidden | ChecklistOnly | Guided`;
- requested authority: `Manual | Assisted | SupervisoryAutomatic`.

Every healthy supervisory row explicitly configures `HoldCurrentOperatingPoint` before requesting `SupervisoryAutomatic`. The automated test records canonical final fingerprints, requested/effective authority, active lifecycle/demand/score context, alarms/protection, accepted actions, a full-replay fingerprint and a checkpoint-prefix/live-continuation fingerprint. It requires the exact `bounded-demand-5-10-5@1` demand evidence to be available after canonical activation and freezes the target window as +4000..+8000 logical steps from `ActivatedLogicalStep`; M10.9.6.1 is rerun as the logical-time owner and M10.9.6.2 remains the owner of demand/request/output separation. At fixed authority, changing assistance must leave the physical/replay fingerprint and other canonical outcomes unchanged.

M10.9.8.2 Hotfix 1 REV5 has a deliberately narrow production App/Application diff: additive exact-v2 mission composition/startup resolution plus F4 COMMANDS presentation/input/error-boundary robustness. It does **not** change Simulation physics/coefficient calibration, protection or scoring ownership, archive schema, fingerprint algorithm or plant-command authority. The demand/score/authority/replay owners are rerun explicitly.

See `M10_9_8_2_AUTOMATED_HEALTHY_ASSISTANCE_AUTHORITY_MATRIX.md`.

## Validation required for active M10.9.8.4

Run:

```bat
dotnet build
dotnet test
scripts\run-m10984-replay-checkpoint-same-seed-integrity-audit.cmd
```

Promotion requires `m10984-replay-checkpoint-same-seed-integrity-passes=True`. M10.9.8.4 adds no separate manual HMI gate; end-to-end manual HMI/keyboard/session acceptance remains M10.9.8.5.

## Evidence and package policy

Candidate source ZIPs intentionally exclude `tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/Evidence/`, generated `artifacts/`, `bin/` and `obj/`.

Compact immutable prerequisites required by ordinary/current tests live under `eng/frozen-evidence/ordinary/`; manifests live under `eng/evidence-manifests/`. Generated audit CSV/TXT payloads remain local validation records and are not copied into each subsequent candidate ZIP.

## Current unresolved items

The authoritative limitation register is `KNOWN_MODEL_LIMITATIONS.md`. In particular:

- Phase I is closed; repaired exact-v4 production and the final cumulative long/reference chain are validated;
- the historical exact-v3 I.3 drift observations remain regression provenance and are not evidence that exact @4 has identical long-horizon means/slopes;
- historical H.28 remains `bounded-but-costly`; repaired Stage 4 separately demonstrated bounded-at-or-below repaired explicit relative wall cost on the validation machine;
- branch overrides disappeared in repaired long-horizon evidence, but previous-phase hysteresis remained materially active and must not be removed without separately scoped post-Phase-I retirement evidence;
- H.5/H.21 historical numerical source seams remain retained for provenance;
- severe-incident, structural-damage and several plant-system models remain reduced-order or incomplete;
- the reduced quadratic hydraulic map is continuous but not differentiable through some near-zero/reversal transitions, and the generic runtime/numerical-conditioning findings from the post-7.3 Simulation review are assigned to M11.3/M12 rather than patched into M10 presentation work; see `SIMULATION_NUMERICAL_REGULARITY_AND_RUNTIME_REVIEW.md`;
- the post-7.3 Application review assigns fingerprint-v1 anchoring plus lifecycle-spine/recent-evidence separation to M10.9.7.4, while recorder notification cost, fingerprint cost, collection-copy traps, long-session memory growth and recorder failure policy belong to M11.2/M11.3; see `APPLICATION_RECORDING_REPLAY_REVIEW.md` and ADR-0184;
- UI-thread runtime/projection responsiveness, notification fan-out and archive-export cost remain M11.3 measurement work; stable command-target identity and `MainWindowViewModel` decomposition remain M13 work; see `DESKTOP_HOST_FAILURE_AND_SESSION_SAVE_INTEGRITY_REVIEW.md` and ADR-0185.

## Continuation rule

Phase I, M10.9.5, M10.9.6 and M10.9.7 are closed. Continue milestone-by-milestone from the latest validated baseline: **M10.9.7.5 Hotfix 1 VALIDATED → M10.9.8.1 REV1 Docs1 VALIDATED → M10.9.8.2 Hotfix 1 REV5 VALIDATED → M10.9.8.3 VALIDATED → active M10.9.8.4 replay/checkpoint/same-seed integrity → M10.9.8.5 manual HMI acceptance → mandatory M10 Final Pre-M11 cumulative + long validation → M11**.

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
