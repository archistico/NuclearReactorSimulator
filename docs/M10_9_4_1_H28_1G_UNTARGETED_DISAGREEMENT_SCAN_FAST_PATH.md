# M10.9.4.1-H.28.1-G — Untargeted Branch-Disagreement Scan Fast Path

## Purpose

H.28.1-F reduced the corrected triggered p95 to 96.5735 ms, leaving only about 8.2 ms above the unchanged H.28 readiness threshold of 88.3812 ms. H.9 itself averaged about 74.58 ms, so the remaining tail is largely outside the corrector. Historical H.28 attribution identified the untargeted branch-disagreement safety scan as the dominant post-corrector cost center.

## Change

For the standard `SimplifiedWaterSteamThermodynamicModel`, the untargeted scan now uses an internal reduced diagnostic that computes exactly the two values consumed by the fail-closed decision:

- production-selected phase;
- `LateBoundarySaturatedShadowedByEarlierSuperheated`.

The branch equations and priority are unchanged. The reduced path skips only work that cannot affect those two outputs. In particular, a coarse-saturated root can return immediately because it is the first production branch and makes the late-shadow predicate false; boundary-aware superheated is evaluated only when all earlier production branches fail.

Any non-standard `IWaterSteamInverseBranchDiagnosticProvider` continues to use the complete public `DiagnoseInverseBranchSelection` path.

## Frozen numerical contract

H.28.1-G does not change:

- H.9 finite-difference Newton mathematics;
- 32 probe evaluations;
- 35 logical hydraulic evaluations;
- Jacobian dimension 32;
- residual formulas or tolerances;
- P060/F040;
- 2% / 5 K bounded hysteresis;
- four-node target set `steam|stop-out|header|turbine-inlet`;
- H.20 activation authority;
- H.22 commit ownership;
- physical coefficients;
- 10 ms simulated fixed step;
- default production mode `ExplicitCommittedState`.

## Qualification target

The focused gate freezes validated H.28.1-D plus failed H.28 Requalification 1, E and F evidence. It requires the exact deterministic fingerprint and the unchanged H.28 triggered-tail threshold:

`triggered p95 <= 88.3812 ms`

No H.28 ceiling is raised.
