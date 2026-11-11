# M10 Final Pre-M11 Validation Plan

## Purpose

Before any M11 work starts, the final M10 candidate must pass one deliberately stronger cumulative gate than the normal milestone workflow. This gate is mandatory once M10.9.8 manual closure is complete; it is not part of the ordinary developer suite.

## Two-stage final gate

### Stage A — cumulative M10 validation

A dedicated orchestrator will perform, from a clean source tree:

- restore/build with warnings-as-errors;
- complete ordinary test suite;
- a curated list of **current authoritative** M10 focused audits, excluding superseded/historical-only candidates;
- archive/checkpoint/replay/fingerprint contracts;
- challenge/mission/demand/scoring contracts;
- assistance/authority/degraded/fault/protection/takeover contracts;
- M10.9.8 integrated validation and documentation/manual consistency checks.

Planned entry point:

```bat
scripts\run-m10-final-validation.cmd
```

### Stage B — explicit long operational validation

A separate scheduled/explicit gate targets approximately one hour of workstation validation. Wall clock is diagnostic; the deterministic workload is frozen independently of machine speed. It must not become part of routine `dotnet test`.

The long gate will include representative healthy and degraded operational trajectories and must check at minimum:

- no unhandled runtime exception;
- no supported water/steam envelope escape on healthy trajectories;
- no unexpected protection trip in healthy profiles;
- expected fault/protection behavior in intentionally degraded profiles;
- challenge/demand-following continuity;
- assistance/authority transitions and manual takeover;
- periodic checkpoint/replay verification during the run;
- deterministic repeat/same-seed evidence at selected sentinels;
- bounded recorder/session evidence growth sufficient for the final M10 supported session envelope.

Planned entry point:

```bat
scripts\run-m10-final-long-validation.cmd
```

The workload is now frozen after M10.9.8.5 and the cumulative gate: 14,400 simulated seconds / 1,440,000 authored 10 ms steps across LR-H1/LR-M1/LR-D1/LR-P1/LR-R1. The duration was timing-calibrated before the first acceptance run; no physics or acceptance tolerance was changed.

## Required final artifact

Stage A emits under `artifacts\m10-final-pre-m11-validation\`; Stage B emits under `artifacts\m10-final-long-validation\`.

The final summary must include at least:

```text
m10-clean-build-passes=True
m10-ordinary-suite-passes=True
m10-authoritative-focused-gates-pass=True
m10-replay-checkpoint-determinism-passes=True
m10-long-operational-soak-passes=True
m10-unexpected-trip-count=0
m10-unhandled-exception-count=0
m10-final-pre-m11-validation-passes=True
```

M11 may start only after this final summary is green.


## Current execution state — 2026-08-22

M10.9.8.5 manual integrated HMI acceptance is complete and M10.9.8 is CLOSED. **M10 Final Pre-M11 Cumulative Validation Hotfix 1 is VALIDATED** with `m10-final-cumulative-validation-passes=True`. The remaining blocking gate is the frozen long campaign implemented by `scripts/run-m10-final-long-validation.cmd`. M10 and M11 remain blocked until that gate is green and M10 closure is explicitly recorded.
