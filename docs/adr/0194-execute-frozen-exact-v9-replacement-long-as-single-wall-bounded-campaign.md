# ADR-0194 — Execute the frozen exact-v9 replacement long as one wall-bounded campaign

**Status:** Proposed

## Context

The exact-v9 authoritative production activation is validated and Replacement-Long Baseline Freeze 1 returned green. The freeze authorizes one new explicit test file while forbidding any change to the 959 frozen `src` files or 351 pre-existing test files. The redesigned campaign has five independent legs totaling 1,920 authored seconds / 192,000 steps and a hard 60-minute workstation job cap.

Running the legs as separate xUnit invocations would make the hard cap difficult to enforce across the complete campaign and could repeat the fail-fast behavior of the first long, where useful evidence from later legs was not collected after an earlier blocker.

## Decision

Implement the authorized replacement campaign in one explicit xUnit test file and one explicit campaign test method.

The method:

1. starts one wall-clock stopwatch before the first leg;
2. executes RL-H1, RL-M1, RL-D1, RL-P1 and RL-R1 in the frozen order;
3. enforces the common 60-minute hard deadline across authored work and replay/checkpoint reconstruction;
4. catches per-leg failures to preserve fail-collect evidence when the remaining wall budget allows;
5. emits the frozen artifact set and fails the overall test if any leg fails or the hard deadline is exceeded.

A PowerShell preflight validates the returned baseline-freeze authorization, all frozen hashes and the exact one-test-file addition before compilation or execution.

## Consequences

- The wall-cap statement is measured over the real campaign rather than inferred by summing independent process timings.
- A single physical/numerical failure does not automatically suppress evidence from later independent legs.
- Frozen production code and historical tests remain untouched.
- The execution test is explicit and cannot enter the ordinary suite accidentally.
- A green run makes M10 closure eligible but does not itself perform M10 closure or start M11.
