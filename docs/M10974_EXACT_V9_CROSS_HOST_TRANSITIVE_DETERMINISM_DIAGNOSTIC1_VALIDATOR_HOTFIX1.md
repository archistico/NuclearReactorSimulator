# M10.9.7.4 Exact-V9 Cross-Host Transitive Determinism Diagnostic 1 - Validator Hotfix 1

## Status

`VALIDATOR-COMPATIBILITY-HOTFIX-ONLY`.

The first local execution stopped in step `[1/4]` before restore, build, or test execution because the diagnostic validator called the PowerShell convenience cmdlet `Get-FileHash`. The proven user Windows PowerShell host does not expose that cmdlet.

This is a validator portability defect only. It provides no evidence about Exact-V9 trajectory determinism and does not alter the diagnostic hypothesis or any runtime result.

## Repair

`eng/validate-m10974-exact-v9-cross-host-transitive-determinism-diagnostic1.ps1` now computes SHA-256 through `System.Security.Cryptography.SHA256` over a read-only file stream and converts the result to the same uppercase hexadecimal representation expected by the existing contract.

The replacement follows `docs/VALIDATOR_AUTHORING_RULES.md`, section 8, and the already-proven repository pattern used by prior M10 validators.

## Frozen boundaries

The following remain unchanged from Diagnostic 1:

- all production source under `src/`;
- all tests, including the instrumented Exact-V9 decision test;
- `sha256-control-room-snapshot-v1` and its frozen H29 golden;
- the frozen Exact-V9 aggregate `7880AD580179B936C584EB0055BE663E0A1CFA65C5191B0DB8A7F3C514DB5418`;
- workflow and ordinary/current-evidence CI commands;
- physics, tolerances, mission semantics, VR2/R3 state and repair authority;
- the expected hosted outcome before adjudication.

No golden update or production change is authorized by this hotfix.

## Execution

Re-run the unchanged diagnostic command:

`.\scripts\run-m10974-exact-v9-cross-host-transitive-determinism-diagnostic1.cmd`

Use the repository PowerShell convention exactly as shown: `.\scripts\...`.
