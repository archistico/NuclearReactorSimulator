# M10.9.4.1-H.24 Requalification 1 Validation Checklist

## Baseline

- [x] candidate is built directly on the user-validated H.28 runtime;
- [x] H.28 green artifacts are frozen in `Application.Tests/.../Evidence` and fingerprint-checked;
- [x] H.28 is recorded as `bounded-but-costly`, not performance-free;
- [x] no production numerical source is changed by this requalification candidate;
- [x] standard current-v2 remains `ExplicitCommittedState` at 10 ms;
- [x] H.29 was blocked until this focused gate passed; the gate is now validated and H.29 is unblocked.

## Ordinary gate

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
```

- [x] build passes with warnings-as-errors;
- [x] complete ordinary suite passes;
- [x] ApplicationDescriptor identifies `H.24 Requalification 1` as the current candidate;
- [x] all four frozen H.28 evidence fingerprints pass;
- [x] H.28 summary assertions preserve the green ratios, 379/379 soak commit result and deterministic fingerprint;
- [x] existing H.24–H.28 ordinary contracts remain green.

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

- [x] all four profiles complete without trip;
- [x] each profile observes at least one P060/F040 trigger;
- [x] each profile observes at least one corrected commit;
- [x] fallback commit violations = 0;
- [x] unsafe corrected commits = 0;
- [x] untargeted branch disagreements = 0;
- [x] mass closure <= `1e-6 kg`;
- [x] energy closure <= `1e-2 J`;
- [x] balance mass-rate residual <= `1e-8 kg/s`;
- [x] balance power residual <= `1e-3 W`;
- [x] 256-interval determinism control repeats exactly;
- [x] standard current-v2 remains `ExplicitCommittedState`.

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


## Validation record — 2026-08-19

User-reported compilation, complete ordinary tests and focused gate passed. The focused artifact reported 30,008 runtime steps, 9,626/9,626 trigger/eligible/authorized/commit, zero rollback/fallback/unsafe/untargeted disagreement, all four profiles trip-free and deterministic fingerprint `7AF233CE51A866B3E00C2C032AA58EEFBD7290DE0940725E5F4B7860EA6287BE`. H.24 Requalification 1 is promoted to VALIDATED.
