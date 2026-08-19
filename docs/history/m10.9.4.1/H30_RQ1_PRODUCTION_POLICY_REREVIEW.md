# H.30 Requalification 1 — Production Policy Re-review

## Purpose

H.30 originally closed Phase H as `OPT-IN ONLY`: exact v2 `ExplicitCommittedState` remained the production default because exact v3 corrected-commit, although technically qualified, was materially more expensive (`bounded-but-costly`).

Phase-I I.3 diagnostics later added evidence that did not exist when H.30 was decided:

- exact v2 reproduced **338 generation-drop steps** over the first 100 simulated seconds;
- those 338/338 drops coincided one-for-one with reverse flow somewhere in the targeted stop/control/admission train;
- exact v3 produced **0 generation drops** and **0 targeted reverse-flow steps** over the same 100-second comparison;
- exact v3 then completed the full **300 s / 30,000-step** healthy reference horizon with 0 generation-health violations, 0 targeted reverse-flow violations, 0 rollback/fallback/unsafe commits and deterministic repeat.

H.30 Requalification 1 therefore re-opens only the production-policy decision. It does not change H.9 mathematics, H.20 authority, H.22 ownership, P060/F040, hysteresis limits, physical coefficients or the 10 ms fixed step.

## Candidate decision

If the frozen prerequisite evidence is intact, the re-review derives:

`ACTIVATE`

The rationale is not that v3 became cheap. H.28 remains `bounded-but-costly`. The new fact is that the cheaper v2 path has a validated healthy-operation continuity defect that the already-qualified v3 path suppresses.

## Production contract if validated

- authoritative desktop default: `integrated-operations-desktop-stable@3` / `FourNodeBranchContinuityCorrectedCommitOptIn`;
- rollback/reference: `integrated-operations-desktop-stable@2` / `ExplicitCommittedState`;
- explicit kill remains fail-closed and resolves to exact v2;
- historical v1/v2/v3 initial-condition identities remain loadable without reinterpretation;
- historical v2 and H.29 candidate scenario identities remain retained for replay compatibility;
- H.30 RQ1 adds the distinct production scenario identity `integrated-normal-operations-training-h30-rq1-production` over exact v3 rather than repurposing the historical H.29 scenario;
- the production desktop startup selects that exact-v3 production scenario/training-plan pair only after the requalified policy is validated.

## Evidence used

H.30 RQ1 does not rerun H.24, H.28 or the I.3 long diagnostics. It fingerprint-checks the frozen validated evidence and verifies the live selector/startup contract.

Key inputs:

- H.28: performance gate green, corrected path `bounded-but-costly`;
- original H.30: `OPT-IN ONLY` closure green;
- I.3 Hotfix 4 Classifier Fix 1: 338/338 explicit drops = targeted-train reverse-flow steps; corrected 0/0;
- I.3 Hotfix 5: corrected 300 s healthy reference green and deterministic.

## Non-goals

H.30 RQ1 does not freeze I.3 tolerance budgets, retire v2, remove historical numerical modes, retune the solver or change the external fixed timestep.
