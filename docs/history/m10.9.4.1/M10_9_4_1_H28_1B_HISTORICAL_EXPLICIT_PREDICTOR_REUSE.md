# M10.9.4.1-H.28.1-B — Historical Explicit Predictor Reuse

## Status

CANDIDATE built directly on user-validated H.28.1-C Hotfix 2.

H.28 remains failed performance evidence. H.29 remains blocked. Standard current-v2 remains `ExplicitCommittedState` at 10 ms.

## Why this milestone exists

Validated H.28.1-C removed about 97.6% of H.9/Jacobian allocations without materially changing triggered wall time. Its attribution still measured a non-trigger predictor cost of about 9.31 ms inside an about 18.62 ms four-node orchestrator step. The historical explicit path already performs the expensive same-step fluid-node integration required for fail-closed fallback, while the sidecar historically materializes another H.4 predictor from the same committed state.

H.28.1-B removes only duplicate work that can be proven bit-exact. It does not change the trigger law.

## Exact selective-reuse design

The historical explicit path remains first and authoritative as the same-step fallback.

The orchestrator now retains:

- the committed-state hydraulic node balances and pipe/valve/pump flow maps already calculated by the historical solve;
- the historical total fluid-node balances actually applied by the explicit path;
- the historical explicit fluid-node candidate integrated from those balances.

The H.4 predictor reconstructs its canonical total balance exactly as before:

```text
canonical H.4 balance = committed hydraulic balance + frozen non-hydraulic balance
```

For each fluid node independently:

1. compare the historical applied total balance with the canonical H.4 balance using exact value equality;
2. if they are exactly equal, reuse the already-integrated historical explicit fluid-node state;
3. otherwise reintegrate that node through the unchanged H.4 predictor path.

This preserves the historical H.4 arithmetic even when the explicit path accumulated physically equivalent terms in a different floating-point order. The end-of-predictor hydraulic evaluation remains mandatory for F040.

The focused telemetry records the number of fluid nodes considered and the number safely reused, so the optimization cannot silently claim savings without actually reusing predictor state.

## Frozen numerical contract

H.28.1-B must not change:

- P060/F040 thresholds or comparison semantics;
- H.9 finite-difference Newton mathematics;
- 35 hydraulic evaluations / 32 probe evaluations per triggered H.9 correction;
- Jacobian dimension 32 in the frozen benchmark;
- H.9 residual definitions or tolerances;
- 2% / 5 K bounded previous-phase continuity;
- `steam|stop-out|header|turbine-inlet` target set;
- H.20 authority;
- H.22 commit seam;
- physical coefficients;
- 10 ms simulated fixed step;
- standard production default `ExplicitCommittedState`.

The deterministic control fingerprint must remain:

```text
518BA948637F0C270C7F8228AB97FEF9148E29A4F5CE4376319AB5D1CFBE7F38
```

## Performance contract

Frozen validated H.28.1-C evidence reports:

```text
nontrigger predictor average        9309.4457627118718 us
nontrigger predictor allocation     26308.203389830509 B
```

The focused H.28.1-B gate requires:

- exact 20/20 trigger/commit behavior over the frozen 256-step manoeuvre;
- exact 35/32 work count and Jacobian dimension 32;
- zero rollback, unsafe commit or fallback-commit violation;
- at least one historical explicit fluid-node reuse and a non-zero reuse denominator;
- the exact deterministic fingerprint above;
- non-trigger predictor average wall cost <= 80% of H.28.1-C;
- non-trigger predictor average allocation <= 85% of H.28.1-C;
- preservation of the validated H.28.1-C H.9/Jacobian allocation reduction.

These thresholds qualify duplicate predictor-work removal only. They do not make H.28 green and do not authorize H.29.

## Validation

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-historical-explicit-predictor-reuse-audit.cmd
```

## After H.28.1-B

The remaining dominant cost is expected to remain the 32 finite-difference hydraulic/thermodynamic probe evaluations. If H.28.1-B is green, the next engineering decision is CPU-focused probe evaluation work before rerunning the original H.28 performance/cost/soak gate.
