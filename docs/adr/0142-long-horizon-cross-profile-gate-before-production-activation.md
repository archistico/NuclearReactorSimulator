# ADR 0142 — Require long-horizon and cross-profile branch-continuity qualification before production activation

## Status

Accepted for M10.9.4.1-H.17 candidate.

## Context

H.16 qualified the unchanged H.13 bounded branch-continuity policy at `steam|stop-out|header` over 2,000 intervals and 15 frozen P060/F040 triggers. Earlier evidence showed why a 500- or 2,000-interval success alone is not sufficient: H.14 discovered interval 723 only after extending the original H.13 window, and H.15 localized the same inverse-map mechanism to a third node.

## Decision

No production activation candidate may be designed directly from H.16.

H.17 must first qualify the unchanged three-node policy over a materially longer, multi-profile shadow set containing steady generation, bounded generator-load manoeuvring, bounded condenser-cooling degradation/recovery and a combined load/cooling profile.

H.17 also searches all thermodynamic nodes at every trigger for a new candidate-only late boundary-aware saturated-root shadow mechanism outside the existing target set.

## Consequences

A green H.17 gate provides evidence that the policy is not merely fitted to the original 2,000-interval trajectory and that no fourth node with the same known mechanism appears in the triggered candidate states. Production remains explicit throughout H.17.

Only after H.17 qualifies may the project design a reversible activation candidate with rollback and retained shadow evidence. A negative H.17 result is diagnostic evidence and does not justify retuning the policy automatically.
