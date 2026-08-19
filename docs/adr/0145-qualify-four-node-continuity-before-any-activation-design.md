# ADR 0145 — Qualify four-node continuity before any activation design

## Status

Accepted and validated in M10.9.4.1-H.19.

## Context

User-validated H.18 Hotfix 1 extended the unchanged bounded 2% / 5 K shadow continuity target set to `steam|stop-out|header|turbine-inlet` and converged on all 261 H.18 samples. It recovered all 245 H.17 failures, including both the 120 turbine-inlet-mismatch failures and the 125 non-mismatch failures, while preserving 16/16 success controls and committed-state transparency.

H.18 intentionally sampled only the H.17 failure set plus limited success controls. The remaining evidence gap is whether the four-node target set preserves convergence, determinism, branch transparency and absence of new untargeted disagreement across the complete H.17 long-horizon/cross-profile representative contract.

## Decision

Before any production activation design, H.19 will:

1. regenerate the same 30,000-interval/four-profile P060/F040 census;
2. require the census to remain 3,046 trigger intervals and 92 episodes;
3. require the regenerated 473 representative keys to exactly match frozen H.17 Hotfix 6 evidence;
4. evaluate all 473 representatives with unchanged H.9 and unchanged 2% / 5 K hysteresis targeted only at `steam|stop-out|header|turbine-inlet`;
5. measure recovery/preservation against the frozen H.17 failure/success classes;
6. observe committed selection across the complete four-profile horizon;
7. reject new untargeted candidate-only late-shadow nodes and new untargeted candidate-vs-explicit phase-mismatch nodes;
8. keep production explicit and commit no shadow state.

## Consequences

A positive H.19 qualification will demonstrate that H.18's four-node result generalizes to the entire validated long-horizon/cross-profile representative contract without changing the sampling contract.

A positive H.19 result still does not activate the policy. Any production change requires a separate activation-design milestone with explicit rollback, authority, observability and failure-mode contracts.

A negative H.19 result keeps production unchanged and directs the next step to the specific failing representative/episode or newly discovered untargeted branch-disagreement node.
