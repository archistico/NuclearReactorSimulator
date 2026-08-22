# Human–Automation Function Allocation — Reviewed Planning Baseline

## Purpose

Chapter 7 of the source argues that the human operator's role should be designed with rigor comparable to hardware/software roles rather than being defined by whatever automation cannot do. This matrix makes the current simulator allocation explicit.

Legend:

- **Owner** = entity allowed to decide/act on the canonical function.
- **Observer/support** = may present, explain, record or evaluate but not own the action.
- **Override** = authority that can supersede the normal owner.

| ID | Operational function | Detect / evidence | Decide | Actuate / mutate | Confirm / observe | Override / takeover | Current status / M11 action |
|---|---|---|---|---|---|---|---|
| HAF-01 | Reactor/plant physical evolution | Canonical plant state | Physics solvers | M2/M3/M4 solvers | Application/HMI/recorder | Protection can alter commanded actuators, not rewrite physics | Frozen architecture. |
| HAF-02 | Local closed-loop regulation | Measured signals / controller state | M5 local controller | Canonical actuator/setpoint seam | HMI + recorder | Protection; operator mode/command where supported | Preserve exact semantics. |
| HAF-03 | Supervisory high-level objective | Measured signals + permissives | M5 supervisory coordinator | Existing controller modes/setpoints/typed canonical commands | HMI MODES/diagnostics + recorder intent stream | Protection; operator Manual takeover | Freeze requested/effective/degraded distinction. |
| HAF-04 | Protection trip decision | Canonical protection measurements/conditions | M5 protection | Canonical trip/latch actuation | HMI alarm/protection + recorder | No normal/supervisory override | Release-blocking if bypassable. |
| HAF-05 | Protection reset | Protection reset-readiness evidence | Canonical protection logic validates | Typed reset command | HMI shows readiness/block reason | No alarm ACK substitution | Preserve as separate function. |
| HAF-06 | Alarm acknowledgement | Alarm state | Operator command validated by alarm owner | Alarm acknowledgement seam | HMI/log | Does not reset physical protection | Documentation consistency check. |
| HAF-07 | Plant command selection | Operator context + command catalog | Operator | Canonical command dispatcher/runtime validation | Observed-response evidence + HMI | Protection/interlocks may reject | Preserve contextual catalog as advisory, not permissive authority. |
| HAF-08 | Command permissive/interlock | Canonical subsystem/protection state | Owning runtime/interlock logic | Accept/reject command | HMI blocking reason / observed response | None from UI | Fail closed. |
| HAF-09 | Training guidance mode | Training state | Operator | Presentation/training configuration only | HMI | None over plant state | Must remain orthogonal to control authority. |
| HAF-10 | Mission/challenge evaluation | Recorded/committed evidence | Application challenge/scoring evaluator | Challenge state/score only | MISSION UI | Cannot command plant | Preserve observational ownership. |
| HAF-11 | External demand profile | Versioned challenge/mission data | Scenario/challenge schedule | External demand input only | MISSION / GRID evidence | Plant controls determine actual output | Preserve GRID DEMAND ≠ REQUESTED LOAD ≠ ACTUAL OUTPUT. |
| HAF-12 | Instrumentation fault activation | Scenario/fault schedule | M8 fault framework | Canonical instrumentation fault seam | HMI quality/provenance + recorder | Supported recovery/clear rules only | No manual-only hidden injector in normal HMI. |
| HAF-13 | Component fault activation | Scenario/fault schedule | M8 fault framework | Canonical component constraint/input seam | HMI + recorder | Protection may react | Fault layer must not write derived outcomes. |
| HAF-14 | Degraded automation decision | Invalid/suspect measurement/equipment/permissive evidence | M5 authority/supervisory logic | Effective authority/hold/fallback through canonical seams | HMI requested/effective/reason | Operator Manual; protection suspension | Preserve fail-closed behavior. |
| HAF-15 | Manual takeover | Operator request | Canonical authority coordinator | Stop new supervisor decisions then hand over committed outputs | HMI + replay journal | Protection still superior | Exact logical ordering is acceptance evidence. |
| HAF-16 | Runtime Run/Pause | Host/UI request | Host runtime coordinator | Host stepping state only | HMI | Not plant safety authority | Must not enter physical fingerprint identity incorrectly. |
| HAF-17 | Recording | Committed snapshots/events | Recorder policy | Recording/evidence state only | SESSION/LOG | No plant authority | Evidence failure policy remains M11.3 decision. |
| HAF-18 | Checkpoint creation | Recorded prefix + fingerprint | Recorder/session service | Versioned replay anchor | SESSION | None | No opaque solver dump. |
| HAF-19 | Replay/seek restoration | Persisted exact identity/action prefix | Replay runner verifies | Reconstructs through canonical simulation execution | SESSION + verification result | Fail closed on mismatch | Preserve action-at-N → apply-at-N+1 semantics. |
| HAF-20 | Session save/load | Immutable/captured session evidence | Session infrastructure policy | File/archive operations | HMI status/diagnostic | Failure must not corrupt current valid archive | M11.2 compatibility + M11.3 responsiveness. |
| HAF-21 | HMI navigation | UI input | Operator | App-only page/focus/selection state | HMI | None over plant state | Navigation has no plant side effects. |
| HAF-22 | HMI state interpretation | Immutable presentation contracts | Operator | Human decision only | Persistent situation/protection/quality context | Protection/automation remain system owners | Apply classic HMI checklist to changes. |

## Allocation acceptance rules

1. Every plant mutation must have exactly one canonical owner path.
2. Every automated action visible to the operator should expose enough evidence to distinguish **intent**, **effective state**, **inhibit/degradation** and **observed result** where those concepts exist.
3. The operator must not be assigned an implicit recovery task that the HMI cannot make observable in time.
4. Manual takeover must be available through a documented deterministic path and must not disable protection.
5. Assistance/training functions may reduce cognitive burden but may not alter the authority allocation.
6. Any M11 change that moves a responsibility between rows requires an explicit architecture decision; release hardening is not authorization to change function allocation silently.
