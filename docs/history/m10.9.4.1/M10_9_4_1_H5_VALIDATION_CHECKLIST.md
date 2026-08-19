# M10.9.4.1-H.5 Hotfix 2 Validation Checklist

**Baseline:** H.4 VALIDATED. **Result:** H.5 Hotfix 2 VALIDATED.

## Required commands

```bat
APPLY_UPDATE.cmd
dotnet build
scripts\run-hybrid-production-integration-tests.cmd
dotnet test
```

Expected ordinary inventory after the Hotfix 2 corrections: **1,047 passed, 0 failed, 37 explicit skipped, 1,084 total**.

## Required contracts

- current-v2 desktop and sustained synchronization production definitions are `ExplicitCommittedState`;
- fixed production timestep remains 10 ms;
- H.4 `P060-F040-R015` remains available only for explicit experimental/shadow qualification;
- no wall-clock branching, coefficient retuning, hidden flow filtering or hidden fallback;
- synthetic opt-in integration uses an H.3-proven convergent corrector control set;
- ordinary desktop, host runtime, replay/protection and all historical current-v2 tests must remain green.

## Required explicit audit artifacts

`artifacts\h5-hybrid-production-integration` must contain:

- `01-current-v2-shadow-qualification-trajectory.csv`
- `01-current-v2-shadow-qualification.summary.txt`
- `02-current-v2-shadow-correction-events.csv`
- `03-current-v2-shadow-final-candidate.csv`

The summary must print `production-hybrid-active=False`, `shadow-candidates-committed=False` and the extended shadow convergence evidence. A false shadow qualification result is not an ordinary-test failure; it is a decision result that blocks later production activation.

Phase H does **not** close merely because this hotfix validates. The next numerical step depends on the shadow evidence.

## Recorded validation outcome

User-reported build, ordinary suite and focused audit all passed. The audit recorded 7/500 triggered shadow corrections, 5/7 convergent, deterministic work ratio 1.492000, observational cost ratio 1.480162 and `extended-shadow-qualification-passes=False`. Production remained explicit and stable. H.6 owns the next numerical-envelope refinement.
