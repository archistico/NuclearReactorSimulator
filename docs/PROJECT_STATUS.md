# Project status

## Authoritative state

**Production policy:** `M10.9.4.1-H.30 Requalification 1 — ACTIVATE`.

- exact v3 `FourNodeBranchContinuityCorrectedCommitOptIn`: authoritative desktop default;
- exact v2 `ExplicitCommittedState`: fail-closed rollback/reference;
- fixed step: 10 ms;
- H.28 performance class: `bounded-but-costly`;
- no H.9/H.20/H.22/P060-F040/hysteresis/physical-coefficient retuning.

## Validated Phase-I baseline

**I.3 Hotfix 2 — VALIDATED.**

The authoritative v3 reference completed 300 s / 30,000 steps with:

- 0 generation-health violations;
- 0 targeted stop/control/admission reverse-flow violations;
- 3,757/3,757 corrected commits with 0 rollback/fallback/unsafe/untargeted disagreement;
- deterministic repeat;
- seven frozen final-window slopes;
- 19 frozen regression budgets.

The reference remains operationally healthy but is not an asymptotic steady-state proof; current drift observations are recorded in `KNOWN_MODEL_LIMITATIONS.md`.

## Current candidate

**M10.9.4.1-I.4 — Known Limitations & Legacy Retirement Review.**

I.4 reviews the two historical numerical modes remaining in source. Neither is a current production, exact-version or current-CI dependency. Source removal is candidate-deferred because executable historical tests still compile against those seams.

## Evidence/package policy

Candidate ZIPs do not bundle `tests/.../Gameplay/Evidence`, `artifacts`, `bin` or `obj`. Compact immutable prerequisites live under `eng/frozen-evidence/ordinary`; decision/reference manifests live under `eng/evidence-manifests`.

## Remaining Phase I

1. validate I.4;
2. I.5 cumulative M10.9.4.1 closure gate;
3. only a green I.5 unblocks M10.9.5.
