# M10 Final — VR2 — R3 Short Exact-v9-Equivalent Shadow / Composition Requalification — Planning 1 — Preexecution Review

## Status

**STATIC PREEXECUTION REVIEW — PLANNING ONLY**

The R2 returned evidence is complete and adjudicated PASS. The future R3 implementation is bounded to one new Application.Tests source and no production changes.

## Review findings

1. R2 independently qualified production mode 2 over the frozen physical/topology domains.
2. R3 is composition-level qualification, not another IF97 benchmark.
3. The canonical exact-v9 factory remains immutable and historical exact-v9 is not reinterpreted.
4. A test-local shadow builder is permitted only if mode-1 shadow equals canonical exact-v9 over 128 deterministic running steps with zero fingerprint mismatch.
5. The only allowed factor change in the scored shadow is closure mode `1 -> 2`.
6. The workload is exactly 120 s / 12,000 steps at the existing 5 MWe exact-v9 operating point; no load change is introduced.
7. Health, conservation, controller and turbine-moisture ownership limits are inherited from the already qualified exact-v9 activation-candidate 120 s envelope.
8. Two fresh mode-2 shadow runs must be deterministic over 128 steps.
9. R4 retains ownership of the P1B-equivalent 5→6 MWe long materiality replay.
10. The future R3 implementation adds exactly one test source; no `src/` or historical test source may change.

No R3 execution, mode-2 default activation, exact-v9 mutation, new exact version, R4 execution, VR3, P3-R1 or second replacement-long authority is granted by this planning candidate.
