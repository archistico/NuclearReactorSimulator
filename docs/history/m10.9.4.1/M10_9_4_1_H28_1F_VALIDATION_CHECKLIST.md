# H.28.1-F validation checklist

1. `APPLY_UPDATE.cmd`
2. `dotnet build`
3. `dotnet test`
4. `scripts\run-jacobian-probe-coordinate-residual-audit.cmd`

Required focused evidence:

- 20 trigger / 20 corrected commit.
- 0 rollback / unsafe commit / fallback-commit violation.
- 32 probe evaluations and Jacobian dimension 32.
- 35 logical hydraulic evaluations.
- probe mapped fluid-node integrations = 0 in the optimized Jacobian path.
- triggered p95 <= 88.3812 ms.
- deterministic fingerprint exactly `518BA948637F0C270C7F8228AB97FEF9148E29A4F5CE4376319AB5D1CFBE7F38`.
- default current-v2 remains `ExplicitCommittedState`.

Do not promote H.28.1-F from candidate without all four local gates.
