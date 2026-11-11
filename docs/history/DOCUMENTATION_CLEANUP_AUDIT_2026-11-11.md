# Documentation cleanup — 2026-11-11

The project README now describes the simulator, features, architecture and startup.
Development status, roadmap and change history have separate entry points.

## Consolidation

- Removed 74 completed or superseded notes from `docs/`, reducing top-level
  Markdown documents from 367 to 293. Of these notes, 48 were already represented
  in historical dossiers.
- Preserved all 74 originals and seven navigation snapshots byte for byte in the
  [ZIP archive](archives/documentation-2026-11-11.zip). The
  [archive index](archives/README.md) identifies each source and explains extraction;
  the [manifest](archives/documentation-2026-11-11.manifest.json) records raw and
  normalized-LF SHA-256 digests.
- Reduced 1,161,476 bytes of archived sources to a 429,742-byte ZIP (63% smaller).
- Retained 217 executable-referenced documents and their linked dependencies
  unchanged. Older documents with executable dependencies remain at their original
  paths; stable subsystem references remain modular.
- Rebuilt the exhaustive top-level index and compacted repeated checkpoint history.

| Document | Lines before | Lines after |
| --- | ---: | ---: |
| Root README | 73 | 65 |
| Documentation guide | 165 | 57 |
| Project checkpoint | 604 | 131 |
| Roadmap | 819 | 69 |
| Changelog | 5,152 | 19 |

## Verification

The archive passed ZIP integrity and per-entry byte/hash checks. Protected
documents and all 962 production files plus 406 test files match their recorded
baseline hashes. Existing navigation markers and 46 previously passing literal
validator checks are retained; historical compatibility excerpts are labeled as
such and do not select current work.

The top-level index covers all 293 live Markdown files exactly once. Link checks
compare against the original navigation snapshots: no broken links were introduced;
one pre-existing broken link remains outside the rewritten navigation. Frozen
historical dossiers retain their original references. `git diff --check` passed.

The current offline regression command passed:

```powershell
.\scripts\run-r3-h-local-model-design1.cmd
```

This runs the numerical stop review and scaled local-model comparison, including
their numerical fixtures and negative contract checks. No physical search or
ordinary .NET suite rerun is claimed for this documentation change.

The current engineering conclusions remain unchanged: H is inconclusive, R3 is
RED, and further nonlinear work requires its own reviewed plan.
