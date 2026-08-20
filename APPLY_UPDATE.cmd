@echo off
setlocal EnableExtensions
set "ROOT=%~dp0"
cd /d "%ROOT%"
if errorlevel 1 exit /b 1

echo Applying M10.9.5.3 Hotfix 2 - XAML contract and schematic-focus semantics fix...
echo Removing stale build outputs...
for /d /r %%D in (bin obj) do @if exist "%%D" rd /s /q "%%D"

echo.
echo M10.9.5.2 is the validated baseline.
echo M10.9.5.3 Hotfix 1 compiled, but the ordinary App XAML contract test exposed a stale SelectedElementId binding expectation.
echo Hotfix 2 aligns that test with the intentional OneWay presentation binding and makes dependency-step schematic focus exact rather than fallback-fabricated.
echo Dispatch/runtime/physics/protection ownership is unchanged.
echo.
echo Run:
echo   dotnet build
echo   dotnet test
echo   scriptsun-m1095-command-context-inspector-schematic-audit.cmd
echo.
echo Then perform the M10.9.5.3 manual HMI check described in docs\M10_9_5_3_MANUAL_VALIDATION_CHECKLIST.md.
echo If all gates are green, M10.9.5.3 is validated and M10.9.5.4 observed-response evidence is next.
exit /b 0
