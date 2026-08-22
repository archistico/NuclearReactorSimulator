# Digital I&C Hazard Catalog — Reviewed Planning Baseline

## Status

Engineering review catalog for release hardening and future design. It is **not** a nuclear PRA, does not assign accident frequencies, and does not claim licensing-grade hazard coverage.

The source report recommends system-level hazard analysis extending into software and recurring human-system deficiencies. This catalog translates that idea into deterministic software/HMI failure conditions relevant to the simulator.

| ID | Category | Hazard / failure condition | Current coverage | Release disposition | M11 action |
|---|---|---|---|---|---|
| DIC-H01 | Measurement / provenance | Required supervisory measurement becomes unavailable/invalid but automation continues using hidden true state. | Covered | BLOCK if regression | Retain fail-closed tests and manual presentation checks. |
| DIC-H02 | Measurement / provenance | UI presents model truth as though it were a measured instrument value. | Covered/partial | BLOCK if silent provenance loss | Documentation/HMI regression check in M11.5/M11.6. |
| DIC-H03 | Measurement / timing | Stale measurement is syntactically valid but operationally too old for the decision using it. | Partial | Document limitation | No feature work; backlog explicit signal-age/delay modeling post-M11. |
| DIC-H04 | Authority / mode | Requested SupervisoryAutomatic is mistaken for effective SupervisoryAutomatic while system has degraded to Assisted/hold. | Covered | BLOCK if conflated | Keep persistent/visible distinction in final acceptance. |
| DIC-H05 | Authority / mode | Training guidance mode changes physical control authority or controller behavior. | Covered | BLOCK | Freeze in architecture/support contract. |
| DIC-H06 | Authority / takeover | Manual takeover leaves supervisory automation still issuing new decisions, causing fighting controllers or mode confusion. | Covered | BLOCK | Representative M11.6 task. |
| DIC-H07 | Protection | Normal/supervisory command bypasses, clears or outranks active protection. | Covered | BLOCK | Mandatory release regression. |
| DIC-H08 | Protection | Alarm ACK/RESET is confused with physical protection reset. | Covered | BLOCK if conflated | Manual and documentation check. |
| DIC-H09 | Protection / diversity | Duplicated software logic is described as independent/diverse although it shares the same functional requirement and failure-prone input space. | Avoided by architecture; no diversity claim | No false claim | Add non-claim; future diversity inventory post-M11 if needed. |
| DIC-H10 | Command / feedback | Command appears accepted but no committed plant effect occurs and operator receives no clear observed-response evidence. | Covered | BLOCK if feedback silently disappears | M11.6 representative command task. |
| DIC-H11 | Command / permissive | HMI availability/catalog is treated as final permissive authority and diverges from canonical runtime validation. | Covered | BLOCK | Architecture invariant. |
| DIC-H12 | HMI / situation awareness | Keyhole effect forces operator to mentally integrate critical state scattered across pages. | Partial/managed | Accept within declared HMI scope if persistent critical context remains visible | Use checklist; no feature expansion in M11. |
| DIC-H13 | HMI / workload | Clumsy automation reduces workload in easy conditions but increases operator burden during degraded/protection states. | Partial | Document scope | Representative degraded/takeover task in M11.6; deeper study post-M11. |
| DIC-H14 | HMI / mode awareness | Operator cannot tell which automation/controller mode is currently active or why it changed. | Covered | BLOCK | Persistent visibility check. |
| DIC-H15 | HMI / information load | Dense alarms/events/mission data obscure the small set of safety-relevant changes requiring attention. | Partial/managed | No release regression | Manual task/checklist; performance not visual redesign. |
| DIC-H16 | HMI / automation transparency | Automation acts correctly but is 'strong and silent': intent, inhibit, reason or result is not observable. | Mostly covered | BLOCK for critical authority changes | Traceability/manual acceptance. |
| DIC-H17 | Timing / host | UI-thread work stalls long enough that operator-visible state/input becomes unacceptably delayed even though logical simulation remains correct. | Known gap | Measure before release | Freeze responsiveness metrics/budgets in M11.3. |
| DIC-H18 | Timing / simulation | Catch-up/backlog policy silently skips deterministic simulation time to maintain wall-clock pace. | Current desktop path bounded without drop; generic API policy pending | BLOCK if silent drop introduced | M11.3 supported-caller inventory and explicit policy. |
| DIC-H19 | State consistency | Concurrent/mutable consumers observe different versions of plant/session state. | Strongly covered | BLOCK if worker redesign weakens it | Worker proposal requires immutable handoff contract. |
| DIC-H20 | Replay / persistence | Replay/checkpoint restores a state inconsistent with the exact scenario/action/authority history. | Covered | BLOCK | Compatibility matrix in M11.2. |
| DIC-H21 | Replay / compatibility | Historical fingerprint/schema/version is silently reinterpreted by a newer implementation. | Covered by policy | BLOCK | M11.2 explicit version selection/migration tests. |
| DIC-H22 | Evidence / recorder | Recorder/evidence production fails but session is still presented as complete/authoritative. | Policy decision pending | Must be explicit before release claim | Define compromised-recorder vs host-fault policy; never silent partial evidence. |
| DIC-H23 | Persistence / file integrity | Failed save destroys a previously valid session archive. | Covered | BLOCK | Packaging/session representative test. |
| DIC-H24 | Configuration | Runtime, manual, dependency versions, persisted formats and release statement describe different product configurations. | Partial until M11 release freeze | BLOCK release | M11.1/M11.5/M11.6 support and documentation consistency. |
| DIC-H25 | COTS / dependency | Runtime dependency update changes behavior or packaging requirements without corresponding release verification. | Partial | Block unsupported drift | Freeze dependency matrix + clean-target publish tests. |
| DIC-H26 | COTS / test infrastructure | Test-runner/tool change causes 'no tests executed' or coverage gaps to be mistaken for a green gate. | Known class encountered historically | BLOCK | Gate must assert nonzero expected discovery/route resolution. |
| DIC-H27 | Assessment | Project publishes invented numerical software reliability/safety probability unsupported by evidence. | No such claim | Forbidden claim | Known-limit/support wording review. |

## Use rule

For each M11 change, review all hazards whose category is touched. A hazard can be closed for release by one or more of: architecture invariant, automated regression, representative system test, compatibility test, manual HMI task, packaging test, or explicit known limitation.

Do not close a hazard merely because the implementation has no known bug. The closure should identify positive evidence or a deliberate scope/non-claim.
