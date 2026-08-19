# M10.9.4.1-H.28 Requalification 1 — Performance, Cost & Long-Running Operational Soak

## Status

**CANDIDATE**, built directly on user-validated **M10.9.4.1-H.28.1-D Preflight Hotfix 1**.

H.28 originally failed only its performance ceilings. The numerical, ownership, determinism and operational-soak evidence remained green. H.28.1-A then attributed the cost, H.28.1-C removed allocation churn, H.28.1-B removed duplicated predictor work and H.28.1-D removed exact duplicate CPU work inside the unchanged 32-probe finite-difference Jacobian.

The validated H.28.1-D focused gate preserved the exact deterministic fingerprint while reducing average Jacobian wall cost to 14.664% of H.28.1-B, H.9 wall cost to 16.731% and trigger-engine wall cost to 17.633%.

## Purpose

Rerun the **original H.28 qualification contract** over the now-optimized corrected runtime. This milestone does not weaken or reinterpret the original ceilings.

Hard ceilings remain:

- corrected median wall / explicit median wall <= **8.0**;
- corrected p95 wall / explicit p95 wall <= **12.0**;
- corrected median allocation / explicit median allocation <= **16.0**.

Advisory classification remains:

- `activation-favorable` when median wall ratio <= 4.0 and median allocation ratio <= 8.0;
- `bounded-but-costly` when the hard ceilings pass but the advisory limits do not.

## Frozen provenance

The gate fingerprint-checks both:

1. user-validated H.27 off-design evidence;
2. user-validated H.28.1-D CPU-hot-path evidence.

The frozen H.28.1-D evidence must retain:

- 20/20 trigger/commit;
- 35 hydraulic evaluations;
- 32 finite-difference probes;
- Jacobian dimension 32;
- exact deterministic fingerprint `518BA948637F0C270C7F8228AB97FEF9148E29A4F5CE4376319AB5D1CFBE7F38`;
- `h28.1d-hydraulic-probe-cpu-hot-path-optimization-passes=True`.

## Runtime scope

This requalification changes no numerical runtime implementation. The runtime under test is exactly the validated H.28.1-D source. Only application metadata, focused H.28 test restoration, frozen H.28.1-D evidence and documentation/runner files are added or changed.

Standard current-v2 remains `ExplicitCommittedState` at a 10 ms simulated fixed step. H.29 remains blocked until this gate is green.

## After a green H.28

A green result removes the performance block but does **not** activate corrected ownership. Because H.28.1-B/C/D changed runtime implementation, H.24 must be repeated once as the rare long-horizon requalification before H.29 activation review.
