# H.28.1-E failed CPU-tail evidence

H.28.1-E Hotfix 2 compiled. Its six preliminary focused contracts passed, but the explicit CPU-tail audit remained red.

Measured evidence:

- Jacobian average: 119404.5 us versus D 228904.075 us (~47.8% reduction).
- H.9 average: 135138.555 us.
- trigger average: 156670.12 us.
- trigger p95: 157754 us.
- unchanged H.28 readiness threshold: 88381.2 us.
- estimated H.28 p95 ratio: 21.4191 versus limit 12.
- 20/20 trigger/commit, 32 probes, 35 logical hydraulic evaluations, fingerprint unchanged.

Therefore E is useful failed performance evidence but not a validated baseline. H.28.1-D remains the validated performance baseline for H.28.1-F.
