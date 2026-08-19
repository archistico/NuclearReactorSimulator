# M10.9.4.1-H.25 — Committed Protection & Operational-Transient Matrix

## Status

**VALIDATED on 2026-08-19**, built directly on user-validated **M10.9.4.1-H.24 Hotfix 1**. Build, complete ordinary tests and focused H.25 gate passed: 5 scenarios / 837 runtime steps / 178 corrected commits / zero rollback, fallback-commit or unsafe-commit violations; focused duration 5m29s.

## Purpose

H.24 qualified duration and the four nominal cross-profile trajectories but was intentionally a rare 4h31m55s gate. H.25 asks a different question: does the unchanged opt-in corrected-commit runtime preserve representative current-v2 protection and operational-transient semantics without forcing another long soak?

H.25 makes no numerical runtime change.

## Targeted matrix

The focused audit uses fresh `FourNodeBranchContinuityCorrectedCommitOptIn` runtimes and five bounded scenarios:

| Scenario | Purpose |
| --- | --- |
| `normal-load-maneuver` | load lower/raise without spurious protection trip |
| `manual-reactor-scram` | canonical reactor scram ownership remains effective |
| `manual-generator-trip` | generator trip latches and opens the breaker |
| `turbine-trip-reverse-power` | turbine trip leads to delayed automatic reverse-power generator trip |
| `breaker-open-turbine-coastdown` | breaker-open supervision blocks reverse-power/underfrequency/loss-of-synchronism eligibility |

The matrix is intentionally short: approximately 900–1,100 committed runtime steps depending on the reverse-power pickup search. It is not a replacement for H.24.

## Protection catalogue contract

A non-explicit ordinary test also confirms the current-v2 protection catalogue and action classes for:

- `very-high-pressure`;
- `turbine-overspeed`;
- `condenser-high-backpressure`;
- `generator-overfrequency`;
- `steam-drum-low-low-level`;
- `generator-reverse-power`;
- `generator-underfrequency`;
- `generator-loss-of-synchronism`.

The three evidence-derived electrical functions must retain `generator-breaker-closed` supervision.

H.25 does not fabricate plant trajectories solely to force every threshold. Existing ordinary protection tests remain authoritative for threshold, pickup-delay and reset-law contracts.

## Corrected-ownership safety

Every H.25 step is checked for the unchanged H.20/H.22 authority chain and H.22 closure/ownership limits.

Safe H.20 rollback/fallback is allowed. The gate forbids:

- fallback corrected commits;
- corrected commits during rollback;
- commits with untargeted disagreement;
- commits without convergence;
- commits outside H.20 residual guards;
- network closure/ownership violations.

## H.24 provenance without rerun

H.25 freezes compact user-validated H.24 artifacts:

- H.24 summary;
- per-profile metrics;
- overall metrics;
- a manifest containing the full 30,008-row telemetry canonical fingerprint.

The approximately 9.95 MB H.24 step telemetry is not duplicated in the source package. Its canonical SHA-256 is:

`8D077BC89D0DBD539476BC33483C0B734F74E84D5BC20D4FE4D55D6A1B4344FA`

## Qualification decision

A green H.25 means representative protection action, supervision and operational-transient interactions are compatible with corrected ownership. It does **not** qualify deliberately forced H.20 rollback behavior; that is H.26.

Default current-v2 remains `ExplicitCommittedState`.
