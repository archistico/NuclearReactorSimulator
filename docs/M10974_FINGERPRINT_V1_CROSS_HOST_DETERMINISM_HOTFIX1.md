# M10.9.7.4 Fingerprint V1 Cross-Host Determinism Hotfix 1 REV2

NRS-MARKER:FPV1-CROSS-HOST-HOTFIX1
NRS-MARKER:FPV1-HOTFIX1-REV2

## Scope

Cross-host Diagnostic 1 proved one culture-sensitive presentation leaf and no physical/numeric divergence: `/primaryCircuit/loops/0/branches/0/voidText` rendered `Void 0,0%` under local `it-IT` and `Void 0.0%` under GitHub hosted `en-US`.

REV2 repairs the live presentation contract by rendering branch outlet void text with invariant formatting, so the H29 step-128 live snapshot contains `Void 0.0%` under both cultures.

NRS-MARKER:FPV1-HOTFIX1-PRESENTATION-ONLY

## Why REV1 was rejected locally

The first Hotfix 1 candidate re-anchored the populated H29 Fingerprint V1 golden from `63643e...f362` to the hosted invariant-payload hash `3e1137...93d4`. Focused fingerprint tests passed, but `CI CURRENT EVIDENCE` then correctly failed the authoritative Exact-V9 deterministic audit: that audit hashes a 128-step sequence of `ControlRoomSnapshotFingerprint.Compute(snapshot)` results, so changing every dependent V1 fingerprint propagated into its frozen second-level anchor. The historical expected value `7880AD...B5418` no longer matched the new sequence; the observed actual began `6B7F8F...`.

That RED is adjudicated as a compatibility failure in the REV1 repair strategy, not as physics drift. REV2 therefore rejects V1 re-anchoring.

## Compatibility-preserving repair

The live presentation and the frozen V1 byte contract are now deliberately separated:

- `ControlRoomSnapshotProjector` emits invariant live text `Void 0.0%`;
- `ControlRoomSnapshotFingerprint` retains `AlgorithmId = sha256-control-room-snapshot-v1` and canonicalizes only `PrimaryCircuitBranchPresentationSnapshot.VoidText` back to the historical decimal-comma representation inside the V1 hashing payload;
- the populated H29 frozen V1 golden remains `63643e5506a6b99f8106950ecb25a5243e9755b3bc96bf2a60e96c219216f362` under both `it-IT` and `en-US`;
- the pre-repair hosted payload hash `3e11375d2e0abce2ccbb4d35434f7e51724d5977341319f7dad188d7c9e593d4` remains returned-evidence provenance only and is not promoted to a V1 golden.

The diagnostic helper uses the production V1 canonical payload routine, so its `payloadSha256` must again equal the runtime `actualFingerprint`.

NRS-MARKER:FPV1-HOTFIX1-V1-COMPATIBILITY-PRESERVED

## Exact-V9 transitive anchor

The authoritative Exact-V9 production audit remains frozen. Its test file is unchanged and its expected deterministic fingerprint remains:

`7880AD580179B936C584EB0055BE663E0A1CFA65C5191B0DB8A7F3C514DB5418`

REV2 does not authorize an Exact-V9 golden update. Because V1 canonical payload bytes are preserved, the 128-step second-level determinism hash must remain historical as well.

NRS-MARKER:FPV1-HOTFIX1-EXACT-V9-ANCHOR-PRESERVED

## Regression qualification

The schema-anchor regression creates fresh H29 step-128 sessions under `it-IT` and `en-US`. Both must prove:

- live branch `VoidText = "Void 0.0%"`;
- V1 fingerprint `63643e...f362`;
- identical V1 fingerprints across cultures.

The full ordinary CI gate must then pass locally, including `CI CURRENT EVIDENCE` and the explicit Debug Exact-V9 production audit, before the same candidate is pushed unchanged to GitHub hosted Windows.

NRS-MARKER:FPV1-HOTFIX1-HOSTED-GREEN-REQUIRED

## VR2/R3 boundary

R3 remains RED. No VR2 repair owner is selected, no thermodynamic/hydraulic implementation changes, and Repair Planning 1 remains blocked until hosted `ordinary-ci` is GREEN.
