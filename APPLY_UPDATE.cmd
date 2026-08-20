@echo off
setlocal EnableExtensions
set "ROOT=%~dp0"
cd /d "%ROOT%"
if errorlevel 1 exit /b 1

echo Applying M10.9.5.5 - Contextual Command Consequence Model Closure Gate...
echo Removing stale build and closure outputs...
for /d /r %%D in (bin obj) do @if exist "%%D" rd /s /q "%%D"
if exist "artifacts\m1095-command-consequence-closure" rd /s /q "artifacts\m1095-command-consequence-closure"

echo.
echo M10.9.5.4 is the validated baseline.
echo M10.9.5.5 adds no new runtime feature. It reruns the validated 5.1-5.4 focused gates,
echo verifies shared consequence-model boundaries and writes cumulative automated closure evidence.
echo Final promotion still requires the manual HMI checklist.
echo Physics, Simulation, protection ownership, command dispatch and exact-version identities are unchanged.
echo.
echo Run:
echo   dotnet build
echo   dotnet test
echo   scripts\run-m1095-command-consequence-closure-audit.cmd
echo.
echo If automated gates are green, perform:
echo   docs\M10_9_5_5_MANUAL_VALIDATION_CHECKLIST.md
echo.
echo If manual HMI is also green, M10.9.5 is VALIDATED and M10.9.6.1 is next.
exit /b 0
