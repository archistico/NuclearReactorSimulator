# ADR 0144 — Split turbine-inlet branch continuity from residual-floor diagnosis

## Status

Accepted and validated in M10.9.4.1-H.18 Hotfix 1.

## Context

Validated H.17 Hotfix 6 qualified 473 deterministic long-horizon/cross-profile representatives and found 245 H.9 line-search failures. `turbine-inlet` candidate-vs-explicit phase mismatch appears in 120 of those failures and strongly predicts failure, but 125 failures remain without that mismatch. Extending branch continuity globally or changing H.9 before separating these classes would confound two potentially different mechanisms.

## Decision

H.18 will:

1. freeze the validated H.17 representative evidence;
2. extend the unchanged H.13 bounded 2%/5 K shadow target set only from `steam|stop-out|header` to `steam|stop-out|header|turbine-inlet`;
3. re-evaluate all 245 validated H.17 failures plus a deterministic success-control set under unchanged H.9;
4. diagnose every remaining failure for node-local mapped-minus-applied residual structure, accepted-iterate merit floor and all-node candidate-vs-explicit inverse-branch disagreement;
5. keep production explicit and unchanged.

## Consequences

A fourth-node recovery can be attributed directly to `turbine-inlet` continuity. Residual failures are not automatically treated as additional branch-selection failures. A later solver/solution-existence milestone is authorized only if H.18 removes the branch-disagreement class without exposing another untargeted node.
