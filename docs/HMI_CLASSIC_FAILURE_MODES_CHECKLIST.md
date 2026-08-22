# HMI Classic Failure Modes Checklist — Reviewed Planning Baseline

## Purpose

Permanent human-system review checklist derived primarily from Chapter 7 of *Digital Instrumentation and Control Systems in Nuclear Power Plants: Safety and Reliability Issues* (1997), mapped to the simulator's operator workstation.

This checklist is intentionally higher-level than pixel/layout style rules. It asks whether the operator can understand and control the coupled system under representative dynamics.

## A. Data overload

- [ ] Safety/protection/availability changes are visually more salient than routine mission/performance detail.
- [ ] Dense timeline/log activity cannot erase first-out, protection or mission lifecycle context.
- [ ] The operator is not required to scan multiple equivalent numerical surfaces to learn the same critical fact.
- [ ] Alarm, trip, signal-quality and authority changes remain identifiable during transients.
- [ ] Red remains reserved for alarm/trip/protection significance rather than decorative emphasis.

**Fail example:** dozens of recent events cause the one protection transition requiring action to disappear from the operator's effective attention surface.

## B. Keyhole effect

- [ ] Critical global context remains visible while navigating F1–F8/workspaces.
- [ ] Current protection state does not depend on remembering another page.
- [ ] Requested/effective authority and degradation remain discoverable without reconstructing state from several pages.
- [ ] Signal quality/provenance relevant to a decision is visible in the decision context.
- [ ] Navigation alone never changes plant state.

**Project interpretation:** the F1–F8 terminal is acceptable only because it sits inside a persistent situation shell and does not require the terminal page itself to be the sole plant mental model.

## C. Mode errors

- [ ] Requested authority and effective authority are distinct.
- [ ] Per-loop/local controller modes are not hidden by a global mode label.
- [ ] Protection suspension/degradation cannot visually resemble normal supervisory control.
- [ ] Momentary commands and persistent modes/states are visually/semantically different.
- [ ] A reset action names what it resets; alarm ACK is not protection reset.
- [ ] Manual takeover completion is explicit rather than inferred from a button press.

## D. Workload imbalance / clumsy automation

- [ ] Automation does not require extra confirmation/navigation precisely when degraded/protection conditions demand fast operator understanding.
- [ ] When automation degrades, the HMI exposes the reason and the operator's next responsibility.
- [ ] Manual takeover does not require reconstructing hidden controller state.
- [ ] Automatic protection remains active during manual operation.
- [ ] The operator is not required to compensate for an unavailable automatic function using information the HMI does not expose.

## E. “Strong and silent” automation

- [ ] High-level automation objective is observable.
- [ ] Effective automation state is observable.
- [ ] Inhibit/degradation/suspension reason is observable.
- [ ] Relevant command or objective outcome is observable.
- [ ] Significant automatic/protection actions have source/provenance in LOG/timeline where supported.
- [ ] The HMI never implies an automatic action occurred merely because it was requested.

A practical project contract is:

```text
INTENT → EFFECTIVE STATE → REASON / CONSTRAINT → OBSERVED RESULT
```

Not every screen must literally show four labels, but the information must be retrievable without guesswork.

## F. Operator role defined by design, not by leftovers

- [ ] For every degraded mode, the required operator responsibility is defined in the function-allocation matrix.
- [ ] The operator is not treated as a generic fallback for every unimplemented automatic behavior.
- [ ] The HMI provides the evidence needed to perform the assigned manual responsibility.
- [ ] The operator can determine whether a condition is actionable, informational, blocked or protection-owned.

## G. Coupled-system surprises

- [ ] A command context can identify direct effect, dependencies/permissives and what to monitor.
- [ ] Expected downstream influence remains explicitly different from observed response.
- [ ] Protection and interlocks remain authoritative even when a command catalog predicts availability.
- [ ] Command rejection is visible as rejection, not as a zero-delta “success”.
- [ ] Related requested load, actual output and external grid demand remain separate concepts.

## H. Information abstraction and accessibility

- [ ] The interface provides enough aggregation for timely decisions without hiding necessary detail.
- [ ] Important details are available by drill-down rather than duplicated everywhere.
- [ ] Instrument range, normal band, target band, warning/alarm and trip limits are not conflated.
- [ ] Measured, model diagnostic and unavailable values are distinct.
- [ ] Units and precision remain consistent with engineering meaning and current project conventions.

## I. Performance-based evaluation

The source report argues that guideline/checklist review is insufficient by itself for human factors. Therefore any release-significant change to what the operator sees or how the system responds to input should have a representative task using actual simulator dynamics.

M11.6 should include at minimum:

- [ ] switch Manual ↔ Assisted/Supervisory and identify requested/effective state;
- [ ] induce/observe an existing degraded-measurement case and explain the automation degradation;
- [ ] observe protection taking precedence over normal/supervisory control;
- [ ] complete Manual takeover;
- [ ] issue an allowed and a blocked command and inspect observed-response feedback;
- [ ] save, close/reload and replay/seek a session;
- [ ] navigate the HMI at the minimum supported window and entirely by keyboard for the validated route.

## J. Scope discipline

- [ ] The review does not claim certified nuclear-control-room usability.
- [ ] The review uses the simulator's educational operator tasks, not invented licensing criteria.
- [ ] New HMI features discovered as desirable during this review are deferred unless a release blocker is proven.
