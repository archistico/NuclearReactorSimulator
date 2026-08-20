@echo off
setlocal EnableExtensions
set "ROOT=%~dp0"
cd /d "%ROOT%"
if errorlevel 1 exit /b 1

echo Applying M10.9.6.2 Hotfix 1 - Nullable Demand-Output Error Compile Fix...
echo Removing stale build and focused-audit outputs...
for /d /r %%D in (bin obj) do @if exist "%%D" rd /s /q "%%D"
if exist "artifacts\m1096-external-energy-demand" rd /s /q "artifacts\m1096-external-energy-demand"

echo.
echo M10.9.6.1 Hotfix 1 is the validated challenge-lifecycle/logical-time baseline.
echo M10.9.6.2 Hotfix 1 preserves the candidate contract and fixes only nullable typing of demand-output error evidence.
echo EXTERNAL GRID DEMAND remains separate from generator requested load and actual electrical output.
echo It adds no scoring arithmetic, UI, automatic generator loading, grid-coupling authority or physics.
echo.
echo Run:
echo   dotnet build
echo   dotnet test
echo   scripts\run-m1096-external-energy-demand-audit.cmd
echo.
echo If all gates are green, M10.9.6.2 is VALIDATED and M10.9.6.3 multidimensional evaluation/scoring is next.
exit /b 0
