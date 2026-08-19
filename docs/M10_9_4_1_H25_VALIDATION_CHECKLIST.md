# M10.9.4.1-H.25 Validation Checklist

## Validated result

User validation on 2026-08-19 passed build, complete ordinary tests and focused H.25: 5 scenarios, 837 runtime steps, 178 corrected commits, zero rollback/fallback-commit/unsafe-commit violations, all expected outcomes satisfied, focused duration 5m29s.

## Preconditions

- [ ] Source is stacked directly on validated H.24 Hotfix 1.
- [ ] No numerical runtime file is changed by H.25.
- [ ] Standard current-v2 remains `ExplicitCommittedState` at 10 ms.
- [ ] H.24 is not automatically rerun.

## Commands

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-four-node-committed-protection-operational-transient-matrix-audit.cmd
```

## Focused evidence

- [ ] frozen H.24 summary/profile/overall metrics fingerprint checks pass;
- [ ] H.24 telemetry manifest retains 30,008 rows and canonical fingerprint;
- [ ] eight-function current-v2 protection catalogue contract passes;
- [ ] five H.25 scenarios complete with expected outcomes;
- [ ] normal load manoeuvre has no spurious trip;
- [ ] manual reactor scram latches;
- [ ] manual generator trip opens the breaker;
- [ ] turbine trip reaches automatic reverse-power generator trip;
- [ ] breaker-open coastdown leaves the three supervised electrical relays ineligible;
- [ ] corrected commits occur somewhere in the matrix;
- [ ] fallback commit violations = 0;
- [ ] unsafe corrected commits = 0;
- [ ] closure/ownership remain within H.22 limits;
- [ ] standard factory remains explicit;
- [ ] `four-node-committed-protection-operational-transient-matrix-passes=True`;
- [ ] `h25-audit-passes=True`.

## Interpretation

Safe rollback/fallback is not automatically a failure. H.25 does not force every physical protection threshold; existing ordinary tests remain authoritative for individual threshold/pickup/reset laws. A green H.25 advances to H.26 integrated rollback/fail-closed stress, not to default activation.
