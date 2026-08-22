# M10.9.8.1 REV1 — Integrated Human / Automation / HMI validation matrix freeze

## Status

**REV1 Docs1 VALIDATED — accepted 2026-08-22 after build, complete ordinary suite, focused matrix audit and explicit user acceptance.**

M10.9.8.1 is a contract/evidence-planning slice only. REV1 deliberately keeps the entire compiled surface unchanged from the validated baseline: all files under `src/` and `tests/` are byte-identical to M10.9.7.5 Hotfix 1 VALIDATED. It adds no plant behavior, XAML/runtime semantics, Simulation physics, authority rule, challenge/scoring rule, protection rule, archive schema, fingerprint algorithm, workstation command authority or production scenario registration.

The machine-readable source of truth is:

`eng/m1098-integrated-human-automation-hmi-matrix.json`

The matrix is frozen before execution work begins. Its schema is validated externally by `eng/validate-m10981-integrated-validation-matrix.ps1`; the focused gate also reuses already-validated M5/Application owner tests instead of adding new compiled M10.9.8.1 tests. A failing future row must be classified to its existing owner; M10.9.8 is not authorization to retune Phase H/I numerics or invent new physics/HMI features.

## Baseline

The only baseline is **M10.9.7.5 Hotfix 1 VALIDATED / M10.9.7 CLOSED**.

The authoritative desktop production identity remains:

`integrated-normal-operations-training-i5-repaired-v4-production | integrated-operations-desktop-stable@4 | CorrelationConsistentInverseDomain | FourNodeBranchContinuityCorrectedCommitOptIn | 10 ms`

Historical exact-version identities remain immutable.

## Fixed axes

Training assistance:

- `Hidden`
- `ChecklistOnly`
- `Guided`

Plant control authority:

- `Manual`
- `Assisted`
- `SupervisoryAutomatic`

The healthy matrix therefore contains exactly **9 rows**. All nine use the same exact bounded-demand challenge/trajectory and command schedule so assistance and authority are the intended independent variables.

## Cross-cutting invariants

1. Assistance-only changes must not change plant physics or control authority.
2. Protection always overrides normal control.
3. Requested and effective authority remain separately visible.
4. Supervisory degradation is fail-closed and evidence-based.
5. Expected command influence remains distinct from observed response.
6. External demand, requested generator load and actual electrical output remain distinct.
7. Scoring remains observational.
8. Unavailable/suspect measurements stay unavailable/suspect; no true-state substitution.
9. MISSION has no plant-command authority.
10. Replay/checkpoint reconstructs equivalent operator-visible state without opaque workstation dumps.
11. Keyboard-only critical operation remains viable.

The JSON matrix assigns an explicit investigation owner to every invariant and every row.

## Frozen rows

| Row | Family | Exact scenario / validation composition | Exact profile | Assistance | Requested → expected effective authority |
| --- | --- | --- | --- | --- | --- |
| HAA-01 | healthy bounded load | `bounded-demand-following-5-10-5@1` | `stable-low-load-parallel-operation@1` | Hidden | Manual → Manual |
| HAA-02 | healthy bounded load | `bounded-demand-following-5-10-5@1` | `stable-low-load-parallel-operation@1` | Hidden | Assisted → Assisted |
| HAA-03 | healthy bounded load | `bounded-demand-following-5-10-5@1` | `stable-low-load-parallel-operation@1` | Hidden | SupervisoryAutomatic → SupervisoryAutomatic |
| HAA-04 | healthy bounded load | `bounded-demand-following-5-10-5@1` | `stable-low-load-parallel-operation@1` | ChecklistOnly | Manual → Manual |
| HAA-05 | healthy bounded load | `bounded-demand-following-5-10-5@1` | `stable-low-load-parallel-operation@1` | ChecklistOnly | Assisted → Assisted |
| HAA-06 | healthy bounded load | `bounded-demand-following-5-10-5@1` | `stable-low-load-parallel-operation@1` | ChecklistOnly | SupervisoryAutomatic → SupervisoryAutomatic |
| HAA-07 | healthy bounded load | `bounded-demand-following-5-10-5@1` | `stable-low-load-parallel-operation@1` | Guided | Manual → Manual |
| HAA-08 | healthy bounded load | `bounded-demand-following-5-10-5@1` | `stable-low-load-parallel-operation@1` | Guided | Assisted → Assisted |
| HAA-09 | healthy bounded load | `bounded-demand-following-5-10-5@1` | `stable-low-load-parallel-operation@1` | Guided | SupervisoryAutomatic → SupervisoryAutomatic |
| INT-10 | synchronization/loading | `grid-synchronization-initial-loading` | `pre-synchronization-grid-loading@1` | Guided | Manual → Manual |
| INT-11 | blocked permissive/interlock | `grid-synchronization-initial-loading` | `pre-synchronization-grid-loading@1` | ChecklistOnly | Manual → Manual |
| INT-12 | degraded required supervisory measurement | validation-only `m1098-supervisory-required-measurement-unavailable@1` | `integrated-operations-desktop-stable@4` | Guided | SupervisoryAutomatic → Assisted |
| INT-13 | canonical protection trip | `m87-protection-fail-safe-response` | `stable-low-load-parallel-operation@1` | Guided | SupervisoryAutomatic → Assisted |
| INT-14 | equipment fault | `hydraulic-component-fault-demonstration` | `stable-low-load-parallel-operation@1` | ChecklistOnly | Assisted → Assisted |
| INT-15 | instrumentation fault | `instrumentation-control-fault-demonstration` | `stable-low-load-parallel-operation@1` | Guided | Assisted → Assisted |
| INT-16 | manual takeover | `integrated-normal-operations-training-i5-repaired-v4-production` | `integrated-operations-desktop-stable@4` | Hidden | SupervisoryAutomatic → Manual |
| INT-17 | challenge/demand-following | `bounded-demand-following-5-10-5@1` | `stable-low-load-parallel-operation@1` | Guided | Assisted → Assisted |
| INT-18 | checkpoint/replay continuation | `bounded-demand-following-5-10-5@1` | `stable-low-load-parallel-operation@1` | ChecklistOnly | SupervisoryAutomatic → SupervisoryAutomatic |
| INT-19 | terminal mission with plant continuing | `bounded-demand-following-5-10-5@1` | `stable-low-load-parallel-operation@1` | Hidden | Manual → Manual |

### Why INT-12 is validation-only

The existing product catalog does not contain a dedicated scenario that invalidates exactly one measurement required by an active `HoldOperatingPoint` supervisory objective while keeping the rest of the row controlled. M10.9.8.1 therefore freezes a **validation-only composition ID**, `m1098-supervisory-required-measurement-unavailable@1`.

M10.9.8.3 may realize that row only by composing the already-existing authoritative exact-v4 profile, measured-signal/fault seam and M5 supervisory coordinator in test/audit code. It must not register a new production scenario or fault type merely to satisfy the validation matrix.

## Execution ownership

M10.9.8.2 owns the nine healthy HAA rows. M10.9.8.3 owns degraded/fault/protection/takeover rows. M10.9.8.4 owns full replay/checkpoint integrity for representative healthy and degraded rows. M10.9.8.5 owns the end-to-end manual HMI acceptance and M10 closure.

If a row fails, classify before changing code:

- presentation/usability → App/HMI presentation owner;
- recorder/replay/checkpoint → M9.1/M10.7 owner;
- challenge/demand/scoring → M10.9.6 owner;
- plant control authority/takeover/degradation → M5 owner;
- protection → M5 protection owner;
- existing fault behavior → M8 fault framework plus physical owner;
- newly discovered physical-model limitation → post-M11 engineering backlog unless it invalidates supported operation;
- Phase H/I numerics → reopen only with direct evidence against an already validated numerical contract.

## Seed contract

The selected rows use deterministic non-RNG owners. `deterministicSeed` is therefore `null` in matrix schema v1. This is explicit, not omitted evidence. A seeded row may be added only through a deliberate versioned matrix revision.

## M10.9.8.1 exit gate

Run:

```bat
dotnet build
dotnet test
scripts\run-m10981-integrated-validation-matrix-audit.cmd
```

Then review `docs\M10_9_8_1_MATRIX_ACCEPTANCE_CHECKLIST.md`.

This exit gate is satisfied. M10.9.8.2 may execute only the accepted frozen HAA-01..HAA-09 contract; changes to the matrix require an explicit versioned revision rather than silent repair.
