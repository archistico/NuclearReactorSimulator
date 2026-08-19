# M10.9.4.1-H.28 Requalification 2 — Performance, Cost & Long-Running Operational Soak

## Status

CANDIDATE. Built directly on user-validated H.28.1-G.

H.28.1-G passed build, the complete ordinary test suite and its focused CPU-tail gate. Its 20 triggered benchmark-equivalent steps had p95 `79,702.3 us`, below the unchanged H.28 readiness threshold `88,381.2 us`, while preserving the deterministic fingerprint `518BA948637F0C270C7F8228AB97FEF9148E29A4F5CE4376319AB5D1CFBE7F38`.

## Purpose

Re-run the original H.28 performance / cost / bounded operational-soak qualification over the validated H.28.1-G optimized runtime.

This requalification does not change numerical runtime code. It restores the original H.28 audit over the current validated implementation and freezes the user-produced H.28.1-G evidence as provenance.

## Unchanged H.28 hard ceilings

- median corrected/explicit wall-cost ratio `<= 8`;
- p95 corrected/explicit wall-cost ratio `<= 12`;
- median corrected/explicit allocation ratio `<= 16`.

The benchmark workload, warmup, 256-step paired runs, 1,536-step soak, deterministic controls, safety assertions and numerical closure ceilings are unchanged.

## Frozen H.28.1-G evidence

Canonical SHA-256:

- summary: `260904E90A4D7B6E64F109BAF6FFE76A27DDCA7B82390348E01EBE4B380CC1E2`;
- steps: `CBEC443C90CA49CB88A878352A5CAC392FBA9825E75C864AD1FD4FBD34AA05A0`;
- metrics: `4E868C072485FC575A4D71CBAC0FB230809F119A26B44CD1F9B6FEDD6F92A2CC`.

The frozen summary must continue to prove:

- 20/20 trigger/commit;
- zero rollback, unsafe commit and fallback-commit violation;
- 35 hydraulic evaluations;
- 32 finite-difference probes;
- Jacobian dimension 32;
- triggered p95 `79,702.3 us`;
- estimated unchanged-H.28 p95 ratio `10.821618172190465`;
- `tail-ready-for-h28=True`;
- exact deterministic fingerprint.

## Numerical contract

No changes are permitted to:

- H.9 finite-difference Newton mathematics;
- 32 probes / 35 logical hydraulic evaluations;
- P060/F040;
- H.9 residuals/tolerances;
- 2% / 5 K continuity hysteresis;
- target set `steam|stop-out|header|turbine-inlet`;
- H.20 authority;
- H.22 commit ownership;
- physical coefficients;
- 10 ms simulated fixed step.

Standard current-v2 remains `ExplicitCommittedState`.

## Validation

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-four-node-performance-cost-operational-soak-audit.cmd
```

If this gate is green, H.28 becomes validated again on the optimized runtime. That removes the performance block, but H.29 still must not start until H.24 long-horizon qualification is rerun once over the stabilized runtime implementation.
