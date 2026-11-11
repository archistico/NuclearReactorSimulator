# M10.9.8 — Integrated Human / Automation / HMI Closure

## Scope

M10.9.8 is the integration/acceptance gate for the M10 operator system. M10.9.8.5 adds no feature or production runtime behavior; it aggregates the validated automated evidence from M10.9.8.1–8.4 and requires explicit end-to-end manual HMI acceptance.

## Validated automated chain

- M10.9.8.1 REV1 Docs1 — accepted validation matrix and manual contract freeze;
- M10.9.8.2 Hotfix 1 REV5 — healthy 3×3 assistance × authority, production mission @2, F4/list stability;
- M10.9.8.3 — degraded measurement / fault / protection / takeover matrix;
- M10.9.8.4 Hotfix 1 — same-seed, full replay, checkpoint-prefix/live-continuation and challenge projection integrity.

M10.9.8.5 freezes the exact M10.9.8.4 compiled/test surface with a SHA-256 manifest and requires `scripts/run-m1098-integrated-human-automation-hmi-audit.cmd` plus `M10_9_8_5_MANUAL_INTEGRATED_HMI_ACCEPTANCE_CHECKLIST.md`.

## Closure boundary

After explicit manual acceptance, M10.9.8 may be marked **VALIDATED / CLOSED**. That does **not** authorize M11 and does not mean M10 itself is closed.

**M10 remains OPEN** until the final pre-M11 validation is complete. The mandatory next gates are:

```bat
scripts\run-m10-final-validation.cmd
scripts\run-m10-final-long-validation.cmd
```

The long gate is an explicit approximately one-hour operational validation and remains separate from the ordinary daily suite. See `M10_FINAL_PRE_M11_VALIDATION_PLAN.md`.

## Non-scope retained

- no Simulation coefficient or physics change;
- no new challenge/scoring/protection owner;
- no archive schema or fingerprint algorithm change;
- no MISSION plant-command authority;
- no manual-only fault injector or challenge launcher added for acceptance;
- known M11 performance/memory/packaging hardening remains deferred to M11 after M10 final validation.
