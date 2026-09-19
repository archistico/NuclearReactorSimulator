# M10.9.7.4 Exact-V9 Cross-Host Transitive Determinism Diagnostic 1

## Status

`DIAGNOSTIC-ONLY`.

Fingerprint V1 REV2 is now cross-host stable for the H29 schema fixture: GitHub hosted returns the frozen V1 hash `63643e5506a6b99f8106950ecb25a5243e9755b3bc96bf2a60e96c219216f362`. The remaining hosted RED occurs only in the Exact-V9 128-step transitive determinism aggregate, where the frozen local value is `7880AD580179B936C584EB0055BE663E0A1CFA65C5191B0DB8A7F3C514DB5418` and the GitHub log exposes an actual prefix beginning `1E8AAF3799059D8C...`.

The existing Exact-V9 test reaches the selector/direct equality assertion successfully on GitHub and fails only on the frozen aggregate assertion. Therefore this diagnostic treats selector-vs-direct policy selection as already equivalent on the hosted run and investigates cross-host trajectory determinism.

## Diagnostic contract

The existing authoritative Exact-V9 decision test remains the executing gate. Immediately before its frozen aggregate assertion it records, when `NRS_FINGERPRINT_V1_DIAGNOSTICS_DIR` is defined:

- all 128 selector-path canonical V1 payloads;
- all 128 direct-factory canonical V1 payloads;
- per-step V1 fingerprints and logical steps;
- the complete selector and direct aggregate hashes;
- runtime/framework/OS/culture metadata.

The historical assert against `7880AD...B5418` is unchanged. Production source, Fingerprint V1 golden, Exact-V9 golden, physics, tolerances, workflow semantics and VR2/R3 are not authorized to change.

## Adjudication after hosted return

Compare local and hosted `sequence-selector.tsv` in step order. Find the first step with a different V1 fingerprint, then diff the corresponding canonical JSON payloads. Report the first divergent JSON pointer, leaf type, local/hosted value, absolute and relative delta for numeric leaves, and the owning presentation/physical component. Only after that evidence may a repair owner be selected.

## Validator Hotfix 1

The first local execution stopped before restore/build/test because the validator depended on `Get-FileHash`, which is unavailable in the proven user PowerShell environment. Validator Hotfix 1 replaces only that hash helper with stream-based `System.Security.Cryptography.SHA256`. Diagnostic semantics and all frozen boundaries remain unchanged.
