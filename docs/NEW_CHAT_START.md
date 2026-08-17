# Nuclear Reactor Simulator — authoritative new-chat start

## Current checkpoint

- **Validated baseline:** `M10.9.4.1-H.19 — Four-Node Long-Horizon & Cross-Profile Qualification`.
- **Working candidate:** `M10.9.4.1-H.20 — Four-Node Activation Contract, Rollback & Shadow Telemetry`.
- **Production numerical path:** current-v2 remains `ExplicitCommittedState` at **10 ms**.
- **Phase G:** complete.
- **Phase H:** open; H.20 defines authority only and does not activate the four-node policy.
- **Phase I:** deferred until Phase H closes.

## What H.19 proved

H.19 passed local compilation, complete ordinary tests and the focused audit on 2026-08-17.

Validated evidence:

- four profiles and 30,000 production-shadow intervals;
- P060/F040 census = 3,046 triggers;
- 92 trigger episodes;
- exact same 473 frozen representative keys as H.17;
- target = `steam|stop-out|header|turbine-inlet`;
- 473/473 converged;
- zero line-search exhaustion;
- 245/245 H.17 failures recovered;
- 228/228 H.17 successes preserved;
- 120/120 mismatch and 125/125 non-mismatch failures recovered;
- 32,829 branch overrides and 127,600 previous-phase holds;
- deterministic work ratio 1.547433;
- exact deterministic repeat;
- 120,000 committed phase-state checks;
- 24,346 committed selection observations and zero overrides;
- 3,992 committed target phase transitions;
- 5,676 inverse sample/node scans;
- no untargeted late-shadow node;
- no untargeted phase mismatch node;
- release challenges 4/4;
- maximum closure/ownership `0 / 0.000000239`;
- `four-node-long-horizon-cross-profile-shadow-qualification-passes=True`;
- `h19-audit-passes=True`.

Production remained explicit and no shadow candidate was committed.

## What H.20 does

H.20 freezes the validated H.19 representative and metric evidence and defines a shadow-only fail-closed activation supervisor.

Default authority remains explicit because the activation arm is disabled.

When the arm is enabled **only inside the H.20 shadow test**, a triggered corrected candidate is eligible only if:

- H.19 qualification evidence is accepted;
- corrector converged;
- line search did not exhaust;
- pressure residual <= `1e-5`;
- flow residual <= `1e-2 kg/s`;
- mass closure <= `1e-8 kg/s`;
- energy ownership residual <= `1e-3 W`;
- no untargeted branch disagreement exists.

Any failed guard proposes immediate `ExplicitCommittedState` with a typed rollback reason.

H.20 introduces no persistent activation state and no production commit path. `ProductionCommitAuthorized` is always false and the new supervisor is not wired into `PlantNetworkOrchestrator`.

## H.20 validation

Run from repository root:

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-four-node-activation-rollback-contract-audit.cmd
```

Expected focused artifacts:

```text
artifacts\h20-four-node-activation-rollback-contract\
  00-progress.txt
  01-four-node-activation-rollback-contract.summary.txt
  02-qualified-representative-authority-decisions.csv
  03-rollback-challenges.csv
  04-four-node-activation-contract-metrics.csv
```

Expected positive contract result:

- frozen H.19 evidence accepted;
- default arm disabled and 473/473 explicit decisions;
- shadow-arm simulation gives 473/473 candidate-eligible decisions but zero production commit authorization;
- rollback challenges 8/8;
- untriggered remains explicit;
- exact deterministic repeat;
- desktop current-v2 remains explicit;
- `activation-contract-passes=True`;
- `h20-audit-passes=True`.

## Interpretation after H.20

If H.20 is green, only the **authority/rollback/telemetry contract** is qualified. A later milestone may prepare a separately reviewed opt-in integration candidate, but standard current-v2 must remain explicit until that production integration passes ordinary, replay, protection, long-running, off-design and full H.19 regression gates.

If H.20 fails, production remains explicit and the authority contract itself is the next repair target.

## Hard constraints

Do not activate the four-node policy in standard production, alter `PlantNetworkOrchestrator`, retune P060/F040/H.9/2%/5 K, change physical coefficients/timestep, broaden the target set, hide failures with filtering/clamping or commit shadow candidates during H.20.

Read `docs/PROJECT_HANDOFF.md` and `docs/M10_9_4_1_H20_FOUR_NODE_ACTIVATION_ROLLBACK_SHADOW_TELEMETRY_CONTRACT.md` before changing code.

## Package-time authority

H.19 is already user-validated and is the authoritative baseline encoded in this package. H.20 is only a candidate until the user reports the local H.20 gate result.
