# ADR 0159 — Close Phase H as OPT-IN ONLY because corrected ownership is qualified but bounded-costly

## Status

Proposed by M10.9.4.1-H.30; becomes accepted only when H.30 is locally validated.

## Context

H.19-H.29 progressively qualified the four-node corrected-commit path across numerical convergence, fail-closed authority, real orchestrator wiring, corrected ownership, replay/checkpoint/protection, committed long-horizon operation, protection/transient behavior, integrated rollback stress, off-design operation, performance/soak, post-optimization long-horizon regression and production activation mechanics.

The technical chain is green. H.29 demonstrated an exact v3 corrected production candidate with 400 qualified commits, zero rollback/fallback/unsafe/untargeted disagreement, deterministic repeat, exact replay/checkpoint behavior, and explicit deployment kill back to exact v2.

H.28 nevertheless classifies the corrected path `bounded-but-costly`: the measured median wall-cost ratio is about 4.62x explicit and p95 ratio about 10.68x, despite remaining inside the deliberately generous qualification ceilings.

## Decision

Close Phase H with the production policy decision `OPT-IN ONLY`:

- exact v2 `ExplicitCommittedState` remains authoritative default, rollback and reference;
- exact v3 `FourNodeBranchContinuityCorrectedCommitOptIn` remains a qualified production opt-in;
- explicit deployment kill always resolves to exact v2;
- no H.30 runtime selector change or numerical retuning is introduced.

## Rationale

The corrected path has sufficient evidence for controlled production availability, so `REMAIN EXPLICIT` would discard useful qualified capability. But the measured runtime penalty remains material and H.28 explicitly labels it costly, so `ACTIVATE` would overstate the evidence. `OPT-IN ONLY` is the narrow outcome supported by both sets of facts.

## Consequences

- Phase H can close without hiding cost through timestep/tolerance changes.
- Existing v2 saves/replays retain exact meaning.
- v3 remains available for qualified use and future performance work.
- Phase I may resume after H.30 validation.
- Any future attempt to make corrected ownership the default requires a separate decision supported by materially improved cost evidence and regression qualification.
