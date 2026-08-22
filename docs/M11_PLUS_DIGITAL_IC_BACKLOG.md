# M11+ Digital I&C / Human-System Backlog — Reviewed Planning Baseline

## Rule

M11 remains release hardening and is feature-frozen. The items below are useful consequences of the Digital I&C review but are **not authorized M11 features** unless an existing release gate proves a blocker.

## Backlog

| ID | Candidate | Why the source makes it interesting | Earliest sensible home | Non-negotiable constraint |
|---|---|---|---|---|
| DIC-B01 | Signal age / stale-value semantics | Appendix F treats timing and information consistency as part of control correctness. | Post-M11 instrumentation/HMI fidelity milestone | Must not break current measured/true-state provenance. |
| DIC-B02 | Delayed measurement/update fault | Data communications can fail by lateness as well as loss. | Post-M11 M8/M13-style fault/HMI extension | Deterministic logical-step schedule; no nondeterministic network simulation. |
| DIC-B03 | Lost update / temporarily missing telemetry | Source identifies lost messages/data-path vulnerability. | Post-M11 fault framework extension | Reuse canonical instrumentation quality; no second state owner. |
| DIC-B04 | Inconsistent redundant indication scenario | Source discusses inconsistent messages to receivers and common-mode concerns. | Post-M11 advanced instrumentation training | Must explicitly model provenance and agreement logic. |
| DIC-B05 | Command-feedback delay training scenario | Human factors + communications together make delayed feedback operationally important. | Post-M11 HMI/training extension | Plant command semantics remain canonical; delay belongs to observation/feedback contract only. |
| DIC-B06 | Protection diversity inventory | Chapter 5 provides a precise distinction between duplication, design diversity and functional diversity. | Documentation first; implementation only in later protection-fidelity work | No claim of independence without different functional requirements/shared-dependency analysis. |
| DIC-B07 | Diverse educational protection mechanism | Could teach defense-in-depth/functionally different sensing principles. | Post-M11 physical/protection roadmap after dedicated design | Must not be cosmetic duplicate algorithms. |
| DIC-B08 | Human workload study / part-task experiments | Chapter 7 recommends performance-based evaluation under representative tasks. | M13 control-room experience | Use actual simulator dynamics and task outcomes; no invented psychometric precision. |
| DIC-B09 | Persistent situation-awareness anti-keyhole review | Multiple workspaces can fragment understanding. | M13 HMI iteration | Keep plant overview primary; do not create one giant all-controls screen. |
| DIC-B10 | Automation transparency contract library | “Strong and silent” automation is a recurring deficiency. | M13 or future automation UX | Expose intent/effective state/reason/result without moving control ownership to UI. |
| DIC-B11 | Formalized digital-I&C FMEA/fault-tree-like model | Chapter 6 values deterministic hazard analysis and structured reliability reasoning. | Future engineering tooling, optional | Do not assign unsupported software probabilities or present it as a nuclear PRA. |
| DIC-B12 | Dependency/SBOM export | COTS/dependency assurance benefits from traceable configuration. | M11.4 if purely packaging metadata; otherwise later tooling | Must not block release unless support policy requires it. |
| DIC-B13 | Historical/action communication consistency sentinel | Appendix F's rollback/orphan-message problem maps conceptually to replay consistency. | M11.2 if only verification; later if new format | Preserve exact v1 schemas/fingerprints and deterministic action ordering. |
| DIC-B14 | UI response deadline characterization | Real-time section emphasizes worst-case timing, while our desktop is soft-real-time only. | M11.3 measurement / M13 if UX changes needed | Characterize, do not claim hard-real-time behavior. |

## Priority after release hardening

Highest educational value appears to be:

1. `DIC-B01/B02/B03` — stale/delayed/lost instrumentation evidence;
2. `DIC-B06` — explicit protection-diversity inventory before any diversity implementation;
3. `DIC-B08/B09/B10` — human-system evaluation and automation transparency during M13;
4. `DIC-B14` — responsiveness characterization tied to real operator tasks.

These extend already existing architecture rather than requiring a distributed-network simulator.
