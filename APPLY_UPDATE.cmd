@echo off
setlocal EnableExtensions
set "ROOT=%~dp0"
cd /d "%ROOT%"
if errorlevel 1 exit /b 1

echo Applying M10.9.7.4 - Deterministic Mission Timeline, Drill-Down ^& Replay Equivalence...
echo Removing stale build and focused-audit outputs...
for /d /r %%D in (bin obj) do @if exist "%%D" rd /s /q "%%D"
if exist "artifacts\m10974-mission-performance-timeline" rd /s /q "artifacts\m10974-mission-performance-timeline"

echo.
echo Baseline: M10.9.7.3 Hotfix 2 REV2 VALIDATED after automatic and manual desktop-host/session-integrity gates.
echo M10.9.7.4 adds presentation/reconstruction only: fingerprint-v1 golden anchor, protected lifecycle spine,
echo bounded deterministic mission timeline, presentation-only drill-down and verified archive/checkpoint mission restoration.
echo Archive schema v1 is unchanged. Restored MISSION requires an explicit exact pack binding and never infers a pack from ScenarioId.
echo No Simulation physics, challenge/scoring/protection authority, plant-command authority, F1-F8 or no-F9 contract changes.
echo.
echo Run:
echo   dotnet build
echo   dotnet test
echo   scripts\run-m10974-mission-performance-timeline-audit.cmd
echo Then complete:
echo   docs\M10_9_7_4_MANUAL_VALIDATION_CHECKLIST.md
exit /b 0
