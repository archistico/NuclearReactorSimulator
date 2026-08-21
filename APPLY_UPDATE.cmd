@echo off
setlocal EnableExtensions
set "ROOT=%~dp0"
cd /d "%ROOT%"
if errorlevel 1 exit /b 1

echo Applying M10.9.7.5 - Mission / Performance Closure...
echo Removing stale build and closure-audit outputs...
for /d /r %%D in (bin obj) do @if exist "%%D" rd /s /q "%%D"
if exist "artifacts\m1097-mission-performance-closure" rd /s /q "artifacts\m1097-mission-performance-closure"

echo.
echo Baseline: M10.9.7.4 Hotfix 1 VALIDATED after build, ordinary tests, focused timeline audit and manual HMI acceptance.
echo M10.9.7.5 is a cumulative closure gate only: closure tests, audit script, documentation/checklist and descriptor metadata.
echo No production XAML/runtime semantics, Simulation physics, challenge/scoring/protection authority, archive schema or plant-command authority change.
echo.
echo Run:
echo   dotnet build
echo   dotnet test
echo   scripts\run-m1097-mission-performance-closure-audit.cmd
echo Then complete:
echo   docs\M10_9_7_5_MANUAL_VALIDATION_CHECKLIST.md
exit /b 0
