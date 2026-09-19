# M10.9.7.4 Fingerprint V1 Cross-Host Determinism Diagnostic 1 - Returned-Evidence Adjudication 1

NRS-MARKER:FPV1-DIAG1-RETURNED-ADJUDICATION1

## Result

`PASS-AS-AUTHORED` with engineering classification `PRESENTATION-CULTURE-DRIFT-CONFIRMED`.

NRS-MARKER:FPV1-DIAG1-PRESENTATION-CULTURE-DRIFT-CONFIRMED

The local Windows capture (`it-IT`, .NET 10.0.5) and the GitHub hosted Windows capture (`en-US`, .NET 10.0.12) both contain 731 JSON nodes and 596 scalar leaves. Node paths and JSON kinds are identical. Exactly one scalar raw value differs:

`/primaryCircuit/loops/0/branches/0/voidText`

- local: `"Void 0,0%"`
- hosted: `"Void 0.0%"`

The normalized payload is 14,947 bytes on both hosts and differs in one byte at zero-based offset 3150: comma (`44`) locally versus period (`46`) on hosted CI. There are zero numeric-leaf differences.

NRS-MARKER:FPV1-DIAG1-NO-PHYSICAL-NUMERIC-DRIFT

Therefore the hosted RED is not serialization shape/order drift, physics drift, floating-point/JIT drift, H29 seed drift, or fingerprint hashing drift. The root cause is culture-sensitive numeric formatting in `ControlRoomSnapshotProjector` while constructing `PrimaryCircuitBranchPresentationSnapshot.VoidText`.

## Fingerprint interpretation

The historical local fingerprint `63643e5506a6b99f8106950ecb25a5243e9755b3bc96bf2a60e96c219216f362` remains immutable provenance for the old `it-IT` capture. It is not accepted as an alternate runtime golden.

The hosted payload fingerprint `3e11375d2e0abce2ccbb4d35434f7e51724d5977341319f7dad188d7c9e593d4` is exactly the SHA-256 obtained by replacing the single culture-dependent comma with the invariant decimal point in the otherwise byte-identical local payload.

## Authority

This adjudication authorizes a narrow Application presentation hotfix planning/implementation candidate. It does not authorize physics changes, fingerprint schema changes, multiple accepted golden hashes, VR2/R3 changes, R3 PASS, or VR2 repair-owner selection.

NRS-MARKER:FPV1-DIAG1-HOTFIX1-AUTHORIZED
