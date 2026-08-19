# M10.9.4.1-H.24 Validation Checklist

## Baseline and scope

- [ ] candidate is built directly on user-validated **H.23 Hotfix 2**;
- [ ] H.23 is documented as validated with 701 recorded steps, 242 corrected commits, exact full replay/checkpoint continuation and reverse-power generator trip;
- [ ] H.22/H.23 numerical runtime code is unchanged by H.24;
- [ ] standard current-v2 remains `ExplicitCommittedState` at 10 ms;
- [ ] H.20 authority and H.22 commit seam are unchanged;
- [ ] P060/F040, H.9, 2% / 5 K hysteresis and `steam|stop-out|header|turbine-inlet` are unchanged;
- [ ] Phase H continuation roadmap H.24–H.30 is present and marks H.24 as duration/cross-profile qualification only.

## Ordinary gate

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
```

- [ ] build passes with warnings-as-errors;
- [ ] complete ordinary suite passes;
- [ ] ApplicationDescriptor identifies H.24 as candidate over validated H.23 Hotfix 2;
- [ ] all three frozen H.23 evidence fingerprints pass;
- [ ] existing H.22/H.23 ordinary contracts remain green;
- [ ] standard desktop factory remains explicit.

## Focused gate

```bat
scripts\run-four-node-committed-long-horizon-cross-profile-qualification-audit.cmd
```

Required profile domain:

```text
steady-long             12,000
load-pulse               6,000
cooling-pulse            6,000
combined-load-cooling    6,000
TOTAL                    30,000 qualification intervals
```

- [ ] 8 deterministic action-transition steps are observed in addition to the 30,000 qualification intervals;
- [ ] all four profiles complete without trip;
- [ ] every profile observes at least one P060/F040 trigger;
- [ ] every profile observes at least one corrected commit;
- [ ] corrected commits are always H.20 eligible and H.22 authorized;
- [ ] any H.20 rollback remains uncommitted/explicit in the same interval;
- [ ] fallback commit violations = 0;
- [ ] unsafe corrected commits = 0;
- [ ] untargeted branch disagreements = 0 across the nominal H.19 profile domain;
- [ ] each corrected commit remains evaluated/converged and within H.20 residual/closure guards;
- [ ] every step stays within H.22 network accounting limits;
- [ ] deterministic 256-interval committed control repeats exactly;
- [ ] standard current-v2 factory remains `ExplicitCommittedState`.

H.24 does **not** require the H.19 explicit-reference counts `3046/92/473` because real corrected commits change the trajectory and therefore the trigger census.

## Expected artifacts

```text
artifacts\h24-four-node-committed-long-horizon-cross-profile-qualification\
  00-progress.txt
  01-four-node-committed-long-horizon-cross-profile.summary.txt
  02-committed-long-horizon-step-telemetry.csv
  03-profile-qualification-metrics.csv
  04-four-node-committed-long-horizon-metrics.csv
```

- [ ] `four-node-committed-long-horizon-cross-profile-qualification-passes=True`;
- [ ] `h24-audit-passes=True`.

## Promotion rule

H.24 may be marked **VALIDATED** only after local build, the complete ordinary suite and the focused H.24 gate pass.

Even after promotion:

- standard current-v2 remains explicit;
- H.25 protection/transient matrix is next;
- H.26 rollback stress, H.27 off-design and H.28 performance/soak remain required;
- production-default activation cannot be considered before H.29/H.30.

## Hotfix 1 compile checkpoint

- [x] first local H.24 build failure identified as audit-only CS0103 for unresolved `ControlRoomSnapshotFingerprint`;
- [x] Hotfix 1 adds only `using NuclearReactorSimulator.Application.Scenarios.Recording;`;
- [x] Hotfix 1 build passes;
- [x] complete ordinary suite passes;
- [ ] focused H.24 gate passes.


## Validation result — 2026-08-19

- [x] focused H.24 gate passed;
- [x] `four-node-committed-long-horizon-cross-profile-qualification-passes=True`;
- [x] `h24-audit-passes=True`;
- [x] 30,008 committed runtime steps;
- [x] 9,626 corrected commits;
- [x] all four profiles trip-free;
- [x] zero rollback/fallback-commit/unsafe-commit/untargeted-disagreement violations.

**H.24 Hotfix 1 is VALIDATED.** Focused duration: **4h31m55s**.
