# Project handoff

## Validated baseline

H.30 Requalification 1 is validated with decision `ACTIVATE`. Exact v3 corrected-commit is the authoritative desktop default; exact v2 explicit remains fail-closed rollback/reference. H.28 remains `bounded-but-costly`; the 10 ms timestep and numerical contracts are unchanged.

## Current candidate

`M10.9.4.1-I.3 Hotfix 2 — Authoritative Production Reference Trajectory, Conservation/Inventory & Tolerance Baseline / Compact Frozen Evidence Contracts`.

The gate runs the production selector for 300 s / 30,000 steps, checks generation and targeted-train continuity every step, samples conservation/inventory each second, verifies corrected telemetry/determinism and derives seven slopes plus 19 internal regression budgets from the final 60 s.

## Validate

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-phase-i-reference-trajectory-conservation-inventory-baseline-audit.cmd
```

Required focused flags:

```text
phase-i-reference-trajectory-baseline-passes=True
phase-i-generation-continuity-baseline-passes=True
phase-i-conservation-inventory-baseline-passes=True
phase-i-production-telemetry-baseline-passes=True
phase-i-reference-determinism-passes=True
i3-audit-passes=True
phase-i-reference-tolerance-baseline-established=True
```

## Packaging rule

Do not bundle `tests\NuclearReactorSimulator.Application.Tests\Scenarios\Gameplay\Evidence` into candidate ZIPs. Keep large audit outputs as local/separate artifact archives. Ordinary tests must use `eng\frozen-evidence\ordinary`; large traces used only for identity checks belong in `eng\frozen-evidence\large-payload-manifest.csv`, not in the source package. `APPLY_UPDATE.cmd` must not delete an optional local Evidence directory.

## After I.3

Proceed to I.4 known-limitations and legacy-retirement review. Do not begin M10.9.5 until the cumulative I.5 gate is green.
