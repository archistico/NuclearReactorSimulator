# M10 Final Long Validation — Frozen Workload & Acceptance Specification v2

## Status

**FROZEN / AWAITING FIRST ACCEPTANCE RUN.**

This is the blocking scheduled-long gate after the validated M10 final cumulative gate and before M10 closure / M11.
It adds no production runtime or Simulation physics. The workload and all acceptance thresholds are frozen before execution.

## Timing calibration

The earlier Draft 1 used 3,600 simulated seconds. The validated cumulative run supplied a timing-only calibration point: the current exact-v4 300 s reference took approximately 62 s wall clock on the validation workstation. That demonstrated that 3,600 simulated seconds would likely under-run the original user requirement for an approximately one-hour workstation validation.

Before the first long acceptance run, and **without changing any physics or pass/fail tolerance**, the workload duration was therefore expanded to **14,400 simulated seconds / 1,440,000 authored logical steps**. Wall clock remains diagnostic rather than a cross-machine acceptance metric.

## Frozen workload

| Leg | Simulated time | Authored steps | Purpose |
|---|---:|---:|---|
| LR-H1 | 7,200 s | 720,000 | Healthy exact-v4 whole-plant soak, conservation, I.3 rolling sentinels, numerical coupling safety |
| LR-M1 | 4,400 s | 440,000 | Production mission @2, demand/request/actual evidence, terminal-mission/continuing-plant stability |
| LR-D1 | 1,800 s | 180,000 | Required-measurement degradation and recovery through the canonical fault/authority owners |
| LR-P1 | 900 s | 90,000 | Expected SCRAM, protection precedence, suspended supervisory authority and later manual takeover |
| LR-R1 | 100 s | 10,000 | Recording, full replay and checkpoint-prefix/live-continuation exact equivalence |
| **Total** | **14,400 s** | **1,440,000** | approximately one-hour-class workstation campaign |

Replay and checkpoint reconstruction perform additional deterministic physical steps beyond the authored LR-R1 trajectory; they do not change the stated simulated exposure contract.

## Frozen exact decisions

- fixed step: 10 ms;
- exact-v4 production identity: `integrated-operations-desktop-stable@4`;
- thermodynamic closure: `CorrelationConsistentInverseDomain`;
- hydraulic mode: `FourNodeBranchContinuityCorrectedCommitOptIn`;
- production mission: `bounded-demand-following-5-10-5@2`;
- LR-D1 unavailable `power` measurement activates at step 54,000 and clears at 90,000;
- LR-P1 SCRAM commits at 54,000, protection-authority observation is checked at 54,001, normal rod command at 60,000 must not defeat SCRAM, manual takeover is requested at 72,000;
- LR-R1 load raise at 500, load lower at 3,000, checkpoint at 5,000, rod hold at 6,000, final step 10,000. The replay sentinel is intentionally shorter than the other legs because the recorder retains full immutable snapshots per frame; long-duration plant exposure is carried by LR-H1/LR-M1/LR-D1/LR-P1 rather than by unbounded recorder memory.

## Global blocking criteria

The acceptance run requires zero unhandled exceptions, non-finite observations, unsupported water/steam envelope excursions, fingerprint mismatches, fallback-commit violations, unsafe corrected commits, untargeted branch disagreements, healthy unexpected trips, unexpected fault activations, exact-version identity drift and replay/timeline structural duplicates.

Current exact-v4 conservation ceilings remain unchanged:

| Metric | Ceiling |
|---|---:|
| `abs(mass-closure-residual)` | `1e-6 kg` |
| `abs(energy-closure-residual)` | `1e-2 J` |
| `abs(balance-mass-rate-residual)` | `1e-8 kg/s` |
| `abs(balance-power-residual)` | `1e-3 W` |

## Healthy rolling I.3 sentinels

The 19 validated I.3 budgets remain immutable. LR-H1 evaluates 60-second windows ending every 300 simulated seconds from 300 through 7,200 s: **24 windows × 19 budgets = 456 comparisons**.

A violation blocks closure. The budget must not be widened after observing this run. If a later window demonstrates that a historical 300 s absolute budget is not a valid long-horizon claim, that is a model/evidence finding to investigate and document, not permission for retrospective tolerance tuning.

## Evidence growth

MISSION lifecycle spine remains capped at 32 and recent operational evidence at 100. Replay recording must contain exactly one contiguous frame per logical step plus the initial frame. Half/full archive serialization sizes are diagnostic; `full / half <= 2.25` is frozen as a superlinear-growth sentinel for LR-R1.

## Required artifacts

`artifacts/m10-final-long-validation/` must contain:

1. `00-progress.txt`
2. `01-m10-final-long-validation.summary.txt`
3. `02-workload-contract.json`
4. `03-leg-summary.csv`
5. `04-conservation-maxima.csv`
6. `05-healthy-window-i3-budget-comparison.csv`
7. `06-numerical-coupling-telemetry.csv`
8. `07-trip-fault-protection-classification.csv`
9. `08-mission-demand-score-evidence.csv`
10. `09-replay-checkpoint-fingerprint-sentinels.csv`
11. `10-evidence-growth.csv`
12. `11-performance-diagnostics.csv`

## Promotion rule

`m10-final-long-validation-passes=True` makes M10 **eligible** for closure. It does not silently rewrite source documentation or start M11. A final closure/promotion step records the long evidence, marks `LONG-SOAK-01` closed and declares M10 CLOSED before M11 begins.
