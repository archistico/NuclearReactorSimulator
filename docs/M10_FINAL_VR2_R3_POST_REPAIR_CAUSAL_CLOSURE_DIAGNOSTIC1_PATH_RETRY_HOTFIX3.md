# M10 Final VR2 R3 Post-Repair Causal-Closure Diagnostic 1 - Path Retry Hotfix 3

NRS-MARKER:M10-FINAL-VR2-R3-POST-REPAIR-CAUSAL-CLOSURE-DIAGNOSTIC1-PATH-RETRY-HOTFIX3

## Scope

Windows PowerShell 5.1 could read the long Diagnostic 3 REV1 CSV path but failed when `System.IO.File.Copy` attempted to duplicate it. The copy was not part of the engineering decision.

This hotfix removes that copy. The adjudicator reads the original CSV in place, applies the unchanged causal-closure criteria, and writes only the short summary under `artifacts/r3-post-repair-closure-d1`.

No production source, test source, thresholds, seed, physics, causal criteria or R3 authority change is made.
