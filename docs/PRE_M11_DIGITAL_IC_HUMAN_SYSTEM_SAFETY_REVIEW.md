# Pre-M11 Digital I&C / Human-System Safety Review — Reviewed Planning Baseline

## Status and purpose

**Planning/review artifact only.** This review does not modify the M10 validated runtime, physics, control, protection, persistence, replay, HMI or long-validation workload. It is intended to inform M11 release hardening and post-M11 human-system work after M10 is formally closed.

Primary source reviewed: *Digital Instrumentation and Control Systems in Nuclear Power Plants: Safety and Reliability Issues*, National Research Council / National Academy Press, 1997.

The report is historically important but old. Its discussion of NRC positions, regulatory documents, specific platforms and technology state must **not** be treated as current regulatory authority. This review therefore separates:

- **source-derived engineering principles** that remain useful as design/review concepts;
- **project-specific consequences** for the educational Nuclear Reactor Simulator;
- **deferred items** that must not become feature work inside M11 merely because the report discusses them.

The simulator remains an educational reduced-order plant simulator. Nothing in this review upgrades it to a safety-related I&C product or licensing-grade simulator.

## 1. Source structure used

The most relevant parts of the report are:

- Chapter 3 — **Systems Aspects of Digital Instrumentation and Control Technology**;
- Chapter 4 — **Software Quality Assurance**;
- Chapter 5 — **Common-Mode Software Failure Potential**;
- Chapter 6 — **Safety and Reliability Assessment Methods**;
- Chapter 7 — **Human Factors and Human-Machine Interfaces**;
- Chapter 8 — **Dedication of Commercial Off-the-Shelf Hardware and Software**;
- Appendix F — **Selected Technical Issues**, especially Real-Time Processing, Data Communications, Multiplexing, Multitasking and Memory Sharing.

The report's overall framing is also useful: digital I&C should be treated as a systems problem, not as isolated software components. Interfaces, allocation of functions, communications, timing, redundancy/diversity, human roles, assurance and configuration management are coupled design concerns.

## 2. Main conclusions retained

### 2.1 Systems architecture must make ownership and interfaces explicit

Chapter 3 describes multilayer digital control architectures with local component control, higher-level supervisory coordination and plant-level supervision/analysis. It also emphasizes the traditional separation between protection and control and independent manual backup paths.

**Project consequence:** the current architecture is directionally correct and should be frozen more explicitly for release:

```text
M2/M3/M4 plant physics
        ↑ canonical actuators / plant state
M5 local control + protection + alarms
        ↑ typed authority/objective intents
M5 supervisory coordination
        ↓ immutable committed state / measured evidence
Application projection / replay / mission evidence
        ↓
Avalonia HMI
```

The HMI, MISSION layer, recorder, challenge/scoring and Application projections must remain non-owners of plant physics and protection. Supervisory automation must continue to act through existing control seams, never by writing physical outcomes directly.

### 2.2 Timing is part of functional correctness for control behavior

Appendix F defines real-time correctness as depending on both logical result and when that result is produced. It stresses worst-case rather than average timing for critical loops, predictable execution, bounded delays and explicit handling of missed deadlines.

**Project consequence:** the simulator is not a hard-real-time reactor control system, but its **logical-time semantics** should be treated as a first-class contract:

- 10 ms deterministic simulation step remains authoritative;
- wall-clock pacing and UI cadence are not simulation authority;
- protection pickup, control response, fault activation, accepted action replay and authority transfer are specified at exact logical-step boundaries;
- M11.3 performance work must not change logical-step ordering or silently drop deterministic time;
- any background/worker redesign must preserve single runtime ownership and immutable snapshot handoff.

This supports the existing M11.3 plan to measure the host rather than opportunistically introducing concurrency.

### 2.3 Protection and normal/supervisory control must remain distinct

Chapter 3 treats vertical independence between protection and control as a major system feature. Chapter 5 shows why redundant digital functions can share hidden common failure modes.

**Project consequence:** preserve these invariants:

- protection outranks Manual/Assisted/Supervisory normal control;
- supervisory automation never resets SCRAM/turbine/generator trips;
- alarm acknowledge/reset is not protection reset;
- invalid required measurements degrade automation fail-closed;
- manual takeover stops new supervisory decisions before handing authority to the operator;
- MISSION/challenge state can observe a trip but cannot own or cause it directly except through already canonical scenario/fault inputs.

### 2.4 Redundancy is not the same as diversity

Chapter 5 distinguishes duplication, design diversity and functional diversity. It also reports experimental evidence that separately developed programs implementing the same requirements can still fail on correlated inputs; different languages, algorithms or development teams do not justify assuming independent failures.

**Project consequence:** the simulator must not create **diversity theater**. Two implementations of the same trip criterion are not automatically independent. If future educational work models diverse protection, the review must identify:

`hazard → sensing basis → functional requirement → actuation path → shared dependencies`

and call something *functionally diverse* only when the component-level requirements/principles are actually different.

For M11 this remains documentation/review only; no new backup protection feature is authorized.

### 2.5 Testing is necessary but cannot be the sole assurance technique

Chapter 4 explicitly concludes that exhaustive testing is infeasible for practical systems and that testing must not be the sole SQA technique. It emphasizes requirements inspection, systematic review, functional/boundary testing, system-level hazard analysis and rigorous configuration management.

**Project consequence:** retain the strong automated suite, but M11 release assurance should also include:

- frozen support and compatibility requirements;
- reviewable architecture invariants;
- digital-I&C hazard catalog;
- configuration/version/dependency inventory;
- current manual HMI acceptance evidence;
- explicit known limitations and support statement;
- release-candidate testing on representative packaged behavior.

This is a useful correction to a purely test-count-driven interpretation of quality.

### 2.6 Requirements errors deserve at least as much attention as code defects

The SQA chapter cites safety-critical experience indicating that requirements problems are a major source of serious defects and that early-life-cycle assurance is weak if attention is concentrated only on source code.

**Project consequence:** for M11, several contracts should be treated as requirements artifacts rather than implementation notes:

- supported OS/runtime/publish model;
- exact persisted schema/fingerprint compatibility;
- requested vs effective authority semantics;
- measured vs true-state provenance;
- failure behavior of recorder/session/archive operations;
- minimum-window/keyboard/HMI behavior;
- release-blocker classification.

A release gate should fail when implementation, manual and support statement disagree even if unit tests are green.

### 2.7 Hazard analysis should extend into software and human-system interaction

Chapter 4 recommends system-level hazard analysis that determines software contribution to hazardous states. Chapter 7 recommends reviews against recurring human-system deficiencies rather than relying only on low-level UI guidelines.

**Project consequence:** create a project-specific **Digital I&C Hazard Catalog** covering at least:

- measurement validity/staleness/provenance;
- mode/authority confusion;
- protection/control priority;
- accepted-command / observed-response mismatch;
- replay/checkpoint inconsistency;
- stale or inconsistent presentation;
- host timing/backlog;
- evidence/recorder failure;
- keyhole/data-overload/clumsy-automation HMI risks;
- persistence/configuration drift.

The catalog is an engineering review aid, not a nuclear PRA and does not assign invented failure probabilities.

### 2.8 Human functions must be designed, not left as leftovers

Chapter 7 criticizes designs in which hardware/software are designed first and the operator is left to fill the remaining gaps. It calls for human roles and activities to be specified with rigor comparable to hardware/software roles.

**Project consequence:** freeze a **Human–Automation Function Allocation** matrix. For each operational function, state who detects, decides, acts, confirms, monitors, can override and must take over among:

- plant/local control;
- protection;
- supervisory automation;
- operator;
- scenario/fault/training layer;
- Application/HMI observation.

This converts existing M10 contracts into an explicit review surface and should be referenced by future control-room work.

### 2.9 Classic HMI failure modes should become a permanent checklist

Chapter 7 identifies recurring computer-based HMI deficiencies including:

- data overload;
- keyhole effect;
- poor workload allocation;
- mode errors;
- failures amplified by tightly coupled systems;
- clumsy automation;
- automation that is capable but insufficiently communicative;
- operator roles defined by default rather than design.

**Project consequence:** create a permanent HMI checklist and apply it to any change that alters operator-visible information or response to operator input. The report specifically argues that such changes require empirical evaluation, not only static checklist review.

The existing M10.9.8.5 manual acceptance is therefore correctly classified as real release evidence rather than cosmetic QA.

### 2.10 Immutable snapshots are a strong answer to shared-state consistency concerns

Appendix F notes that shared memory/data requires management so consumers see valid and consistent data. It also lists distributed communication failure modes such as lost/late/misdirected messages, rollback-related orphan messages and inconsistent messages to different receivers.

**Project consequence:** the current committed immutable snapshot boundary is a significant architectural strength:

```text
single committed simulation step
        ↓
immutable snapshot/evidence
        ↓
control-room projection / recorder / replay / UI consumers
```

Do not replace it with ad-hoc mutable shared state. If M11.3 introduces background work, the handoff must remain immutable and version/logical-step identifiable.

### 2.11 Do not invent quantitative software reliability claims

Chapter 6 discusses deterministic and probabilistic assessment but also documents controversy and uncertainty around assigning software failure probabilities. Its strongest transferable lesson for this project is methodological rather than numerical: include digital/software failure modes in safety reasoning, document assumptions, use deterministic hazard analysis, and expose uncertainty rather than hiding it.

**Project consequence:** do **not** add a fake `99.999% reliable` metric to the simulator or documentation. For this educational project, deterministic contracts, fault scenarios, coverage matrices and explicit limitations are more defensible than invented software failure probabilities.

### 2.12 COTS assurance should be proportional to significance and complexity

Chapter 8 concludes that assurance activities for commercial hardware/software should be commensurate with safety significance and application complexity.

**Project consequence:** M11 should maintain a modest dependency assurance inventory, not attempt nuclear-grade qualification of .NET or Avalonia. The current direct dependencies identified in the candidate are:

- .NET SDK/runtime contract (`net10.0`, SDK baseline in `global.json`);
- Avalonia 12.1.0 packages used by the desktop App;
- xUnit v3 3.2.2 / Microsoft.Testing.Platform for test-only use.

For each dependency, record purpose, release impact, whether it can alter runtime semantics, packaging requirements, version freeze and representative release verification.

## 3. What changes in the M11 plan

The existing M11 sequence remains valid and feature-frozen. The review **does not add a new M11 feature milestone**. Instead it strengthens the acceptance content of existing milestones.

### M11.1 — Release/support contract and version freeze

Add:

- Digital I&C architecture invariants;
- Human–Automation Function Allocation reference;
- direct dependency/COTS inventory;
- explicit terminology for safety-related educational claims and non-claims.

### M11.2 — Compatibility/migration hardening

Add checks that persisted evidence preserves:

- authority/objective intent ordering;
- exact scenario/profile identity;
- snapshot/fingerprint algorithm identity;
- logical-step ordering;
- no reinterpretation of historical measured/protection state.

Treat rollback/replay inconsistency as an information-consistency hazard, not merely a serialization bug.

### M11.3 — Performance, memory and long-run release budgets

Add measurement requirements for:

- worst observed desktop batch duration, not only average;
- UI input/render responsiveness under representative load;
- projection/PropertyChanged fan-out;
- long-session recorder/archive growth;
- evidence of backlog or skipped/dropped deterministic time;
- immutable handoff integrity if worker execution is ever proposed.

No worker/background redesign is authorized merely by this review.

### M11.4 — Packaging/publish/deployment

Add:

- dependency/runtime inventory verification;
- packaged version reporting;
- exact asset/font/runtime dependency checks;
- supported-target statement narrowed to targets actually tested.

### M11.5 — Documentation/manual alignment

Add:

- Digital I&C hazard catalog;
- HMI recurring-deficiency checklist;
- architecture/authority/protection wording consistency checks;
- clear educational non-claims.

### M11.6 — Final release-candidate acceptance

Use representative operator tasks with actual simulation dynamics, not only static UI inspection. Re-run a compact subset covering:

- mode/authority transitions;
- protection priority;
- degraded measurement;
- manual takeover;
- command feedback;
- session save/load/replay;
- HMI navigation at minimum supported window.

This is directly aligned with the report's preference for performance-based human-system evaluation.

## 4. What remains post-M11

The report suggests several potentially useful future capabilities, but M11 is feature-frozen. Therefore the following are **backlog only**:

- delayed/stale/lost/inconsistent measurement-update scenarios;
- communication-latency or message-loss training faults;
- explicitly diverse protection-function modeling;
- richer operator workload/attention experiments;
- formal part-task HMI experiments beyond current manual acceptance;
- any new controller/network real-time simulation.

These require their own design/validation milestones and must not be smuggled into release hardening.

## 5. Review outcome

The second review changes the project less than it may first appear because many recommended principles are already embodied in M10. The value is that those principles are now **named, traceable and reviewable**.

Current strengths confirmed by the source:

- deterministic fixed logical time;
- explicit owner boundaries;
- measured-state discipline;
- protection priority;
- requested/effective authority separation;
- fail-closed degraded automation;
- manual takeover;
- immutable snapshots;
- replay/checkpoint exactness;
- manual HMI acceptance;
- versioned persistence and frozen compatibility evidence.

Main gaps exposed for release hardening:

1. architecture invariants are distributed rather than frozen in one Digital I&C contract;
2. operator/automation function allocation is distributed rather than tabulated;
3. no single Digital I&C hazard catalog exists;
4. recurring HMI failure modes are not yet a permanent regression checklist;
5. dependency/COTS assurance is implicit rather than an M11 release artifact;
6. performance work should include worst-observed host responsiveness and timing semantics, not only average throughput.

## 6. Recommendation

Adopt the companion artifacts produced with this review as **M11 planning inputs**, after M10 closure:

- `DIGITAL_IC_ARCHITECTURE_INVARIANTS.md`;
- `HUMAN_AUTOMATION_FUNCTION_ALLOCATION.md`;
- `DIGITAL_IC_HAZARD_CATALOG.md + `../eng/pre-m11-digital-ic-hazard-catalog.json``;
- `HMI_CLASSIC_FAILURE_MODES_CHECKLIST.md`;
- `M11_COTS_DEPENDENCY_ASSURANCE_PLAN.md`;
- `M11_PLUS_DIGITAL_IC_BACKLOG.md`;
- `../eng/pre-m11-digital-ic-review-traceability.csv`.

Promotion into authoritative project documentation should occur only after the current M10 final long gate and explicit M10 closure.
