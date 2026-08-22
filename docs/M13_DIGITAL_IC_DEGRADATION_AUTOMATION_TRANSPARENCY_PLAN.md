# M13.9 — Digital I&C Degradation & Automation Transparency — Detailed Plan

## Status

**PLANNED — post-M11 feature work.** Prerequisite: M13.1–M13.8 complete on the validated post-release baseline.

## Goal

Teach and expose realistic digital-instrumentation/human-automation failure modes without turning the simulator into a distributed-network emulator and without moving plant/control/protection ownership into the HMI.

## Fixed constraints

- deterministic 10 ms logical time remains authoritative;
- faults are authored in logical steps;
- reuse the canonical instrumentation/fault framework;
- true state, measured state, signal quality and presentation remain distinct;
- no second plant-state owner;
- no hidden manual-only injector in normal operator mode;
- Instructor/Fault authority remains visibly distinct;
- communication faults affect evidence/measurement/feedback only unless an existing owning controller explicitly consumes that degraded evidence;
- protection ownership remains M5;
- replay/checkpoint/session must reconstruct every authored degradation exactly;
- no claim of hard-real-time communications or safety-grade redundancy.

## M13.9.1 — Signal age and stale-value semantics

Introduce explicit age/freshness evidence where operationally meaningful.

Candidate contract:

```text
MeasurementValue
Quality
SourceLogicalStep
AgeSteps / AgeSeconds
FreshnessState = Fresh | Stale | Unavailable
```

A stale value is not automatically `Unavailable`; the consuming function owns its freshness requirement. Required supervisory evidence must fail closed when outside its declared freshness bound.

### Acceptance

- same seed/action/fault schedule yields identical age/freshness transitions;
- UI never presents stale value as fresh;
- replay/checkpoint reproduces source step and freshness;
- no direct true-state fallback.

## M13.9.2 — Deterministic delayed measurement/update fault

Add a fault definition that delays delivery of selected instrumentation evidence by an exact number of logical steps.

The implementation models **delivery delay**, not packet/network physics.

### Acceptance

- authored delay is exact in logical steps;
- controller/HMI receives the same delayed evidence stream on replay;
- clearing/recovery rules are explicit;
- protection behavior changes only through its existing measurement ownership.

## M13.9.3 — Lost update / temporarily missing telemetry

Add bounded deterministic update loss using existing signal-quality/provenance semantics.

### Acceptance

- no mutation of physical true state;
- missing update cannot silently become a held “fresh” reading;
- recovery is explicit;
- operator can identify unavailable/stale quality.

## M13.9.4 — Inconsistent redundant indication training case

Allow two educational indications of the same underlying quantity to disagree through explicitly different observation paths.

This is a **training/provenance feature**, not a claim of safety-grade redundant instrumentation.

### Acceptance

- each indication has a stable source identity;
- disagreement is visible and replayable;
- no automatic “majority truth” is invented unless a dedicated owning model defines it;
- documentation states that redundant display does not establish protection independence/diversity.

## M13.9.5 — Delayed command observed-response evidence

Model delayed **feedback/observation** after a canonical plant command while keeping command acceptance/execution semantics unchanged.

### Acceptance

- command remains accepted/rejected by canonical runtime logic at the original logical boundary;
- delayed feedback cannot cause duplicate command execution;
- UI distinguishes pending/unknown response from confirmed plant effect;
- replay preserves command and feedback timing separately.

## M13.9.6 — Automation transparency contract

For automation that materially affects operator responsibility, expose the concepts that actually exist in the owning model:

```text
Intent / Objective
RequestedAuthority
EffectiveAuthority
State / Mode
InhibitOrDegradationReason
ObservedResult / Response
```

Not every surface needs every field; the contract forbids hiding a concept that is necessary to understand an authority change or required takeover.

### Acceptance

- automation transition is understandable without reading source/debug logs;
- degraded/suspended state cannot resemble normal operation;
- HMI derives from canonical evidence only;
- no new control authority is introduced by the presentation.

## M13.9.7 — Anti-keyhole persistent situation context

Audit the F1–F8/workspace system against critical context fragmentation.

Persistently or immediately discoverably retain at least:

- protection/trip state;
- requested/effective authority and degradation;
- critical signal quality affecting current decisions;
- plant run/pause/session identity as appropriate;
- first-out/active alarm significance where relevant.

Do not replace the subsystem/workspace model with a giant all-controls page.

## M13.9.8 — Human-system part-task evaluation

Run deterministic representative tasks rather than relying only on screenshot/static review.

Task families:

- healthy assisted/supervisory control;
- degraded measurement with responsibility transfer;
- protection activation and post-trip understanding;
- Manual takeover;
- delayed/missing indication diagnosis;
- delayed command-feedback diagnosis;
- navigation while retaining critical context.

Measure task outcome and observable interaction failures. Do not invent psychological reliability percentages.

## Protection diversity inventory

M13.9 may consume or update the project Protection Diversity Inventory for presentation/training purposes, but it does **not** authorize a second protection channel.

Any future functionally diverse protection mechanism requires a separate M5-owned design and validation milestone.

## Replay/persistence contract

Every new degradation/training state must be either:

- derivable deterministically from exact scenario/fault definitions plus canonical history; or
- persisted in an explicit versioned Application contract when derivation is insufficient.

Opaque physical-state checkpoint blobs remain forbidden.

## M13.9 exit gate

Close only when:

- all new degradation definitions are deterministic and versioned;
- same-seed, full replay and checkpoint continuation are equivalent;
- control/protection/physics ownership remains unchanged;
- HMI provenance/freshness/authority semantics pass automated and manual gates;
- classic HMI failure-mode checklist is rerun;
- ordinary suite remains green.

After M13.9, proceed to **M13.10 Integrated UX Closure**.
