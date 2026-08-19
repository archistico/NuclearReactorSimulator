# M10.9.4.1-H.6 Validation Checklist

## Candidate

**M10.9.4.1-H.6 — Shadow Corrector Rescue Envelope & Two-Tier Qualification**

H.5 Hotfix 2 is the validated baseline. Production current-v2 must remain explicit at 10 ms throughout this gate.

## Required commands

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-corrector-rescue-envelope-audit.cmd
```

## Ordinary-suite expectation

H.6 adds one explicit audit and no new ordinary test. Expected discovery:

```text
passed:   1047
failed:      0
skipped:    38 explicit
total:    1085
```

## Focused evidence requirements

The H.6 summary must show:

- `production-shadow-steps=500`;
- `frozen-trigger=P060-F040`;
- `triggered-events=7`;
- `H4-primary-converged=5/7`;
- `H4-primary-nonconverged=2`;
- six rescue-envelope profiles;
- a deterministic selected rescue profile;
- deterministic two-tier primary `R015-I072` plus rescue evidence;
- exact deterministic repeat;
- `production-hybrid-active=False`;
- `shadow-candidates-committed=False`;
- `trigger-retuning=False`;
- `physical-coefficient-retuning=False`;
- `hidden-flow-filtering=False`.

A value of `refined-envelope-qualification-passes=False` is a valid H.6 evidence outcome and does not fail the milestone by itself. It means the algorithmic envelope is insufficient and production must remain explicit.

## Required artifacts

```text
artifacts\h6-corrector-rescue-envelope\
    01-current-v2-corrector-envelope-sweep.csv
    01-current-v2-corrector-rescue-envelope.summary.txt
    02-current-v2-triggered-event-matrix.csv
    03-current-v2-two-tier-shadow-ladder.csv
```

## Promotion rule

Promote H.6 only when build, ordinary suite and focused audit execution are green. The numerical qualification result is then used to choose the next Phase H step:

- qualification true -> broader scenario/free-running shadow qualification;
- qualification false -> corrector-algorithm revision in shadow mode.

No H.6 outcome directly authorizes production hybrid activation.
