# M10 documentation and validator separation — 2026-11-11

Readable M10 documentation is consolidated into a
[validation summary](../validation/README.md) and
[engineering decisions](../validation/MODEL_DECISIONS.md). The current checkpoint
and continuation remain in [PROJECT.md](../PROJECT.md).

## Scope

- Moved 154 top-level M10 documents, 18 milestone sheets and two research records
  to [engineering contracts](../../eng/contracts/m10/README.md). All 174 retain
  their exact original bytes, including line endings.
- Reduced top-level Markdown documents from 293 to 139. No `M10*.md` files remain
  in live `docs/` subdirectories; historical records retain their original context.
- Replaced the scattered human narrative with two summaries. The exact originals
  remain engineering inputs and provenance; this is a reduction of reading and
  navigation duplication, not deletion of executable specifications.
- Preserved four former navigation files as exact snapshots for historical
  validators. Removed historical compatibility appendices from live guides and
  retained the current markers required by active numerical validation.
- Updated 45 PowerShell validators, 39 operational JSON files and eight CMD scripts.
  Changes are limited to document paths and derived file digests. Encoding, line
  endings, numerical settings, acceptance criteria and authority flags are retained.
- Updated live documentation links and rebuilt the exhaustive top-level index.

## Provenance

The [manifest](../../eng/contracts/m10/manifest.json) maps original paths to the
relocated files and records raw SHA-256 digests, navigation snapshots, operational
changes and production/test tree baselines.

The [original-input archive](archives/m10-contract-migration-2026-11-11.zip)
retains every moved document and the pre-migration bytes of affected tooling and
navigation. Frozen returned evidence under `eng/frozen-evidence/` is unchanged.
The earlier [documentation archive](archives/documentation-2026-11-11.zip) is also
unchanged.

Hash propagation follows the referenced path. When an operational contract and a
frozen execution-contract copy originally have identical bytes, relocating the
operational references updates only the operational digest. The frozen copy keeps
its original hash; returned evidence is not rewritten to match newer tooling.

`.gitattributes` preserves raw Markdown fixture bytes across checkout and permits
the existing Windows tooling's CRLF endings in whitespace checks.

## Validation

- All 174 moved documents and four navigation snapshots match their originals.
- All 232 previously passing path/hash assertions examined in modified operational
  JSON remain valid after relocation.
- All 45 modified PowerShell validators parse successfully; the stable ordinary-CI
  harness contract passes.
- All 962 production files and 406 test files remain byte-identical. No production,
  test or frozen-evidence file is changed by this migration.
- Release build passes with zero warnings and zero errors.
- Complete ordinary Release suite passes: 1,382 passed, zero failures; 160 explicit
  tests are excluded by the ordinary runner.
- Current H numerical review/scaled local-model comparison and H feasibility static
  validation pass. No new H physical search is run.
- Current-evidence CI gates pass: Phase-I I.2 baseline, I.5 synchronization activation
  and authoritative Exact-V9 production/mission checks.
- All 452 readable documents examined by the link audit have no missing paths or
  anchors. The top-level index covers its 139 documents exactly once;
  `git diff --check` passes.

Historical validators retain their checkpoint-specific applicability. The earlier
equilibrium-planning and residual-diagnostic validators require 403 and 402 test
files respectively, while the unchanged current tree contains 406. Their existing
`tests count drift` stops are not relaxed or presented as newly qualified gates.

R3 remains RED, H remains inconclusive and the next activity is review of the
offline local-model predictions before a separately frozen nonlinear plan.
