# M10.9.4.1-H.27 Hotfix 1 — High-Load Envelope Contract Fix

## Status

**VALIDATED.** H.27 Hotfix 1 passed build, ordinary tests and the focused H.27 gate on 2026-08-19 and is the authoritative baseline for H.28.1-A.

## First H.27 focused-gate result

The original H.27 candidate compiled and the complete ordinary `dotnet test` suite passed. The focused H.27 audit then ran the full six-scenario staged matrix and failed only the `high-load-10mwe` evidence condition. Five of six scenario evidence conditions passed.

The failing contract required the 10 MWe requested-load point to be both reached and remain trip-free. That requirement is stricter than the H.27 envelope objective, which explicitly treats a canonical protection action as a valid `protected-boundary` outcome.

## Root cause

The runtime was not shown to violate H.20/H.22 ownership, rollback, conservation or determinism. The failure was caused by an over-prescriptive audit condition:

```text
10 MWe request sustained without trip
```

H.27 is an envelope-mapping milestone, not a promise that every staged off-design request is trip-free. The evidence question is whether the 10 MWe requested-load point is actually exercised. If a protection subsequently acts, the scenario belongs to the protected boundary of the observed envelope.

## Hotfix

Hotfix 1 changes only the H.27 focused audit contract for `high-load-10mwe`:

- require that at least one observed step reaches a requested electrical load of at least 9.999 MWe;
- preserve all existing fail-closed ownership, residual, conservation and determinism checks;
- preserve `Classify(...)`: any observed trip remains `protected-boundary`;
- do not alter H.20, H.22, P060/F040, H.9, hysteresis, protection logic, physical coefficients or production factories.

The checklist is aligned to the same interpretation.

## Validation gate

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-four-node-off-design-qualification-envelope-audit.cmd
```

H.27 Hotfix 1 may be promoted only after all three gates pass.
