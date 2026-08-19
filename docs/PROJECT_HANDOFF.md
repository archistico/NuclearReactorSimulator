# Nuclear Reactor Simulator — Project Handoff

> **Authoritative validated baseline:** M10.9.4.1-I.2 — Audit Consolidation & CI Baseline Hardening — VALIDATED on 2026-08-19.
>
> **Phase H:** CLOSED as `OPT-IN ONLY`.
>
> **Current candidate:** M10.9.4.1-I.3 Hotfix 4 Classifier Fix 1 — Targeted-Train Reverse-Flow Classification. I.3 remains unvalidated. The completed 10 ms comparison found 338/338 exact-v2 generation drops coincident with reverse flow on the targeted stop/control/admission train (8 stop, 0 control, 330 admission), versus 0 drops and 0 targeted reverse-flow steps in exact v3.

## 1. Authoritative production policy

```text
exact v2 integrated-operations-desktop-stable
  ExplicitCommittedState
  authoritative default / rollback / reference

exact v3 integrated-operations-desktop-stable
  FourNodeBranchContinuityCorrectedCommitOptIn
  qualified opt-in

explicit deployment kill
  exact v2 ExplicitCommittedState
```

H.28 remains `bounded-but-costly`; Phase I does not reopen the Phase-H activation decision.

## 2. Validated Phase-I baselines

### I.1 Hotfix 1 — compatibility

- 12 registered exact versions across 9 IDs;
- two older exact identities compatibility-retained;
- zero delete-now profile versions;
- H.5 `DeterministicHybridSemiImplicit` and H.21 `FourNodeBranchContinuityShadowIntegrated` classified historical audit-only retirement candidates.

### I.2 — audit/CI topology

I.2 passed build, ordinary tests and focused audit. It established four validation tiers:

```text
ORDINARY
CURRENT-EVIDENCE
SCHEDULED-LONG
HISTORICAL-FROZEN
```

Provider-neutral commands remain:

```text
eng\ci-ordinary.cmd
eng\ci-current-evidence.cmd
eng\ci-long.cmd
```

H.24 post-H.28, H.28 and H.5/H.21 historical research are not ordinary/current-CI reruns. Historical evidence is preserved. H.5/H.21 still have source-level executable dependencies, so `legacy-mode-retirement-authorized=False` remains authoritative.


## 3. I.3 red diagnostic now established

The exact-v2 300 s run completed with 295/300 healthy one-second operating samples and five isolated shaft-floor violations at 55, 66, 72, 79 and 88 s. Each violation showed canonical turbine shaft power = 0 MW, turbine stage flow = 0 kg/s, admission flow approximately -26 to -27 kg/s and a turbine-inlet pressure spike, while the phase remained `SuperheatedVapor`, no trip occurred and conservation remained green.

Hotfix 4 does not repair or retune this behavior. It freezes the red evidence and performs a 100 s v2/v3 comparison at 10 ms resolution. If the issue is explicit-only and the corrected candidate remains free of drops/rollback/fallback, H.30 must be explicitly reviewed before I.3 budgets are frozen.

## 4. I.3 purpose

I.3 creates the quantitative Phase-I reference baseline without changing runtime behavior.

Exact contract:

```text
trajectory:          phase-i-desktop-v2-healthy-300s-v1
initial condition:   integrated-operations-desktop-stable@2
policy:              ExplicitCommittedState
fixed step:          10 ms
runtime:             30,000 logical steps = 300 simulated seconds
sampling:            every 100 steps = 1 second
final slope window:  60 seconds
```

Every sample records generation health, presentation fingerprint, conservation closure, total fluid mass/internal energy and key conserved inventories (`exhaust`, `hotwell`, `feedwater-inventory`, drum and main-steam header). I.3 derives final-window slopes and first-generation regression tolerance budgets only after independent no-trip/generation/conservation gates pass.

Generated budgets are **internal regression evidence**, not historical plant calibration and not targets that authorize tuning physics or seed values.

## 5. I.3 gate

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-phase-i-reference-trajectory-conservation-inventory-baseline-audit.cmd
```

Required final flags:

```text
phase-i-reference-trajectory-baseline-passes=True
phase-i-conservation-inventory-baseline-passes=True
i3-audit-passes=True
phase-i-reference-tolerance-baseline-established=True
```

Expected focused artifacts:

```text
01-phase-i-reference-trajectory-conservation-inventory-baseline.summary.txt
02-reference-trajectory-contract.csv
03-reference-trajectory-samples.csv
04-conservation-inventory-final-window-slopes.csv
05-versioned-tolerance-budgets.csv
```

## 5. After a green I.3

Freeze the exact I.3 trajectory/slope/tolerance artifacts as Phase-I v1 regression evidence. Then close the known-limitations/compatibility documentation and remaining legacy/cumulative M10.9.4.1 acceptance work. Do not begin M10.9.5 until the acceptance gate includes ordinary suite, 60-second journeys, healthy 300-second reference, per-step protection evidence, replay determinism, conservation/inventory slopes, scale contract, versioned trajectory evidence and performance budget.

Do not delete H.5/H.21 source solely because current CI no longer executes those historical gates.

Read also:

- `M10_9_4_1_I3_REFERENCE_TRAJECTORY_CONSERVATION_INVENTORY_BASELINE.md`;
- `M10_9_4_1_I3_VALIDATION_CHECKLIST.md`;
- `M10_9_4_1_I3_STATIC_REVIEW.md`;
- `adr/0162-establish-phase-i-reference-baseline-before-freezing-regression-budgets.md`;
- `OPERATIONAL_ENVELOPE_NUMERICAL_HARDENING_PLAN.md`;
- `ROADMAP.md`.
