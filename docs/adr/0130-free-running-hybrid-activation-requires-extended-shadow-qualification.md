# ADR 0130 — Free-running hybrid activation requires extended shadow qualification

## Status

Accepted / H.5 Hotfix 2 user-validated

## Context

H.4 showed excellent numerical improvement over 50 frozen-forcing intervals, but H.5 Hotfix 1 failed ordinary free-running tests because some later triggered correctors did not converge within the bounded budget.

## Decision

1. Restore all ordinary current-v2 production profiles to the validated explicit 10 ms path.
2. Preserve the hybrid implementation as explicit opt-in experimental infrastructure.
3. Qualify the selected H.4 profile in shadow mode against a longer committed production trajectory.
4. Never commit a shadow result and never interpret non-convergence as a runtime fallback.
5. Do not increase iteration limits or retune physical coefficients merely to force activation.
6. Require a later separate activation candidate after extended evidence demonstrates a bounded convergence envelope.

## Consequences

Normal gameplay remains stable on the validated numerical path while the semi-implicit method can continue to mature with deterministic evidence.

## Validation note

H.5 Hotfix 2 user validation confirmed the rollback contract and produced the expected negative activation evidence: 7 shadow corrections over 500 intervals, 5/7 convergent and `extended-shadow-qualification-passes=False`. ADR 0131 therefore refines the bounded numerical envelope without reactivating production.
