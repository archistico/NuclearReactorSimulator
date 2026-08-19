# M10.9.4.1 Phase H Completion Roadmap — H.24 through H.30

## Status and authority

This roadmap is the authoritative Phase H continuation plan. **M10.9.4.1-H.27 Hotfix 1** was user-validated on 2026-08-19. H.28 subsequently failed only its performance/cost gate as `unbounded-regression`; **H.28.1-A Hotfix 2 is validated diagnostic evidence**, **H.28.1-C Hotfix 2 is validated allocation/hot-path optimization evidence**, **H.28.1-B is validated historical-explicit predictor reuse**, and **H.28.1-D** is the current hydraulic-probe CPU hot-path candidate. H.29 remains blocked.

The standard production numerical path remains:

```text
current-v2 = ExplicitCommittedState
fixed step = 10 ms
```

The corrected ownership path remains separately opt-in:

```text
FourNodeBranchContinuityCorrectedCommitOptIn
```

No milestone in this roadmap is automatically entitled to promote that path to the standard default merely because its focused gate passes. Default activation is a separate H.29/H.30 decision.

## Validated entry point

The validated provenance entering this roadmap is:

- **H.19 VALIDATED:** four-node shadow policy qualified 473/473 over the complete 30,000-interval / four-profile H.17 census contract.
- **H.20 VALIDATED:** deterministic fail-closed eligibility/rollback contract, 473/473 armed eligibility and 8/8 rollback challenges.
- **H.21 Hotfix 1 VALIDATED:** real-orchestrator sidecar integration, 2,000/2,000 trajectory transparency, 15/15 eligibility, zero corrected commits.
- **H.22 VALIDATED:** first separately opt-in corrected ownership, 443 corrected commits in 2,000 steps with zero unsafe/fallback-commit violations.
- **H.23 Hotfix 2 VALIDATED:** 701-step recording/full replay/checkpoint/protection qualification with 242 corrected commits, exact replay and checkpoint continuation, reverse-power latch and generator trip, zero unsafe commits.
- **H.24 Hotfix 1 VALIDATED:** 30,000 qualification intervals + 8 transition steps, 9,626 corrected commits, zero rollback/fallback-commit/unsafe-commit/untargeted-disagreement violations, all four nominal profiles trip-free; focused duration 4h31m55s.
- **H.25 VALIDATED:** targeted five-scenario protection/transient matrix completed 837 runtime steps in 5m29s with 178 corrected commits, zero H.20 rollback/fallback-commit/unsafe-commit violations and all expected outcomes satisfied.

The numerical controls remain frozen unless later evidence specifically invalidates them:

```text
trigger                 P060/F040
corrector               H.9 finite-difference Jacobian + damped Newton
branch continuity       bounded previous-phase hysteresis
hysteresis bounds       2% pressure / 5 K
nodes                    steam | stop-out | header | turbine-inlet
production fixed step   10 ms
```

## H.24 — Committed Long-Horizon & Cross-Profile Qualification

### Question

Can the **real committed H.22 opt-in trajectory** operate safely over the same nominal long-horizon/cross-profile domain that originally qualified the four-node policy in H.19?

### Scope

Reuse the H.19 operational profile domain without requiring the H.19 trigger census to remain numerically identical after corrected commits:

```text
steady-long             12,000 intervals
load-pulse               6,000 intervals
cooling-pulse            6,000 intervals
combined-load-cooling    6,000 intervals
TOTAL                    30,000 intervals
```

The load-pulse legs retain the validated 5→0→5 MWe request trajectory. Cooling degradation remains 100%→75%→100% in the same action windows used by H.19.

### Qualification rules

Require:

- every profile completes without trip;
- each profile actually exercises P060/F040 and corrected ownership;
- corrected commits occur only behind H.20 eligibility and H.22 authorization;
- H.20 rollback/fallback is allowed and is considered healthy when fail-closed;
- zero fallback-commit violations;
- zero unsafe corrected commits;
- zero new untargeted branch disagreement in the nominal H.19 profile domain;
- network mass/energy closure and ownership remain inside the validated H.22 bounds;
- standard current-v2 factories remain explicit;
- deterministic evidence remains anchored by validated H.23 plus an H.24 committed repeat control.

Do **not** require the committed trajectory to reproduce H.19's 3,046 triggers, 92 episodes or 473 representative keys. Corrected ownership legitimately changes the trajectory and therefore its trigger census.

### Decision

H.24 is green and qualifies duration and nominal cross-profile operation only. Its 4h31m55s focused gate is classified as a rare qualification gate and must not be chained automatically while committed runtime code is unchanged. It does not qualify the complete protection matrix, integrated rollback stress, off-design operation or default activation.

## H.25 — Committed Protection & Operational-Transient Matrix

### Question

Does corrected ownership preserve the broader set of already-implemented protection and operational-transient semantics, beyond H.23's reverse-power case?

### Scope

Build the matrix from protections and transients already implemented and already covered by ordinary tests. Do not invent new physical protection laws merely to expand H.25.

The validated H.25 gate used a deliberately bounded matrix so development feedback stays practical:

- normal load lower/raise with no spurious trip;
- manual reactor scram;
- manual generator trip and breaker opening;
- turbine trip followed by automatic delayed reverse-power generator trip;
- breaker-open turbine coastdown with reverse-power/underfrequency/loss-of-synchronism supervision disabled.

A cheap ordinary contract also preserves all eight current-v2 protection functions and their action classes. H.25 does not fabricate long trajectories solely to force every threshold; existing ordinary protection tests remain authoritative for individual threshold/pickup/reset laws.

### Qualification rules

Require:

- no lost protection;
- no corrected commit may override or bypass protection authority;
- no unexplained spurious trip introduced by corrected ownership;
- pickup/latch/reset and breaker semantics remain coherent;
- H.23 replay/checkpoint evidence remains the deterministic critical-case prerequisite; H.25 does not duplicate it because numerical runtime is unchanged;
- any timing shift relative to the explicit trajectory must be deterministic, physically attributable to the changed committed trajectory and must not weaken the protection function.

H.25 is not required to reproduce every explicit pickup time step-for-step.

### Decision

H.25 is green: five scenarios, 837 runtime steps, 178 corrected commits, zero rollback/fallback-commit/unsafe-commit violations and all expected outcomes satisfied. Its 5m29s duration validates the targeted-gate strategy.

## H.26 — Integrated Rollback & Fail-Closed Stress Qualification

### Question

Does the real committed orchestrator correctly execute the complete fallback path when H.20 refuses corrected ownership?

H.20 validated rollback reasons in isolation, while H.22/H.23 observed zero real rollbacks. H.26 closes that gap.

### Scope

H.26 must remain a short integration gate. H.20 remains the semantic owner of observation-to-reason mapping and H.22 remains the owner of commit-denial semantics. H.26 adds only an `internal` test-only authority-decision transform to `PlantNetworkOrchestrator`; the public constructor and all standard factories never supply it.

The focused matrix exercises:

- natural untriggered fallback;
- activation-arm-disabled denial;
- explicit H.20 authority denial;
- integrated `ShadowCorrectionNotEvaluated` denial;
- all eight typed H.20 rollback reasons.

The unchanged H.22 unit contract continues to cover `CorrectedCandidateUnavailable`. H.24 is not rerun.

### Core invariant

For every interval:

```text
corrected state is wholly authoritative
OR
explicit state is wholly authoritative
```

Never allow mixed ownership where fluid state, balances, pump work or thermal/accounting data come from different candidate authorities.

### Qualification rules

Require:

- immediate explicit fallback in the same network step;
- exact physical equivalence to a historical `ExplicitCommittedState` reference for the same deterministic test state;
- zero partial/mixed commits;
- typed H.20/H.22 denial reason preserved in telemetry;
- deterministic repeat;
- public production construction remains hook-free;
- standard factory remains explicit.

H.26 Hotfix 1 is green and validated: 12/12 same-step explicit fallbacks, eight typed rollback reasons plus four denial controls, zero corrected/partial commits and deterministic repeat. It does not authorize default activation.

## H.27 — Off-Design Robustness & Qualification Envelope

**Current status:** **VALIDATED (Hotfix 1)** on 2026-08-19. Six scenarios / 2,080 runtime steps / 529 corrected commits; four `corrected-qualified`, two `protected-boundary`, zero unsafe/fallback commits.


### Question

Where does the corrected-commit policy remain qualified outside the nominal H.19 profile domain, and where should H.20 deliberately fall back to explicit?

### Scope

Use a staged evidence matrix within the educational model's defensible domain. This is not a stress contest using physically meaningless states.

Explore combinations near important operating boundaries, including:

- lower/higher generation demand within the modelled operating range;
- degraded cooling levels and recovery;
- steam/header temperature and pressure conditions near the inverse-map switching regions;
- combined secondary-side disturbances;
- transients approaching protection thresholds;
- states near, but not beyond, documented thermodynamic/model validity boundaries.

### Qualification rule

Rollback is not failure. The desired result is a documented **operational qualification envelope**:

- corrected ownership where evidence is strong;
- fail-closed explicit fallback where it is not.

A single unsafe corrected commit is more serious than many safe rollbacks.

## H.28 — Performance, Cost & Long-Running Operational Soak

### Question

Is the qualified corrected path operationally affordable at the fixed 10 ms production step and over long desktop sessions?

### Measures

Measure at minimum:

- wall cost per simulated second;
- average and worst triggered-step cost;
- corrected-commit fraction;
- H.9 iteration/evaluation work;
- fallback/rollback frequency;
- telemetry/memory growth;
- sustained UI/control-room publication behavior;
- long-running session stability.

The production timestep must remain 10 ms. H.28 must not "solve" cost by changing timestep or weakening the numerical contract.

### Actual H.28 result

H.28 compiled and passed the ordinary suite but its focused performance gate returned `unbounded-regression`:

```text
median wall ratio       9.1252571494799053
p95 wall ratio        100.01553278882017
trigger average step  1702179.99 us
trigger allocation    43460418 bytes
```

Numerical safety/determinism remained green. This blocks H.29 but does not justify relaxing performance ceilings.

### H.28.1 attribution/optimization branch

Before H.29, insert a bounded branch:

1. **H.28.1-A — Performance Attribution:** measure historical explicit preparation, duplicate predictor, H.9 layout/residual/Jacobian/line-search, disagreement scan, authority and accounting without changing numerical behavior.
2. **H.28.1-B — Duplicate-Work Reduction:** only if attribution shows repeated predictor/base-path work is material; preserve exact trigger and committed trajectory.
3. **H.28.1-C — H.9 Allocation/Hot-Path Optimization:** only if attribution shows avoidable allocation/layout/buffer churn; preserve finite-difference Newton mathematics.
4. **H.28 rerun:** only a green rerun can move the branch toward H.29.
5. **One H.24 rerun after optimization stabilizes:** because B/C change committed-runtime implementation code, rerun the rare long-horizon qualification once after the optimization branch is final, not after every intermediate iteration. H.29 remains blocked until both H.28 and this post-optimization H.24 regression are green.

A numerically correct path may still remain opt-in if its runtime cost remains unsuitable after conservative optimization.

## H.29 — Production Activation Candidate

### Question

Given H.24–H.28 evidence, should the already-qualified corrected path become a separately reviewed production-default candidate?

### Scope

H.29 should not invent another numerical algorithm. It should integrate the already-qualified chain:

```text
P060/F040
  → four-node branch continuity
  → H.9 corrected candidate
  → H.20 eligibility / rollback
  → H.22 commit seam
```

Define and test:

- production configuration/default selection;
- immediate explicit kill/fallback path;
- production telemetry and rollback reason counters;
- save/replay/version compatibility;
- operator-facing versus internal diagnostic exposure;
- deployment/rollback compatibility with existing explicit mode.

`ExplicitCommittedState` must remain available as an operational rollback/reference mode.

## H.30 — Phase H Closure & Production Qualification Decision

### Question

What numerical policy should be authoritative at the end of Phase H?

### Cumulative gate

Re-evaluate the relevant validated evidence chain:

```text
H.19 numerical qualification
H.20 authority / rollback
H.21 orchestrator wiring
H.22 corrected ownership
H.23 replay / checkpoint / protection
H.24 committed long horizon / cross profile
H.25 protection / transient matrix
H.26 integrated rollback stress
H.27 off-design envelope
H.28 failed performance / soak evidence
H.28.1-A/B/C/D attribution and conservative optimization
H.28 green rerun required
H.29 activation candidate
```

### Legitimate closure outcomes

Phase H has three acceptable evidence-derived closure decisions:

1. **ACTIVATE** — corrected ownership becomes the standard current-v2 production policy.
2. **OPT-IN ONLY** — corrected ownership remains available but explicit stays default because cost/envelope/operational evidence does not justify default activation.
3. **REMAIN EXPLICIT** — corrected ownership remains research/diagnostic evidence only because the total risk/cost does not justify production use.

The roadmap must not assume outcome 1 in advance.

## Phase H closure criterion

Phase H is ready to close only when the project can state, with executable evidence:

> the chosen numerical policy works for long duration, preserves transient/protection behavior, fails closed safely, has a documented operating envelope, has acceptable runtime cost, and remains deterministic across replay/checkpoint/save authority.

Only after that closure should Phase I resume.
