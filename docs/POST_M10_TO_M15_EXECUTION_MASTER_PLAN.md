# Post-M10 to M15 Execution Master Plan

## Status

**PLANNING — adopt only after the currently running M10 final long validation is green and M10 is explicitly CLOSED.**

This document is the operational execution map from the M10 closeout through M15. The immediate closure handoff is specified separately in [`M10_FINAL_CLOSURE_AND_M11_BOOTSTRAP_PLAN.md`](M10_FINAL_CLOSURE_AND_M11_BOOTSTRAP_PLAN.md). It does not replace `PROJECT.md` as the current-state source and does not authorize changes to the active M10 long-validation candidate.

It consolidates the existing milestone plans with three pre-M11 source-driven engineering review streams:

- nuclear-code development / V&V review: phenomenon-scoped verification, integral qualification, non-regression, explicit limits;
- Digital I&C / human-system review: protection/control separation, deterministic logical time, human–automation allocation, software hazards, HMI failure modes, proportional dependency assurance;
- reactor-engineering operating-point review: coupled-state self-consistency, residual diagnosis before correction, explicit equilibrium/stability qualification and exact-version preservation.

The simulator remains an educational reduced-order plant model. No licensing-grade, hard-real-time, quantified software-reliability or safety-grade redundancy/diversity claim is introduced by this plan.

---

## 1. Immediate transition: finish M10 correctly

### T0-A — If the current long gate passes

The required order is:

1. preserve the complete long-run artifact set unchanged;
2. verify final markers, especially `m10-final-long-validation-passes=True` and `m10-closure-eligible=True`;
3. create a **documentation/closure-only** M10 final candidate stacked on the validated long baseline;
4. change the final V&V matrix row `LONG-SOAK-01` from pending to validated and record the exact long contract/workload as provenance;
5. record the already completed M10.9.8.5 manual acceptance as release provenance;
6. update `PROJECT.md`, `CHANGELOG.md`, roadmap/closure documents and the final M10 V&V summary;
7. run a closure validator proving no `src/` or production/test semantic changes occurred during the closure-only step;
8. declare **M10 CLOSED**;
9. only then overlay the pre-M11 planning/review documentation and promote that documentation checkpoint;
10. start M11.1 from the resulting closed/documented baseline.

### T0-B — If the current long gate fails

Do not widen a frozen tolerance merely to obtain green evidence.

Classify the failure first:

| Failure class | Required response |
| --- | --- |
| **Production physics/control defect** | minimal owner fix; rerun ordinary + affected focused owners + final cumulative gate; then rerun long gate in full. |
| **Application/HMI/persistence semantic defect** | minimal owner fix; rerun ordinary + affected owner/replay/compatibility evidence + final cumulative gate; then long gate in full. |
| **Long-test harness defect only** | fix harness/validator without changing frozen workload or acceptance criteria; cumulative gate remains valid if production and baseline tests are byte-identical; rerun long gate in full. |
| **Documentation/contract typo only** | fix documentation/validator; no physics requalification unless the executable contract changed. |
| **Unexpected resource/performance issue** | preserve evidence, diagnose cause, and decide whether it is a release blocker; no post-hoc reduction of workload merely to pass. |

Any production `src/` change after a validated cumulative gate invalidates that cumulative result for closure purposes.

---

## 2. Adoption checkpoint after M10 closure

Before M11.1 implementation, create one documentation-only checkpoint that imports and indexes:

- `PRE_M11_IMPLEMENTATION_DECISIONS.md`;
- `M11_DIGITAL_IC_RELEASE_ASSURANCE_PLAN.md`;
- `M13_DIGITAL_IC_DEGRADATION_AUTOMATION_TRANSPARENCY_PLAN.md`;
- this execution master plan;
- `CHANGE_IMPACT_REVALIDATION_POLICY.md`;
- `M11_RELEASE_EVIDENCE_MATRIX_PLAN.md`;
- machine-readable execution maps under `eng/`.

The adoption checkpoint must prove:

- zero `src/` changes;
- zero `tests/` changes;
- all documentation links resolve;
- the current `PROJECT.md` says M10 CLOSED and M11 is next;
- M11/M12/M13/M14/M15 milestone numbering and dependencies are internally consistent;
- M13.9 is Digital I&C Degradation & Automation Transparency and former Integrated UX Closure is M13.10.

---

# 3. M11 — Release Hardening

M11 is deliberately **feature-frozen**. Its job is to turn the closed M10 product into a reproducible release candidate with explicit support, compatibility, performance, packaging and human-system assurance.

## M11.1 — Release/support contract and architecture freeze

### Entry criteria

- M10 CLOSED;
- pre-M11 documentation checkpoint validated;
- final M10 27-row V&V matrix available as provenance.

### Work packages

**M11.1-A — Product/support identity**

Freeze:

- product/release version policy;
- supported OS/architecture target(s);
- framework-dependent vs self-contained publish decision;
- portable ZIP vs installer policy;
- minimum display/window and input expectations;
- user/session/log storage locations;
- support status for experimental features;
- blocker severity vocabulary.

**M11.1-B — Digital I&C architecture invariants**

Convert the review invariants into machine-readable and human-readable contracts. At minimum enforce:

- canonical ownership of physical state;
- control/protection separation;
- protection precedence;
- bounded supervisory authority;
- deterministic manual takeover;
- measured/true/diagnostic state distinction;
- 10 ms logical-time authority;
- no silent timestep dropping;
- immutable committed-state observation;
- HMI presentation/intent-only authority;
- exact identity/version/configuration discipline;
- no diversity claim without shared-dependency analysis.

**M11.1-C — Human–Automation Function Allocation**

Freeze the function-allocation matrix across detection, decision, actuation, confirmation, monitoring, override and takeover.

**M11.1-D — Dependency/COTS inventory**

Freeze direct runtime and test dependencies, version, role, packaging requirement and update policy.

### Planned artifacts

- `eng/m11-release-support-matrix.json`
- `eng/m11-digital-ic-architecture-invariants.json`
- `eng/m11-human-automation-function-allocation.json`
- `eng/m11-dependency-assurance-matrix.json`
- `docs/M11_RELEASE_SUPPORT_CONTRACT.md`
- `docs/M11_DIGITAL_IC_ARCHITECTURE_CONTRACT.md`
- `docs/M11_HUMAN_AUTOMATION_FUNCTION_ALLOCATION.md`
- focused validator/script for M11.1

### Exit gate

Build + ordinary suite + focused contract validator. No product feature is allowed to change in this slice.

---

## M11.2 — Compatibility and migration hardening

### Principle

Compatibility is semantic, not merely successful deserialization.

### Work packages

**M11.2-A — Compatibility inventory**

Enumerate every exact scenario/profile/session/checkpoint/archive identity that the release promises to read.

**M11.2-B — Archive schema decision**

Default decision: **retain session archive schema v1 for M11 unless a concrete release blocker proves v2 necessary**. A v2 migration is not undertaken for cosmetic cleanup alone.

**M11.2-C — Fingerprint identity**

Keep `sha256-control-room-snapshot-v1` immutable. Do not introduce fingerprint v2 in M11 unless a real compatibility requirement forces it.

**M11.2-D — Semantic replay sentinels**

Add compatibility evidence for:

- logical-step/action ordering;
- requested/effective authority;
- automation objective ordering;
- protection state;
- measured/provenance semantics;
- checkpoint prefix + continuation;
- action/history identity.

**M11.2-E — Unsupported/future data behavior**

Fail closed with precise operator-facing compatibility errors; never reinterpret historical exact identities.

### Planned artifacts

- `eng/m11-compatibility-matrix.json`
- compact historical fixtures/manifests only for explicitly supported inputs;
- compatibility audit script;
- documentation of supported/unsupported migration behavior.

### Exit gate

Every supported input has executable evidence; every unsupported/future case has deterministic fail-closed behavior; replay semantics are equivalent.

---

## M11.3 — Performance, memory and long-session release budgets

### Rule

**Measure first; optimize only if evidence justifies it.**

### Phase 1 — Characterization

Measure on a named release-reference workstation/configuration:

- simulation throughput;
- median/p95/worst observed desktop batch;
- input-to-visible-response latency where measurable;
- projection/publication cost;
- `PropertyChanged`/binding fan-out where material;
- managed allocation and GC behavior;
- long-session recorder/frame growth;
- archive/checkpoint/session size;
- save/load wall cost;
- startup-to-usable-control-room;
- workspace-switch responsiveness;
- archive export responsiveness;
- repeated load/reset/save cycles.

The just-completed M10 long gate is baseline evidence, not a substitute for release-HMI/performance characterization.

### Phase 2 — Budget freeze

Freeze release budgets **before** any optimization candidate is judged.

Budgets must distinguish:

- semantic blockers (no dropped logical time, no replay divergence, no incomplete evidence presented as complete);
- hard release ceilings;
- diagnostic performance targets.

### Phase 3 — Targeted optimization only if justified

Allowed candidates include:

- allocation reductions that preserve exact outputs;
- recorder view/copy reduction;
- fingerprint allocation reduction preserving v1 exact hash;
- persistence streaming if measurements prove material LOH/allocation pressure;
- UI projection/notification suppression when state is structurally unchanged;
- safe worker ownership only after a single-owner/immutable-handoff design is documented.

Not allowed in M11.3:

- near-zero hydraulic constitutive changes;
- new solver semantics;
- new physics;
- silent timestep/backlog dropping;
- opportunistic background access to mutable simulation/session state.

### Phase 4 — Requalification

Any accepted optimization reruns the affected owner tests, ordinary suite, replay/checkpoint equivalence and the relevant performance comparison. Production-semantic changes route out of M11.3.

### Exit gate

Release budgets frozen and green; no unbounded growth or semantic time loss; recorder evidence-failure policy explicitly decided.

---

## M11.4 — Packaging and supported-target assurance

### Decision checkpoint

M11.1 decides the supported target. Preferred first-release strategy should be the smallest target set that can be genuinely tested, not every platform Avalonia can theoretically run on.

### Work packages

- clean publish from source on supported target;
- runtime/dependency inclusion verification;
- assets/fonts/configuration verification;
- executable version/identity verification;
- clean-user-data first startup;
- save/reload in packaged form;
- no developer-path or source-tree dependency;
- packaging artifact inventory;
- optional deterministic SBOM-like metadata if low-cost and reproducible.

### Exit gate

A packaged artifact works from a clean target environment exactly as documented.

---

## M11.5 — Documentation, hazard and HMI assurance closure

### Work packages

- freeze the Digital I&C hazard catalog;
- close each hazard by automated evidence, representative task, explicit limitation or justified non-applicability;
- execute the classic HMI failure-mode checklist: data overload, keyhole effect, mode error, clumsy/opaque automation, missing feedback, misleading state;
- align README, manual, support contract, known limitations and packaged behavior;
- state non-claims explicitly: no hard-real-time, licensing, nuclear-grade COTS or unproven redundancy/diversity claim.

### Exit gate

No release-facing contradiction between code, package, manual, README, support statement or known limitations.

---

## M11.6 — Release-candidate acceptance and M11 closure

### Automated release tasks

- clean restore/build/test from release source;
- compatibility matrix;
- release performance/memory budgets;
- package verification;
- replay/checkpoint/session round-trip;
- deterministic configuration/dependency identity;
- zero-test-discovery fail-closed sentinel.

### Representative operator tasks

Perform on the packaged candidate:

1. clean startup and normal operation;
2. COMMANDS command → observed response;
3. authority degradation → manual takeover;
4. protection activation vs alarm acknowledgement/reset distinction;
5. degraded measurement/provenance interpretation;
6. checkpoint/save/reload/full replay;
7. keyboard navigation at minimum supported window;
8. workspace switching without loss of critical protection/authority context;
9. clean shutdown/restart and session recovery.

### Closure output

- release candidate identity;
- support matrix;
- compatibility statement;
- performance/memory statement;
- hazard/HMI closure summary;
- package acceptance record;
- final known limitations;
- `M11 CLOSED` only after all evidence agrees.

---

# 4. M12 — Extreme Operations Foundations

M12 changes physical/numerical foundations and therefore uses a stricter revalidation ladder than M11.

Execution order is now:

1. **M12.0 reference operating-point equilibrium & stability qualification**;
2. M12.1 flow-owner directionality inventory;
3. M12.2 near-zero hydraulic regularity/conditioning audit;
4. M12.3 extreme-state matrix;
5. M12.4 pump mechanical/electrical/thermal energy ownership;
6. M12.5 full-plant post-trip decay heat;
7. M12.6 integrity/stress primitives;
8. M12.7 physical `IncidentSeverity`;
9. M12.8 closure.

### Planning refinement

M12.0 exists specifically to prevent later extreme-state work from being built on an unexamined drifting reference point. Each M12 slice must begin with an **observational census** and freeze its pre-change evidence before changing equations or state ownership. Every production physics change must identify which rows of the M10 V&V provenance it invalidates and what replacement evidence is required.

M12 does not add user-visible leak/rupture/fire/core-damage consequence progression yet.

---

# 5. M13 — Control-Room Experience

Execution order:

1. M13.1 industrial control/presentation boundary;
2. M13.2 maintained handle/selector semantics;
3. M13.3 stable canonical-ID command target selection;
4. M13.4 first-class mimic viewport;
5. M13.5 versioned persistent mimic layout;
6. M13.6 workspace presets;
7. M13.7 real operating procedures;
8. M13.8 Instructor/Fault mode;
9. **M13.9 Digital I&C Degradation & Automation Transparency**;
10. **M13.10 Integrated UX Closure**.

### M13.9 implementation order

1. signal age/stale semantics;
2. deterministic delayed measurement/update;
3. lost/temporarily missing update;
4. inconsistent redundant indication training case;
5. delayed observed-response feedback;
6. automation intent/effective-state/reason transparency;
7. anti-keyhole persistent critical context;
8. representative part-task evaluation.

Every M13.9 information-degradation feature must remain logical-step deterministic, replayable and checkpoint/session compatible, and must reuse existing instrumentation/fault/evidence seams rather than create a network simulator.

---

# 6. M14 — Spatial Reactor

Execution order remains:

1. M14.1 quasi-spatial contract/fidelity limits;
2. M14.2 multi-zone/equivalent-channel-group core;
3. M14.3 rod/group spatial mapping;
4. M14.4 local/quasi-spatial evidence;
5. M14.5 selectable 2D educational layers;
6. M14.6 drill-down/trends;
7. M14.7 deterministic/replay/aggregate/performance/fidelity closure.

### Planning refinement

Before any spatial visualization is enabled, the model contract must declare whether each displayed local quantity is independently solved, derived, mapped or interpolated. The UI must not imply channel-by-channel neutronics when only aggregated zones exist.

---

# 7. M15 — Accident Progression & Consequence Models

M15 remains blocked until M12, M13 and M14 are closed.

Execution order:

1. M15.1 pressure-boundary degradation/leak/rupture;
2. M15.2 rotating-equipment degradation/failure;
3. M15.3 electrical damage/fire;
4. M15.4 core-damage prerequisite gate;
5. M15.5 bounded core-damage progression only if M15.4 passes;
6. M15.6 physical incident severity/post-incident integration;
7. M15.7 Instructor/spatial consequence presentation;
8. M15.8 integrated accident closure.

Every consequence must be physically owned and persistent. No scenario, challenge, HMI or Instructor label may directly manufacture a consequence that lacks a modeled initiating condition and deterministic state transition.

---

# 8. Cross-milestone execution discipline

## 8.1 One validated baseline at a time

Every slice stacks only on the latest explicitly validated baseline. Failed candidates become evidence, never new baselines.

## 8.2 Plan → implement → focused gate → ordinary gate → closure

For each slice:

1. write/update the contract and acceptance criteria first;
2. implement the smallest owner-consistent change;
3. run focused owner evidence;
4. run ordinary suite;
5. run replay/checkpoint/long/manual evidence when the change class requires it;
6. update documentation only after behavior is known;
7. promote only after all required gates are green.

## 8.3 No post-hoc tolerance tuning

A tolerance may change only because a new model/requirement is explicitly justified and re-baselined. It is never widened simply because a candidate failed.

## 8.4 Exact historical identities are immutable

Historical exact-version inputs, fingerprint algorithms and archive identities are never reinterpreted in place.

## 8.5 Claims remain bounded

Every physical or Digital-I&C claim states domain, evidence class and limitation. Internal verification is not presented as experimental validation.

---

# 9. Decision points that must not be made prematurely

The following are intentionally deferred to evidence-producing milestones:

- Windows-only vs additional release targets — M11.1;
- framework-dependent vs self-contained package — M11.1;
- archive schema v2 — default NO, revisit only in M11.2 if needed;
- fingerprint v2 — default NO, only explicit compatibility need;
- persistence streaming — M11.3 measurement first;
- background runtime/export workers — M11.3 architecture + measurement first;
- genuinely diverse protection system — separate future M5-owned design, not M11/M13;
- user-facing simulation-speed controls — separate product decision;
- multi-monitor/multi-computer — after M13;
- detailed severe-accident models — only through M15 prerequisite gates.

---

# 10. Definition of success for the next major transition

The immediate objective is not merely “start M11”. It is to reach a state in which:

- M10 is explicitly closed with cumulative + long evidence;
- the three source-driven engineering review streams are adopted as project planning contracts;
- M11 begins from a release-support freeze rather than from ad hoc optimization;
- every M11 change has a known revalidation route;
- post-release Digital I&C features have an owner and do not leak into release hardening;
- M12–M15 retain clear physical prerequisites and bounded claims.

That is the required launch point for M11.1.
