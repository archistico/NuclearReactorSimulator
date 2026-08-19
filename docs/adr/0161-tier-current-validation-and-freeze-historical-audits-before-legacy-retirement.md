# ADR 0161 — Tier current validation and freeze historical audits before legacy retirement

## Status

Accepted for M10.9.4.1-I.2 candidate.

## Context

Phase H produced a large executable research lineage. Re-running every historical audit on every change would make CI impractical, while simply deleting old audits or numerical modes would destroy provenance and could hide remaining source dependencies.

I.1 already proved that H.5 hybrid and H.21 shadow-integrated modes are not production-selectable but remain retirement candidates only.

## Decision

Validation is split into four explicit tiers: `ORDINARY`, `CURRENT-EVIDENCE`, `SCHEDULED-LONG` and `HISTORICAL-FROZEN`.

Current CI executes only ordinary and current-evidence work. Current long-running operational/reference gates execute on a separate scheduled/manual path. Validated Phase-H research gates that no longer establish current policy are retained as frozen evidence rather than automatically rerun.

Historical H.5/H.21 modes are not deleted in I.2. They may be removed only after a later retirement milestone proves that no executable source/test dependency remains.

## Consequences

- CI cost becomes bounded and intentional;
- frozen evidence remains fingerprintable and reviewable;
- current production policy is still checked on every ordinary CI execution;
- expensive Phase-H research is not silently repeated;
- historical-code retirement remains a separate, evidence-driven change.
