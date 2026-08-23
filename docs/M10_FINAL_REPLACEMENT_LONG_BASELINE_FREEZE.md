# M10 Final — Exact-v9 Replacement-Long Baseline Freeze

## Status

**CANDIDATE — baseline/workload freeze only.** The exact-v9 authoritative activation decision has returned green and is recorded in `eng/m10-final-v9-production-activation-decision-record.json`. This candidate does not execute the replacement long and does not close M10.

A green baseline-freeze gate authorizes a separate execution candidate to run the redesigned replacement-long campaign against the exact manifests and contract frozen here. Any later `src/` change, or any change to a pre-existing test file, invalidates that authorization and requires a new freeze.

## Authoritative production baseline

The validated production state entering this freeze is:

- policy: `M10FinalExactV9QualifiedCandidate`;
- initial condition: `integrated-operations-desktop-stable@9`;
- production scenario: `integrated-normal-operations-training-m10-final-v9-production`;
- production mission: `bounded-demand-following-5-10-5@3`;
- exact-v4 retained as historical I.5 production;
- exact-v3 retained as historical H.30 production;
- exact-v2 retained as explicit fail-closed rollback.

The failed first-long exact-v4 manifests remain untouched provenance:

- `eng/m10-final-long-baseline-src.sha256`;
- `eng/m10-final-long-baseline-tests.sha256`.

They are **not** rebased, rewritten or reused.

The replacement baseline is frozen separately as:

- `eng/m10-final-replacement-long-v9-baseline-src.sha256` — 959 production files;
- `eng/m10-final-replacement-long-v9-baseline-tests.sha256` — 351 pre-execution test files.

The later execution candidate may add exactly one explicit replacement-long test file plus orchestration/finalization support. It may not modify any frozen production file or pre-existing test file.

## Why the workload is redesigned rather than proportionally shortened

The failed first campaign used 14,400 authored simulated seconds. It combined two defects that are no longer representative of the current baseline:

1. exact-v4 contained a real whole-cycle operating-point drift that crossed the water/steam domain after 300 s and before 600 s;
2. MISSION live projection rescanned an ever-growing prefix on every presentation step, giving O(n²) aggregate session work.

Diagnostic 1–11 and the activation gates removed both blockers. Exact-v9 is analytically balanced and demonstrated 600 s with near-zero mass/energy drift. LR-M1 Hotfix 1 demonstrated prefix-independent live projection through a 100,000-sample synthetic equivalence/scaling census.

The replacement campaign therefore spends wall time on **independent coverage**, not on reproducing thousands of redundant steady-state seconds:

| Leg | Simulated time | Steps | Information gained |
|---|---:|---:|---|
| RL-H1 | 900 s | 90,000 | Exact-v9 healthy soak beyond the 600 s qualification, with 60 s equilibrium windows ending at 300/600/900 s, conservation and moisture-owner checks. |
| RL-M1 | 480 s | 48,000 | Production mission @3, 5→10→5 evidence, >400 s post-terminal continuation and eight real live-projection wall-cost windows. |
| RL-D1 | 300 s | 30,000 | Required power-measurement degradation at 90 s, clear at 150 s, then 150 s of recovered authority observation. |
| RL-P1 | 180 s | 18,000 | SCRAM at 60 s, blocked normal command at 75 s, manual takeover at 120 s and 60 s post-takeover observation. |
| RL-R1 | 60 s | 6,000 | Mission @3 recording, full replay and checkpoint/live-continuation equivalence with bounded archive growth. |
| **Total** | **1,920 s** | **192,000** | Information-dense replacement campaign. |

Replay/checkpoint reconstruction performs additional deterministic physical steps beyond the 1,920 authored seconds; the hard wall deadline applies to the complete job, not only authored exposure.

## Workstation wall budget

The operational policy previously frozen by Diagnostic 1 remains unchanged:

- target: **35–45 minutes** on the validation workstation;
- hard campaign cap: **60 minutes**;
- wall time is a validation-job budget, **not** a physics tolerance.

Diagnostic 11 Hotfix 2 reported 600 simulated seconds in 12m30.059s on the validation workstation. A simple linear projection of 1,920 authored seconds is approximately **40.003 minutes**, before replay overhead. This places the replacement workload in the requested target band while preserving headroom to the 60-minute hard cap.

The execution harness must pass a common UTC deadline to all five legs and stop/fail when the deadline is exceeded. The long cannot be made green by widening physics/conservation limits or by silently skipping a leg.

## Frozen exact-v9 sentinels

The replacement contract does **not** reinterpret the exact-v4 I.3 absolute targets. Those remain historical exact-v4 evidence. Exact-v9 uses the already validated activation envelope:

- electrical export: `4.99..5.01 MWe`;
- primary pump flow: `99.9..100.1 kg/s`;
- drum level: `0.49..0.51`;
- governor output: `29.27..29.30 %`;
- moisture drain: at least `0.30 kg/s`;
- commanded-vs-total-transfer mismatch: at most `1e-8 kg/s`;
- stage energy-ownership residual: at most `1e-3 W`.

The exact-v9 healthy windows additionally freeze:

- absolute node mass slope <= `1e-5 kg/s` for every canonical fluid node;
- absolute late mean net external power <= `1e-4 MW`.

These are frozen before replacement execution. The mass-slope ceiling is more than two orders of magnitude above the largest Diagnostic-11 final-60 observed node slope while remaining many orders tighter than the old exact-v4 drift. The net-power ceiling is likewise far above Diagnostic-11 numerical residual while still detecting meaningful stored-energy drift.

The pre-existing instantaneous conservation ceilings remain unchanged:

- mass closure `<= 1e-6 kg`;
- energy closure `<= 1e-2 J`;
- balance mass-rate residual `<= 1e-8 kg/s`;
- balance power residual `<= 1e-3 W`.

## LR-M1 live scalability sentinel

The synthetic 100,000-sample equivalence/scaling census remains a prerequisite. The real mission leg complements it with eight 60 s wall windows over 48,000 actual live samples.

The within-run late/early wall-cost ratio is frozen at `<= 2.0`. This is a same-machine/session **scalability-shape sentinel**, not a cross-machine absolute speed requirement. Lifecycle spine remains capped at 32 and recent operational evidence at 100.

## Baseline-freeze gate

Run:

```bat
scripts\run-m10-final-replacement-long-baseline-freeze.cmd
```

It performs:

1. exact-v9 activation prerequisite + contract/manifest validation;
2. restore/build with warnings as errors;
3. complete ordinary suite;
4. LR-M1 Hotfix 1 semantic-equivalence/scaling regression;
5. baseline-freeze artifact finalization.

Return the complete:

```text
artifacts\m10-final-replacement-long-baseline-freeze
```

before running the replacement long.

A green freeze gate produces `replacement-long-authorized=True`; it does **not** produce `m10-closure-eligible=True` because the long itself has not yet run.
