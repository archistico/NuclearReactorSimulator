# M10.9.4.1-H.7 Validation Checklist

## Candidate

**M10.9.4.1-H.7 — Corrector Algorithm Revision**

H.6 is the validated baseline. Production current-v2 must remain explicit at 10 ms throughout this gate.

## Required commands

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-corrector-algorithm-revision-audit.cmd
```

## Ordinary-suite expectation

H.7 adds six ordinary Simulation tests and one explicit Application audit. Based on the validated H.6 discovery count, expected ordinary discovery is:

```text
passed:   1053
failed:      0
skipped:    39 explicit
total:    1092
```

Treat the actual local discovery count as authoritative if the test runner version reports explicit tests differently; any ordinary failure is blocking.

## Focused evidence requirements

The H.7 summary must preserve the frozen H.6 evidence:

- `production-shadow-steps=500`;
- `frozen-trigger=P060-F040`;
- `triggered-events=7`;
- `H4-primary-converged=5/7`;
- `H6-selected-rescue=R0125-I096`;
- `H6-selected-rescue-converged=6/7`.

It must also report the revised algorithm contract:

- `H7-algorithm=residual-fixed-point+deterministic-backtracking`;
- fixed pressure and flow tolerances;
- maximum iterations, initial relaxation, backtracking factor and minimum relaxation;
- converged-event count;
- line-search-exhausted count;
- maximum true fixed-point pressure/flow residuals;
- normalized merit residual;
- minimum accepted relaxation;
- deterministic hydraulic-evaluation work ratio;
- `accepted-merit-strictly-decreases=True`;
- `deterministic-repeat=True`;
- inventory/conservation/ownership residuals;
- `production-hybrid-active=False`;
- `shadow-candidates-committed=False`;
- `historical-picard-replaced=False`;
- `plant-network-orchestrator-routing-changed=False`;
- `trigger-retuning=False`;
- `physical-coefficient-retuning=False`;
- `hidden-flow-filtering=False`.

A value of `corrector-algorithm-revision-qualification-passes=False` is a valid H.7 evidence outcome and does not fail the milestone by itself. It blocks broader shadow qualification and requires further nonlinear-solver work.

## Required artifacts

```text
artifacts\h7-corrector-algorithm-revision\
    01-current-v2-corrector-algorithm-revision.summary.txt
    02-current-v2-triggered-event-algorithm-comparison.csv
    03-current-v2-residual-backtracking-trace.csv
    04-current-v2-revised-candidate-gaps.csv
```

## Promotion rule

Promote H.7 when build, ordinary suite and focused audit execution are green. The numerical qualification result then selects the next Phase H step:

- qualification true -> broader free-running/scenario shadow qualification, still with explicit production;
- qualification false -> continue corrector algorithm development in shadow mode.

No H.7 outcome directly authorizes production hybrid activation.
