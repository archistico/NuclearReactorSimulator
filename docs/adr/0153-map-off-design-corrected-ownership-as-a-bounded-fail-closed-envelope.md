# ADR 0153 — Map off-design corrected ownership as a bounded fail-closed envelope

## Status

Accepted for M10.9.4.1-H.27 candidate.

## Context

H.24 proves the corrected path over the nominal four-profile long-horizon domain. H.25 and H.26 prove representative protection behavior and atomic rollback. Default activation still requires evidence outside those nominal amplitudes.

Treating every off-design rollback or protection trip as a failed numerical qualification would be incorrect: fail-closed fallback and canonical protection are intended safety behavior. Conversely, demanding corrected ownership everywhere would encourage unjustified retuning and silent envelope expansion.

## Decision

H.27 uses a staged, physically defensible off-design matrix and records an explicit per-scenario classification:

- corrected-qualified;
- safe-fallback-envelope;
- protected-boundary;
- observed-no-trigger.

Unsafe corrected commits, fallback commits, ownership/conservation violations and nondeterministic repeat remain hard failures.

H.27 does not retune the algorithm in response to a safe rollback and does not broaden thermodynamic/model validity boundaries.

## Consequences

The project gains an operational qualification envelope instead of a binary claim that the corrected path works everywhere. This evidence feeds H.28 cost/soak and later H.29 activation review.
