# M10 Final — Exact-v9 Authoritative Production Activation Decision 1

**CANDIDATE — the preceding exact-v9 opt-in production-policy gate is validated green. This candidate deliberately switches the desktop authoritative default to exact-v9 and advances the current production mission binding to `bounded-demand-following-5-10-5@3`, while preserving exact-v4/exact-v3/exact-v2 and mission-pack `@2`/`@1` as immutable historical identities. Replacement long remains unauthorized.**

> **Hotfix 1 status:** the original candidate was BUILD RED before the ordinary suite because its new focused test declared the canonical nullable decimal mission score as `double` and omitted the `Application.Scenarios.Recording` import required by `ControlRoomSnapshotFingerprint`. Hotfix 1 fixes only those two test-contract compile defects. All activation-decision runtime/source semantics documented below are unchanged.

## 1. Prerequisite evidence

Diagnostic 11 Hotfix 2 qualified exact-v9 over 600 simulated seconds with effectively stationary whole-cycle behavior around 5 MWe and 100 kg/s, negligible inventory/governor drift, zero trip/rollback and conservative mass/energy ownership.

The returned exact-v9 qualified opt-in production-activation gate then validated the real selector path for 12,000 steps:

- electrical range `4.9999999795104797..4.999999999572232 MWe`;
- primary-pump range `99.999999968963579..100.00000021068621 kg/s`;
- drum-level range `0.49999999993197591..0.50000000007990097`;
- governor-output range `29.281329614794107..29.281329977118531 %`;
- minimum moisture drain `0.31123523307475764 kg/s`;
- maximum commanded-transfer mismatch `0 kg/s`;
- maximum stage energy-ownership residual `1.1175870895385742e-8 W`;
- maximum network mass closure `2.1827872842550278e-11 kg`;
- maximum network energy closure `4.5662359334528412e-5 J`;
- zero corrected trigger/commit, rollback, fallback violation, unsafe commit and untargeted disagreement;
- selector equals direct factory over 128 deterministic steps;
- fingerprint `7880AD580179B936C584EB0055BE663E0A1CFA65C5191B0DB8A7F3C514DB5418`.

That gate explicitly returned `production-activation=False`; its purpose was to prove that the deployment path is ready for this separate decision.

## 2. Proposed authoritative switch

Within this candidate source tree:

`DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy`

resolves:

`M10FinalExactV9QualifiedCandidate -> integrated-operations-desktop-stable@9`.

A distinct authoritative production scenario is introduced:

`integrated-normal-operations-training-m10-final-v9-production`.

The earlier activation-candidate scenario:

`integrated-normal-operations-training-m10-final-v9-activation-candidate`

is retained separately and is not reinterpreted.

Historical deployment identities remain explicitly selectable:

- exact-v4 — `I5RepairedProductionPolicy`;
- exact-v3 — `H29ActivationCandidatePolicy`;
- exact-v2 — `ExplicitRollbackPolicy` / fail-closed kill.

No exact-version factory, governor/moisture-drain physics, operating-point constant or first-long frozen manifest is modified by the activation decision.

## 3. Production mission rebinding

The historical production pack remains:

`bounded-demand-following-5-10-5@2 -> historical exact-v4 production scenario`.

The new current production pack is:

`bounded-demand-following-5-10-5@3 -> exact-v9 authoritative production scenario`.

Version `@3` changes only the exact composed scenario binding. Objective, external demand profile, scoring policy, evaluator, logical-time contract, assistance contract and score-evidence bindings are inherited unchanged from `@2`.

Historical M10.9.8 and failed-long tests that intentionally describe exact-v4 evidence are pinned explicitly to pack `@2`; they no longer follow the symbolic current production pack.

## 4. Historical evidence preservation

The switch is intentionally accompanied by test/evidence pinning. Tests whose purpose is Phase-I exact-v4, H.29/H.30 exact-v3, or the first failed exact-v4 long now name those exact policies rather than `AuthoritativeDefaultPolicy`.

This prevents a current-default change from silently converting historical validation into exact-v9 evidence.

The frozen first-long source manifest remains provenance only. It must not be reused as the replacement-long baseline because the authoritative source tree has intentionally changed.

## 5. Validation gate

Run:

```bat
scripts\run-m10-final-v9-production-activation-decision.cmd
```

The script performs:

1. restore + Debug build with warnings-as-errors;
2. complete ordinary suite after the proposed switch;
3. LR-M1 Hotfix 1 semantic-equivalence regression;
4. exact-v9 600 s Diagnostic-11 requalification on the switched source tree;
5. focused authoritative exact-v9 selector/scenario/mission-v3 audit;
6. post-switch cumulative current-evidence routing.

The focused audit additionally executes 12,000 authoritative health steps, checks exact-v2 fail-closed rollback, compares selector construction with the qualified direct exact-v9 fingerprint, and runs 1,200 logical steps of the current production mission `@3`.

The frozen gate contract is:

`eng/m10-final-v9-production-activation-decision-contract.json`.

## 6. Required returned artifacts

Return the complete:

`artifacts\m10-final-v9-production-activation-decision`

containing:

- `00-progress.txt`;
- `01-v9-production-activation-decision.summary.txt`;
- `02-selector-matrix.csv`;
- `03-mission-pack-matrix.csv`;
- `04-activation-decision-contract.json`.

## 7. Decision after this gate

Only a complete green result promotes exact-v9 from qualified opt-in to authoritative production.

Even after that promotion, the replacement long is a separate evidence campaign. The next candidate must freeze a **new exact-v9 production baseline manifest** and a redesigned replacement-long contract/workload; the failed exact-v4 long manifest is not reused or rewritten.
