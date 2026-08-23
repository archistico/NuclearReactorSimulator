# M10 Final — Exact-v9 Replacement-Long Execution

## Status

**CANDIDATE — authorized execution only.** The returned exact-v9 baseline-freeze artifact is green and records:

- `replacement-long-authorized=True`;
- `replacement-long-executed=False`;
- `m10-closure-eligible=False`;
- 959 frozen production `src` files;
- 351 frozen pre-existing test files;
- 1,920 authored simulated seconds / 192,000 deterministic 10 ms steps across five legs;
- target workstation wall time 35–45 minutes;
- hard campaign cap 60 minutes.

This candidate may add exactly one test file. It may not modify any frozen `src` file or any of the 351 pre-existing test files.

## Authorized execution surface

The only additional test file is:

```text
tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/M10FinalReplacementLongValidationTests.cs
```

The preflight validates every frozen hash and rejects any second test addition. The five legs execute inside one explicit xUnit test so the common wall-clock deadline covers the entire campaign, including replay and checkpoint reconstruction.

The entry point is:

```bat
scripts\run-m10-final-replacement-long-validation.cmd
```

The test requires the explicit environment opt-in set by the script:

```text
NRS_M10_FINAL_REPLACEMENT_LONG=1
```

## Frozen workload

| Leg | Seconds | Steps | Purpose |
|---|---:|---:|---|
| RL-H1 | 900 | 90,000 | Exact-v9 healthy soak beyond the validated 600 s domain; rolling 300/600/900 s operating-point, conservation, moisture-owner and numerical-safety sentinels. |
| RL-M1 | 480 | 48,000 | Current production mission `bounded-demand-following-5-10-5@3`; full 5→10→5 handling, >400 s post-terminal continuation, bounded live evidence and eight real 60 s projection-cost windows. |
| RL-D1 | 300 | 30,000 | Required power-measurement degradation at step 9,000, clear at 15,000 and long post-clear authority recovery. |
| RL-P1 | 180 | 18,000 | SCRAM at 6,000, authority observation at 6,001, blocked normal command at 7,500, manual takeover at 12,000 and post-takeover observation. |
| RL-R1 | 60 | 6,000 | Mission @3 recording, full replay and checkpoint/live-continuation equivalence, with archive-growth bounds. |

The 1,920 seconds count authored exposure only. Replay and checkpoint reconstruction execute additional deterministic physical steps; those extra steps are included in the common 60-minute wall cap.

## Healthy exact-v9 acceptance

RL-H1 samples the canonical exact-v9 whole-cycle state once per simulated second. Sixty-second windows ending at 300, 600 and 900 s must retain:

- electrical export `4.99..5.01 MWe`;
- primary pump flow `99.9..100.1 kg/s`;
- drum level `0.49..0.51`;
- governor output `29.27..29.30 %`;
- moisture drain `>= 0.30 kg/s`;
- commanded-vs-total turbine transfer mismatch `<= 1e-8 kg/s`;
- turbine stage energy-ownership residual `<= 1e-3 W`;
- absolute mass slope for each canonical whole-cycle fluid node `<= 1e-5 kg/s`;
- absolute net external power in each final rolling window `<= 1e-4 MW`.

The instantaneous conservation ceilings remain the already frozen values:

- mass closure `<= 1e-6 kg`;
- full energy closure `<= 1e-2 J`;
- balance mass-rate residual `<= 1e-8 kg/s`;
- balance power residual `<= 1e-3 W`.

Four-node telemetry must show zero rollback, explicit fallback, fallback-commit violation, unsafe commit and untargeted branch disagreement.

## Mission scalability acceptance

RL-M1 uses the authoritative pack `bounded-demand-following-5-10-5@3` and exact-v9 scenario. It dispatches exactly one accepted generator load raise on the first 10 MWe demand and one accepted lower on the subsequent return to 5 MWe.

The leg records 48,000 live samples and eight 6,000-step projection timing windows. The late/early live-projection wall-cost ratio must remain `<= 2.0`. This is an intra-run scalability-shape sentinel, not an absolute cross-machine performance threshold.

Lifecycle spine remains capped at 32, recent operational evidence at 100, duplicate timeline rows at zero and challenge lifecycle may not end in `Failed`. The frozen logical-time contract must leave at least 40,000 authored steps after terminal mission state.

## Fault, protection and replay acceptance

RL-D1 requires deterministic activation/clear and fail-closed degraded authority followed by recovery to requested/effective supervisory automatic authority with normal health.

RL-P1 requires no protection before the authored SCRAM, protection precedence after SCRAM, blocked normal rod command preserving the trip, and successful manual authority takeover without clearing the SCRAM latch.

RL-R1 requires exact final snapshot equivalence across live execution, full replay and checkpoint continuation; exact challenge-projection fingerprint equivalence; recording equivalence; exactly one contiguous frame per authored step plus the initial frame; and full-to-half archive size ratio `<= 2.25`.

## Wall-clock policy

The explicit campaign test starts one stopwatch before RL-H1 and stops it after RL-R1, including replay/checkpoint reconstruction. The job fails if the elapsed wall time exceeds 60 minutes.

The 35–45 minute target remains diagnostic only. A run outside that target can still pass if it remains under 60 minutes and every physics, conservation, determinism and evidence criterion is green. The 60-minute cap may not be made green by skipping a leg or weakening physical tolerances.

## Artifacts

The run must return the complete:

```text
artifacts/m10-final-replacement-long-validation
```

with:

```text
00-progress.txt
01-m10-final-replacement-long-validation.summary.txt
02-workload-contract.json
03-leg-summary.csv
04-conservation-maxima.csv
05-healthy-window-v9-operating-point-sentinels.csv
06-numerical-coupling-telemetry.csv
07-trip-fault-protection-classification.csv
08-mission-demand-score-evidence.csv
09-replay-checkpoint-fingerprint-sentinels.csv
10-evidence-growth.csv
11-performance-diagnostics.csv
12-wall-budget-summary.txt
```

A fully green finalizer may record:

```text
replacement-long-validation-passes=True
replacement-long-executed=True
m10-closure-eligible=True
```

That result does **not** close M10 automatically. A separate explicit M10 closure/promote step is still required before M11.
