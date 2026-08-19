# H.28.1-G validation checklist

Run from the repository root:

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-untargeted-disagreement-scan-fast-path-audit.cmd
```

Required focused evidence:

- 20/20 trigger and corrected commit;
- 0 rollback, unsafe commit and fallback-commit violations;
- 35 logical hydraulic evaluations per triggered step;
- 32 finite-difference probes;
- Jacobian dimension 32;
- no untargeted branch disagreement;
- triggered p95 <= 88.3812 ms;
- no material H.9/Jacobian regression from frozen F evidence;
- deterministic fingerprint exactly `518BA948637F0C270C7F8228AB97FEF9148E29A4F5CE4376319AB5D1CFBE7F38`;
- standard current-v2 mode remains `ExplicitCommittedState`.

Do not promote H.28.1-G until build, ordinary `dotnet test`, and the focused gate are explicitly reported green.
