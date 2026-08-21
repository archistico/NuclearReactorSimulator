# ADR 0131 — Refine the hybrid corrector with a bounded two-tier shadow envelope before reactivation

## Status

Accepted — M10.9.4.1-H.6 validated

## Context

H.4 qualified `P060-F040-R015` over a 0.5 s frozen-forcing window. H.5 Hotfix 1 demonstrated that direct free-running production activation was premature. H.5 Hotfix 2 restored explicit 10 ms production and extended the same profile over 500 committed intervals in shadow mode. User validation found 7 triggered corrections, 5 convergent and 2 non-convergent, with low trigger frequency and bounded aggregate cost.

The evidence does not justify changing physical coefficients, raising trigger thresholds or silently falling back to explicit after a failed authoritative correction. It does justify determining whether the two hard intervals are recoverable through a bounded numerical iteration envelope.

## Decision

Keep production current-v2 explicit. Freeze H.4 trigger thresholds at P060/F040. Evaluate a bounded set of relaxation / iteration profiles on the exact H.5-triggered committed intervals.

If a rescue profile qualifies, evaluate a deterministic two-tier shadow policy:

- primary: H.4 `R015-I072`;
- rescue: selected bounded profile;
- rescue starts again from the same committed state;
- neither shadow candidate is committed;
- deterministic work includes both primary and rescue effort.

No wall-clock data, physical retuning, trigger retuning or hidden filtering may participate in the numerical decision.

## Consequences

A positive H.6 result permits only broader shadow qualification across free-running scenarios. It does not reactivate production hybrid coupling.

A negative H.6 result requires revision of the corrector algorithm rather than additional hidden runtime repair.


## Validated outcome

H.6 was validated after build, ordinary tests and focused audit passed. Over the frozen seven P060/F040 events, the selected `R0125-I096` profile converged on 6/7; the two-tier ladder therefore also reached only 6/7, with deterministic work ratio `1.700000` and `refined-envelope-qualification-passes=False`. This negative qualification result activates the ADR consequence above: production remains explicit and H.7 revises the corrector algorithm rather than extending the fixed-relaxation Picard envelope further.
