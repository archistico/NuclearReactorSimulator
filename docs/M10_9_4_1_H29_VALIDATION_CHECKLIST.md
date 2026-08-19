# M10.9.4.1-H.29 Validation Checklist

## Baseline and isolation

- [ ] candidate is built directly on user-validated H.24 Requalification 1 post-H.28;
- [ ] frozen H.23, post-H.28 H.24, H.25, H.26, H.27 and H.28 evidence fingerprints pass;
- [ ] v2 `integrated-operations-desktop-stable` remains `ExplicitCommittedState`;
- [ ] v3 is a separate exact-version H.29 corrected candidate;
- [ ] standard integrated-operations scenario remains pinned to v2;
- [ ] H.28 remains classified `bounded-but-costly`;
- [ ] no H.9/P060-F040/hysteresis/physical coefficient/fixed-step retuning is introduced;
- [ ] H.30 remains the sole authority for final default activation.

## Ordinary gate

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
```

- [ ] build passes with warnings-as-errors;
- [ ] complete ordinary suite passes;
- [ ] `ApplicationDescriptor` identifies H.29 as the current candidate;
- [ ] deployment-policy ordinary tests pass;
- [ ] v2/v3 exact-version registry tests pass;
- [ ] explicit kill resolves a corrected request to v2;
- [ ] internal telemetry-counter tests pass for a qualified commit and all typed H.20 rollback reasons;
- [ ] `ControlRoomSnapshot` exposes none of the H.29 internal numerical telemetry.

## Focused H.29 gate

```bat
scripts\run-four-node-production-activation-candidate-audit.cmd
```

The gate must verify, without rerunning H.24/H.28:

- [ ] frozen prerequisite evidence is fingerprint-identical;
- [ ] default policy is v2 explicit;
- [ ] H.29 candidate policy is exact v3 corrected;
- [ ] explicit kill of the candidate resolves to v2 explicit;
- [ ] both explicit default and kill runtime use the 10 ms step;
- [ ] bounded candidate run observes P060/F040 triggers;
- [ ] every triggered candidate in the qualification run is H.20 eligible;
- [ ] every eligible candidate is H.22 commit-authorized;
- [ ] every authorized candidate is committed;
- [ ] rollback = 0 in the nominal candidate qualification run;
- [ ] explicit fallback = 0 in the nominal candidate qualification run;
- [ ] fallback-commit violations = 0;
- [ ] unsafe commits = 0;
- [ ] untargeted branch disagreements = 0;
- [ ] fail-closed safety assertions hold for every sampled runtime row;
- [ ] 256-interval deterministic candidate control repeats exactly;
- [ ] exact v3 recording/full replay/checkpoint/seek are equivalent;
- [ ] replay preserves the v3 initial-condition identity;
- [ ] v2 remains independently resolvable as the explicit rollback/reference;
- [ ] operator snapshot remains free of internal H.29 numerical diagnostics.

Expected artifacts:

```text
artifacts\h29-four-node-production-activation-candidate\
  00-progress.txt
  01-four-node-production-activation-candidate.summary.txt
  02-production-activation-candidate-step-telemetry.csv
  03-production-activation-candidate-metrics.csv
```

Required final flags:

```text
four-node-production-activation-candidate-passes=True
h29-audit-passes=True
h30-closure-review-unblocked=True
```

## Promotion rule

Promote **H.29** only after build, complete ordinary tests and the focused H.29 audit are explicitly reported green. Promotion makes H.30 the next milestone. It does **not** make v3 the authoritative production default; v2 explicit remains authoritative until H.30 records the closure decision.
