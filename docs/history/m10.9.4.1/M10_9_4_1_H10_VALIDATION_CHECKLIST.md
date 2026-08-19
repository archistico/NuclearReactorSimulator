# M10.9.4.1-H.10 Validation Checklist

**Validation result:** PASSED as H.10 Hotfix 1 — build, ordinary tests and focused audit user-validated.

## Build and ordinary suite

```bat
dotnet build
dotnet test
```

Both commands must complete without errors. Warnings remain errors under the repository contract.

## Focused H.10 gate

```bat
scripts\run-hydraulic-map-switching-nonsmoothness-audit.cmd
```

Expected artifact directory:

```text
artifacts\h10-hydraulic-map-switching-nonsmoothness
```

Expected files:

1. `01-current-v2-hydraulic-map-switching-nonsmoothness.summary.txt`
2. `02-persistent-event-overview.csv`
3. `03-hydraulic-path-local-probes.csv`
4. `04-thermodynamic-node-local-probes.csv`

## Required frozen-baseline evidence

The focused audit must reproduce:

- 500 production-shadow intervals;
- frozen trigger `P060/F040`;
- 7 triggered events;
- H.4 primary 5/7;
- H.6 rescue 6/7;
- H.7 5/7;
- H.8 5/7;
- H.9 5/7;
- exactly two persistent H.9 failures.

## Required H.10 safety evidence

- `deterministic-repeat=True`;
- `switching-nonsmoothness-diagnostic-passes=True`;
- `production-hybrid-active=False`;
- `production-fixed-step=10.000 ms`;
- `shadow-candidates-committed=False`;
- `H9-corrector-replaced=False`;
- `plant-network-orchestrator-routing-changed=False`;
- `trigger-retuning=False`;
- `physical-coefficient-retuning=False`;
- `hidden-flow-filtering=False`.

`switching-evidence-found` and `non-smooth-evidence-found` are decision outputs rather than forced pass criteria. Their values determine the next diagnostic direction.
