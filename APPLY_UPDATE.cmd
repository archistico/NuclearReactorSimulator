@echo off
setlocal EnableExtensions
set "ROOT=%~dp0"
cd /d "%ROOT%"
if errorlevel 1 exit /b 1

echo Applying M10.9.7.3 Hotfix 1 REV2 - Live Mission / Performance Historical Shell Contract Alignment...
echo Removing stale build and focused-audit outputs...
for /d /r %%D in (bin obj) do @if exist "%%D" rd /s /q "%%D"
if exist "artifacts\m10973-mission-performance-live-workspace" rd /s /q "artifacts\m10973-mission-performance-live-workspace"

echo.
echo Baseline: M10.9.7.2 Hotfix 3 REV1 VALIDATED.
echo Original M10.9.7.3 candidate: SUPERSEDED / NOT VALIDATED after two compile-contract failures.
echo Hotfix 1 REV2 preserves the validated REV1 live-source ordering fix, keeps GRID DEMAND scoped to MISSION, and aligns the historical shell test to the actual top runtime step binding RuntimeProgressText without changing runtime semantics.
echo MISSION is activated as a read-only peer workspace; COMPUTER F1-F8 remains fixed and no F9 is added.
echo Normal desktop startup does not infer a challenge. Manual live validation may bind an exact authored pack with --mission-pack=^<exact-id^>.
echo Demand history is deterministic-step evidence; UI publication follows presentation cadence with explicit structural change detection.
echo Archive-restored mission binding/timeline equivalence remains M10.9.7.4 scope.
echo No new challenge, scoring arithmetic, protection authority, plant command authority or physics change is included.
echo.
echo Run:
echo   dotnet build
echo   dotnet test
echo   scripts\run-m10973-mission-performance-live-workspace-audit.cmd
echo Then complete:
echo   docs\M10_9_7_3_MANUAL_VALIDATION_CHECKLIST.md
exit /b 0
