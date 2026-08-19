# M10.9.4.1-H.24 Requalification 1 Validation Checklist

## Baseline

- [ ] candidate is built directly on the user-validated H.28 runtime;
- [ ] H.28 green artifacts are frozen in `Application.Tests/.../Evidence` and fingerprint-checked;
- [ ] H.28 is recorded as `bounded-but-costly`, not performance-free;
- [ ] no production numerical source is changed by this requalification candidate;
- [ ] standard current-v2 remains `ExplicitCommittedState` at 10 ms;
- [ ] H.29 remains blocked until this focused gate passes.

## Ordinary gate

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
```

- [ ] build passes with warnings-as-errors;
- [ ] complete ordinary suite passes;
- [ ] ApplicationDescriptor identifies `H.24 Requalification 1` as the current candidate;
- [ ] all four frozen H.28 evidence fingerprints pass;
- [ ] H.28 summary assertions preserve the green ratios, 379/379 soak commit result and deterministic fingerprint;
- [ ] existing H.24–H.28 ordinary contracts remain green.

## Focused gate

```bat
scripts\run-four-node-post-h28-committed-long-horizon-requalification-audit.cmd
```

Required domain:

```text
steady-long             12,000
load-pulse               6,000
cooling-pulse            6,000
combined-load-cooling    6,000
TOTAL                    30,000 qualification intervals
+ 8 action-transition steps
```

- [ ] all four profiles complete without trip;
- [ ] each profile observes at least one P060/F040 trigger;
- [ ] each profile observes at least one corrected commit;
- [ ] fallback commit violations = 0;
- [ ] unsafe corrected commits = 0;
- [ ] untargeted branch disagreements = 0;
- [ ] mass closure <= `1e-6 kg`;
- [ ] energy closure <= `1e-2 J`;
- [ ] balance mass-rate residual <= `1e-8 kg/s`;
- [ ] balance power residual <= `1e-3 W`;
- [ ] 256-interval determinism control repeats exactly;
- [ ] standard current-v2 remains `ExplicitCommittedState`.

Expected artifacts:

```text
artifacts\h24-post-h28-four-node-committed-long-horizon-cross-profile-requalification\
  00-progress.txt
  01-post-h28-four-node-committed-long-horizon-cross-profile-requalification.summary.txt
  02-post-h28-committed-long-horizon-step-telemetry.csv
  03-post-h28-profile-qualification-metrics.csv
  04-post-h28-four-node-committed-long-horizon-requalification-metrics.csv
```

Required final flags:

```text
post-h28-four-node-committed-long-horizon-cross-profile-requalification-passes=True
h24-post-h28-requalification-audit-passes=True
```

## Promotion rule

Promote **H.24 Requalification 1** only after build, full ordinary tests and the focused gate are explicitly reported green. After promotion, H.29 becomes the next candidate milestone; default production remains explicit until H.29/H.30 decide otherwise.
