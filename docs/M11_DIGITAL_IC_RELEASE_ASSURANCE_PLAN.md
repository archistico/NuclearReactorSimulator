# M11 Digital I&C Release-Assurance Plan


Execution sequencing: [`POST_M10_TO_M15_EXECUTION_MASTER_PLAN.md`](POST_M10_TO_M15_EXECUTION_MASTER_PLAN.md).  
Release-readiness rows: [`M11_RELEASE_EVIDENCE_MATRIX_PLAN.md`](M11_RELEASE_EVIDENCE_MATRIX_PLAN.md).  
Change/rerun rules: [`CHANGE_IMPACT_REVALIDATION_POLICY.md`](CHANGE_IMPACT_REVALIDATION_POLICY.md).
## Status

**PLANNED — feature-frozen M11 augmentation.**

This document defines how the Digital I&C / Human-System Safety review is implemented during M11 without turning release hardening into new control-system feature development.

## 1. M11.1 — Architecture, support and configuration freeze

### Deliverables

- `eng/m11-digital-ic-architecture-invariants.json`;
- `eng/m11-human-automation-function-allocation.json`;
- `eng/m11-runtime-dependency-matrix.json`;
- human-readable support/non-claim statement;
- blocker severity mapping for Digital-I&C hazards.

### Gate

Fail if a release change:

- creates a second physical owner;
- weakens control/protection separation;
- allows hidden true-state fallback;
- makes wall clock authoritative;
- introduces mutable shared runtime state without immutable handoff;
- reinterprets exact historical identity;
- claims unsupported redundancy/diversity or software reliability.

## 2. M11.2 — Compatibility and historical consistency

Extend the existing compatibility matrix to verify that supported persisted evidence preserves:

- exact scenario/profile identity;
- logical-step/action ordering;
- requested/effective authority semantics;
- automation objective/intent ordering where persisted;
- protection state semantics;
- measured/provenance semantics represented by the persisted contract;
- fingerprint algorithm identifier and immutable v1 expected values;
- checkpoint/replay prefix consistency.

### Required sentinel

A supported archive/replay route must never reconstruct a different effective authority/protection/history while still passing a structural deserialization test.

Unsupported future schema/algorithm identities fail closed with a precise support error.

## 3. M11.3 — Timing, responsiveness, memory and evidence integrity

### Semantic timing

Retain:

- deterministic 10 ms logical steps;
- no silent step dropping;
- exact action/fault/protection/authority ordering;
- immutable committed-state consumption.

### Wall-clock characterization

Measure at least:

- median/p95/worst observed desktop runtime batch;
- worst representative input-to-visible-response latency where measurable;
- snapshot projection/publication cost;
- `PropertyChanged` fan-out or equivalent binding work;
- recorder/fingerprint cost;
- long-session memory/GC/LOH growth;
- archive size and save/load/export cost;
- startup-to-usable-control-room;
- representative workspace-switch responsiveness.

Do not advertise a hard-real-time guarantee.

### Recorder evidence-failure decision

Before M11.3 closure explicitly choose and test one policy:

1. evidence failure faults/stops the relevant host/session path; or
2. plant execution may continue but recorder becomes visibly **COMPROMISED**, authoritative completion/save is blocked and partial evidence cannot be presented as complete.

Silent loss of authoritative recording evidence is forbidden.

## 4. M11.4 — Packaging and dependency assurance

For each supported target verify:

- exact .NET/runtime policy;
- Avalonia/native/runtime assets;
- package version reporting;
- fonts/resources;
- writable data/session/log paths;
- clean start/run/pause/command/save/reload/replay;
- dependency inventory matches the supported build.

A lightweight SBOM/dependency export may be added if it is deterministic and low-risk. It is not a release blocker unless the support policy makes it one.

## 5. M11.5 — Digital-I&C documentation and HMI review

Publish/freeze:

- Digital-I&C architecture invariants;
- Human–Automation Function Allocation;
- Digital-I&C Hazard Catalog;
- HMI Classic Failure Modes Checklist;
- COTS/dependency assurance statement;
- explicit hard-real-time/redundancy/licensing non-claims;
- communication/staleness limitations until M13.9.

### HMI checklist themes

At minimum review:

- data overload;
- keyhole effect;
- mode errors;
- clumsy automation/workload transfer;
- “strong and silent” automation;
- protection/authority visibility;
- signal quality/provenance;
- separation of ACK/reset/protection-reset operations.

## 6. M11.6 — Representative release-candidate tasks

The final manual/system acceptance should include at least these task families on the packaged release candidate:

1. **Healthy control task** — start/run, issue representative canonical command, verify observed response.
2. **Authority task** — request Supervisory Automatic, observe requested/effective distinction, induce supported degradation, perform deterministic Manual takeover.
3. **Protection task** — reach supported protection condition, confirm protection outranks control, verify alarm ACK does not reset physical protection.
4. **Information-quality task** — inspect unavailable/suspect measurement presentation and confirm no hidden true-state substitution.
5. **Session task** — save, close, reload, checkpoint/replay and confirm exact identity/history.
6. **Navigation task** — keyboard-first use across core workspaces while persistent critical context remains understandable.
7. **Minimum-window task** — verify supported minimum window without hiding essential protection/authority state.
8. **Long-session task** — use current M10/M11 long evidence to demonstrate bounded memory/evidence growth and no silent time loss.

## 7. Hazard closure rule

Each applicable Digital-I&C hazard must close through positive evidence or an explicit support limitation. “No bug has been observed” is not sufficient closure.

Permitted closure evidence:

- architecture invariant;
- automated regression;
- compatibility/replay test;
- representative system test;
- manual operator task;
- packaging test;
- explicit known limitation/non-claim.

## 8. M11 exit condition

M11 can close only when the release artifact, source baseline, compatibility matrix, dependency/support matrix, V&V evidence, Digital-I&C hazard closure, manual, README and known limitations all describe the same product.