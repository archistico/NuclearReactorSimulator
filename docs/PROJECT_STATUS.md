# Project status

## Authoritative state

**Validated production policy:** `M10.9.4.1-H.30 Requalification 1 — ACTIVATE`.

- authoritative desktop default: `integrated-operations-desktop-stable@3` / `FourNodeBranchContinuityCorrectedCommitOptIn`;
- rollback/reference: `integrated-operations-desktop-stable@2` / `ExplicitCommittedState`;
- explicit kill resolves to exact v2;
- H.28 performance class remains `bounded-but-costly`;
- fixed step remains 10 ms;
- no H.9/H.20/H.22/P060-F040/hysteresis/physical-coefficient retuning was required.

H.30 RQ1 was promoted after validated Phase-I evidence showed 338/338 exact-v2 generation-drop steps coincident with targeted steam-train reverse flow, while exact v3 produced 0 drops / 0 targeted reverse flow and then completed 300 s / 30,000 steps continuously healthy and deterministic.

## Current candidate

**M10.9.4.1-I.3 Hotfix 2 — Authoritative Production Reference Trajectory, Conservation/Inventory & Tolerance Baseline / Compact Frozen Evidence Contracts**.

I.3 runs the authoritative production selector for 300 s. It checks every 10 ms step for generation health and reverse flow across stop/control/admission, samples conservation/inventory every second, measures seven final-window slopes, verifies corrected telemetry/determinism and derives 19 versioned regression budgets.

The budgets are candidate evidence until I.3 is explicitly validated. They are not calibration targets and must not drive hidden physics retuning.

## Evidence/package policy

Large audit outputs under `artifacts/` or historical `tests/.../Gameplay/Evidence` are not bundled into new candidate ZIPs. The original validation artifact archives remain separate. Ordinary tests use the bounded immutable prerequisites under `eng/frozen-evidence/ordinary`; intentionally omitted large trace identities are stored in `eng/frozen-evidence/large-payload-manifest.csv`, and decision provenance remains under `eng/evidence-manifests/`. An existing local Evidence directory is optional and must not be deleted by candidate application.

## Phase I

Validated: I.1, I.2, H.30 RQ1 production re-review and the diagnostic/requalification evidence that supported it.

Remaining:

1. I.3 authoritative reference/budget baseline;
2. I.4 known-limitations and legacy-retirement review;
3. I.5 cumulative M10.9.4.1 closure gate.

M10.9.5 remains blocked until I.5 is green.
