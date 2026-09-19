# M10.9.7.4 Exact-V9 Cross-Host Transitive Determinism Diagnostic 1 - Validator Hotfix 2

NRS-MARKER:M10974-EXACT-V9-CROSS-HOST-TRANSITIVE-DIAGNOSTIC1-VALIDATOR-HOTFIX2

## Scope

Validator-only compatibility correction. The Diagnostic 1 engineering contract, target test instrumentation, frozen Fingerprint V1 anchor, frozen Exact-V9 aggregate, production source, workflow, physics, tolerances and VR2/R3 authority remain unchanged.

## Returned failure

The local validator reported generated build products under `tests/**/obj/**` and `tests/**/bin/**` as `unfrozen file present`. Those files are produced by restore/build and are not members of the frozen source manifest.

The failure occurred in static validation before the Exact-V9 diagnostic test executed, so it carries no engineering evidence about cross-host runtime determinism.

## Correction

`Validate-Manifest` now excludes files whose normalized repository-relative path contains a complete `bin` or `obj` path segment before cardinality and frozen-file comparison.

The exclusion is segment-bound, `(^|/)(bin|obj)(/|$)`, so source files whose ordinary names merely contain `bin` or `obj` are not hidden.

The existing explicit target-test exclusion remains separate and unchanged.

## Authority

This hotfix does not authorize any production change, golden change, physics/tolerance change, workflow change, VR2/R3 repair or interpretation of the Exact-V9 hosted RED. After the validator passes, Diagnostic 1 must still execute and return its 128-step trace before causal adjudication.
