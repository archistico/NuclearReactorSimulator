# M10.9.4.1-H.28.1-D Preflight Hotfix 1 — Unused mapped-reuse local cleanup

## Status

**CANDIDATE.** Supersedes the first H.28.1-D candidate before any user build/test execution.

## Finding

A stricter pre-build static review found one local in the new focused audit, `mappedProbeReuseFraction`, that was calculated but never consumed. With warnings/code-style enforcement treated as errors, retaining an unnecessary local creates avoidable compilation/analyzer risk.

## Correction

The unused local calculation was removed. Mapped probe reuse remains fully recorded because the summary and metrics compute it directly from `ProbeMappedFluidNodeReuseCount` and `ProbeMappedFluidNodeCount`.

No production/runtime file changed. The H.28.1-D numerical and performance contract is unchanged.
