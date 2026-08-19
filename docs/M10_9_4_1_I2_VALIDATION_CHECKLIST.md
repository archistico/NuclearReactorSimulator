# M10.9.4.1-I.2 Validation Checklist

> **VALIDATED NOTE — 2026-08-19:** user-reported compilation, complete ordinary tests and focused I.2 audit passed. This checklist is retained as the promotion record; I.3 is the current continuation.
## Baseline and isolation

- [ ] candidate is built directly on user-validated I.1 Hotfix 1;
- [ ] H.30 remains closed as `OPT-IN ONLY`;
- [ ] exact v2 remains authoritative explicit default/rollback/reference;
- [ ] exact v3 remains qualified corrected opt-in;
- [ ] validated I.1 summary/matrix/retirement inventory are frozen and fingerprint-checked;
- [ ] no solver, selector, H.9/H.20/H.22, P060/F040, hysteresis, coefficient, persistence or timestep change exists;
- [ ] under `src/`, only `ApplicationDescriptor.cs` changes as metadata.

## Ordinary gate

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
```

- [ ] build passes with warnings-as-errors;
- [ ] complete ordinary suite passes;
- [ ] descriptor identifies validated I.1 and current I.2 candidate;
- [ ] I.1 frozen evidence fingerprint contract passes;
- [ ] audit tier manifest contract passes;
- [ ] CI workflow separation contract passes.

## Focused I.2 gate

```bat
scripts\run-phase-i-audit-consolidation-ci-baseline-audit.cmd
```

Require:

- [ ] 11 audit-contract entries are classified;
- [ ] ordinary/current-evidence work is required on every ordinary CI execution;
- [ ] gameplay-long, operational-envelope and reference-scale are scheduled/manual long gates;
- [ ] H.24 post-H.28 and H.28 remain frozen rather than rerun by CI;
- [ ] H.5 and H.21 are not current-CI dependencies;
- [ ] H.5/H.21 source dependencies are explicitly reported as remaining;
- [ ] no numerical-mode deletion is authorized by I.2;
- [ ] GitHub ordinary and scheduled-long workflows use `global.json` and the provider-neutral `eng` scripts.

Expected artifacts:

```text
artifacts\i2-phase-i-audit-consolidation-ci-baseline\
  00-progress.txt
  01-phase-i-audit-consolidation-ci-baseline.summary.txt
  02-phase-i-audit-tier-manifest.csv
  03-legacy-mode-retirement-readiness.csv
```

Required final flags:

```text
phase-i-audit-consolidation-passes=True
i2-audit-passes=True
phase-i-ci-baseline-established=True
```

## Promotion rule

Promote I.2 only after local build, complete ordinary tests and focused I.2 audit are explicitly green. I.2 validation establishes CI/audit topology; it does not authorize H.5/H.21 code deletion.
