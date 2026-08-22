# Pre-M11 Implementation Decisions

## Status

**PLANNING — apply only after M10 is explicitly CLOSED.**

This document converts the three pre-M11 source-driven engineering review streams into implementation decisions. It does not modify the active M10 final long-validation candidate and is not promotion evidence.

Source review streams (full provenance: [`research/PRE_M11_ENGINEERING_REVIEW_SOURCES.md`](research/PRE_M11_ENGINEERING_REVIEW_SOURCES.md)):

- *Nuclear Power Plant Design and Analysis Codes: Development, Validation, and Application* — used for verification/validation structure, phenomenon-by-phenomenon qualification, non-regression, integral testing and explicit model limitations.
- *Digital Instrumentation and Control Systems in Nuclear Power Plants: Safety and Reliability Issues* — used for digital-I&C architecture, software assurance, human–automation allocation, common-mode/diversity reasoning, HMI failure modes, timing and proportional COTS assurance.
- Lamarsh & Baratta, *Introduction to Nuclear Engineering*, 3rd ed. — Section 8.6 is used for the coupled-state self-consistency principle behind the planned operating-point equilibrium work; reactor-type-specific numerical values/correlations are not imported.

The source material is used as engineering guidance only. Nuclear Reactor Simulator remains an educational reduced-order simulator and does not claim licensing-grade nuclear analysis, safety-I&C qualification, hard-real-time behavior or quantified software reliability.

The detailed execution order, pass/fail branching after the M10 long gate, M11 work packages and later M12–M15 dependencies are defined in [`POST_M10_TO_M15_EXECUTION_MASTER_PLAN.md`](POST_M10_TO_M15_EXECUTION_MASTER_PLAN.md). Revalidation scope is governed by [`CHANGE_IMPACT_REVALIDATION_POLICY.md`](CHANGE_IMPACT_REVALIDATION_POLICY.md).

## 1. Decisions that are now committed

### 1.1 M11 remains feature-frozen

M11 is still **Release Hardening**. The reviews do not authorize new physics, a new protection system, network simulation, new accident phenomena or a control-room redesign.

The review-derived work implemented in M11 is assurance and release evidence:

1. freeze architecture and function-allocation contracts;
2. freeze compatibility and configuration identities;
3. measure timing, memory and long-session behavior;
4. verify packaged dependencies and supported targets;
5. add deterministic digital-I&C hazard review and HMI failure-mode acceptance;
6. run representative operator tasks on the release candidate.

### 1.2 Qualification language becomes explicit

Every release claim must distinguish at least:

- **VERIFICATION** — implementation/numerical/contract correctness;
- **MODEL ASSESSMENT / VALIDATION EVIDENCE** — comparison against an external or independently defined reference when available;
- **INTEGRAL SYSTEM QUALIFICATION** — whole-plant behavior inside an explicitly bounded educational domain;
- **USER/HMI ACCEPTANCE** — operator-facing behavior under representative tasks.

A green test suite alone is not sufficient reason to call the simulator or a model “validated”.

### 1.3 The 27-row M10 final V&V matrix becomes release provenance

After M10 closure, the final V&V matrix is retained as the release starting point. Future changes must identify which phenomenon rows they touch and what regression evidence remains authoritative.

The release documentation must state qualified ranges and known limitations instead of making general claims of physical accuracy.

### 1.4 Digital-I&C architecture invariants become a formal release contract

M11.1 will freeze a human-readable and machine-readable contract covering at least:

- one canonical owner for each physical state/inventory;
- strict separation of normal/local control and protection;
- supervisory automation operating only through existing bounded control seams;
- protection precedence over Manual/Assisted/Supervisory Automatic normal control;
- deterministic manual takeover;
- training assistance orthogonal to plant authority;
- measured state, true state and diagnostics kept distinct;
- 10 ms logical simulation time as authoritative semantic time;
- no silent dropping of deterministic simulation time;
- immutable committed-state handoff;
- recorder/replay/checkpoint observational/versioned ownership;
- HMI as presentation/intent-dispatch authority only;
- exact-version/configuration identity as part of behavior;
- no redundancy/diversity claim without shared-dependency analysis.

Any M11 change that violates one of these rules is a release blocker or requires an explicit ADR and new validation plan.

### 1.5 Human–automation function allocation becomes explicit

M11.1 will freeze a matrix that identifies, for each operational function:

`detects → decides → acts → confirms → monitors → can override → must take over`

across plant/local control, protection, supervisory automation, operator, fault/training layer and Application/HMI observation.

Release hardening may not silently move responsibility from one owner to another.

### 1.6 Digital-I&C hazard review becomes part of release assurance

M11 will maintain a deterministic hazard catalog. It is not a PRA and assigns no invented failure probabilities.

At minimum the release gate must address:

- invalid measurement with hidden true-state fallback;
- stale/provenance ambiguity;
- requested/effective authority confusion;
- incomplete manual takeover;
- normal-control/protection priority inversion;
- alarm acknowledgement confused with protection reset;
- accepted command without clear observed response;
- keyhole/data-overload/mode-error/clumsy-automation risks;
- UI responsiveness delay while logical simulation remains correct;
- silent deterministic timestep loss;
- mutable/concurrent state inconsistency;
- replay/checkpoint/history inconsistency;
- recorder evidence failure presented as complete evidence;
- archive-save integrity;
- runtime/dependency/configuration drift;
- “zero tests executed” or route-discovery gaps being mistaken for green validation;
- unsupported software reliability/safety probability claims.

### 1.7 Timing is part of semantic correctness where order matters

The desktop application is **not** declared hard real-time. However, M11 treats exact logical-step ordering as part of correctness for:

- fault activation;
- command acceptance/application boundaries;
- protection commit/observation;
- authority degradation/takeover;
- recording/replay/checkpoint reconstruction.

M11.3 additionally characterizes wall-clock responsiveness as release performance evidence, without allowing wall clock to become simulation authority.

### 1.8 COTS/dependency assurance is proportional

M11 will not attempt nuclear-grade qualification of .NET, Avalonia or the test stack.

It will instead freeze and verify a dependency matrix containing at least:

- dependency/version;
- runtime vs test-only role;
- whether it can affect runtime semantics;
- packaging/runtime requirement;
- supported-target verification;
- update policy.

A runtime dependency update is not accepted as harmless version drift without representative release verification.


### 1.9 Operating-point equilibrium becomes explicit engineering evidence

The project now distinguishes:

```text
conservation closure
!= bounded trajectory
!= physical/closed-loop equilibrium
```

The current exact-v4 300 s reference remains valid evidence for its frozen budgets, but it is not reinterpreted as proof of asymptotic steady state. The detailed design is in [`REFERENCE_OPERATING_POINT_EQUILIBRIUM_AND_STABILITY_PLAN.md`](REFERENCE_OPERATING_POINT_EQUILIBRIUM_AND_STABILITY_PLAN.md).

Committed rules:

- implement an observational residual inspector before any general trimmer;
- do not reinterpret the historical M4.7 `FullPlantSteadyStateCriteria` or supervisory `HoldCurrentOperatingPoint` names as equilibrium certification;
- keep M11 feature-frozen;
- formal reusable equilibrium/stability tooling belongs to new **M12.0** before the existing M12.1 physical-envelope work;
- a minimal test/harness-only residual census may be used earlier if required to diagnose the current M10 long-gate blocker;
- any production seed repair creates a new exact version rather than overwriting exact `@4`;
- model parameters, protection thresholds and V&V tolerances are not trim variables.

## 2. Concrete M11 implementation map

| Milestone | Review-derived implementation |
| --- | --- |
| **M11.1** | Freeze Digital-I&C architecture invariants, Human–Automation Function Allocation, release non-claims and runtime/COTS dependency inventory. |
| **M11.2** | Extend compatibility evidence to authority/objective intent ordering, logical-step ordering, measured/protection state semantics and exact fingerprint/schema algorithm identity; add action/history consistency sentinel. |
| **M11.3** | Measure worst observed runtime batch, UI responsiveness, projection/notification fan-out, recorder growth, persistence cost and evidence-failure policy; prove no silent deterministic-time dropping. |
| **M11.4** | Verify packaged runtime/dependencies/assets on supported targets; optionally export SBOM-like metadata if cheap and deterministic. |
| **M11.5** | Publish Digital-I&C hazard catalog, HMI classic-failure-mode checklist and explicit non-claims/limitations; mechanically verify documentation/configuration alignment. |
| **M11.6** | Close hazards with automated evidence plus representative tasks: authority degradation/takeover, command/observed response, protection vs ACK/reset, session/replay, keyboard/minimum-window and clean packaged startup/use/save/reload. |

## 3. Product features committed after release hardening

The second review identified valuable functionality that should **not** be squeezed into M11. It is assigned to M13 because it primarily concerns instrumentation presentation, operator situation awareness, training faults and human–automation interaction.

### M13.9 — Digital I&C Degradation & Automation Transparency

M13 will gain a dedicated implementation slice before integrated UX closure.

Committed candidates:

1. **Signal age / stale-value semantics** — measured evidence can be valid in representation but too old for a decision.
2. **Deterministic delayed measurement/update fault** — delay authored in logical steps, not a nondeterministic network model.
3. **Lost update / temporarily missing telemetry** — reuse canonical instrumentation quality/provenance.
4. **Inconsistent redundant indication training case** — explicitly model source/provenance/agreement; do not imply safety-grade redundancy.
5. **Delayed command-feedback training case** — delay observation/feedback only; canonical plant command execution remains owned by the existing command/control path.
6. **Automation transparency contract** — where applicable expose intent, effective state, inhibit/degradation/reason and observed result.
7. **Anti-keyhole persistent-context review** — critical protection/authority/quality context must remain understandable across workspaces without creating one giant all-controls page.
8. **Part-task human-system evaluation** — representative simulator tasks under healthy, degraded and protection/takeover conditions; no invented psychometric precision.

M13.9 must reuse the existing deterministic fault framework and immutable evidence boundaries. It must not create a second physical-state owner or a general distributed-network simulator.

Existing **M13.9 Integrated UX Closure** is therefore renumbered to **M13.10** in the proposed roadmap.

## 4. Explicitly not committed

The reviews do **not** authorize the following work:

- a hard-real-time scheduler or claim that the desktop simulator is a hard-real-time control system;
- a realistic Ethernet/fieldbus/network stack merely to simulate message delay/loss;
- nuclear-grade qualification of .NET/Avalonia/COTS packages;
- an invented quantitative software failure probability;
- a duplicated “backup protection algorithm” presented as independent merely because its code differs;
- new physical protection channels without a dedicated physical/protection-fidelity design;
- UI logic that owns protection, control or plant physics;
- weakening replay, conservation, protection or exact-version compatibility to meet performance targets.

## 5. Protection diversity disposition

A **Protection Diversity Inventory** is committed as documentation/review work before any future diversity claim. For each protection function it should record:

`hazard → sensing basis → trip criterion → shared measurements → shared algorithms/models → actuation path → claimed diversity level`

The inventory may conclude that no diversity claim is justified. That is an acceptable result.

Implementation of a genuinely diverse educational protection mechanism is **not currently committed to M13**. If later approved, it must be owned by the existing M5 protection domain through a dedicated post-release design/validation milestone, not by HMI or Instructor mode.

## 6. Release non-claims to preserve

The final product documentation must remain explicit that:

- Nuclear Reactor Simulator is educational and reduced-order;
- the desktop host is soft-real-time / wall-clock paced, while simulation semantics use deterministic 10 ms logical time;
- the project does not claim licensing-grade nuclear analysis;
- modeled LOCA/blackout/fault behavior is bounded educational qualification, not general severe-accident analysis;
- no safety-grade redundancy/diversity claim is made unless explicitly established later;
- communication delay/loss/staleness is not modeled until the dedicated post-release implementation exists;
- green regression evidence applies only to the declared qualified range.

## 7. Adoption rule

This planning contract becomes project documentation only after:

1. M10 final long validation passes;
2. M10 is explicitly promoted to CLOSED;
3. the documentation-only merge is stacked on that validated M10 baseline;
4. link/index/documentation validation passes.

Until then it remains external planning and must not alter the active long-run candidate.
