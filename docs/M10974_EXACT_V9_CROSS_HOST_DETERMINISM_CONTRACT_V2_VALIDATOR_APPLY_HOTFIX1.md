# M10.9.7.4 Exact-V9 Cross-Host Determinism Contract V2 - Validator/Apply Hotfix 1

NRS-MARKER:M10974-EXACT-V9-CROSS-HOST-DETERMINISM-CONTRACT-V2-VALIDATOR-APPLY-HOTFIX1

## Problem

The V2 candidate correctly omits the adjudicated one-shot workflow `.github/workflows/exact-v9-runtime-alignment-diagnostic.yml`. Applying the candidate ZIP over an existing working copy cannot delete files that are absent from the ZIP, so a workflow left by the preceding runtime-alignment candidate can remain on disk. The V2 validator then correctly fails because the superseded workflow is still present in the working tree.

## Repair

The V2 runner now removes exactly that superseded workflow, if present, before static validation. The validator itself remains unchanged and still requires the workflow to be absent. On a clean working copy the cleanup is a no-op.

No source, test, golden, V1/V2 algorithm, physics, tolerance, ordinary-CI, global.json or VR2/R3 contract is changed.
