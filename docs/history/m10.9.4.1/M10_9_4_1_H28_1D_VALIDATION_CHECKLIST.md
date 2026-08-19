# M10.9.4.1-H.28.1-D validation checklist

## Required local gate

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-hydraulic-probe-cpu-hot-path-optimization-audit.cmd
```

## Required focused evidence

- frozen H.28.1-B evidence fingerprints pass;
- 20/20 triggers and commits;
- 0 rollback, unsafe commit or fallback-commit violation;
- 35 hydraulic evaluations / 32 probes / dimension 32 remain unchanged;
- applied probe exact-reuse fraction >= 0.80;
- Jacobian wall <= 0.85 x H.28.1-B;
- H.9 wall <= 0.87 x H.28.1-B;
- trigger engine wall <= 0.90 x H.28.1-B;
- H.28.1-C/B allocation gains preserved;
- non-trigger predictor reuse not materially regressed;
- deterministic fingerprint exactly `518BA948637F0C270C7F8228AB97FEF9148E29A4F5CE4376319AB5D1CFBE7F38`;
- standard current-v2 remains `ExplicitCommittedState`.

## Expected artifacts

```text
artifacts\h28-1d-hydraulic-probe-cpu-hot-path-optimization\
  00-progress.txt
  01-hydraulic-probe-cpu-hot-path-optimization.summary.txt
  02-hydraulic-probe-cpu-hot-path-optimization-steps.csv
  03-hydraulic-probe-cpu-hot-path-optimization-cost-centers.csv
  04-hydraulic-probe-cpu-hot-path-optimization-metrics.csv
```

H.28.1-D does not validate H.28 automatically. H.28 must be rerun separately after this candidate is validated.
