# M10 Final VR2 R3 Post-Repair Causal-Closure Diagnostic 1 - Path/Retry Hotfix 2

NRS-MARKER:M10-FINAL-VR2-R3-POST-REPAIR-CAUSAL-CLOSURE-DIAGNOSTIC1-PATH-HOTFIX2

## Scope

Retry-plumbing only. No production, test-source, threshold, seed, physics, acceptance or R3-authority change.

## Root cause

Path Hotfix 1 shortened the adjudication output path, but its retry runner assumed that the runtime evidence emitted by the preceding Diagnostic 3 REV1 test still existed in the working copy. Applying a complete candidate or replacing the working tree can legitimately remove runtime artifacts because they are not source-controlled candidate content.

## Repair

The retry runner now:

1. validates the frozen repair/evidence contract;
2. reuses the post-repair `01-step1-suction-energy-balance.csv` when present;
3. if missing, executes only the already-built exact Diagnostic 3 REV1 focused test with `--no-build` to recreate the runtime evidence;
4. runs the unchanged short-path adjudicator.

No full restore/build/test campaign is repeated.
