@echo off
setlocal EnableExtensions
cd /d "%~dp0\.."

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-replacement-long-failure-diagnostic6"
set "NRS_M10_FINAL_REPLACEMENT_LONG_DIAGNOSTIC6=1"

if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
mkdir "%REPORT_DIR%"

echo M10 Final replacement-long failure Diagnostic 6
echo Diagnostic 5 PASS: measured thermal readiness did not establish mechanical readiness or a stable 6 MWe first stage within 20 s.
echo This gate changes no production source, workload, authority, generator-load semantics, protection, exact-v9 or mission pack.
echo It holds the first 5.5/6 MWe stage for 180 s and decomposes steam-path lag, rotor acceleration and grid coupling.
echo.
echo [1/2] Ordinary Release gate identical to CI entry point...
call eng\ci-ordinary.cmd
if errorlevel 1 goto :fail

echo.
echo [2/2] Focused explicit long-settling / synchronous-recovery diagnostic...
dotnet test --project "%PROJECT%" --configuration Release --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalReplacementLongFailureDiagnostic6Tests.ExactV9_FirstStageLongSettlingSteamPathAndSynchronousRecoveryCensus" --parallel none
if errorlevel 1 goto :fail

if not exist "%REPORT_DIR%\190-long-settle-probe-summary.csv" goto :missing
if not exist "%REPORT_DIR%\191-long-settle-events.csv" goto :missing
if not exist "%REPORT_DIR%\192-long-settle-trajectories.csv" goto :missing
if not exist "%REPORT_DIR%\193-long-settle-decision-summary.txt" goto :missing

echo.
echo M10 Final replacement-long failure Diagnostic 6 completed.
echo Return the full "%REPORT_DIR%" folder before changing the replacement workload, authority policy, generator-load semantics, protection semantics, exact-v9 runtime, mission pack, or freezing a second replacement-long baseline.
exit /b 0

:missing
echo.
echo M10 Final replacement-long failure Diagnostic 6 FAILED: expected diagnostic artifacts are missing.
echo Preserve and return any files already written under "%REPORT_DIR%".
exit /b 1

:fail
echo.
echo M10 Final replacement-long failure Diagnostic 6 FAILED.
echo Preserve and return any files already written under "%REPORT_DIR%".
exit /b 1
