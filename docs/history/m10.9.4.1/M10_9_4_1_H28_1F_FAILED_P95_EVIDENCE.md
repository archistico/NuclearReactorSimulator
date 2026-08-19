# M10.9.4.1-H.28.1-F — failed p95 evidence

H.28.1-F compiled and its focused gate executed on 2026-08-19, but it did not qualify because the triggered p95 remained above the unchanged H.28 ceiling.

Observed evidence:

- triggered: 20/256; committed: 20/20;
- rollback / unsafe / fallback-commit violations: 0 / 0 / 0;
- trigger average: 96,029.09 us;
- trigger p95: 96,573.5 us;
- unchanged H.28 readiness threshold: 88,381.2 us;
- estimated p95 ratio: 13.1123 versus limit 12;
- H.9 average: 74,579.555 us;
- Jacobian average: 58,854.575 us;
- 35 logical hydraulic evaluations, 32 probes, Jacobian dimension 32;
- deterministic fingerprint unchanged: `518BA948637F0C270C7F8228AB97FEF9148E29A4F5CE4376319AB5D1CFBE7F38`.

H.28.1-F is therefore frozen failed performance evidence, not a validated baseline. H.28.1-D Preflight Hotfix 1 remains the validated performance baseline.
