# M10.9.7.4 Fingerprint V1 Cross-Host Determinism Diagnostic 1

## Status

`TEST-ONLY-DIAGNOSTIC / HOSTED-CI-BLOCKER`

The hosted `ordinary-ci` workflow now reaches the complete ordinary suite under Stable Contract V2.1.1 and fails one historical Application test:

`M10974FingerprintV1SchemaAnchorTests.FingerprintV1_PopulatedExactVersionFixtureMatchesFrozenGoldenHash`

The frozen fingerprint is:

`63643e5506a6b99f8106950ecb25a5243e9755b3bc96bf2a60e96c219216f362`

The captured hosted run returned a different SHA-256 beginning with:

`3e11375d2e0abce2ccbb4d35434f7e51724d5977341319f7da...`

The ordinary suite passes locally. All other hosted test assemblies pass.

## Diagnostic question

The whole-snapshot SHA alone cannot distinguish:

1. a serialized-shape/order difference;
2. a culture/runtime representation difference;
3. a numerical leaf difference accumulated during the H29 128-step fixture;
4. a genuine fingerprint-v1 compatibility break.

The current source review already makes the first two less likely: the principal fingerprint-visible collections are canonically ordered and presentation scalar text is formatted invariantly. This is evidence, not yet a final classification.

## Capture design

The existing schema-anchor test keeps the same golden and the same final assertion. When `NRS_FINGERPRINT_V1_DIAGNOSTICS_DIR` is present, a test-only helper writes diagnostic evidence immediately before that assertion from the same snapshot and the same test execution.

The ZIP contains:

- `summary.json` / `summary.txt` — expected/actual/payload hashes plus runtime, OS, architecture and culture;
- `normalized-control-room-snapshot-v1.json` — exact compact UTF-8 JSON recreated with the same run-state normalization and serializer options as fingerprint v1;
- `top-level.tsv` — SHA-256 for every top-level serialized property;
- `nodes.tsv` — deterministic JSON-pointer map of every object, array and scalar leaf with subtree/value SHA-256.

The diagnostic also records whether its independently reconstructed payload SHA equals `ControlRoomSnapshotFingerprint.Compute(snapshot)`. This is observational only and adds no assertion.

## Local / hosted comparison

Local command:

`.\scripts\run-m10974-fingerprint-v1-cross-host-diagnostic1.cmd`

Return:

`artifacts/ci/fingerprint-v1-local/fingerprint-v1-cross-host-diagnostic.zip`

The hosted ordinary workflow sets the same capture variable to `artifacts/ci/fingerprint-v1`. The existing V2.1.1 upload step therefore publishes `fingerprint-v1/fingerprint-v1-cross-host-diagnostic.zip` inside `ordinary-ci-diagnostics` from the original failing test execution. There is no diagnostic rerun.

The first differing path in `nodes.tsv` decides the next investigation branch:

- collection/object structure or order difference -> canonicalization defect;
- string leaf difference -> presentation/environment source review;
- numeric leaf difference with identical structure -> cross-host numerical determinism investigation;
- no leaf difference but different production-compute hash -> fingerprint implementation reconstruction defect in this diagnostic, not evidence against v1.

## Frozen non-scope

This diagnostic does not authorize:

- changing `sha256-control-room-snapshot-v1`;
- changing the frozen golden hash;
- accepting multiple golden hashes;
- rounding or quantizing fingerprint values;
- changing production physics or H29 seed/runtime semantics;
- suppressing, retrying, filtering or skipping the hosted failure;
- R3 repair planning or any repair-owner selection.

R3 remains RED and the energy-transport repair-planning branch remains blocked until hosted ordinary CI is GREEN.
