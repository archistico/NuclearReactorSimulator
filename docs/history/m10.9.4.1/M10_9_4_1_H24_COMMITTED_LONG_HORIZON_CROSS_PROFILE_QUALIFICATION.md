# M10.9.4.1-H.24 — Committed Long-Horizon & Cross-Profile Qualification

## Status

**VALIDATED (Hotfix 1)** on 2026-08-19. Built directly on user-validated **M10.9.4.1-H.23 Hotfix 2**.

Standard current-v2 remains `ExplicitCommittedState` at 10 ms. H.24 exercises `FourNodeBranchContinuityCorrectedCommitOptIn` only through the audit-only H.22 evidence factory.

## Why H.24 exists

H.19 qualified the exact four-node numerical mechanism in shadow over four long-horizon profiles. H.22 then proved actual corrected ownership for 2,000 intervals, and H.23 proved that the same committed path remains deterministic through recording/full replay/checkpoint continuation and interacts correctly with reverse-power generator protection.

The missing question is duration:

> Can the real committed path remain fail-closed, conservative and operationally stable over the full nominal long-horizon/cross-profile domain?

H.24 answers only that question.

## Frozen H.23 prerequisite evidence

H.24 freezes the user-validated H.23 focused artifacts in the ordinary-test evidence directory:

```text
H23_ValidatedCommittedReplayProtectionSummary.txt
H23_ValidatedCommittedReplayProtectionTrace.csv
H23_ValidatedCommittedReplayProtectionMetrics.csv
```

Canonical newline-normalized SHA-256:

```text
summary  933ED5D40C0329D14EBF2F757F87F631118485221B4ED272AF092AEA60E0CB25
trace    C0F2CC4B1B2C4CBDB64DB3C689FBC00ACE58788A0F5F0A125A60CBDB4B46CC95
metrics  5335D6ACBB65A4443E73DF9032444249851372183217D4792E89371BC2114469
```

Validated H.23 headline evidence:

```text
recorded steps                    701
checkpoint logical step           502
checkpoint→generator trip         199 steps
corrected commits                 242
H.20 rollback                       0
fallback commit violations          0
unsafe corrected commits            0
untargeted disagreements             0
full replay equivalent           True
checkpoint continuation          True
deterministic trace repeat       True
reverse-power latched            True
generator trip final             True
```

H.24 changes no H.22/H.23 numerical runtime code, so this prerequisite is fingerprint-checked rather than rerunning the expensive predecessor focused chain automatically. The complete ordinary suite remains mandatory.

## Operational profile domain

H.24 reuses the H.19 nominal profile geometry:

```text
steady-long             12,000 intervals
load-pulse               6,000 intervals
cooling-pulse            6,000 intervals
combined-load-cooling    6,000 intervals
TOTAL                    30,000 intervals
```

Profile actions are unchanged in meaning:

- load pulse: lower request at interval 501, raise at 3,501;
- cooling pulse: condenser cooling 100%→75% at 501, restore at 3,501;
- combined: load lower 501, cooling degrade 1,001, load raise 3,501, cooling restore 4,001.

As in H.19, each action is followed by one deterministic transition step before the numbered profile interval. Therefore the H.24 committed runtime executes 30,000 qualification intervals plus 8 action-transition steps.

## Why H.24 does not freeze H.19 trigger counts

H.19 observed 3,046 P060/F040 triggers on the explicit reference trajectories. H.22 already demonstrated that actual corrected ownership materially changes the committed trajectory and its trigger frequency.

Therefore H.24 **must not** require:

- 3,046 triggers;
- 92 trigger episodes;
- the original 473 representative keys.

Those are provenance for the shadow qualification, not invariants of a changed committed trajectory.

H.24 instead records the new committed census and judges safety and completeness.

## Fail-closed committed safety

Every corrected commit must still satisfy the unchanged H.20/H.22 chain:

```text
H.20 candidate eligible        = true
H.22 commit authorized         = true
H.20 rollback required         = false
untargeted branch disagreement = false
shadow correction evaluated    = true
shadow converged                = true
shadow line-search exhausted    = false
pressure residual              <= 1e-5
flow residual                  <= 1e-2 kg/s
shadow mass closure            <= 1e-8 kg/s
shadow energy ownership        <= 1e-3 W
H.20 proposed authority        = CorrectedCandidate
H.20 reason                    = QualifiedTriggeredCorrection
H.22 commit reason             = QualifiedH20Authority
```

H.20 rollback or another safe refusal is permitted. Any such interval must remain wholly explicit. H.24 does not treat a safe rollback as a qualification failure by itself.

## Network accounting

Every committed runtime step, corrected or explicit, must stay inside the established H.22 accounting bounds:

```text
mass closure                   <= 1e-6 kg
energy closure                 <= 1e-2 J
balance mass-rate residual     <= 1e-8 kg/s
balance power residual         <= 1e-3 W
```

## Determinism contract

Full long-horizon duplicate execution would double an already expensive committed gate without adding a new runtime algorithm. Determinism therefore has two layers:

1. H.23 validated exact full replay and checkpoint-continuation trace determinism on the unchanged H.22 runtime and is frozen as a mandatory prerequisite;
2. H.24 repeats a fresh 256-interval committed control twice and requires exact presentation/authority/accounting fingerprint equality.

H.24 still emits one canonical fingerprint over the complete four-profile committed telemetry for future regression provenance.

## Positive qualification

H.24 passes only if:

- all four profiles complete without trip;
- each profile observes at least one P060/F040 trigger and at least one corrected commit;
- fallback commit violations = 0;
- unsafe corrected commits = 0;
- untargeted branch disagreements = 0 in this nominal domain;
- closure/ownership bounds hold on every observed step;
- the repeated committed determinism control is exact;
- default standard current-v2 factory remains `ExplicitCommittedState`.

## Boundary after H.24

A green H.24 does not authorize default activation. Per `M10_9_4_1_PHASE_H_COMPLETION_ROADMAP_H24_H30.md`, the next milestone is **H.25 — Committed Protection & Operational-Transient Matrix**, followed by H.26 integrated rollback stress, H.27 off-design, H.28 performance/soak, H.29 activation candidate and H.30 closure decision.


## Validated Hotfix 1 result

Local build, complete ordinary suite and focused gate passed on 2026-08-19.

```text
qualification intervals          30,000
action-transition steps               8
committed runtime steps          30,008
corrected commits                 9,626
H.20 rollbacks                        0
fallback commit violations            0
unsafe corrected commits              0
untargeted disagreements               0
all profiles trip-free              True
deterministic repeat                True
fingerprint
F079CA4BAB5A866BE3DD1E1F57ADC342C865B5E1889B971628B1549426E88B78
focused gate duration             4h31m55s
```

Because of the 4h31m55s cost, this gate is now classified as a rare qualification/closure gate rather than a routine regression.
