# M10.9.4.1-I.2 — Audit Consolidation & CI Baseline Hardening

## Status

**VALIDATED.** User-reported compilation, complete ordinary tests and the focused I.2 audit passed on 2026-08-19. H.30 remains closed as `OPT-IN ONLY`; exact v2 explicit remains authoritative default/rollback/reference and exact v3 corrected remains qualified opt-in.

I.2 changes no plant physics, numerical mathematics, production selection, exact-version persistence semantics or the 10 ms fixed step.

## Purpose

I.1 established which exact-version profiles and numerical modes are current, compatibility-retained or historical. I.2 establishes how validation itself is executed after Phase H so that current engineering work does not depend on rerunning every historical research audit.

The central rule is that **validated historical evidence may be frozen, but it must not be silently erased or rewritten**.

I.2 therefore separates four audit tiers:

1. `ORDINARY` — clean restore/build and complete ordinary suite;
2. `CURRENT-EVIDENCE` — cheap focused contracts that validate the current production decision and Phase-I compatibility baseline;
3. `SCHEDULED-LONG` — current long-running operational/reference gates, run manually or on schedule rather than on every commit;
4. `HISTORICAL-FROZEN` — validated H-series research evidence retained for provenance but not rerun by ordinary/current CI.

## Frozen I.1 prerequisite

The user-validated I.1 artifacts are copied into the test evidence directory and canonical-fingerprint checked:

- profile-compatibility summary;
- exact-version compatibility matrix;
- numerical-mode retirement inventory.

This establishes that I.2 cannot reinterpret I.1 while changing the audit machinery.

## CI entry points

Provider-neutral Windows entry points live under `eng/`:

```text
eng\ci-ordinary.cmd
  restore
  Release build with warnings-as-errors
  complete ordinary test suite
  current frozen-evidence contracts

eng\ci-current-evidence.cmd
  H.30 closure contract
  I.1 compatibility contract
  I.2 consolidation contract

eng\ci-long.cmd
  long-running gameplay/reference journey
  operational-envelope audit
  reference-plant-scale audit
```

GitHub Actions wiring is provided as a concrete CI implementation:

- `.github/workflows/ordinary-ci.yml` — push, pull request and manual;
- `.github/workflows/scheduled-long-gates.yml` — weekly plus manual dispatch.

Both workflows use `global.json` as the .NET SDK authority. Ordinary CI never runs H.24/H.28 long-horizon/performance requalification and never runs H.5/H.21 historical numerical modes.

## Historical evidence policy

H.24 post-H.28 and H.28 are frozen validated provenance after Phase-H closure and are not default CI work.

H.5 and H.21 are also removed from **current CI dependency**, but their numerical source seams remain compiled in the repository because historical executable tests still reference them. Therefore I.2 does **not** authorize their deletion.

This distinction prevents a false conclusion:

```text
no longer required by current CI != safe to delete source today
```

A later retirement milestone may delete the old numerical modes only after those remaining executable historical source dependencies are explicitly archived, removed or replaced by frozen-evidence contracts.

## Focused gate

Run:

```bat
scripts\run-phase-i-audit-consolidation-ci-baseline-audit.cmd
```

Required final flags:

```text
phase-i-audit-consolidation-passes=True
i2-audit-passes=True
phase-i-ci-baseline-established=True
```

## Validated continuation

I.2 now establishes the Phase-I validation topology. I.3 is the current continuation: consolidate current conservation/inventory observations and establish an exact-version 300-second reference trajectory plus versioned tolerance budgets before any actual legacy-mode deletion or M10.9.5 work.
