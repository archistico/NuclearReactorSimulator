# M10.9.4.1-H.30 Validation Checklist

## Baseline and isolation

- [ ] candidate is built directly on user-validated H.29;
- [ ] H.29 focused artifacts are promoted as frozen evidence with canonical fingerprints;
- [ ] H.19-H.29 prerequisite fingerprints all pass;
- [ ] no H.24 or H.28 long-running gate is rerun;
- [ ] no solver, H.20/H.22, P060/F040, hysteresis, physical coefficient or fixed-step retuning is introduced;
- [ ] production policy selector implementation remains unchanged;
- [ ] exact v2 remains `ExplicitCommittedState` default/rollback/reference;
- [ ] exact v3 remains the qualified corrected opt-in path;
- [ ] candidate decision is evidence-derived `OPT-IN ONLY` because H.28 remains `bounded-but-costly`.

## Ordinary gate

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
```

- [ ] build passes with warnings-as-errors;
- [ ] complete ordinary suite passes;
- [ ] `ApplicationDescriptor` identifies H.30 closure candidate;
- [ ] frozen H.19-H.29 evidence contract passes;
- [ ] closure policy contract proves v2 default, v3 opt-in and explicit kill -> v2.

## Focused H.30 gate

```bat
scripts\run-phase-h-closure-production-qualification-decision-audit.cmd
```

Require:

- [ ] H.19 numerical qualification green;
- [ ] H.20 fail-closed authority/rollback green;
- [ ] H.21 orchestrator wiring green;
- [ ] H.22 corrected ownership green;
- [ ] H.23 replay/checkpoint/protection green;
- [ ] post-H.28 H.24 long-horizon/cross-profile green;
- [ ] H.25 protection/transient matrix green;
- [ ] H.26 integrated fallback stress green;
- [ ] H.27 off-design envelope green;
- [ ] H.28 performance/soak gate green and still `bounded-but-costly`;
- [ ] H.29 production activation candidate green;
- [ ] authoritative default resolves to exact v2 explicit;
- [ ] qualified opt-in resolves to exact v3 corrected;
- [ ] explicit kill resolves to exact v2 explicit;
- [ ] no runtime selector change is required by H.30;
- [ ] closure decision is exactly `OPT-IN ONLY`;
- [ ] Phase H closure flags are green.

Expected artifacts:

```text
artifacts\h30-phase-h-closure-production-qualification-decision\
  00-progress.txt
  01-phase-h-closure-production-qualification-decision.summary.txt
  02-phase-h-closure-decision-metrics.csv
```

Required final flags:

```text
phase-h-production-policy-decision=OPT-IN ONLY
phase-h-closure-evidence-chain-passes=True
h30-audit-passes=True
phase-h-closed=True
phase-i-unblocked=True
```

## Promotion rule

Promote H.30 only after local build, complete ordinary tests and focused H.30 audit are explicitly reported green. Promotion closes Phase H as `OPT-IN ONLY`; it does not change exact v2 into corrected ownership. Exact v3 remains available as the qualified opt-in path.
