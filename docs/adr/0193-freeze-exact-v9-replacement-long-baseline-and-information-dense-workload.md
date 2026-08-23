# ADR-0193 — Freeze exact-v9 replacement-long baseline and information-dense workload

**Status:** Proposed

## Context

The first M10 final long campaign used exact-v4 and mission @2. It failed physically in LR-H1 and exposed O(n²) MISSION live-projection cost in LR-M1. Diagnostic 1–11 repaired/qualified the whole-cycle operating point and live projection; the separate activation-decision gate then promoted exact-v9 and mission @3 to authoritative production without reinterpreting historical identities.

Reusing or rewriting the old exact-v4 long manifest would destroy provenance. Re-running the original 14,400 simulated-second shape would also spend most workstation time repeating evidence already superseded by the exact-v9 600 s equilibrium qualification and the 100,000-sample LR-M1 scaling proof.

## Decision

Freeze a new exact-v9 production source/test baseline and a five-leg 1,920 s replacement workload:

- RL-H1 900 s;
- RL-M1 480 s;
- RL-D1 300 s;
- RL-P1 180 s;
- RL-R1 60 s.

Preserve the 35–45 minute workstation target and enforce a 60-minute hard job cap. Keep all existing conservation ceilings unchanged. Replace exact-v4-specific I.3 absolute targets with exact-v9 sentinels derived from already validated Diagnostic-11/activation evidence; historical I.3 budgets remain immutable exact-v4 provenance.

The execution candidate may add one explicit replacement-long test file and orchestration/finalization support, but may not modify any file covered by the frozen exact-v9 `src` manifest or any pre-existing test covered by the frozen test manifest.

## Consequences

A green baseline-freeze gate authorizes execution of the replacement long against the exact frozen baseline. A source/pre-existing-test change invalidates authorization. A green replacement long makes M10 eligible for explicit closure; it does not itself start M11.
