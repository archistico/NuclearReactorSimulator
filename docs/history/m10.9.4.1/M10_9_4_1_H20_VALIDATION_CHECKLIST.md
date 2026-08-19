# M10.9.4.1-H.20 Validation Checklist

## Authoritative baseline

- [x] Candidate is built only on user-validated **H.19**.
- [x] H.19 documentation is promoted to VALIDATED with the reported 473/473 result.
- [x] frozen H.19 representative, metrics and summary evidence are present.
- [x] canonical SHA-256 fingerprints of all three frozen H.19 evidence files match the user-validated artifacts.
- [x] production remains `ExplicitCommittedState` at 10 ms.
- [x] `PlantNetworkOrchestrator` production routing is unchanged.
- [x] H.9, P060/F040, 2% / 5 K hysteresis limits, target nodes and physical coefficients are unchanged.

## Ordinary gates

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
```

- [x] build passes.
- [x] ordinary suite passes.
- [x] H.19 frozen-evidence regression passes.
- [x] default activation options remain arm-disabled.
- [x] unit tests prove disabled, untriggered, eligible and all fail-closed rollback branches.
- [x] `ApplicationDescriptorTests` describes H.20 and H.19 as validated.

## Focused gate

```bat
scripts\run-four-node-activation-rollback-contract-audit.cmd
```

Required evidence:

- [x] frozen H.19 evidence accepted.
- [x] frozen representatives = 473.
- [x] default arm disabled.
- [x] default explicit decisions = 473/473.
- [x] default candidate eligible = 0/473.
- [x] shadow-arm simulation candidate eligible = 473/473.
- [x] shadow-arm qualification-set rollbacks = 0.
- [x] production commit authorized = 0.
- [x] rollback challenges = 8.
- [x] rollback challenges pass = 8/8.
- [x] every rollback proposes `ExplicitCommittedState`.
- [x] every rollback emits the expected typed reason.
- [x] untriggered observation remains explicit without rollback.
- [x] exact decision fingerprint repeat passes.
- [x] desktop current-v2 definition remains `ExplicitCommittedState`.
- [x] `activation-contract-passes=True`.
- [x] `h20-audit-passes=True`.

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


## Validation result

**PASSED — 2026-08-17.** User-reported local build and complete ordinary suite passed. Focused H.20 audit: 473/473 default explicit, 473/473 armed shadow eligibility, 0 production commits, 8/8 rollback challenges, deterministic repeat, `activation-contract-passes=True`, `h20-audit-passes=True`. H.20 is the validated baseline for H.21.
