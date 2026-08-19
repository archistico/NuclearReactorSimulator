# ADR 0154 — Attribute corrected-path cost before optimizing H.9

## Status

Accepted; M10.9.4.1-H.28.1-A Hotfix 2 diagnostic attribution was user-validated on 2026-08-19.

## Context

H.28 preserved numerical safety and determinism but failed its performance gate with `unbounded-regression`, including ~1.70 s average triggered-step cost and ~43.46 MB allocation per trigger. Aggregate corrected/explicit ratios cannot distinguish duplicated predictor work from finite-difference Newton cost or allocation overhead.

## Decision

Before any optimization, instrument the unchanged corrected path and attribute wall time/allocation to explicit fallback preparation, predictor, H.9 subphases, disagreement scan, authority and commit/accounting.

Store nondeterministic measurements outside deterministic record equality through weak-reference registries. Freeze the failed H.28 artifacts and require the fresh numerical fingerprint to remain unchanged.

## Consequences

- H.28 remains failed.
- H.29 remains blocked.
- No numerical retuning or algorithm replacement is justified by attribution alone.
- Optimization work must target measured cost centers and preserve H.19–H.27 numerical contracts.
