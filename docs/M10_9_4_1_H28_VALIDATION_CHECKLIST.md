# M10.9.4.1-H.28 Requalification 2 Validation Checklist

## Baseline and provenance

- [ ] Baseline is user-validated H.28.1-G.
- [ ] Frozen H.27 summary/telemetry/envelope/metrics fingerprints pass.
- [ ] Frozen H.28.1-G summary/steps/metrics fingerprints pass.
- [ ] Frozen H.28.1-G evidence confirms 20/20 trigger/commit, 35 hydraulic evaluations, 32 probes, Jacobian dimension 32, trigger p95 79.7023 ms and exact deterministic fingerprint.
- [ ] Frozen H.28.1-G evidence confirms `tail-ready-for-h28=True`.
- [ ] No H.9/H.20/H.22 numerical or ownership algorithm is changed by the requalification package.
- [ ] Standard current-v2 remains `ExplicitCommittedState` at 10 ms.
- [ ] Original H.28 ceilings are unchanged.

## Build and ordinary suite

- [ ] `dotnet build` passes.
- [ ] `dotnet test` passes.

## Paired benchmark — unchanged original H.28 contract

- [ ] 64 warmup steps per mode complete without trip.
- [ ] 256 explicit benchmark steps complete.
- [ ] 256 corrected-commit benchmark steps complete under the same 5→0→5 MWe manoeuvring workload.
- [ ] Corrected benchmark observes at least one P060/F040 trigger and one corrected commit.
- [ ] Zero unsafe corrected commits.
- [ ] Median corrected/explicit wall-cost ratio <= 8.0.
- [ ] P95 corrected/explicit wall-cost ratio <= 12.0.
- [ ] Median corrected/explicit allocation ratio <= 16.0.
- [ ] `activation-favorable` or `bounded-but-costly` classification is emitted.

## Bounded operational soak

- [ ] 1,536 corrected-commit steps complete.
- [ ] Two 5→0→5 MWe request manoeuvres are exercised.
- [ ] At least one trigger and one corrected commit occur.
- [ ] Zero fallback-commit violations.
- [ ] Zero unsafe corrected commits.
- [ ] Zero untargeted branch disagreements.
- [ ] Zero unexpected trip steps.
- [ ] Mass closure <= 1e-6 kg.
- [ ] Energy closure <= 1e-2 J.
- [ ] Balance mass-rate residual <= 1e-8 kg/s.
- [ ] Balance power residual <= 1e-3 W.

## Determinism and artifacts

- [ ] Two fresh 128-step deterministic controls produce the same fingerprint.
- [ ] `00-progress.txt` exists.
- [ ] `01-four-node-performance-cost-operational-soak.summary.txt` exists.
- [ ] `02-performance-benchmark.csv` exists.
- [ ] `03-operational-soak-samples.csv` exists.
- [ ] `04-performance-cost-soak-metrics.csv` exists.
- [ ] `four-node-performance-cost-operational-soak-passes=True`.
- [ ] `h28-audit-passes=True`.

## Interpretation

- [ ] Green H.28 removes the performance block only; it does not change the default mode.
- [ ] H.24 long-horizon requalification is required once before H.29 because the B/C/D/E/F/G optimization chain changed runtime implementation.
- [ ] H.29 remains a separate activation decision.
