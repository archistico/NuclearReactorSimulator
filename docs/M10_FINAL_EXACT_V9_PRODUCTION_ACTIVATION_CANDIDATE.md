# M10 Final — Exact-v9 Qualified Production Activation Candidate

**CANDIDATE — exact-v9 is engineering QUALIFIED by returned Diagnostic 11 Hotfix 2 evidence; exact-v4 remains authoritative production; exact-v9 is staged only as an explicit opt-in production policy; replacement long remains unauthorized.**

> **Returned result:** this opt-in activation-candidate gate is now locally validated GREEN. Its returned evidence authorizes only the separate authoritative activation-decision candidate documented in `M10_FINAL_EXACT_V9_PRODUCTION_ACTIVATION_DECISION.md`; this historical note remains the opt-in staging contract and is not rewritten as the authoritative switch.

## 1. Qualification result entering this gate

The returned Diagnostic 11 Hotfix 2 artifacts complete 600 simulated seconds / 60,000 deterministic 10 ms steps on `integrated-operations-desktop-stable@9` with zero trip steps and zero hydraulic rollbacks.

The returned endpoint and late-window evidence are effectively stationary:

- electrical export: `4.999999982116509 MWe` at 600 s;
- primary pump/channel/return: `100.000000974 / 100.000001357 / 100.000000320 kg/s`;
- drum level: `0.4999999996725085`;
- drum final-60 mass slope: `-1.06214247e-8 kg/s`;
- maximum node pressure slope in the returned final-60 table: below `1e-4 Pa/s`;
- governor integral slope: `+2.22552684e-11 %/s`;
- governor output / control-valve slope: about `-3.8877e-10 %/s`;
- final-60 net external / stored-energy rate: about `9.78e-8 MW` each;
- mean absolute full-energy closure residual: `1.12160695e-5 J`;
- mean absolute turbine-stage ownership residual: `3.05351664e-9 W`;
- turbine admission: `13.339237094 kg/s` total = `13.028001861 kg/s` vapor + `0.311235233 kg/s` moisture drain.

The exact-v9 operating point is therefore **QUALIFIED** for activation staging. This qualification accepts the authored operating point and the already validated governor/moisture-drain semantics; it does not itself switch production.

## 2. Why this gate does not switch the default

The first failed long campaign exposed two structural defects and one operating-point mismatch. Those are now repaired and exact-v9 is qualified, but deployment selection is a separate contract involving:

- authoritative/default policy resolution;
- exact-version registry availability in the desktop app composition root;
- scenario/training identity;
- fail-closed explicit rollback;
- deterministic equivalence between direct factory construction and policy-path construction;
- preservation of current exact-v4 evidence until the activation decision is explicitly promoted.

Following the existing H.29 -> H.30 activation pattern, this milestone therefore adds an **opt-in qualified policy** but leaves `AuthoritativeDefaultPolicy` on exact-v4.

## 3. New staged policy

The candidate adds:

`DesktopHydraulicProductionPolicy.M10FinalExactV9QualifiedCandidate`

resolved by:

`DesktopHydraulicProductionPolicySelector.M10FinalQualifiedCandidatePolicy`

Its exact initial condition is:

`integrated-operations-desktop-stable@9`

Its replayable training scenario identity is:

`integrated-normal-operations-training-m10-final-v9-activation-candidate`

The desktop composition root registers the exact-v9 factory so the identity is resolvable by scenario/archive workflows.

The authoritative default remains:

`integrated-operations-desktop-stable@4 | I5RepairedFourNodeCorrectedCommit`

The explicit kill/rollback remains:

`integrated-operations-desktop-stable@2 | ExplicitCommittedState`

## 4. Candidate gate

Run:

```bat
scripts\run-m10-final-v9-production-activation-candidate.cmd
```

The script performs, in order:

1. restore + Debug build with warnings-as-errors;
2. complete ordinary suite;
3. LR-M1 Hotfix 1 semantic-equivalence regression;
4. current-evidence suite with exact-v4 still authoritative;
5. exact-v9 600 s Diagnostic-11 requalification on the activation-candidate source tree;
6. exact-v9 policy-path activation-candidate audit.

The final focused audit runs 120 simulated seconds through the production-policy selector, requires no trip/breaker-open/rollback/fallback/unsafe/untargeted-disagreement observations, keeps the existing conservation ceilings, verifies the moisture-drain owner, and requires the selector-path deterministic fingerprint to equal direct exact-v9 factory construction.

The frozen activation-candidate contract is `eng/m10-final-v9-production-activation-candidate-contract.json`.

## 5. Required returned artifacts

Return the complete:

`artifacts\m10-final-v9-production-activation-candidate`

containing:

- `00-progress.txt`;
- `01-v9-production-activation-candidate.summary.txt`;
- `02-selector-matrix.csv`;
- `03-activation-candidate-contract.json`.

The Diagnostic-11 artifact folder produced by step 4 is also retained locally as prerequisite evidence.

## 6. Decision after the gate

A green candidate gate authorizes **only** a separate production-activation decision candidate that changes the authoritative default from exact-v4 to exact-v9 and deliberately rebinds current production scenario/mission identities where required.

It does not authorize the replacement long directly. The replacement-long contract and workload are created only after the authoritative exact-v9 activation itself is validated.
