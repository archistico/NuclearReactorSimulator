# ADR 0199: Attribute the rare exact-v9 wall-clock tail before RP1C selection or candidate mutation

## Status

Accepted for planning; implementation remains separately gated.

## Context

C4 is physically qualified and closes the R1 allocation/tail problem, but returned full-domain evidence contains two exact-v9 single-call maxima above the unchanged `409.30666666666673 us` ceiling. All seam calls, medians, p95s, median allocation and measured-region GC deltas remain green.

All five returned exact-v9 calls above `100 us` occur late in the measured campaign at passes `15,17,17,18,18`, use `C2-MIXTURE-PREFIX`, and allocate `0 B` on the measured call.

This pattern is compatible with a runtime/JIT transition hypothesis but does not establish one.

## Decision

Before RP1C selection, threshold change or any new thermodynamic candidate mutation, run a separately versioned test-only attribution experiment over immutable C4 and the frozen exact-v9 corpus.

The experiment will compare fresh processes under four runtime compilation modes: clean ambient control, tiering off, tiering on with Dynamic PGO off, and tiering on with Dynamic PGO on.

The parent environment must be clean of the five controlled `DOTNET_*` compilation variables. Child-process environment settings are the only permitted runtime-mode change.

The experiment is evidence-only and cannot automatically prove causality or authorize selection.

## Consequences

- C4, C2/C3, exact-v9 and all thresholds remain immutable.
- Seam is not rerun because returned seam evidence is already complete and green.
- The future attribution gate uses 20 fresh processes and 460,800 exact-v9 measured calls.
- `100 us` is a diagnostic tail floor only; the qualification maximum stays `409.30666666666673 us`.
- Returned evidence must be adjudicated before any subsequent branch.
- `SELECT-C4`, production repair, exact-v9 change, VR3, P3-R1 and second replacement-long remain unauthorized.
