# M10.9.4.1-H.23 — Deterministic Replay, Checkpoint & Protection Interaction Qualification

## Status

**VALIDATED (Hotfix 2)** on 2026-08-18, built directly on user-validated **M10.9.4.1-H.22 — Four-Node Corrected-Candidate Commit Seam**.

H.23 introduces **no new numerical runtime behavior**. Standard current-v2 remains `ExplicitCommittedState` at 10 ms. The H.22 opt-in mode remains `FourNodeBranchContinuityCorrectedCommitOptIn` and is exercised only by audit-specific construction.

## Validated result

Local compilation, the complete ordinary suite and the focused H.23 gate passed after Hotfix 1 repaired the audit-only `Domain.Plant` import and Hotfix 2 repaired the case-sensitive descriptor-test expectation. No numerical runtime behavior changed in either hotfix.

```text
recorded steps                         701
checkpoint logical step                502
steps checkpoint→generator trip        199
corrected candidates committed          242
H.20 rollbacks                            0
fallback commit violations                0
unsafe corrected commits                  0
untargeted disagreements                   0
full replay trace equivalent           True
checkpoint continuation equivalent     True
deterministic trace repeat             True
reverse-power latched                  True
generator trip final                   True
breaker finally closed                False
telemetry/protection fingerprint
7C8FBA8ECB197F65AB263A79268653E3C2988F700A5A863BB0304D377C82FB54
```

Maximum network residuals remained within H.22 bounds: mass closure `2.1827872842550278E-11 kg`, energy closure `4.4293613427726086E-05 J`, balance mass rate `1.2256862191861728E-13 kg/s`, balance power `1.3923272490501404E-07 W`.

The next milestone is H.24 committed long-horizon/cross-profile qualification under the H.24–H.30 Phase H completion roadmap.

## Why H.23 exists

H.22 proved that an H.20-qualified four-node H.9 corrected candidate can actually own committed fluid state without unsafe commits, fallback violations or conservation loss. That result is still only a short committed control-window qualification.

Before extending the committed trajectory to long-horizon/cross-profile operation, H.23 answers two narrower integration questions:

1. does the H.22 committed path remain deterministic through the existing versioned recorder, full replay and replay-backed checkpoint authority?;
2. does that same committed path interact correctly with an already-validated delayed electrical protection, including an in-flight pickup checkpoint and eventual trip?

## Frozen H.22 prerequisite evidence

H.23 copies the three user-validated H.22 focused artifacts into the ordinary-test evidence directory and verifies canonical newline-normalized SHA-256 fingerprints:

```text
H22_ValidatedCorrectedCommitSeamSummary.txt
H22_ValidatedCorrectedCommitSeamTelemetry.csv
H22_ValidatedCorrectedCommitSeamMetrics.csv
```

Frozen headline evidence:

```text
intervals                         2000
P060/F040 triggers                443
H.20 candidate eligible           443
H.22 commit authorized            443
corrected candidates committed    443
H.20 rollback                       0
fallback commit violations          0
unsafe corrected commits            0
untargeted disagreements             0
deterministic repeat             True
```

Because H.23 does not modify H.22 numerical runtime code, its focused script fingerprint-checks this evidence instead of automatically rerunning the expensive cumulative H.22 -> H.21 -> H.19/H.20 chain. A future milestone that modifies the corrected-commit runtime must restore the full numerical regression prerequisite.

## Audit-only exact-version factory

Replay reconstruction is authoritative only when the same exact initial-condition recipe can be recreated. H.23 therefore defines an `IVersionedInitialConditionFactory` **inside the Application.Tests assembly only**.

The factory:

- has a dedicated audit-only initial-condition identity;
- delegates directly to `DesktopSustainedGenerationInitialConditionFactory.CreateFourNodeCorrectedCommitEvidenceRuntimeEngine(10 ms)`;
- returns the unchanged H.22 runtime directly, then attaches a test-only observer to `ControlRoomRuntimeCoordinator.DeterministicStepCompleted` to record per-step H.20/H.22/protection telemetry without masking or replacing any runtime interface;
- is registered only in the H.23 test-local `VersionedInitialConditionRegistry`;
- is not discoverable or selected by standard product factories.

This keeps replay semantics real without adding a production selection path.

## Replay and checkpoint contract

The focused scenario uses the normal desktop objectives/actions with the audit-only H.23 initial-condition reference.

The recording phase:

1. runs the H.22 committed path at 10 ms;
2. establishes a pre-trip stable interval;
3. commands turbine trip plus generator load reduction;
4. advances until the validated `generator-reverse-power` function has a non-zero pickup timer while not yet latched;
5. creates a replay-backed checkpoint at that exact in-flight state;
6. advances until generator trip occurs;
7. records the final snapshot fingerprint and the complete internal H.20/H.22/protection trace.

The gate then requires:

- full archive replay reproduces every existing scenario fingerprint/event check;
- the captured internal H.20/H.22/protection trace is exactly equal to the recording trace;
- seek to the in-flight checkpoint reproduces the checkpoint fingerprint and pickup state;
- continuing from that restored checkpoint for the same number of deterministic steps reproduces the original final trip fingerprint;
- the reconstructed internal trace prefix and continuation equal the original full trace exactly.

## Protection interaction contract

H.23 uses the already-implemented evidence-derived reverse-power generator protection rather than inventing a new protection test law.

The committed path must demonstrate:

- normal pre-trip operation remains trip-free;
- reverse-power pickup becomes in-flight before latching;
- the checkpoint captures that in-flight state;
- generator trip subsequently latches;
- the reverse-power function is the latched protection function under observation;
- the generator breaker is open after the trip;
- replay and checkpoint continuation reproduce the same protection evolution.

H.23 does **not** require protection timing to match the explicit trajectory step-for-step. H.22 commits may legitimately alter the physical trajectory. It requires deterministic self-replay and preservation of the protection semantics.

## Fail-closed commit invariants under the protection transient

Every captured step is checked against the unchanged H.20/H.22 contracts.

Whenever a corrected candidate is committed:

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

Every captured protection-transient step, corrected or explicit, must also remain within the H.22 network-accounting bounds:

```text
mass closure                   <= 1e-6 kg
energy closure                 <= 1e-2 J
balance mass-rate residual     <= 1e-8 kg/s
balance power residual         <= 1e-3 W
```

Whenever H.20 requests rollback:

```text
corrected candidate committed = false
H.22 commit authorized        = false
```

A protection transient is allowed to cause safe explicit fallback. H.23 therefore does not require rollback count to remain zero; it requires **zero fallback-commit violations and zero unsafe corrected commits**.

## Qualification boundary

A green H.23 result proves only:

- exact deterministic recording/full replay of the H.22 opt-in committed path;
- exact replay-backed checkpoint seek and continuation across an in-flight delayed protection pickup;
- correct reverse-power generator-trip interaction;
- continued H.20/H.22 fail-closed authority semantics and H.20 residual guards during the transient;
- continued H.22 network mass/energy closure and ownership bounds during the transient;
- default product factory isolation.

It does **not** yet prove:

- committed long-horizon/cross-profile robustness;
- off-design numerical robustness;
- default production activation suitability.

The intended next milestone after a green H.23 is committed long-horizon/cross-profile qualification, followed by off-design robustness before any default activation decision.
