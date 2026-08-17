# M10.9.4.1-H.20 Validation Checklist

## Authoritative baseline

- [ ] Candidate is built only on user-validated **H.19**.
- [ ] H.19 documentation is promoted to VALIDATED with the reported 473/473 result.
- [ ] frozen H.19 representative, metrics and summary evidence are present.
- [ ] canonical SHA-256 fingerprints of all three frozen H.19 evidence files match the user-validated artifacts.
- [ ] production remains `ExplicitCommittedState` at 10 ms.
- [ ] `PlantNetworkOrchestrator` production routing is unchanged.
- [ ] H.9, P060/F040, 2% / 5 K hysteresis limits, target nodes and physical coefficients are unchanged.

## Ordinary gates

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
```

- [ ] build passes.
- [ ] ordinary suite passes.
- [ ] H.19 frozen-evidence regression passes.
- [ ] default activation options remain arm-disabled.
- [ ] unit tests prove disabled, untriggered, eligible and all fail-closed rollback branches.
- [ ] `ApplicationDescriptorTests` describes H.20 and H.19 as validated.

## Focused gate

```bat
scripts\run-four-node-activation-rollback-contract-audit.cmd
```

Required evidence:

- [ ] frozen H.19 evidence accepted.
- [ ] frozen representatives = 473.
- [ ] default arm disabled.
- [ ] default explicit decisions = 473/473.
- [ ] default candidate eligible = 0/473.
- [ ] shadow-arm simulation candidate eligible = 473/473.
- [ ] shadow-arm qualification-set rollbacks = 0.
- [ ] production commit authorized = 0.
- [ ] rollback challenges = 8.
- [ ] rollback challenges pass = 8/8.
- [ ] every rollback proposes `ExplicitCommittedState`.
- [ ] every rollback emits the expected typed reason.
- [ ] untriggered observation remains explicit without rollback.
- [ ] exact decision fingerprint repeat passes.
- [ ] desktop current-v2 definition remains `ExplicitCommittedState`.
- [ ] `activation-contract-passes=True`.
- [ ] `h20-audit-passes=True`.

## Expected artifacts

```text
artifacts\h20-four-node-activation-rollback-contract\
  00-progress.txt
  01-four-node-activation-rollback-contract.summary.txt
  02-qualified-representative-authority-decisions.csv
  03-rollback-challenges.csv
  04-four-node-activation-contract-metrics.csv
```

A green H.20 gate qualifies only the authority/rollback/telemetry contract. It does not authorize production activation.
