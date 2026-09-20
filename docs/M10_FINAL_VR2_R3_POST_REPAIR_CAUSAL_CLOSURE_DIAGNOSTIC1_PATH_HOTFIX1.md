# M10 Final VR2 R3 Post-Repair Causal-Closure Diagnostic 1 - Path Hotfix 1

NRS-MARKER:R3-POST-REPAIR-CAUSAL-CLOSURE-DIAGNOSTIC1-PATH-HOTFIX1

## Scope

Windows PowerShell 5.1 raised `PathTooLongException` while copying the already-produced Diagnostic 3 REV1 evidence into the adjudication output folder. Build and the authoritative Diagnostic 3 REV1 test had already passed.

This hotfix changes only adjudication artifact plumbing:

- output folder becomes `artifacts/r3-post-repair-closure-d1`;
- copied evidence filename becomes `01-step1-energy.csv`;
- copy uses `System.IO.File.Copy` instead of `Copy-Item`.

No production source, test source, acceptance criterion, threshold, seed, C4 payload, Family B repair behavior, or R3 authority changes.

## Retry

If the prior run reached step `[4/4]`, the Diagnostic 3 REV1 evidence already exists. Re-run only validator plus adjudication with:

`./scripts/run-r3-post-repair-causal-closure-d1-adjudication-retry.cmd`

(The repository convention on Windows is `\.\scripts\...`; the Markdown representation above is informational.)
