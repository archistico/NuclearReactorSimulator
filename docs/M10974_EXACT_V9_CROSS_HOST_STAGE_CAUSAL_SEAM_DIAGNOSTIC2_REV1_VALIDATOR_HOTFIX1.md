# M10.9.7.4 Exact-V9 Cross-Host Stage Causal-Seam Diagnostic 2 REV1 Validator Hotfix 1

NRS-MARKER:M10974-EXACT-V9-CROSS-HOST-STAGE-CAUSAL-SEAM-DIAGNOSTIC2-REV1-VALIDATOR-HOTFIX1
NRS-MARKER:M10974-EXACT-V9-DIAG2-REV1-VALIDATOR-EXACT-LINE-MARKERS

## Scope

The first local REV1 execution stopped during static validation with `Diagnostic 2 marker cardinality drift`. No restore, build, focused test or Exact-V9 diagnostic execution occurred.

The target test contains two distinct marker lines: the base Diagnostic 2 marker and the REV1 marker. The previous validator used substring regex matching for the base marker, so the base text was counted once on its own line and once again as the prefix of the REV1 marker.

Validator Hotfix 1 changes only marker cardinality validation. Both markers are now counted as exact trimmed C# comment lines. The Diagnostic 2 REV1 test/evidence behavior, contract, runner, source manifests, golden anchors and engineering authority are unchanged.

## Authority

This hotfix is validator-only. It authorizes no production, physics, tolerance, golden, workflow, Exact-V9 or VR2/R3 change.
