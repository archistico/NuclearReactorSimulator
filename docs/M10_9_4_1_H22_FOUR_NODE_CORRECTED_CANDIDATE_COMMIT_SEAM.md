# M10.9.4.1-H.22 — Four-Node Corrected-Candidate Commit Seam

## Status

**VALIDATED — 2026-08-18.** Built directly on user-validated M10.9.4.1-H.21 Hotfix 1; local build, complete ordinary suite and focused H.22 gate passed.

Standard current-v2 remains `ExplicitCommittedState` at 10 ms. H.22 is separately opt-in and is not selected by normal desktop/current-v2 factories.

## Validated H.22 result

```text
intervals=2000
P060-F040-triggered=443
H20-candidate-eligible=443
H22-commit-authorized=443
corrected-candidates-committed=443
H20-rollbacks=0
fallback-commit-violations=0
unsafe-corrected-commits=0
untargeted-branch-disagreements=0
repeat-presentation-equivalent=2000/2000
deterministic-repeat=True
telemetry-fingerprint=3366BCFFF62EBCC8C097EDC36DAF543D80BFBF05936AF6DAFE08EA34A7DBB178
four-node-corrected-candidate-commit-seam-passes=True
h22-audit-passes=True
```

Standard current-v2 remained `ExplicitCommittedState` at 10 ms. H.23 continues from this validated baseline without changing H.22 numerical runtime behavior.

## Purpose

H.21 proved that the H.19-qualified four-node correction and H.20 fail-closed authority supervisor can execute from the real `PlantNetworkOrchestrator` with zero committed effect. H.22 introduces the smallest possible next seam: allow an H.20-qualified corrected candidate to own the returned fluid state in a dedicated opt-in mode, while preserving an already-computed explicit candidate as immediate same-step fallback.

H.22 is intentionally **not** a default activation milestone.

## Frozen prerequisites

H.22 may not change:

- P060/F040 trigger thresholds;
- H.9 algorithm or tolerances;
- bounded previous-phase hysteresis limits of 2% pressure / 5 K temperature;
- target set `steam|stop-out|header|turbine-inlet`;
- H.20 qualification/rollback rules;
- physical coefficients;
- production inverse-thermodynamic branch order;
- standard current-v2 10 ms timestep.

The H.21 validated focused artifacts are frozen and fingerprinted as ordinary evidence.

## New numerical mode

```text
FourNodeBranchContinuityCorrectedCommitOptIn
```

This mode is distinct from both:

```text
ExplicitCommittedState
DeterministicHybridSemiImplicit
FourNodeBranchContinuityShadowIntegrated
```

Factory construction rejects simultaneous selection of the old H.5 hybrid, H.21 shadow integration and H.22 corrected commit modes.

## Two-stage authority

H.22 deliberately preserves the distinction between **eligibility** and **commit**.

### Stage 1 — unchanged H.20 supervisor

H.20 sees the actual H.9 result and checks:

- trigger;
- qualification evidence;
- convergence;
- line-search exhaustion;
- pressure residual;
- flow residual;
- mass closure;
- energy ownership;
- untargeted branch disagreement.

It proposes either explicit authority or `CorrectedCandidate` eligibility.

### Stage 2 — H.22 commit seam

`FourNodeBranchContinuityCorrectedCommitSeam` permits commit only when:

```text
H.22 commit arm enabled
AND H.20 trigger observed
AND H.20 activation arm enabled
AND H.20 rollback not required
AND H.20 corrected candidate eligible
AND H.20 reason == QualifiedTriggeredCorrection
AND shadow correction evaluated
AND corrected candidate exists
```

Otherwise it returns a typed explicit-fallback reason. No H.22 rule can make a candidate eligible if H.20 denied it.

## Explicit-first fallback

The historical explicit network path is evaluated before the sidecar on every H.22 step. This is intentional rather than an optimization defect: it guarantees that H.22 always has a complete same-step explicit candidate and accounting basis available before commit authority is considered.

If corrected ownership is denied, the historical explicit candidate is returned immediately; no retry, partial corrected state or hybrid mix is committed.

## Corrected candidate composition

The H.9 result is extended with `AppliedPumpHydraulicPowerExchange` so H.22 can audit the same applied iterate it commits.

On a corrected commit:

- fluid nodes = corrected H.9 candidate fluid nodes;
- fluid balances = corrected applied hydraulic balances + unchanged non-hydraulic fluid balances;
- pump hydraulic power = applied H.9 iterate pump power;
- thermal bodies = normal outer-step thermal integration from unchanged thermal balances;
- valves/pumps/heat-source states = unchanged committed actuator/source states for that network step.

`BuildAudit(...)` therefore sees the state and balances actually selected by H.22.

## Telemetry

Existing four-node integration telemetry remains source-compatible and gains:

```text
CorrectedCommitArmEnabled
CorrectedCommitAuthorized
CorrectedCommitReason
```

The existing positional `CorrectedCandidateCommitted` now reflects the outer orchestrator's actual H.22 ownership choice. The H.9/H.21 evaluator itself still never commits a candidate.

## Focused gate philosophy

H.21 could demand explicit trajectory equality because it committed nothing. H.22 cannot use that same criterion: a legitimate corrected commit may alter the next committed state and therefore future trigger timing.

The H.22 gate instead validates invariants:

- positive but fully authorized corrected ownership;
- immediate explicit fallback on all denied paths;
- zero unsafe commits;
- deterministic H.22 replay against an identical H.22 runtime;
- conservation/accounting limits every step;
- no control-window protection trip;
- standard factory remains explicit.

The test reports the actual trigger/rollback/disagreement counts rather than forcing them to equal H.21's 15/0/0.

## Validation

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-four-node-corrected-commit-seam-audit.cmd
```

The focused script first runs the full H.21 gate, which itself re-runs H.19 and H.20.

## Interpretation

A green H.22 result validates a first **opt-in corrected-commit seam** only. It does not justify selecting H.22 in standard production.

The next activation-hardening work must test committed corrected states through recorder/replay, protections, long-running profiles and off-design conditions before any default activation review.
