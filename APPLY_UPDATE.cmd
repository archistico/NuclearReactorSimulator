@echo off
setlocal EnableExtensions
set "ROOT=%~dp0"
cd /d "%ROOT%"
if errorlevel 1 exit /b 1

echo Applying M10.9.6.1 Hotfix 1 - xUnit2013 Collection-Size Assertion Compile Fix...
echo Removing stale build and focused-audit outputs...
for /d /r %%D in (bin obj) do @if exist "%%D" rd /s /q "%%D"
if exist "artifacts\m1096-challenge-lifecycle" rd /s /q "artifacts\m1096-challenge-lifecycle"

echo.
echo M10.9.5 is the validated and closed baseline.
echo M10.9.6.1 Hotfix 1 preserves the deterministic Application-layer challenge lifecycle and fixes only two xUnit2013 test assertions.
echo It uses logical simulation steps, immutable presentation snapshots and accepted operator-action evidence.
echo It adds no energy-demand profile, score arithmetic, UI, plant-control authority, protection change or physics.
echo.
echo Run:
echo   dotnet build
echo   dotnet test
echo   scripts\run-m1096-challenge-lifecycle-audit.cmd
echo.
echo If all gates are green, M10.9.6.1 is VALIDATED and M10.9.6.2 deterministic external energy-demand profiles is next.
exit /b 0
