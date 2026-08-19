# M10.9.4.1-I.1 Validation Checklist

## Baseline and isolation

- [ ] candidate is built directly on user-validated H.30;
- [ ] H.30 summary/metrics are frozen and fingerprint-checked;
- [ ] Phase H closure remains `OPT-IN ONLY`;
- [ ] exact v2 remains authoritative `ExplicitCommittedState` default/rollback/reference;
- [ ] exact v3 remains qualified corrected opt-in;
- [ ] no exact-version identity is deleted or reinterpreted;
- [ ] no solver, H.9/H.20/H.22, P060/F040, hysteresis, coefficient or timestep change exists;
- [ ] under `src/`, only `ApplicationDescriptor.cs` changes as metadata.

## Ordinary gate

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
```

- [ ] build passes with warnings-as-errors;
- [ ] complete ordinary suite passes;
- [ ] descriptor identifies H.30 as validated and I.1 as current candidate;
- [ ] frozen H.30 evidence contract passes;
- [ ] exact-version inventory contains 12 unique registered versions across 9 profile IDs;
- [ ] zero exact-version profile is classified `DELETE-NOW`.

## Focused I.1 gate

```bat
scripts\run-profile-compatibility-legacy-retirement-inventory-audit.cmd
```

Require:

- [ ] every registered exact-version factory resolves without ambiguity;
- [ ] all registered runtime factories use the expected hydraulic mode;
- [ ] all registered runtime factories retain the 10 ms fixed step;
- [ ] desktop v1 and pre-synchronization v1 are compatibility-retained, not reinterpreted;
- [ ] desktop v2 is authoritative default;
- [ ] desktop v3 is qualified opt-in;
- [ ] explicit kill resolves to desktop v2 explicit;
- [ ] H.5 hybrid and H.21 shadow-integrated modes are classified audit-only retirement candidates, not production-selectable;
- [ ] no historical audit-only mode is deleted before audit consolidation.

Expected artifacts:

```text
artifacts\i1-profile-compatibility-legacy-retirement-inventory\
  00-progress.txt
  01-phase-i-profile-compatibility-legacy-retirement-inventory.summary.txt
  02-profile-compatibility-matrix.csv
  03-numerical-mode-retirement-inventory.csv
```

Required final flags:

```text
profile-compatibility-inventory-passes=True
i1-audit-passes=True
phase-i-compatibility-baseline-established=True
```

## Promotion rule

Promote I.1 only after local build, complete ordinary tests and focused I.1 audit are explicitly reported green. I.1 validation authorizes audit consolidation work; it does not itself authorize deletion of compatibility-retained exact versions or historical audit-only numerical modes.


## Hotfix 1 xUnit2031 build repair

- [ ] `dotnet build` reports zero xUnit2031 diagnostics.
- [ ] `dotnet test` passes.
- [ ] `scripts\run-profile-compatibility-legacy-retirement-inventory-audit.cmd` passes.
- [ ] No runtime/profile/selector/evidence semantics changed relative to I.1 candidate.
