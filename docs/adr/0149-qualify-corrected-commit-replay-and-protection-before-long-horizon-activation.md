# ADR 0149 — Qualify corrected-commit replay and protection before long-horizon activation

## Status

Accepted for M10.9.4.1-H.23 candidate.

## Context

H.22 validated the first separately opt-in corrected-candidate commit seam. It demonstrated actual corrected ownership over 443/2,000 intervals with unchanged H.20 authority, zero unsafe/fallback-commit violations, deterministic repeat and tight conservation. Standard current-v2 remained explicit.

A short committed control window does not yet establish that the new ownership path participates correctly in the simulator's already-authoritative replay/checkpoint and protection architecture.

## Decision

Before committed long-horizon/cross-profile qualification, qualify the unchanged H.22 opt-in path through:

1. exact-version scenario recording and full replay;
2. replay-backed checkpoint seek and continuation;
3. an in-flight delayed reverse-power protection pickup;
4. eventual generator trip and breaker opening;
5. exact internal H.20/H.22/protection trace repeat;
6. fail-closed explicit fallback for any rollback encountered during the transient.

Use an initial-condition factory declared only in the test assembly. It delegates to the existing H.22 audit-only factory and therefore adds no standard product selection path.

Freeze the user-validated H.22 focused artifacts and fingerprint them instead of automatically rerunning the expensive H.22 cumulative gate, because H.23 changes no H.22 numerical runtime code.

## Consequences

A green H.23 result permits progression to committed long-horizon/cross-profile qualification. It does not authorize default production activation. Off-design robustness remains a separate mandatory gate.

If H.23 reveals rollback during the protection transient, rollback is not itself a failure; any corrected commit during rollback is a failure.
