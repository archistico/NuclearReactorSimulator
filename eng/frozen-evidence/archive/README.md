# Frozen evidence archive

This directory stores compressed immutable payloads that are too large for the expanded `eng/frozen-evidence/ordinary` compact prerequisite store.

Canonical identity is defined by `eng/frozen-evidence/large-payload-manifest.csv`, not by ZIP metadata. The archive packs are retention containers only.

Use `.\scripts\restore-frozen-evidence-large-payloads.ps1` only when a historical validator requires the expanded raw payloads, then use `.\scripts\compact-frozen-evidence-ordinary.ps1` to restore compact form.

Current maintenance policy: direct files in `ordinary` must be at most 1 MiB unless a separately adjudicated exception is recorded.

- `rp1c-c4-runtimeconfigurationimpactassessment1-large-payloads.zip` preserves the 10 large exact-v9 raw CSV payloads from returned A2 Runtime Configuration Impact Assessment 1. Canonical path, byte count and SHA-256 are recorded in `../large-payload-manifest.csv`; restore through `scripts/restore-frozen-evidence-large-payloads.ps1` rather than manual extraction.
