# M10 Final Frozen Evidence Ordinary Compaction 1

## Purpose

`eng/frozen-evidence/ordinary` is a compact prerequisite store, not a permanent expanded raw-evidence warehouse. Recent performance gates accumulated large immutable per-call CSV payloads and expanded the directory to roughly 171 MiB. This maintenance gate restores the compact-store boundary without changing any engineering result.

## Retention rule

Files at or below 1 MiB remain directly under `eng/frozen-evidence/ordinary` at their existing logical paths. Immutable files above 1 MiB are removed from the expanded ordinary store, retained byte-for-byte inside gate-scoped ZIP packs under `eng/frozen-evidence/archive`, and authenticated by `eng/frozen-evidence/large-payload-manifest.csv`.

The compaction externalizes 76 payloads representing 167,307,127 uncompressed bytes. The compact ordinary store contains 499 files totaling 9,247,351 bytes; its largest direct file is 724,772 bytes. Seven compressed archive packs retain the large payloads.

## Historical rerun rule

Historical validators that directly require an externalized raw file are not silently weakened. Before rerunning such a historical gate, restore the authenticated payloads with:

```powershell
.\scripts\restore-frozen-evidence-large-payloads.ps1
```

After the historical rerun, return the repository to compact form with:

```powershell
.\scripts\compact-frozen-evidence-ordinary.ps1
```

Both scripts verify SHA-256 identity and fail closed on mismatch.

## Scope

This is maintenance-only. `src/`, tests, C4, exact-v9, thresholds, runtime-profile decisions and all production authority are unchanged. Runtime Configuration Impact Assessment 1 is not implemented by this gate.
